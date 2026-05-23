using UnityEngine;

/// <summary>
/// Arrow behavior when stuck in terrain.
/// Arrows embed in ground/walls and stay for a duration.
/// </summary>
public class ArrowStuck : MonoBehaviour
{
    [Header("Stuck Settings")]
    public float stickDepth = 0.15f;
    public float lifetime = 10f;
    public float wobbleAmount = 5f;
    public float wobbleSpeed = 3f;
    
    [Header("Layers")]
    public LayerMask terrainLayers;
    
    private bool isStuck = false;
    private float stuckTime;
    private Rigidbody2D rb;
    private Transform visualTransform;
    private Vector3 originalLocalPos;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        visualTransform = transform.Find("Visual") ?? transform;
        originalLocalPos = visualTransform.localPosition;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isStuck) return;
        
        // Check if hit terrain
        if ((terrainLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            StickToSurface(collision);
        }
    }
    
    void StickToSurface(Collision2D collision)
    {
        isStuck = true;
        stuckTime = Time.time;
        
        // Get contact point and normal
        ContactPoint2D contact = collision.contacts[0];
        Vector2 point = contact.point;
        Vector2 normal = contact.normal;
        
        // Position arrow at contact point, embedded slightly
        transform.position = point + normal * stickDepth;
        
        // Rotate to align with surface normal
        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Disable physics but keep collider for visual
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.simulated = false;
        }
        
        // Disable trail
        var trail = GetComponent<ArrowTrail>();
        if (trail != null) trail.StopTrail();
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        if (!isStuck) return;
        
        // Wobble effect (loose arrow vibration)
        float wobble = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 0.01f;
        visualTransform.localPosition = originalLocalPos + Vector3.right * wobble;
        
        // Fade out near end of life
        float remaining = lifetime - (Time.time - stuckTime);
        if (remaining < 2f)
        {
            float alpha = remaining / 2f;
            var sr = visualTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
    
    /// <summary>
    /// Check if arrow is currently stuck.
    /// </summary>
    public bool IsStuck()
    {
        return isStuck;
    }
    
    void OnDrawGizmos()
    {
        if (isStuck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
