using UnityEngine;

/// <summary>
/// Damage zone collider for different body parts.
/// Attach to child GameObjects of the archer (Head, Body, Limbs).
/// </summary>
public class HitZone : MonoBehaviour
{
    public enum ZoneType { Head, Body, LeftArm, RightArm, LeftLeg, RightLeg }
    
    [Header("Zone Settings")]
    public ZoneType zoneType = ZoneType.Body;
    
    [Header("Damage")]
    [Tooltip("Percentage damage (0-1) for this zone")]
    public float damagePercent = 0.3f;
    [Tooltip("Instant kill on headshot")]
    public bool isInstantKill = false;
    
    [Header("Knockback")]
    public float knockbackMultiplier = 1f;
    
    // Reference to parent archer
    private Archer parentArcher;
    private ArcherLocal parentArcherLocal;
    private int playerIndex;
    
    void Start()
    {
        // Find parent archer
        parentArcher = GetComponentInParent<Archer>();
        parentArcherLocal = GetComponentInParent<ArcherLocal>();
        
        if (parentArcher != null)
            playerIndex = parentArcher.playerIndex;
        else if (parentArcherLocal != null)
            playerIndex = parentArcherLocal.playerIndex;
        
        // Setup collider if not present
        SetupCollider();
    }
    
    void SetupCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            // Auto-create appropriate collider based on zone
            switch (zoneType)
            {
                case ZoneType.Head:
                    var circle = gameObject.AddComponent<CircleCollider2D>();
                    circle.radius = 0.25f;
                    break;
                case ZoneType.Body:
                    var box = gameObject.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(0.4f, 0.6f);
                    break;
                default: // Limbs
                    var cap = gameObject.AddComponent<CapsuleCollider2D>();
                    cap.size = new Vector2(0.15f, 0.4f);
                    break;
            }
        }
        
        // Ensure it's a trigger for arrow detection
        if (col != null)
            col.isTrigger = true;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for arrow hit
        Arrow arrow = other.GetComponent<Arrow>();
        ArrowLocal arrowLocal = other.GetComponent<ArrowLocal>();
        
        if (arrow != null)
        {
            HandleArrowHit(arrow, other.GetComponent<Rigidbody2D>());
        }
        else if (arrowLocal != null)
        {
            HandleLocalArrowHit(arrowLocal, other.GetComponent<Rigidbody2D>());
        }
    }
    
    void HandleArrowHit(Arrow arrow, Rigidbody2D arrowRb)
    {
        if (arrow.ownerActorNumber == playerIndex) return;
        if (parentArcher == null || parentArcher.isDead) return;

        float damage = isInstantKill ? 100f : damagePercent * 100f;
        Vector3 force = arrowRb != null ? (Vector3)arrowRb.velocity * 0.5f : Vector3.zero;
        parentArcher.SetLastHit(force, transform.position);
        parentArcher.OnHitReceived(arrow.ownerActorNumber, damage);

        if (zoneType == ZoneType.Head) ShowHeadshotFeedback();
    }

    void HandleLocalArrowHit(ArrowLocal arrow, Rigidbody2D arrowRb)
    {
        if (arrow.ownerPlayerIndex == playerIndex) return;
        if (parentArcherLocal == null || parentArcherLocal.isDead) return;

        float damage = isInstantKill ? 100f : damagePercent * 100f;
        Vector3 force = arrowRb != null ? (Vector3)arrowRb.velocity * 0.5f : Vector3.zero;
        parentArcherLocal.SetLastHit(force, transform.position);
        parentArcherLocal.OnHitReceived(arrow.ownerPlayerIndex, damage);

        if (zoneType == ZoneType.Head) ShowHeadshotFeedback();
    }
    
    void ShowHeadshotFeedback()
    {
        // Trigger headshot UI effect
        var headshotUI = FindObjectOfType<HeadshotFeedback>();
        if (headshotUI != null)
        {
            headshotUI.Show(transform.position);
        }
        
        // Extra camera shake
        CameraShaker.Instance?.ShakeKill();
    }
    
    /// <summary>
    /// Get the appropriate damage for this zone.
    /// </summary>
    public int GetDamage()
    {
        return isInstantKill ? 100 : Mathf.RoundToInt(damagePercent * 100f);
    }
    
    /// <summary>
    /// Check if this zone is a headshot.
    /// </summary>
    public bool IsHeadshot()
    {
        return zoneType == ZoneType.Head;
    }
}
