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
        
        if (arrow != null)
        {
            // Online arrows apply damage from Arrow.cs through Photon RPCs so all
            // clients receive the same health, score, and respawn state.
            return;
        }
        // Local arrow damage is applied directly by ArrowLocal so trigger callback
        // ordering cannot make hits disappear or double-apply.
    }

    void ShowHeadshotFeedback()
    {
        // Trigger headshot UI effect
        var headshotUI = FindObjectOfType<HeadshotFeedback>();
        if (headshotUI != null)
            headshotUI.Show(transform.position);

        // Extra camera shake + post-FX punch (chromatic aberration + lens distortion)
        CameraShaker.Instance?.ShakeKill();
        PostFXTriggers.Instance?.OnHeadshot();
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
