using UnityEngine;

/// <summary>
/// Automatically sets up hit zone colliders on archer prefabs at runtime.
/// Attach to archer prefab root - creates child hit zone objects.
/// </summary>
public class ArcherAutoSetup : MonoBehaviour
{
    [Header("Hit Zone Setup")]
    public bool autoSetupOnStart = true;
    public float headSize = 0.3f;
    public float bodyWidth = 0.4f;
    public float bodyHeight = 0.6f;
    public float limbWidth = 0.12f;
    public float limbLength = 0.35f;
    
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
        // Check if already set up
        if (transform.Find("HitZones") != null)
        {
            Debug.Log("[ArcherAutoSetup] Hit zones already exist, skipping setup.");
            return;
        }
        
        // Create container
        GameObject hitZonesContainer = new GameObject("HitZones");
        hitZonesContainer.transform.SetParent(transform, false);
        hitZonesContainer.transform.localPosition = Vector3.zero;
        
        // Get archer references for player index
        int playerIndex = 1;
        var archer = GetComponent<Archer>();
        var archerLocal = GetComponent<ArcherLocal>();
        if (archer != null) playerIndex = archer.playerIndex;
        else if (archerLocal != null) playerIndex = archerLocal.playerIndex;
        
        // Create HEAD (top)
        GameObject head = CreateHitZoneObject(hitZonesContainer, "Head", 
            new Vector3(0, 0.7f, 0), 
            HitZone.ZoneType.Head, 1f, headSize);
        
        // Create BODY (torso)
        GameObject body = CreateHitZoneObject(hitZonesContainer, "Body",
            new Vector3(0, 0.1f, 0),
            HitZone.ZoneType.Body, 0.3f, 0f);
        // Body uses box collider
        var bodyCol = body.AddComponent<BoxCollider2D>();
        bodyCol.size = new Vector2(bodyWidth, bodyHeight);
        bodyCol.isTrigger = true;
        
        // LEFT ARM
        GameObject leftArm = CreateHitZoneObject(hitZonesContainer, "LeftArm",
            new Vector3(-0.25f, 0.25f, 0),
            HitZone.ZoneType.LeftArm, 0.15f, 0f);
        var leftArmCol = leftArm.AddComponent<BoxCollider2D>();
        leftArmCol.size = new Vector2(limbWidth, limbLength);
        leftArmCol.offset = new Vector2(0, -0.1f);
        leftArmCol.isTrigger = true;
        
        // RIGHT ARM (includes bow)
        GameObject rightArm = CreateHitZoneObject(hitZonesContainer, "RightArm",
            new Vector3(0.25f, 0.25f, 0),
            HitZone.ZoneType.RightArm, 0.15f, 0f);
        var rightArmCol = rightArm.AddComponent<BoxCollider2D>();
        rightArmCol.size = new Vector2(limbWidth, limbLength * 1.5f); // Longer for bow
        rightArmCol.offset = new Vector2(0, 0.1f);
        rightArmCol.isTrigger = true;
        
        // LEFT LEG
        GameObject leftLeg = CreateHitZoneObject(hitZonesContainer, "LeftLeg",
            new Vector3(-0.12f, -0.35f, 0),
            HitZone.ZoneType.LeftLeg, 0.15f, 0f);
        var leftLegCol = leftLeg.AddComponent<BoxCollider2D>();
        leftLegCol.size = new Vector2(limbWidth, limbLength);
        leftLegCol.isTrigger = true;
        
        // RIGHT LEG
        GameObject rightLeg = CreateHitZoneObject(hitZonesContainer, "RightLeg",
            new Vector3(0.12f, -0.35f, 0),
            HitZone.ZoneType.RightLeg, 0.15f, 0f);
        var rightLegCol = rightLeg.AddComponent<BoxCollider2D>();
        rightLegCol.size = new Vector2(limbWidth, limbLength);
        rightLegCol.isTrigger = true;
        
        Debug.Log($"[ArcherAutoSetup] Hit zones created for Player {playerIndex}");
    }
    
    GameObject CreateHitZoneObject(GameObject parent, string name, Vector3 localPos, 
        HitZone.ZoneType zoneType, float damagePercent, float radius)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent.transform, false);
        zone.transform.localPosition = localPos;
        
        // Add HitZone component
        HitZone hitZone = zone.AddComponent<HitZone>();
        hitZone.zoneType = zoneType;
        hitZone.damagePercent = damagePercent;
        hitZone.isInstantKill = (zoneType == HitZone.ZoneType.Head);
        
        // Add collider (circle for head)
        if (zoneType == HitZone.ZoneType.Head)
        {
            var circle = zone.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.isTrigger = true;
        }
        
        // Add debug visual
        #if UNITY_EDITOR
        // In editor, add a sprite for visualization
        var sr = zone.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDebugSprite();
        sr.color = GetZoneColor(zoneType);
        sr.sortingOrder = -1; // Behind character
        sr.sortingLayerName = "HitZones";
        #endif
        
        return zone;
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
    
    Sprite CreateDebugSprite()
    {
        // Create a simple white 2x2 texture
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixel(0, 0, Color.white);
        tex.SetPixel(1, 0, Color.white);
        tex.SetPixel(0, 1, Color.white);
        tex.SetPixel(1, 1, Color.white);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }
    
    void OnDrawGizmos()
    {
        // Draw wireframes in editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 0.7f, 0), headSize);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, 0.1f, 0), 
            new Vector3(bodyWidth, bodyHeight, 0));
    }
}
