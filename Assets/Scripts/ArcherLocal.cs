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
    public float maxChargeTime  = 1.5f;
    public float minLaunchForce = 3f;
    public float maxLaunchForce = 9f;

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

    private float chargeTimer    = 0f;
    private bool  isCharging     = false;
    private bool  touchHoldInput  = false;

    private Rigidbody2D rb;
    private Animator    animator;
    private LineRenderer aimLine;

    // Manual aim state
    [HideInInspector] public float currentChargeRatio = 0f;
    [HideInInspector] public Vector2 aimDirInput = Vector2.right;

    // Physics for ballistic preview (must match ArrowLocal prefab Rigidbody2D)
    [Header("Ballistic Preview")]
    public float arrowMass    = 0.5f;
    public float gravityScale = 1.2f;
    public int   aimLineSteps = 24;
    public LayerMask groundLayer;

    // Child body-part names whose SpriteRenderers should be hidden on ragdoll
    static readonly string[] BodyPartNames = { "Body", "Pants", "Head", "Hair", "ArmBack", "ArmFront", "Legs", "BowShaft", "BowTip_Top", "BowTip_Bot" };

    void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        animator  = GetComponent<Animator>();
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

        BuildAimLine();

        // Force all child SpriteRenderers onto Default sorting layer with known order
        NormalizeSortingLayers();

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
            if (sr.gameObject == this.gameObject) continue;
            if (sr.gameObject.name.Contains("Outline")) continue;
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
        aimLine.material      = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor    = new Color(1f, 0.95f, 0.20f, 0.85f);
        aimLine.endColor      = new Color(1f, 0.95f, 0.20f, 0f);
        aimLine.startWidth    = 0.10f;
        aimLine.endWidth      = 0.02f;
        aimLine.useWorldSpace = true;
        aimLine.positionCount = 0;
        aimLine.sortingOrder  = 20;
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
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;
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

    void Update()
    {
        if (isDead) return;
        HandleCharge();
        if (isPlayerControlled)
            UpdateAimLine();
    }

    // ── Charge / fire ────────────────────────────────────────────
    public void SetHoldInput(bool holding) => touchHoldInput = holding;

    void HandleCharge()
    {
#if UNITY_EDITOR
        if (isPlayerControlled)
        {
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
            if (isPlayerControlled)
                UIManager.Instance?.UpdateChargeMeter(ratio);
        }
        if (isCharging && !holdInput)
        {
            if (chargeTimer > 0.02f)
                FireArrow();
            isCharging = false;
            chargeTimer = 0f;
            currentChargeRatio = 0f;
            animator?.SetBool("IsCharging", false);
            if (isPlayerControlled)
                UIManager.Instance?.UpdateChargeMeter(0f);
            AudioManager.Instance?.PlayArrowFire();
        }
    }

    void FireArrow()
    {
        float ratio = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float force = Mathf.Lerp(minLaunchForce, maxLaunchForce, ratio);

        Vector2 dir = aimDirInput.sqrMagnitude > 0.001f
            ? aimDirInput.normalized
            : new Vector2(playerIndex == 2 ? -1f : 1f, 0.5f).normalized;

        Vector3 spawnPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;

        GameObject arrowObj = null;

        // Try instantiating from prefab first
        if (arrowLocalPrefab != null)
        {
            arrowObj = Instantiate(arrowLocalPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Fallback: create arrow from scratch
            Debug.Log("[ArcherLocal] Creating arrow from scratch (no prefab).");
            arrowObj = new GameObject("Arrow_Runtime");
            arrowObj.transform.position = spawnPos;

            // Visual: thin brown shaft
            var sr = arrowObj.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSquareSpriteCache.Get();
            sr.color = new Color(0.55f, 0.35f, 0.15f);
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
            Debug.Log($"[ArcherLocal] Arrow fired! Dir={dir}, Force={force}, Ratio={ratio}");
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

    // ── Health / Damage / Respawn ─────────────────────────────────

    /// <summary>Called by ArrowLocal or HitZone. damage is 0-100 percentage points.</summary>
    public void OnHitReceived(int shooterPlayerIndex, float damage = 34f)
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

        if (playerIndex == 1)
            CameraShaker.Instance?.ShakeHit();

        if (currentHealth <= 0f)
        {
            isDead = true;
            TriggerRagdoll();
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
        isDead        = false;
        currentHealth = maxHealth;

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
        if (isPlayerControlled)
            UIManager.Instance?.UpdateChargeMeter(0f);
    }

    public void TriggerRagdoll()
    {
        animator?.SetTrigger("Ragdoll");

        var ragdoll = GetComponent<Ragdoll2D>();
        if (ragdoll == null) ragdoll = gameObject.AddComponent<Ragdoll2D>();
        ragdoll.Activate(lastHitForce, lastHitPoint != Vector3.zero ? lastHitPoint : transform.position);

        // Hide body-part child sprites so ragdoll parts are the only visuals
        HideBodyPartRenderers();
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
