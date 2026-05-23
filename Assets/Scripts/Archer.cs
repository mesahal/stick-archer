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
    public float maxChargeTime  = 1.5f;
    public float minLaunchForce = 3f;
    public float maxLaunchForce = 9f;

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

    [HideInInspector] public int playerIndex;

    // Manual aim state
    [HideInInspector] public float currentChargeRatio = 0f;
    [HideInInspector] public Vector2 aimDirInput = Vector2.right;

    // Physics for ballistic preview (must match Arrow prefab Rigidbody2D)
    [Header("Ballistic Preview")]
    public float arrowMass    = 0.5f;
    public float gravityScale = 1.2f;
    public int   aimLineSteps = 24;
    public LayerMask groundLayer;

    // Child body-part names whose SpriteRenderers should be hidden on ragdoll
    static readonly string[] BodyPartNames = { "Body", "Pants", "Head", "Hair", "ArmBack", "ArmFront", "Legs", "BowShaft", "BowTip_Top", "BowTip_Bot" };

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
            playerIndex = (int)data[0];
    }

    void Start()
    {
        Color teamColor = playerIndex == 2
            ? new Color(0.85f, 0.20f, 0.18f)
            : new Color(0.20f, 0.40f, 0.85f);

        Color skinColor  = new Color(0.92f, 0.76f, 0.60f);
        Color darkTeam   = teamColor * 0.65f;
        Color outlineCol = new Color(0.10f, 0.12f, 0.18f);

        // Color every body part for visibility
        ColorChild("Body",  teamColor);
        ColorChild("Pants", darkTeam);
        ColorChild("Head",  skinColor);
        ColorChild("Hair",  teamColor * 0.8f);
        ColorChild("Legs",  darkTeam);

        // Create arm children if missing (prefab may not have them)
        EnsureChild("ArmBack",  new Vector3(-0.15f, 0.22f, 0), new Vector3(0.10f, 0.40f, 1), darkTeam);
        EnsureChild("ArmFront", new Vector3( 0.15f, 0.22f, 0), new Vector3(0.10f, 0.40f, 1), teamColor);

        // Create dark outline shadows behind each visible body part for contrast
        AddOutlineShadows(outlineCol);

        if (playerIndex == 2)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x),
                                               transform.localScale.y, 1);

        // Build the aim trajectory line (only used by local player)
        BuildAimLine();

        // Force all child SpriteRenderers onto Default sorting layer with known order
        NormalizeSortingLayers();

        // Publish initial health to the HUD
        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
    }

    void EnsureChild(string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        if (transform.Find(name) != null) { ColorChild(name, color); return; }
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSquareSpriteCache.Get();
        sr.color = color;
    }

    void AddOutlineShadows(Color outlineCol)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject == this.gameObject) continue; // skip root
            if (sr.gameObject.name.Contains("Outline")) continue; // don't double-outline
            if (sr.gameObject.name == "AimLine") continue;

            var shadow = new GameObject(sr.gameObject.name + "_Outline");
            shadow.transform.SetParent(sr.transform.parent, false);
            shadow.transform.localPosition = sr.transform.localPosition;
            shadow.transform.localScale = sr.transform.localScale * 1.2f;
            shadow.transform.localRotation = sr.transform.localRotation;

            var ssr = shadow.AddComponent<SpriteRenderer>();
            ssr.sprite = sr.sprite != null ? sr.sprite : WhiteSquareSpriteCache.Get();
            ssr.color = outlineCol;
            ssr.sortingLayerID = 0;
            ssr.sortingOrder = sr.sortingOrder - 1;
        }
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

    void ColorChild(string name, Color c)
    {
        var t = transform.Find(name);
        if (t == null) return;
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = c;
    }

    void NormalizeSortingLayers()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerID = 0; // Default layer

            string n = sr.gameObject.name;

            // Outline shadows get one order below their body part
            if (n.EndsWith("_Outline"))
            {
                string baseName = n.Replace("_Outline", "");
                sr.sortingOrder = GetBodyPartOrder(baseName) - 1;
                continue;
            }

            sr.sortingOrder = GetBodyPartOrder(n);
        }
    }

    int GetBodyPartOrder(string name)
    {
        switch (name)
        {
            case "LegL": case "LegR": case "Legs": case "Pants": return 5;
            case "Body":                                          return 6;
            case "ArmBack":                                       return 4;
            case "ArmFront":                                      return 7;
            case "Head":                                          return 8;
            case "Hair":                                          return 9;
            case "BowShaft": case "BowTip_Top": case "BowTip_Bot": return 10;
            default:                                              return 7;
        }
    }

    void HideBodyPartRenderers()
    {
        foreach (var name in BodyPartNames)
        {
            var t = transform.Find(name);
            if (t != null)
            {
                var sr = t.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
        }
        // Also hide any direct child SpriteRenderers not matched above
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;
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
        float ratio = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, ratio);

        Vector2 dir = aimDirInput.sqrMagnitude > 0.001f
            ? aimDirInput
            : new Vector2(playerIndex == 2 ? -1f : 1f, 0.5f);

        GameObject arrow = PhotonNetwork.Instantiate("Arrow",
            arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position,
            Quaternion.identity);
        arrow.GetComponent<Arrow>().Launch(dir * force, photonView.Owner.ActorNumber);
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
        Vector2 v0   = aimDirInput.normalized * speed;
        Vector3 spawn = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;

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

    // ────────────────────────────────────────────────────────────
    //  HEALTH / DAMAGE / RESPAWN
    // ────────────────────────────────────────────────────────────

    /// <summary>Called by Arrow RPC or HitZone. damage is 0-100 percentage points.</summary>
    public void OnHitReceived(int shooterActorNumber, float damage = 34f)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
        StartCoroutine(ShowHitHeart());

        var hitFlash = GetComponent<HitFlash>();
        if (hitFlash == null) hitFlash = gameObject.AddComponent<HitFlash>();
        hitFlash.Flash();

        Color dmgColor = playerIndex == 1 ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.2f, 0.4f, 0.85f);
        DamageNumber.Spawn(Mathf.RoundToInt(damage), transform.position + Vector3.up * 0.8f, dmgColor);

        if (photonView.IsMine)
            CameraShaker.Instance?.ShakeHit();

        if (currentHealth <= 0f)
        {
            isDead = true;
            TriggerRagdoll();
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
        isDead        = false;
        currentHealth = maxHealth;

        // Look up the spawn point by player index — this works on ALL clients
        // (the remote archer doesn't know its spawnPosition field, which would
        // default to (0,0,0) and warp the archer into the gap between buildings)
        string spawnName = playerIndex == 1 ? "Player1Spawn" : "Player2Spawn";
        var spawnGO = GameObject.Find(spawnName);
        if (spawnGO != null)
            transform.position = spawnGO.transform.position;
        else if (spawnPosition != Vector3.zero)
            transform.position = spawnPosition;
        else
            transform.position = playerIndex == 1
                ? new Vector3(-3.5f, 1f, 0)
                : new Vector3( 3.5f, 1f, 0);

        if (rb != null) { rb.velocity = Vector2.zero; rb.angularVelocity = 0; rb.WakeUp(); }
        chargeTimer        = 0f;
        isCharging         = false;
        touchHoldInput     = false;
        currentChargeRatio = 0f;
        aimDirInput        = new Vector2(playerIndex == 2 ? -1f : 1f, 0.5f);
        UIManager.Instance?.SetPlayerHealth(playerIndex, currentHealth, maxHealth);
        UIManager.Instance?.UpdateChargeMeter(0f);
    }

    public void TriggerRagdoll()
    {
        animator?.SetTrigger("Ragdoll");

        // Activate physics ragdoll
        var ragdoll = GetComponent<Ragdoll2D>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<Ragdoll2D>();
        ragdoll.Activate(lastHitForce, lastHitPoint != Vector3.zero ? lastHitPoint : transform.position);

        // Hide body-part child sprites so ragdoll parts are the only visuals
        HideBodyPartRenderers();
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
