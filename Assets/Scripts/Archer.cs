using UnityEngine;
using System.Collections;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
public class Archer : MonoBehaviourPun, IPunInstantiateMagicCallback, IPunObservable
{
    [Header("Shooting")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float maxChargeTime  = 1.0f;
    public float minLaunchForce = 3f;
    public float maxLaunchForce = 9f;
    [Tooltip("Legacy fixed-power value (unused now that power is charge-based).")]
    public float launchForce = 7f;

    /// <summary>True while this archer is actively drawing the bow (used for hold-to-sweep aim).</summary>
    public bool IsAiming => isCharging || touchHoldInput;

    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool isDead = false;

    [Header("Spawn")]
    public Vector3 spawnPosition;

    private float chargeTimer = 0f;
    private bool  isCharging  = false;
    private bool  touchHoldInput  = false;

    private Rigidbody2D rb;
    private Animator    animator;
    private LineRenderer aimLine;
    private ArcherSpriteController spriteController;
    private FloatingHealthBar healthBar;

    [HideInInspector] public int playerIndex;
    [HideInInspector] public int selectedCharacterIndex = -1; // 0 = adventurer, 1 = soldier

    // Manual aim state
    [HideInInspector] public float currentChargeRatio = 0f;
    [HideInInspector] public Vector2 aimDirInput = Vector2.right;

    // Physics for ballistic preview (must match Arrow prefab Rigidbody2D)
    [Header("Ballistic Preview")]
    public float arrowMass    = 0.5f;
    public float gravityScale = 1.2f;
    public int   aimLineSteps = 24;
    public LayerMask groundLayer;

    // Stored for ragdoll activation
    private Vector3 lastHitForce;
    private Vector3 lastHitPoint;

    void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        // Auto-set ground layer mask if not assigned in inspector
        if (groundLayer == 0)
        {
            int gl = LayerMask.NameToLayer("Ground");
            groundLayer = gl >= 0 ? (1 << gl) : (1 << 0);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        if (data != null && data.Length > 0)
        {
            playerIndex = (int)data[0];
            if (data.Length > 1)
                selectedCharacterIndex = (int)data[1];
        }
    }

    void Start()
    {
        // Hand off all visual setup to the sprite controller (real character art,
        // team tint, mirror for P2). Falls back gracefully if the component is missing.
        spriteController = GetComponent<ArcherSpriteController>();
        if (spriteController == null)
            spriteController = gameObject.AddComponent<ArcherSpriteController>();
        spriteController.Setup(playerIndex, EffectiveCharacterIndex());

        var autoSetup = GetComponent<ArcherAutoSetup>();
        if (autoSetup == null)
            autoSetup = gameObject.AddComponent<ArcherAutoSetup>();
        autoSetup.autoSetupOnStart = true;

        // Build the aim trajectory line (only used by local player)
        BuildAimLine();

        // Floating health bar above the archer's head (reference-game style).
        healthBar = gameObject.AddComponent<FloatingHealthBar>();
        healthBar.Init(transform);

        // Publish initial health to the HUD
        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
    }

    void BuildAimLine()
    {
        var go = new GameObject("AimLine");
        go.transform.SetParent(transform, false);
        aimLine = go.AddComponent<LineRenderer>();
        aimLine.material         = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor       = new Color(1f, 0.95f, 0.20f, 0.85f);
        aimLine.endColor         = new Color(1f, 0.95f, 0.20f, 0f);
        aimLine.startWidth       = 0.10f;
        aimLine.endWidth         = 0.02f;
        aimLine.useWorldSpace    = true;
        aimLine.positionCount    = 0;
        aimLine.sortingOrder     = 20;
    }

    /// <summary>Called by TouchControls and AIController every frame while dragging/holding.</summary>
    public void SetAimAndCharge(Vector2 aimDir, float chargeRatio01)
    {
        if (aimDir.sqrMagnitude > 0.001f)
            aimDirInput = aimDir.normalized;
        currentChargeRatio = Mathf.Clamp01(chargeRatio01);

        // Rotate arrowSpawnPoint to match aim direction
        if (arrowSpawnPoint != null)
        {
            float angle = Mathf.Atan2(aimDirInput.y, aimDirInput.x) * Mathf.Rad2Deg;
            arrowSpawnPoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>Set ONLY the aim direction (does not touch charge). Used by the continuous
    /// pendulum sway so it never clobbers the player's charge level.</summary>
    public void SetAimDirection(Vector2 aimDir)
    {
        if (aimDir.sqrMagnitude > 0.001f)
            aimDirInput = aimDir.normalized;
        if (arrowSpawnPoint != null)
        {
            float angle = Mathf.Atan2(aimDirInput.y, aimDirInput.x) * Mathf.Rad2Deg;
            arrowSpawnPoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (isDead) return;
        HandleCharge();
        UpdateAimLine();
    }

    public void SetHoldInput(bool holding) => touchHoldInput = holding;

    void HandleCharge()
    {
#if UNITY_EDITOR
        // Editor keyboard fallback: hold Space to charge, arrow keys adjust angle
        if (Input.GetKey(KeyCode.Space))
        {
            float hInput = Input.GetAxis("Horizontal");
            float vInput = Input.GetAxisRaw("Vertical");
            Vector2 kbDir = new Vector2(playerIndex == 2 ? -1f : 1f, 0.5f);
            if (hInput != 0 || vInput != 0)
                kbDir = new Vector2(hInput, vInput).normalized;
            float kbCharge = Mathf.Clamp01(chargeTimer / maxChargeTime);
            SetAimAndCharge(kbDir, kbCharge);
            touchHoldInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            touchHoldInput = false;
        }
#endif

        bool holdInput = touchHoldInput;

        if (holdInput && !isCharging)
        {
            isCharging  = true;
            chargeTimer = 0f;
            animator?.SetBool("IsCharging", true);
            AudioManager.Instance?.PlayBowDraw();
        }
        if (isCharging && holdInput)
        {
            chargeTimer += Time.deltaTime;
            float ratio = Mathf.Clamp01(chargeTimer / maxChargeTime);
            currentChargeRatio = ratio;
            UIManager.Instance?.UpdateChargeMeter(ratio);
        }
        if (isCharging && !holdInput)
        {
            // Only fire if the player held for more than 0.08s
            if (chargeTimer > 0.08f)
                FireArrow();
            isCharging = false;
            chargeTimer = 0f;
            currentChargeRatio = 0f;
            animator?.SetBool("IsCharging", false);
            UIManager.Instance?.UpdateChargeMeter(0f);
            AudioManager.Instance?.PlayArrowFire();
        }
    }

    void FireArrow()
    {
        // Charge-based power: longer hold = faster arrow.
        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, Mathf.Clamp01(currentChargeRatio));

        Vector2 dir = GetCurrentLaunchDirection();
        Vector3 spawnPos = GetCurrentLaunchPosition(dir);

        GameObject arrow = PhotonNetwork.Instantiate("Arrow",
            spawnPos,
            Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
        arrow.GetComponent<Arrow>().Launch(dir * force, photonView.Owner.ActorNumber);
    }

    Vector2 GetCurrentLaunchDirection()
    {
        if (aimDirInput.sqrMagnitude > 0.001f)
            return aimDirInput.normalized;

        Vector2 baseDirection = playerIndex == 2 ? Vector2.left : Vector2.right;
        float bodyRotation = transform.eulerAngles.z;
        if (bodyRotation > 180f) bodyRotation -= 360f;
        return ((Vector2)(Quaternion.Euler(0f, 0f, bodyRotation) * (Vector3)baseDirection)).normalized;
    }

    Vector3 GetCurrentLaunchPosition(Vector2 direction)
    {
        // Arrows leave from the centre of the bow/gun, nudged along the aim to clear the body.
        Vector3 bowCenter = GetAimPreviewOrigin();
        return bowCenter + (Vector3)(direction.normalized * 0.45f);
    }

    Vector3 GetAimPreviewOrigin()
    {
        Transform sprite = transform.Find("__Sprite");
        if (sprite != null)
        {
            var sr = sprite.GetComponent<SpriteRenderer>();
            if (sr != null && sr.enabled && sr.sprite != null)
                return sr.bounds.center;
        }

        var bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider != null)
            return bodyCollider.bounds.center;

        return transform.position + Vector3.up * 0.7f;
    }

    void UpdateAimLine()
    {
        if (aimLine == null) return;
        if (!isCharging)
        {
            aimLine.positionCount = 0;
            return;
        }

        float ratio  = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float force  = Mathf.Lerp(minLaunchForce, maxLaunchForce, ratio);
        float speed  = force / arrowMass;
        float g      = Physics2D.gravity.magnitude * gravityScale;
        // Include wind force for honest preview
        float windAccel = WindSystem.Instance != null ? WindSystem.Instance.windForce : 0f;
        Vector2 launchDir = GetCurrentLaunchDirection();
        Vector2 v0   = launchDir * speed;
        Vector3 spawn = GetAimPreviewOrigin();

        int count = aimLineSteps;
        aimLine.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float t = i * 0.05f;
            Vector2 p = new Vector2(spawn.x, spawn.y)
                        + v0 * t
                        + new Vector2(0.5f * windAccel * t * t, -0.5f * g * t * t);
            aimLine.SetPosition(i, new Vector3(p.x, p.y, spawn.z));
            if (i > 0 && Physics2D.OverlapPoint(p, groundLayer))
            {
                aimLine.positionCount = i + 1;
                break;
            }
        }
    }

    //  HEALTH / DAMAGE / RESPAWN

    /// <summary>Called by Arrow RPC or HitZone. damage is 0-100 percentage points.</summary>
    public void OnHitReceived(int shooterActorNumber, float damage = 34f)
    {
        if (isDead) return;
        if (shooterActorNumber == photonView.Owner.ActorNumber) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
        healthBar?.SetHealth(currentHealth, maxHealth);
        StartCoroutine(ShowHitHeart());

        var hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
        hitFlash.Flash();

        // Physical flinch in the arrow's travel direction (scaled by damage).
        if (currentHealth > 0f && spriteController != null)
        {
            Vector2 hitDir = lastHitForce.sqrMagnitude > 0.001f
                ? (Vector2)lastHitForce.normalized
                : new Vector2(playerIndex == 1 ? 1f : -1f, 0f);
            spriteController.TriggerHitReaction(hitDir, Mathf.Clamp(damage / 34f, 0.6f, 1.8f));
        }

        Color dmgColor = playerIndex == 1 ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 0.85f);
        DamageNumber.Spawn(Mathf.RoundToInt(damage), transform.position + Vector3.up * 0.8f, dmgColor);

        // Confetti pop on the body (reference-style hit feedback).
        HitConfetti.Burst(transform.position + Vector3.up * 0.8f, Mathf.Clamp(damage / 34f, 0.6f, 1.8f));

        if (photonView.IsMine)
            CameraShaker.Instance?.ShakeHit();

        PostFXTriggers.Instance?.OnHit();

        if (currentHealth <= 0f)
        {
            isDead = true;
            healthBar?.Show(false);
            TriggerRagdoll();
            PostFXTriggers.Instance?.OnRoundEnd();
            if (PhotonNetwork.IsMasterClient)
                GameManager.Instance?.RecordKill(shooterActorNumber);
        }
    }

    /// <summary>Store arrow impact info so TriggerRagdoll can use it.</summary>
    public void SetLastHit(Vector3 force, Vector3 point)
    {
        lastHitForce = force;
        lastHitPoint = point;
    }

    public void Respawn()
    {
        // Clean up active ragdoll parts before re-activating the archer
        var ragdoll = GetComponent<Ragdoll2D>();
        if (ragdoll != null)
        {
            ragdoll.ForceCleanup();
            Destroy(ragdoll);
        }

        isDead        = false;
        currentHealth = maxHealth;
        transform.rotation = Quaternion.identity;

        // Look up the spawn point by player index - this works on ALL clients
        // (the remote archer doesn't know its spawnPosition field, which would
        // default to (0,0,0) and warp the archer into the gap between buildings)
        string spawnName = playerIndex == 1 ? "Player1Spawn" : "Player2Spawn";
        var spawnGO = GameObject.Find(spawnName);
        Vector3 target;
        if (spawnGO != null)
            target = spawnGO.transform.position;
        else if (spawnPosition != Vector3.zero)
            target = spawnPosition;
        else
            target = playerIndex == 1
                ? new Vector3(-3.5f, 1f, 0)
                : new Vector3( 3.5f, 1f, 0);

        transform.position = SpawnAlignment.AlignFeetTo(gameObject, target);

        if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0; rb.WakeUp(); }
        chargeTimer        = 0f;
        isCharging         = false;
        touchHoldInput     = false;
        currentChargeRatio = 0f;
        aimDirInput        = new Vector2(playerIndex == 2 ? -1f : 1f, 0.5f);
        if (spriteController != null)
            spriteController.Setup(playerIndex, EffectiveCharacterIndex());
        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
        healthBar?.Show(true);
        healthBar?.SetHealth(currentHealth, maxHealth);
        UIManager.Instance?.UpdateChargeMeter(0f);
    }

    /// <summary>Character art index (0 = Adventurer, 1 = Soldier) — used by Ragdoll2D for tint.</summary>
    public int CharacterIndex => EffectiveCharacterIndex();

    public void TriggerRagdoll()
    {
        animator?.SetTrigger("Ragdoll");

        // Articulated ragdoll death: hide the live sprite, park the body, spawn the jointed
        // skeleton that flails and tumbles.
        if (spriteController == null) spriteController = GetComponent<ArcherSpriteController>();
        spriteController?.SetBodyVisible(false);

        var bodyCol = GetComponent<Collider2D>();
        if (bodyCol != null) bodyCol.enabled = false;
        if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0f; rb.simulated = false; }

        Vector3 force = lastHitForce * 2.2f;
        if (force.sqrMagnitude < 9f)
            force = new Vector3(playerIndex == 1 ? 5f : -5f, 4f, 0f);
        force.y += 5f;

        var ragdoll = GetComponent<Ragdoll2D>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<Ragdoll2D>();
        ragdoll.Activate(force, lastHitPoint != Vector3.zero ? lastHitPoint : transform.position + Vector3.up * 0.8f);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(aimDirInput.x);
            stream.SendNext(aimDirInput.y);
        }
        else
        {
            aimDirInput = new Vector2((float)stream.ReceiveNext(), (float)stream.ReceiveNext());
            // Apply bow rotation on remote client
            if (arrowSpawnPoint != null)
            {
                float angle = Mathf.Atan2(aimDirInput.y, aimDirInput.x) * Mathf.Rad2Deg;
                arrowSpawnPoint.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    IEnumerator ShowHitHeart()
    {
        var heart = new GameObject("HitHeart");
        heart.transform.position   = transform.position + new Vector3(0, 0.9f, 0);
        heart.transform.localScale = new Vector3(0.30f, 0.30f, 1);
        var sr = heart.AddComponent<SpriteRenderer>();
        sr.sprite       = WhiteSquareSpriteCache.Get();
        sr.color        = new Color(1f, 0.25f, 0.35f);
        sr.sortingOrder = 50;

        float t = 0;
        Vector3 startPos = heart.transform.position;
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            heart.transform.position = startPos + new Vector3(0, t * 0.7f, 0);
            sr.color = new Color(1f, 0.25f, 0.35f, 1f - t);
            yield return null;
        }
        if (heart != null) Destroy(heart);
    }

    int EffectiveCharacterIndex()
    {
        if (selectedCharacterIndex >= 0)
            return Mathf.Clamp(selectedCharacterIndex, 0, 1);
        return playerIndex == 2 ? 1 : 0;
    }
}

// Helper for runtime sprite creation when the cached _WhiteSquare asset isn't available
public static class WhiteSquareSpriteCache
{
    static Sprite _cached;
    public static Sprite Get()
    {
        if (_cached != null) return _cached;
        var loaded = Resources.Load<Sprite>("_WhiteSquare");
        if (loaded != null) { _cached = loaded; return _cached; }
        // Fallback: build at runtime
        var tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        _cached = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return _cached;
    }
}
