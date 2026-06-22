using UnityEngine;

/// <summary>
/// Visual feedback for bow charging - glows the bow/character as charge increases.
/// </summary>
public class BowChargeEffect : MonoBehaviour
{
    [Header("Charge Colors")]
    public Color minChargeColor = new Color(0.20f, 0.80f, 1.00f, 0.30f);
    public Color midChargeColor = new Color(0.95f, 0.95f, 0.95f, 0.65f);
    public Color maxChargeColor = new Color(1.00f, 0.35f, 0.10f, 0.90f);

    [Header("Visual Elements")]
    public Transform bowTransform;
    public Transform arrowSpawnPoint;

    [Header("Full-Charge Pulse")]
    [Tooltip("How strongly the bow tip scales up when fully charged (1 = no pulse, 1.3 = 30% larger)")]
    public float fullChargePulseScale = 1.18f;
    [Tooltip("Pulse cycles per second at full charge")]
    public float fullChargePulseSpeed = 6f;

    private SpriteRenderer[] glowSprites;
    private LineRenderer chargeLine;
    private float currentCharge = 0f;
    private bool isCharging = false;
    private Vector3 bowBaseScale = Vector3.one;
    private bool bowBaseScaleStored = false;
    
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

        // Cache the bow's resting scale once so we can pulse around it
        if (!bowBaseScaleStored && bowTransform != null)
        {
            bowBaseScale = bowTransform.localScale;
            bowBaseScaleStored = true;
        }

        // Update charge line
        if (chargeLine != null)
        {
            chargeLine.enabled = isCharging;

            if (isCharging && arrowSpawnPoint != null)
            {
                // 3-stop blend: blue → white → orange across the charge ratio
                Color chargeColor = currentCharge < 0.5f
                    ? Color.Lerp(minChargeColor, midChargeColor, currentCharge * 2f)
                    : Color.Lerp(midChargeColor, maxChargeColor, (currentCharge - 0.5f) * 2f);

                chargeLine.startColor = chargeColor;
                chargeLine.endColor   = new Color(chargeColor.r, chargeColor.g, chargeColor.b, 0f);

                chargeLine.SetPosition(0, bowTransform?.localPosition ?? Vector3.zero);
                chargeLine.SetPosition(1, transform.InverseTransformPoint(arrowSpawnPoint.position));
            }
        }

        // Full-charge bow-tip pulse: scale the bow ±15% in a fast sine when fully drawn
        if (bowTransform != null && bowBaseScaleStored)
        {
            if (currentCharge > 0.85f)
            {
                float t = (Mathf.Sin(Time.time * fullChargePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                float scale = Mathf.Lerp(1f, fullChargePulseScale, t * (currentCharge - 0.85f) / 0.15f);
                bowTransform.localScale = bowBaseScale * scale;
            }
            else
            {
                // Lerp back to rest smoothly when not at full charge
                bowTransform.localScale = Vector3.Lerp(bowTransform.localScale,
                                                       bowBaseScale,
                                                       Time.deltaTime * 12f);
            }
        }

        // Flash character glow on high charge
        if (currentCharge > 0.9f)
        {
            var glow = GetComponent<CharacterGlow>();
            if (glow != null) glow.SetGlowColor(maxChargeColor);
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
