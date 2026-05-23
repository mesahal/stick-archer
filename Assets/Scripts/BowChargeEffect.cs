using UnityEngine;

/// <summary>
/// Visual feedback for bow charging - glows the bow/character as charge increases.
/// </summary>
public class BowChargeEffect : MonoBehaviour
{
    [Header("Charge Colors")]
    public Color minChargeColor = new Color(0.2f, 0.8f, 1f, 0.3f);
    public Color maxChargeColor = new Color(1f, 0.3f, 0.1f, 0.8f);
    
    [Header("Visual Elements")]
    public Transform bowTransform;
    public Transform arrowSpawnPoint;
    
    private SpriteRenderer[] glowSprites;
    private LineRenderer chargeLine;
    private float currentCharge = 0f;
    private bool isCharging = false;
    
    void Awake()
    {
        // Find bow visuals
        if (bowTransform == null)
        {
            var bow = transform.Find("Bow");
            if (bow != null) bowTransform = bow;
        }
        
        // Create charge line effect
        CreateChargeLine();
    }
    
    void CreateChargeLine()
    {
        GameObject lineObj = new GameObject("ChargeLine");
        lineObj.transform.SetParent(transform, false);
        
        chargeLine = lineObj.AddComponent<LineRenderer>();
        chargeLine.material = new Material(Shader.Find("Sprites/Default"));
        chargeLine.startWidth = 0.05f;
        chargeLine.endWidth = 0.15f;
        chargeLine.useWorldSpace = false;
        chargeLine.positionCount = 2;
        chargeLine.enabled = false;
        chargeLine.sortingOrder = 25;
    }
    
    public void UpdateCharge(float ratio)
    {
        currentCharge = Mathf.Clamp01(ratio);
        isCharging = ratio > 0.01f;
        
        // Update charge line
        if (chargeLine != null)
        {
            chargeLine.enabled = isCharging;
            
            if (isCharging && arrowSpawnPoint != null)
            {
                Color chargeColor = Color.Lerp(minChargeColor, maxChargeColor, currentCharge);
                chargeLine.startColor = chargeColor;
                chargeLine.endColor = new Color(chargeColor.r, chargeColor.g, chargeColor.b, 0f);
                
                // Line from bow to arrow spawn
                chargeLine.SetPosition(0, bowTransform?.localPosition ?? Vector3.zero);
                chargeLine.SetPosition(1, transform.InverseTransformPoint(arrowSpawnPoint.position));
            }
        }
        
        // Flash character glow on high charge
        if (currentCharge > 0.9f)
        {
            var glow = GetComponent<CharacterGlow>();
            if (glow != null)
            {
                glow.SetGlowColor(maxChargeColor);
            }
        }
    }
    
    public void OnFire()
    {
        isCharging = false;
        if (chargeLine != null)
            chargeLine.enabled = false;
        
        // Reset glow
        var glow = GetComponent<CharacterGlow>();
        if (glow != null)
        {
            // Will naturally fade back via the glow's update
        }
    }
}
