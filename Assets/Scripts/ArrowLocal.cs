using UnityEngine;

/// <summary>
/// A fully local (non-Photon) arrow used in Practice mode.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArrowLocal : MonoBehaviour
{
    public float destroyAfterSeconds = 4f;

    private Rigidbody2D rb;
    [HideInInspector] public int ownerPlayerIndex;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Add trail if not present
        if (GetComponent<ArrowTrail>() == null)
            gameObject.AddComponent<ArrowTrail>();
    }

    public void Launch(Vector2 force, int shooterPlayerIndex)
    {
        ownerPlayerIndex = shooterPlayerIndex;
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
        if (rb.velocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Skip other arrows
        if (other.GetComponent<ArrowLocal>() != null) return;

        // Check for LOCAL archer hit (practice mode)
        // Use GetComponentInParent because HitZone colliders are on child objects
        ArcherLocal archerLocal = other.GetComponentInParent<ArcherLocal>();
        if (archerLocal != null && archerLocal.playerIndex != ownerPlayerIndex && !archerLocal.isDead)
        {
            hasHit = true;
            GetComponent<ArrowTrail>()?.StopTrail();
            ImpactEffect.Spawn(transform.position, rb.velocity.normalized);

            Vector3 impactForce = rb.velocity * 0.5f;
            archerLocal.SetLastHit(impactForce, transform.position);
            archerLocal.OnHitReceived(ownerPlayerIndex);
            AudioManager.Instance?.PlayArrowHit();
            Destroy(gameObject);
            return;
        }

        // Check for ONLINE archer hit
        Archer archerNet = other.GetComponentInParent<Archer>();
        if (archerNet != null && archerNet.playerIndex != ownerPlayerIndex && !archerNet.isDead)
        {
            hasHit = true;
            GetComponent<ArrowTrail>()?.StopTrail();
            ImpactEffect.Spawn(transform.position, rb.velocity.normalized);
            AudioManager.Instance?.PlayArrowHit();
            Destroy(gameObject);
            return;
        }

        // Stick into terrain / environment (non-trigger, non-arrow surfaces)
        if (!other.isTrigger)
        {
            hasHit = true;
            rb.velocity        = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic     = true;
            GetComponent<ArrowTrail>()?.StopTrail();
            Destroy(gameObject, 10f);
        }
    }
}
