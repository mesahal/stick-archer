using UnityEngine;

/// <summary>
/// A fully local (non-Photon) arrow used in Practice mode.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowLocal : MonoBehaviour
{
    public float destroyAfterSeconds = 4f;

    private Rigidbody2D rb;
    private Collider2D[] arrowColliders;
    [HideInInspector] public int ownerPlayerIndex;
    private bool hasHit = false;
    private Vector3 previousPosition;

    /// <summary>
    /// Grace period after launch during which ALL trigger collisions are ignored.
    /// This prevents the arrow from hitting the shooter's own hitzone colliders
    /// when the arrow spawns inside (or very close to) the character body.
    /// </summary>
    private float spawnGraceTimer = 0f;
    private const float SPAWN_GRACE_DURATION = 0.15f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        arrowColliders = GetComponentsInChildren<Collider2D>(true);
        previousPosition = transform.position;
        // Add trail if not present
        if (GetComponent<ArrowTrail>() == null)
            gameObject.AddComponent<ArrowTrail>();
    }

    public void Launch(Vector2 force, int shooterPlayerIndex)
    {
        ownerPlayerIndex = shooterPlayerIndex;
        spawnGraceTimer = SPAWN_GRACE_DURATION;
        previousPosition = transform.position;
        rb.AddForce(force, ForceMode2D.Impulse);
        
        // Start trail effect
        var trail = GetComponent<ArrowTrail>();
        trail?.StartTrail();
        
        Destroy(gameObject, destroyAfterSeconds);
    }

    void FixedUpdate()
    {
        if (!hasHit)
            WindSystem.Instance?.ApplyWind(rb);
    }

    void Update()
    {
        if (spawnGraceTimer > 0f)
            spawnGraceTimer -= Time.deltaTime;

        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (!hasHit && spawnGraceTimer <= 0f)
            CheckSweptHit();

        previousPosition = transform.position;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // During grace period, ignore ALL trigger collisions so the arrow
        // can escape the shooter's body without self-destructing.
        if (spawnGraceTimer > 0f) return;

        TryResolveHit(other, transform.position);
    }

    void CheckSweptHit()
    {
        Vector3 currentPosition = transform.position;
        if (CheckOpponentVisualBodySamples(previousPosition, currentPosition))
            return;

        if (CheckCurrentOpponentColliderOverlaps())
            return;

        if ((currentPosition - previousPosition).sqrMagnitude < 0.0001f)
            return;

        if (CheckOpponentColliderSamples(previousPosition, currentPosition))
            return;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(currentPosition, 0.14f);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap != null && TryResolveHit(overlap, currentPosition))
                return;
        }

        RaycastHit2D[] hits = Physics2D.LinecastAll(previousPosition, currentPosition);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && TryResolveHit(hit.collider, hit.point))
                return;
        }
    }

    // Shared archer cache: only two archers exist and they persist across respawns,
    // so re-scanning every frame per arrow (×3 methods) was a needless hot path.
    static ArcherLocal[] _cachedArchers;
    static float _cachedArchersTime = -999f;
    const float ArcherCacheTTL = 1f;

    static ArcherLocal[] GetArchersCached()
    {
        bool stale = _cachedArchers == null || Time.time - _cachedArchersTime > ArcherCacheTTL;
        if (!stale)
            for (int i = 0; i < _cachedArchers.Length; i++)
                if (_cachedArchers[i] == null) { stale = true; break; }
        if (stale)
        {
            _cachedArchers = FindObjectsOfType<ArcherLocal>();
            _cachedArchersTime = Time.time;
        }
        return _cachedArchers;
    }

    bool CheckOpponentVisualBodySamples(Vector3 from, Vector3 to)
    {
        ArcherLocal[] archers = GetArchersCached();
        foreach (ArcherLocal archer in archers)
        {
            if (archer == null || archer.playerIndex == ownerPlayerIndex || archer.isDead)
                continue;

            if (TryGetVisualBodyBounds(archer.transform, out Bounds bodyBounds)
                && SegmentOverlapsBounds(bodyBounds, from, to, out Vector3 hitPoint)
                && ApplyDamageToArcher(archer, ZoneDamage(archer, hitPoint, out bool isHead), hitPoint, isHead))
                return true;
        }

        return false;
    }

    bool CheckCurrentOpponentColliderOverlaps()
    {
        ArcherLocal[] archers = GetArchersCached();
        foreach (ArcherLocal archer in archers)
        {
            if (archer == null || archer.playerIndex == ownerPlayerIndex || archer.isDead)
                continue;

            Collider2D[] opponentColliders = archer.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D opponentCollider in opponentColliders)
            {
                if (opponentCollider == null || !opponentCollider.enabled)
                    continue;

                if (ArrowColliderTouches(opponentCollider, out Vector3 hitPoint)
                    && TryResolveHit(opponentCollider, hitPoint))
                    return true;
            }
        }

        return false;
    }

    bool CheckOpponentColliderSamples(Vector3 from, Vector3 to)
    {
        ArcherLocal[] archers = GetArchersCached();
        foreach (ArcherLocal archer in archers)
        {
            if (archer == null || archer.playerIndex == ownerPlayerIndex || archer.isDead)
                continue;

            Collider2D[] colliders = archer.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D collider in colliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (SegmentOverlapsCollider(collider, from, to, out Vector3 hitPoint)
                    && TryResolveHit(collider, hitPoint))
                    return true;
            }
        }

        return false;
    }

    bool ArrowColliderTouches(Collider2D opponentCollider, out Vector3 hitPoint)
    {
        if (arrowColliders == null || arrowColliders.Length == 0)
            arrowColliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D arrowCollider in arrowColliders)
        {
            if (arrowCollider == null || !arrowCollider.enabled)
                continue;

            ColliderDistance2D distance = arrowCollider.Distance(opponentCollider);
            if (distance.isValid && (distance.isOverlapped || distance.distance <= 0.08f))
            {
                hitPoint = distance.pointB;
                return true;
            }
        }

        hitPoint = transform.position;
        return false;
    }

    bool SegmentOverlapsCollider(Collider2D collider, Vector3 from, Vector3 to, out Vector3 hitPoint)
    {
        int travelSamples = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / 0.035f), 16, 96);
        const float arrowHalfLength = 0.35f;
        const float arrowHalfWidth = 0.12f;
        Vector3 travelDirection = to - from;
        if (travelDirection.sqrMagnitude < 0.0001f)
            travelDirection = transform.right;
        else
            travelDirection.Normalize();

        Vector3 perpendicular = new Vector3(-travelDirection.y, travelDirection.x, 0f);

        for (int i = 0; i <= travelSamples; i++)
        {
            float t = i / (float)travelSamples;
            Vector3 center = Vector3.Lerp(from, to, t);
            for (int lengthIndex = -1; lengthIndex <= 1; lengthIndex++)
            {
                for (int widthIndex = -1; widthIndex <= 1; widthIndex++)
                {
                    Vector3 point = center
                        + travelDirection * (arrowHalfLength * lengthIndex)
                        + perpendicular * (arrowHalfWidth * widthIndex);

                    if (collider.OverlapPoint(point))
                    {
                        hitPoint = point;
                        return true;
                    }
                }
            }
        }

        hitPoint = to;
        return false;
    }

    bool SegmentOverlapsBounds(Bounds bounds, Vector3 from, Vector3 to, out Vector3 hitPoint)
    {
        int travelSamples = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / 0.035f), 16, 96);
        const float arrowHalfLength = 0.42f;
        const float arrowHalfWidth = 0.14f;
        bounds.Expand(new Vector3(0.08f, 0.08f, 0f));

        Vector3 travelDirection = to - from;
        if (travelDirection.sqrMagnitude < 0.0001f)
            travelDirection = transform.right;
        else
            travelDirection.Normalize();

        Vector3 perpendicular = new Vector3(-travelDirection.y, travelDirection.x, 0f);

        for (int i = 0; i <= travelSamples; i++)
        {
            float t = i / (float)travelSamples;
            Vector3 center = Vector3.Lerp(from, to, t);
            for (int lengthIndex = -1; lengthIndex <= 1; lengthIndex++)
            {
                for (int widthIndex = -1; widthIndex <= 1; widthIndex++)
                {
                    Vector3 point = center
                        + travelDirection * (arrowHalfLength * lengthIndex)
                        + perpendicular * (arrowHalfWidth * widthIndex);

                    if (bounds.Contains(point))
                    {
                        hitPoint = point;
                        return true;
                    }
                }
            }
        }

        hitPoint = to;
        return false;
    }

    bool TryGetVisualBodyBounds(Transform archerRoot, out Bounds bounds)
    {
        Transform spriteTransform = archerRoot.Find("__Sprite");
        if (spriteTransform != null)
        {
            SpriteRenderer mainSprite = spriteTransform.GetComponent<SpriteRenderer>();
            if (mainSprite != null && mainSprite.enabled)
            {
                bounds = mainSprite.bounds;
                bounds.Expand(new Vector3(-bounds.size.x * 0.28f, -bounds.size.y * 0.08f, 0f));
                return true;
            }
        }

        bool hasBounds = false;
        bounds = new Bounds(archerRoot.position + Vector3.up * 0.55f, new Vector3(0.7f, 1.4f, 0.2f));
        foreach (SpriteRenderer renderer in archerRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null || !renderer.enabled)
                continue;
            if (renderer.gameObject.name == "__FootShadow")
                continue;
            if (renderer.GetComponentInParent<HitZone>() != null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
        {
            bounds.Expand(new Vector3(-bounds.size.x * 0.20f, -bounds.size.y * 0.06f, 0f));
            return true;
        }

        return false;
    }

    bool TryResolveHit(Collider2D other, Vector3 hitPoint)
    {
        if (other == null || hasHit) return false;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return false;

        // Skip other arrows
        if (other.GetComponent<ArrowLocal>() != null) return false;

        ArcherLocal archerOnCollider = other.GetComponentInParent<ArcherLocal>();
        if (archerOnCollider != null && archerOnCollider.playerIndex == ownerPlayerIndex)
            return false;

        // Bird deflection - arrow bounces off, bird dies
        BirdController bird = other.GetComponent<BirdController>();
        if (bird != null)
        {
            hasHit = true;
            bird.OnArrowHit(rb.velocity);
            DeflectOffBird();
            return true;
        }

        // Apply segmented damage directly here. Relying on HitZone's trigger callback
        // order can make fast local arrows pass through without damage.
        HitZone hitZone = other.GetComponent<HitZone>();
        if (hitZone != null)
        {
            ArcherLocal parentArcher = hitZone.GetComponentInParent<ArcherLocal>();
            if (parentArcher != null && parentArcher.playerIndex != ownerPlayerIndex && !parentArcher.isDead)
            {
                return ApplyDamageToArcher(parentArcher, hitZone.GetDamage(), hitPoint, hitZone.IsHeadshot());
            }
        }

        ArcherLocal directArcher = archerOnCollider;
        if (directArcher != null && directArcher.playerIndex != ownerPlayerIndex && !directArcher.isDead)
        {
            return ApplyDamageToArcher(directArcher, ZoneDamage(directArcher, hitPoint, out bool isHead), hitPoint, isHead);
        }

        // Stick into terrain / environment (non-trigger surfaces)
        if (!other.isTrigger)
        {
            hasHit = true;
            rb.velocity        = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic     = true;
            GetComponent<ArrowTrail>()?.StopTrail();
            Destroy(gameObject, 10f);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Body-part scoring (reference-style): classify the hit by its vertical position
    /// on the target and return the damage. Headshot = instant kill, torso = medium,
    /// arms/legs = light. maxHealth is 100, so 100 head = 1 shot, 50 body = 2, 25 limbs = 4.
    /// </summary>
    float ZoneDamage(ArcherLocal archer, Vector3 hitPoint, out bool isHeadshot)
    {
        isHeadshot = false;

        Bounds b;
        if (!TryGetVisualBodyBounds(archer.transform, out b))
        {
            // Fallback: feet at the archer origin (bottom-center pivot), ~1.5u tall.
            float feet = archer.transform.position.y;
            b = new Bounds(new Vector3(archer.transform.position.x, feet + 0.75f, 0f),
                           new Vector3(0.6f, 1.5f, 0f));
        }

        float height = Mathf.Max(0.01f, b.size.y);
        float rel = Mathf.Clamp01((hitPoint.y - b.min.y) / height); // 0 = feet, 1 = top of head

        // Decisive, score-per-hit feel: a clean body/head hit ends the round; only a
        // glancing limb hit leaves the target alive (and needs one more).
        if (rel >= 0.74f) { isHeadshot = true; return 100f; } // head → instant kill (+ banner)
        if (rel >= 0.34f) return 100f;                         // torso → instant kill
        return 55f;                                            // arms / legs → graze (2 hits)
    }

    bool ApplyDamageToArcher(ArcherLocal archer, float damage, Vector3 hitPoint, bool isHeadshot)
    {
        if (archer == null || archer.playerIndex == ownerPlayerIndex || archer.isDead || hasHit)
            return false;

        hasHit = true;
        Vector3 velocity = rb != null ? (Vector3)rb.velocity : Vector3.zero;
        Vector3 impactForce = velocity * 0.5f;
        archer.SetLastHit(impactForce, hitPoint);
        archer.OnHitReceived(ownerPlayerIndex, damage);
        if (isHeadshot)
        {
            var headshotUI = FindObjectOfType<HeadshotFeedback>();
            if (headshotUI != null)
                headshotUI.Show(hitPoint);
            CameraShaker.Instance?.ShakeKill();
            PostFXTriggers.Instance?.OnHeadshot();
        }
        GetComponent<ArrowTrail>()?.StopTrail();
        ImpactEffect.Spawn(hitPoint, velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.right);
        AudioManager.Instance?.PlayArrowHit();
        Destroy(gameObject);
        return true;
    }

    void DeflectOffBird()
    {
        // Lose most velocity, flip Y slightly, let gravity take over - arrow tumbles down
        rb.velocity = new Vector2(rb.velocity.x * 0.25f, Mathf.Abs(rb.velocity.y) * -0.3f - 1f);
        rb.angularVelocity = Random.Range(-360f, 360f);
        GetComponent<ArrowTrail>()?.StopTrail();
        // Arrow is no longer lethal after deflection - destroy after short delay
        Destroy(gameObject, 2.5f);
    }
}
