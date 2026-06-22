using UnityEngine;

/// <summary>
/// Automatically sets up hit zone colliders on archer prefabs at runtime.
/// Attach to archer prefab root - creates child hit zone objects.
/// </summary>
public class ArcherAutoSetup : MonoBehaviour
{
    [Header("Hit Zone Setup")]
    public bool autoSetupOnStart = true;
    public float headSize = 0.34f;
    public float bodyWidth = 0.56f;
    public float bodyHeight = 0.9f;
    public float limbWidth = 0.16f;
    public float limbLength = 0.46f;
    
    [Header("Visual Colors (for debugging)")]
    public Color headColor = new Color(1f, 0.3f, 0.3f, 0.3f);
    public Color bodyColor = new Color(0.3f, 0.5f, 1f, 0.3f);
    public Color limbColor = new Color(0.3f, 1f, 0.3f, 0.3f);
    
    void Start()
    {
        if (autoSetupOnStart)
            SetupHitZones();
    }
    
    void SetupHitZones()
    {
        Transform existingContainer = transform.Find("HitZones");
        GameObject hitZonesContainer = existingContainer != null
            ? existingContainer.gameObject
            : new GameObject("HitZones");
        hitZonesContainer.transform.SetParent(transform, false);
        hitZonesContainer.transform.localPosition = Vector3.zero;
        hitZonesContainer.transform.localRotation = Quaternion.identity;
        hitZonesContainer.transform.localScale = Vector3.one;
        
        // Get archer references for player index
        int playerIndex = 1;
        var archer = GetComponent<Archer>();
        var archerLocal = GetComponent<ArcherLocal>();
        if (archer != null) playerIndex = archer.playerIndex;
        else if (archerLocal != null) playerIndex = archerLocal.playerIndex;
        
        // Create HEAD (top)
        GameObject head = CreateHitZoneObject(hitZonesContainer, "Head",
            new Vector3(0, 0.82f, 0),
            HitZone.ZoneType.Head, 0.35f, headSize);
        
        // Create BODY (torso)
        GameObject body = CreateHitZoneObject(hitZonesContainer, "Body",
            new Vector3(0, 0.28f, 0),
            HitZone.ZoneType.Body, 0.34f, 0f);
        // Body uses box collider
        var bodyCol = GetOrAddCollider<BoxCollider2D>(body);
        bodyCol.size = new Vector2(bodyWidth, bodyHeight);
        bodyCol.offset = Vector2.zero;
        bodyCol.isTrigger = true;
        
        // LEFT ARM
        GameObject leftArm = CreateHitZoneObject(hitZonesContainer, "LeftArm",
            new Vector3(-0.31f, 0.34f, 0),
            HitZone.ZoneType.LeftArm, 0.15f, 0f);
        var leftArmCol = GetOrAddCollider<BoxCollider2D>(leftArm);
        leftArmCol.size = new Vector2(limbWidth, limbLength);
        leftArmCol.offset = new Vector2(0, -0.1f);
        leftArmCol.isTrigger = true;
        
        // RIGHT ARM (includes bow)
        GameObject rightArm = CreateHitZoneObject(hitZonesContainer, "RightArm",
            new Vector3(0.31f, 0.34f, 0),
            HitZone.ZoneType.RightArm, 0.15f, 0f);
        var rightArmCol = GetOrAddCollider<BoxCollider2D>(rightArm);
        rightArmCol.size = new Vector2(limbWidth, limbLength * 1.5f); // Longer for bow
        rightArmCol.offset = new Vector2(0, 0.1f);
        rightArmCol.isTrigger = true;
        
        // LEFT LEG
        GameObject leftLeg = CreateHitZoneObject(hitZonesContainer, "LeftLeg",
            new Vector3(-0.12f, -0.35f, 0),
            HitZone.ZoneType.LeftLeg, 0.15f, 0f);
        var leftLegCol = GetOrAddCollider<BoxCollider2D>(leftLeg);
        leftLegCol.size = new Vector2(limbWidth, limbLength);
        leftLegCol.offset = Vector2.zero;
        leftLegCol.isTrigger = true;
        
        // RIGHT LEG
        GameObject rightLeg = CreateHitZoneObject(hitZonesContainer, "RightLeg",
            new Vector3(0.12f, -0.35f, 0),
            HitZone.ZoneType.RightLeg, 0.15f, 0f);
        var rightLegCol = GetOrAddCollider<BoxCollider2D>(rightLeg);
        rightLegCol.size = new Vector2(limbWidth, limbLength);
        rightLegCol.offset = Vector2.zero;
        rightLegCol.isTrigger = true;

        // Broad fallback hurtbox matching the visible full-body sprite. This catches
        // upper-body gaps between the segmented zones without making every hit lethal.
        GameObject fullBody = CreateHitZoneObject(hitZonesContainer, "FullBody",
            new Vector3(0, 0.42f, 0),
            HitZone.ZoneType.Body, 0.34f, 0f);
        var fullBodyCol = GetOrAddCollider<BoxCollider2D>(fullBody);
        fullBodyCol.size = new Vector2(0.64f, 1.18f);
        fullBodyCol.offset = Vector2.zero;
        fullBodyCol.isTrigger = true;
        
        Debug.Log($"[ArcherAutoSetup] Hit zones created for Player {playerIndex}");
    }
    
    GameObject CreateHitZoneObject(GameObject parent, string name, Vector3 localPos,
        HitZone.ZoneType zoneType, float damagePercent, float radius)
    {
        Transform existing = parent.transform.Find(name);
        GameObject zone = existing != null ? existing.gameObject : new GameObject(name);
        zone.transform.SetParent(parent.transform, false);
        zone.transform.localPosition = localPos;
        zone.transform.localRotation = Quaternion.identity;
        zone.transform.localScale = Vector3.one;
        
        // Add HitZone component
        HitZone hitZone = zone.GetComponent<HitZone>();
        if (hitZone == null)
            hitZone = zone.AddComponent<HitZone>();
        hitZone.zoneType = zoneType;
        hitZone.damagePercent = damagePercent;
        hitZone.isInstantKill = false;
        
        // Add collider (circle for head)
        if (zoneType == HitZone.ZoneType.Head)
        {
            var circle = GetOrAddCollider<CircleCollider2D>(zone);
            circle.radius = radius;
            circle.offset = Vector2.zero;
            circle.isTrigger = true;
        }
        
        // No sprite renderers on hit zones - ArcherSpriteController hides all child renderers
        // and debug visualization is handled by OnDrawGizmos below
        
        return zone;
    }

    T GetOrAddCollider<T>(GameObject target) where T : Collider2D
    {
        T collider = target.GetComponent<T>();
        if (collider == null)
            collider = target.AddComponent<T>();

        collider.enabled = true;
        return collider;
    }
    
    Color GetZoneColor(HitZone.ZoneType type)
    {
        switch (type)
        {
            case HitZone.ZoneType.Head: return headColor;
            case HitZone.ZoneType.Body: return bodyColor;
            default: return limbColor;
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw wireframes in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 0.82f, 0), headSize);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.28f, 0),
            new Vector3(bodyWidth, bodyHeight, 0));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.42f, 0),
            new Vector3(0.64f, 1.18f, 0));
    }
}
