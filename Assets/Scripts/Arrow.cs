using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhotonView))]
public class Arrow : MonoBehaviourPun
{
    public float destroyAfterSeconds = 4f;

    private Rigidbody2D rb;
    [HideInInspector] public int ownerActorNumber;
    private bool hasHit = false;
    private Vector3 previousPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        previousPosition = transform.position;
        // Add trail if not present
        if (GetComponent<ArrowTrail>() == null)
            gameObject.AddComponent<ArrowTrail>();
    }

    public void Launch(Vector2 force, int shooterActorNumber)
    {
        ownerActorNumber = shooterActorNumber;
        previousPosition = transform.position;
        rb.AddForce(force, ForceMode2D.Impulse);
        
        // Start trail effect
        var trail = GetComponent<ArrowTrail>();
        trail?.StartTrail();
        
        photonView.RPC("RPC_SyncLaunch", RpcTarget.OthersBuffered, force, shooterActorNumber);
        StartCoroutine(DestroyAfterDelay());
    }

    [PunRPC]
    void RPC_SyncLaunch(Vector2 force, int shooterActorNumber)
    {
        ownerActorNumber = shooterActorNumber;
        rb.AddForce(force, ForceMode2D.Impulse);
        StartCoroutine(DestroyAfterDelay());
    }

    System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyAfterSeconds);
        if (hasHit || gameObject == null) yield break;
        DestroyNetworkObject();
    }

    System.Collections.IEnumerator DestroyNetworkAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (gameObject == null) yield break;
        DestroyNetworkObject();
    }

    void DestroyNetworkObject()
    {
        if (photonView != null && photonView.IsMine && photonView.ViewID != 0)
            PhotonNetwork.Destroy(gameObject);
        else
            Destroy(gameObject);
    }

    void FixedUpdate()
    {
        WindSystem.Instance?.ApplyWind(rb);
    }

    void Update()
    {
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        if (!hasHit && photonView.IsMine)
            CheckSweptVisualHit();

        previousPosition = transform.position;
    }

    // Cached archer list shared by all in-flight arrows. There are only ever two
    // archers and they persist across respawns, so re-scanning every frame per arrow
    // (FindObjectsOfType) was a needless hot-path allocation. Refresh lazily.
    static Archer[] _cachedArchers;
    static float _cachedArchersTime = -999f;
    const float ArcherCacheTTL = 1f;

    static Archer[] GetArchersCached()
    {
        bool stale = _cachedArchers == null || Time.time - _cachedArchersTime > ArcherCacheTTL;
        if (!stale)
        {
            // Invalidate if any cached reference died (scene rebuild / destroy).
            for (int i = 0; i < _cachedArchers.Length; i++)
                if (_cachedArchers[i] == null) { stale = true; break; }
        }
        if (stale)
        {
            _cachedArchers = FindObjectsOfType<Archer>();
            _cachedArchersTime = Time.time;
        }
        return _cachedArchers;
    }

    void CheckSweptVisualHit()
    {
        Archer[] archers = GetArchersCached();
        foreach (Archer archer in archers)
        {
            if (archer == null || archer.isDead)
                continue;
            if (archer.photonView != null && archer.photonView.Owner.ActorNumber == ownerActorNumber)
                continue;

            if (TryGetVisualBodyBounds(archer.transform, out Bounds bounds)
                && SegmentOverlapsBounds(bounds, previousPosition, transform.position, out Vector3 hitPoint))
            {
                ApplyDamageToArcher(archer, ZoneDamage(archer.transform, hitPoint, out bool isHead), hitPoint, isHead);
                return;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (!photonView.IsMine) return;

        // Bird deflection - arrow bounces, bird dies
        BirdController bird = other.GetComponent<BirdController>();
        if (bird != null)
        {
            hasHit = true;
            bird.OnArrowHit(rb.velocity);
            DeflectOffBird();
            return;
        }

        // HitZone (segmented body part) - let the RPC path handle damage via the zone
        HitZone hitZone = other.GetComponent<HitZone>();
        if (hitZone != null)
        {
            Archer archer = hitZone.GetComponentInParent<Archer>();
            if (archer != null
                && archer.photonView.Owner.ActorNumber != ownerActorNumber
                && !archer.isDead)
            {
                ApplyDamageToArcher(archer, hitZone.GetDamage(), transform.position, hitZone.IsHeadshot());
            }
            return;
        }

        // Direct archer-body hit (fallback if HitZone colliders aren't set up)
        Archer archerDirect = other.GetComponent<Archer>();
        if (archerDirect != null
            && archerDirect.photonView.Owner.ActorNumber != ownerActorNumber
            && !archerDirect.isDead)
        {
            ApplyDamageToArcher(archerDirect, ZoneDamage(archerDirect.transform, transform.position, out bool isHeadDirect), transform.position, isHeadDirect);
            return;
        }

        // Stick into terrain (non-trigger solid surfaces)
        if (!other.isTrigger)
        {
            hasHit = true;
            rb.velocity        = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic     = true;
            GetComponent<ArrowTrail>()?.StopTrail();
            StartCoroutine(DestroyNetworkAfterSeconds(10f));
        }
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

    /// <summary>Body-part scoring by vertical hit position (head = 1-shot, torso 50, limbs 25).</summary>
    float ZoneDamage(Transform archerRoot, Vector3 hitPoint, out bool isHeadshot)
    {
        isHeadshot = false;
        Bounds b;
        if (!TryGetVisualBodyBounds(archerRoot, out b))
        {
            float feet = archerRoot.position.y;
            b = new Bounds(new Vector3(archerRoot.position.x, feet + 0.75f, 0f), new Vector3(0.6f, 1.5f, 0f));
        }
        float height = Mathf.Max(0.01f, b.size.y);
        float rel = Mathf.Clamp01((hitPoint.y - b.min.y) / height);
        if (rel >= 0.74f) { isHeadshot = true; return 100f; } // head → instant kill
        if (rel >= 0.34f) return 100f;                         // torso → instant kill
        return 55f;                                            // arms / legs → graze
    }

    void ApplyDamageToArcher(Archer archer, float damage, Vector3 hitPoint, bool isHeadshot)
    {
        if (archer == null || archer.isDead || hasHit)
            return;

        hasHit = true;
        GetComponent<ArrowTrail>()?.StopTrail();
        Vector3 velocity = rb != null ? (Vector3)rb.velocity : Vector3.zero;
        ImpactEffect.Spawn(hitPoint, velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.right);

        Vector3 impactForce = velocity * 0.5f;
        photonView.RPC("RPC_OnHit", RpcTarget.All,
            archer.photonView.ViewID, ownerActorNumber,
            impactForce, hitPoint, damage);
        AudioManager.Instance?.PlayArrowHit();
        if (isHeadshot) CameraShaker.Instance?.ShakeKill();
        PhotonNetwork.Destroy(gameObject);
    }

    void DeflectOffBird()
    {
        rb.velocity = new Vector2(rb.velocity.x * 0.25f, Mathf.Abs(rb.velocity.y) * -0.3f - 1f);
        rb.angularVelocity = Random.Range(-360f, 360f);
        GetComponent<ArrowTrail>()?.StopTrail();
        Destroy(gameObject, 2.5f);
    }

    [PunRPC]
    void RPC_OnHit(int archerViewID, int shooterActorNumber, Vector3 impactForce, Vector3 hitPoint, float damage)
    {
        PhotonView view = PhotonView.Find(archerViewID);
        if (view == null) return;
        Archer archer = view.GetComponent<Archer>();
        if (archer == null) return;
        archer.SetLastHit(impactForce, hitPoint);
        archer.OnHitReceived(shooterActorNumber, damage);
    }
}
