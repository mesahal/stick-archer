using UnityEngine;
using System.Collections;

/// <summary>
/// A fully local (non-Photon) Archer used in Practice mode.
/// Mirrors the gameplay logic of Archer.cs but uses local physics
/// and does not require a PhotonView.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArcherLocal : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject arrowLocalPrefab;
    public Transform  arrowSpawnPoint;
    public float maxChargeTime  = 1.0f;
    public float minLaunchForce = 3f;
    public float maxLaunchForce = 9f;
    [Tooltip("Reference-style: power is FIXED (no charge). Every shot fires at this force; the only skill is the release angle.")]
    public float launchForce = 7f;

    /// <summary>True while the player is holding to aim (used by BowSwayController to sweep only while aiming).</summary>
    public bool IsAiming => isCharging || touchHoldInput;

    [Header("Health")]
    public float maxHealth = 100f;
    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool  isDead = false;

    private Vector3 lastHitForce;
    private Vector3 lastHitPoint;

    [Header("Spawn")]
    public Vector3 spawnPosition;

    [HideInInspector] public int  playerIndex;  // 1 = human, 2 = AI
    [HideInInspector] public bool isPlayerControlled = true;
    [HideInInspector] public int selectedCharacterIndex = -1; // 0 = adventurer, 1 = soldier

    private float chargeTimer    = 0f;
    private bool  isCharging     = false;
    private bool  touchHoldInput  = false;

    private Rigidbody2D rb;
    private Animator    animator;
    private LineRenderer aimLine;
    private ArcherSpriteController spriteController;
    private FloatingHealthBar healthBar;

    // Manual aim state
    [HideInInspector] public float currentChargeRatio = 0f;
    [HideInInspector] public Vector2 aimDirInput = Vector2.right;

    [Header("Arm Bobbing")]
    [Tooltip("Maximum angular offset in degrees for the continuous arm bob (set to 0 to disable)")]
    public float bobAmplitude = 0f;
    [Tooltip("Bob oscillation speed (cycles per second)")]
    public float bobFrequency = 0.75f;
    private float bobPhase = 0f;
    /// <summary>Current bob angle offset in degrees (kept for compatibility but not used for aim).</summary>
    [HideInInspector] public float currentBobAngle = 0f;

    // Physics for ballistic preview (must match ArrowLocal prefab Rigidbody2D)
    [Header("Ballistic Preview")]
    public float arrowMass    = 0.5f;
    public float gravityScale = 1.2f;
    public int   aimLineSteps = 24;
    public LayerMask groundLayer;

    // Original rigidbody tuning, cached so the heavy death-tumble overrides can be
    // restored cleanly on respawn.
    private float _rbMass = 1f, _rbGravity = 1f, _rbDrag = 0f, _rbAngularDrag = 0.05f;

    void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            _rbMass        = rb.mass;
            _rbGravity     = rb.gravityScale;
            _rbDrag        = rb.drag;
            _rbAngularDrag = rb.angularDrag;
        }
        // Animator is optional - no Animator component means no animation clips.
        // Must use explicit Unity null check (not ?.) because GetComponent returns fake-null.
        var foundAnimator = GetComponent<Animator>();
        animator = (foundAnimator != null) ? foundAnimator : null;
        currentHealth = maxHealth;

        // Auto-set ground layer mask if not assigned in inspector
        if (groundLayer == 0)
        {
            int gl = LayerMask.NameToLayer("Ground");
            groundLayer = gl >= 0 ? (1 << gl) : (1 << 0);
        }

        // Auto-load arrow prefab from Resources if not assigned
        if (arrowLocalPrefab == null)
            arrowLocalPrefab = Resources.Load<GameObject>("ArrowLocal");
    }

    void Start()
    {
        // Hand off all visual setup to the sprite controller (real character art,
        // team tint, mirror for P2). Falls back to creating one if not present.
        spriteController = GetComponent<ArcherSpriteController>();
        if (spriteController == null)
            spriteController = gameObject.AddComponent<ArcherSpriteController>();
        spriteController.Setup(playerIndex, EffectiveCharacterIndex());

        BuildAimLine();

        // Floating health bar above the archer's head (reference-game style).
        healthBar = gameObject.AddComponent<FloatingHealthBar>();
        healthBar.Init(transform);

        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
    }

    void BuildAimLine()
    {
        var go = new GameObject("AimLine");
        go.transform.SetParent(transform, false);
        aimLine = go.AddComponent<LineRenderer>();
        aimLine.material      = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor    = new Color(1f, 0.95f, 0.20f, 0.85f);
        aimLine.endColor      = new Color(1f, 0.95f, 0.20f, 0f);
        aimLine.startWidth    = 0.10f;
        aimLine.endWidth      = 0.02f;
        aimLine.useWorldSpace = true;
        aimLine.positionCount = 0;
        aimLine.sortingOrder  = 20;
    }

    /// <summary>Called by TouchControls and AIController every frame while dragging/holding.</summary>
    public void SetAimAndCharge(Vector2 aimDir, float chargeRatio01)
    {
        if (aimDir.sqrMagnitude > 0.001f)
            aimDirInput = aimDir.normalized;
        currentChargeRatio = Mathf.Clamp01(chargeRatio01);

        if (arrowSpawnPoint != null)
        {
            float angle = Mathf.Atan2(aimDirInput.y, aimDirInput.x) * Mathf.Rad2Deg;
            arrowSpawnPoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>Set ONLY the aim direction (does not touch the charge). Used by the
    /// continuous pendulum sway so it never clobbers the player's charge level.</summary>
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
        if (isDead) return;

        // Continuous arm bob - oscillates the aim angle up/down
        bobPhase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
        currentBobAngle = Mathf.Sin(bobPhase) * bobAmplitude;

        HandleCharge();
        // Reference-style: no trajectory preview — reading the arc yourself is the
        // skill. The bow/body lean shows the launch direction; that's the only cue.
        if (aimLine != null && aimLine.positionCount != 0)
            aimLine.positionCount = 0;
    }

    // Charge / fire
    public void SetHoldInput(bool holding) => touchHoldInput = holding;

    void HandleCharge()
    {
#if UNITY_EDITOR
        if (isPlayerControlled)
        {
            // Aim comes from the continuous sway; Space just charges (hold = power).
            if (Input.GetKeyDown(KeyCode.Space)) touchHoldInput = true;
            if (Input.GetKeyUp(KeyCode.Space))   touchHoldInput = false;
        }
#endif

        bool holdInput = touchHoldInput;

        if (holdInput && !isCharging)
        {
            isCharging  = true;
            chargeTimer = 0f;
            if (animator != null) animator.SetBool("IsCharging", true);
            AudioManager.Instance?.PlayBowDraw();
        }
        if (isCharging && holdInput)
        {
            // Hold longer = more power (faster arrow). currentChargeRatio drives both the
            // launch force and the charge-meter fill.
            chargeTimer += Time.deltaTime;
            currentChargeRatio = Mathf.Clamp01(chargeTimer / maxChargeTime);
            if (isPlayerControlled)
                UIManager.Instance?.UpdateChargeMeter(currentChargeRatio);
        }
        if (isCharging && !holdInput)
        {
            if (chargeTimer > 0.02f)
            {
                FireArrow();
                if (spriteController == null)
                    spriteController = GetComponent<ArcherSpriteController>();
                if (spriteController != null)
                    spriteController.TriggerFireAnimation();
            }
            isCharging = false;
            chargeTimer = 0f;
            currentChargeRatio = 0f;
            if (animator != null) animator.SetBool("IsCharging", false);
            if (isPlayerControlled)
                UIManager.Instance?.UpdateChargeMeter(0f);
            AudioManager.Instance?.PlayArrowFire();
        }
    }

    void FireArrow()
    {
        // Charge-based power: longer hold = faster arrow (reference feel). A quick tap
        // still throws a weak projectile.
        float ratio = Mathf.Clamp01(currentChargeRatio);
        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, ratio);

        Vector2 dir = GetCurrentLaunchDirection();
        Vector3 spawnPos = GetCurrentLaunchPosition(dir);

        GameObject arrowObj = null;

        // Try instantiating from prefab first
        if (arrowLocalPrefab != null)
        {
            arrowObj = Instantiate(arrowLocalPrefab, spawnPos, Quaternion.Euler(0, 0,
                Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
        }
        else
        {
            // Fallback: create arrow from scratch
            Debug.Log("[ArcherLocal] Creating arrow from scratch (no prefab).");
            arrowObj = new GameObject("Arrow_Runtime");
            arrowObj.transform.position = spawnPos;

            // Visual: clean procedural arrow shaft
            var sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSquareSpriteCache.Get();
            sr.color = new Color(0.85f, 0.75f, 0.55f); // Light wooden brown
            arrowObj.transform.localScale = new Vector3(0.5f, 0.06f, 1f);
            sr.sortingOrder = 10;

            // Physics
            var arb = arrowObj.AddComponent<Rigidbody2D>();
            arb.mass = 0.5f;
            arb.gravityScale = 1.2f;
            arb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Trigger collider
            var col = arrowObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f);

            // ArrowLocal script
            arrowObj.AddComponent<ArrowLocal>();
        }

        var arrowLocal = arrowObj.GetComponent<ArrowLocal>();
        if (arrowLocal != null)
        {
            arrowLocal.Launch(dir * force, playerIndex);
        }
        else
        {
            // Last resort: just apply force manually
            var arb2 = arrowObj.GetComponent<Rigidbody2D>();
            if (arb2 != null)
                arb2.AddForce(dir * force, ForceMode2D.Impulse);
            Destroy(arrowObj, 4f);
        }
    }

    Vector2 GetCurrentLaunchDirection()
    {
        // Use the aim vector set by BowSwayController, AI, or keyboard input.
        if (aimDirInput.sqrMagnitude > 0.001f)
            return aimDirInput.normalized;

        Vector2 baseDirection = playerIndex == 2 ? Vector2.left : Vector2.right;
        float bodyRotation = transform.eulerAngles.z;
        if (bodyRotation > 180f) bodyRotation -= 360f;
        return ((Vector2)(Quaternion.Euler(0f, 0f, bodyRotation) * (Vector3)baseDirection)).normalized;
    }

    Vector3 GetCurrentLaunchPosition(Vector2 direction)
    {
        // Arrows leave from the centre of the bow/gun (the sprite/body centre), nudged a
        // little along the aim so they clear the archer's own collider.
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

    // Health / Damage / Respawn

    /// <summary>Called by ArrowLocal or HitZone. damage is 0-100 percentage points.</summary>
    public void OnHitReceived(int shooterPlayerIndex, float damage = 34f)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
        healthBar?.SetHealth(currentHealth, maxHealth);
        StartCoroutine(ShowHitHeart());

        var hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
        hitFlash.Flash();

        Vector2 hitDir = lastHitForce.sqrMagnitude > 0.001f
            ? (Vector2)lastHitForce.normalized
            : new Vector2(playerIndex == 1 ? 1f : -1f, 0f);
        float strength = Mathf.Clamp(damage / 34f, 0.6f, 2f);

        // Physical flinch in the arrow's travel direction, scaled by damage so a
        // headshot rocks the body harder than a graze.
        if (currentHealth > 0f)
        {
            if (spriteController == null) spriteController = GetComponent<ArcherSpriteController>();
            spriteController?.TriggerHitReaction(hitDir, strength);
            // NOTE: no physical rigidbody knockback on non-fatal hits — repeated hits
            // could slide an archer off its platform. The strong flinch animation sells
            // the impact, and the death tumble provides the dramatic physics moment.
        }

        Color dmgColor = playerIndex == 1 ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 0.85f);
        DamageNumber.Spawn(Mathf.RoundToInt(damage), transform.position + Vector3.up * 0.8f, dmgColor);

        // Confetti pop on the body (reference-style hit feedback).
        HitConfetti.Burst(transform.position + Vector3.up * 0.8f, strength);

        // Punchy impact feedback for every hit, regardless of which side took it.
        CameraShaker.Instance?.ShakeHit();
        CameraShaker.Instance?.HitStop(0.05f, 0.08f);

        PostFXTriggers.Instance?.OnHit();

        if (currentHealth <= 0f)
        {
            isDead = true;
            healthBar?.Show(false);
            TriggerRagdoll();
            // Bigger freeze + shake on the kill for a satisfying finish.
            CameraShaker.Instance?.HitStop(0.10f, 0.04f);
            CameraShaker.Instance?.ShakeKill();
            PostFXTriggers.Instance?.OnRoundEnd();
            PracticeGameManager.Instance?.RecordKill(shooterPlayerIndex);
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

        // Re-enable the body collider the death disabled (needed before AlignFeetTo,
        // which reads collider bounds).
        var bodyCol = GetComponent<Collider2D>();
        if (bodyCol != null) bodyCol.enabled = true;

        // Reset rotation BEFORE computing foot alignment - collider bounds depend on it
        transform.rotation = Quaternion.identity;

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

        if (rb != null)
        {
            rb.isKinematic     = false;
            rb.simulated       = true;
            rb.velocity        = Vector2.zero;
            rb.angularVelocity = 0f;
            // Restore the original rigidbody tuning the death tumble overrode.
            rb.mass        = _rbMass;
            rb.gravityScale = _rbGravity;
            rb.drag        = _rbDrag;
            rb.angularDrag = _rbAngularDrag;
            // Restore upright stance after a death tumble (which unfroze rotation).
            rb.constraints     = RigidbodyConstraints2D.FreezeRotation;
            rb.rotation        = 0f;
            rb.WakeUp();
        }
        spriteController?.SetRagdollMode(false);
        // Show the single sprite again (the death hid it for the articulated ragdoll).
        spriteController?.SetBodyVisible(true);

        chargeTimer        = 0f;
        isCharging         = false;
        touchHoldInput     = false;
        currentChargeRatio = 0f;
        aimDirInput        = new Vector2(playerIndex == 2 ? -1f : 1f, 0.5f);

        // Reinitialise sprite controller so the idle sprite shows immediately
        // and any stale ragdoll child renderers are hidden
        if (spriteController != null)
            spriteController.Setup(playerIndex, EffectiveCharacterIndex());

        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
        healthBar?.Show(true);
        healthBar?.SetHealth(currentHealth, maxHealth);
        if (isPlayerControlled)
            UIManager.Instance?.UpdateChargeMeter(0f);
    }

    public void TriggerRagdoll()
    {
        if (animator != null) animator.SetTrigger("Ragdoll");

        // Articulated ragdoll death: hide the live single sprite, park the archer body
        // so it doesn't interfere, and spawn a jointed multi-part skeleton that launches,
        // flails on its joints and tumbles off the platforms — the reference game's
        // signature floppy death.
        if (spriteController == null) spriteController = GetComponent<ArcherSpriteController>();
        spriteController?.SetBodyVisible(false);

        var bodyCol = GetComponent<Collider2D>();
        if (bodyCol != null) bodyCol.enabled = false;
        if (rb != null)
        {
            rb.velocity        = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated       = false; // freeze the (now hidden) archer body
        }

        // Launch impulse derived from the killing blow.
        Vector3 force = lastHitForce * 2.2f;
        if (force.sqrMagnitude < 9f)
            force = new Vector3(playerIndex == 1 ? 5f : -5f, 4f, 0f);
        force.y += 5f; // upward pop so the body launches into a heavy arc

        var ragdoll = GetComponent<Ragdoll2D>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<Ragdoll2D>();
        Vector3 hitPt = lastHitPoint != Vector3.zero ? lastHitPoint : transform.position + Vector3.up * 0.8f;
        ragdoll.Activate(force, hitPt);
    }

    /// <summary>Character art index (0 = Adventurer, 1 = Soldier) — used by Ragdoll2D for tint.</summary>
    public int CharacterIndex => EffectiveCharacterIndex();

    int EffectiveCharacterIndex()
    {
        if (selectedCharacterIndex >= 0)
            return Mathf.Clamp(selectedCharacterIndex, 0, 1);
        return playerIndex == 2 ? 1 : 0;
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

        float elapsed = 0;
        Vector3 startPos = heart.transform.position;
        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            heart.transform.position = startPos + new Vector3(0, elapsed * 0.7f, 0);
            sr.color = new Color(1f, 0.25f, 0.35f, 1f - elapsed);
            yield return null;
        }
        if (heart != null) Destroy(heart);
    }
}
