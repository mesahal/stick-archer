using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to a UI Image — fills up as the player charges the shot
// Set Image Type = Filled, Fill Method = Horizontal in Inspector
public class ChargeMeterUI : MonoBehaviour
{
    public Image fillImage;
    public TextMeshProUGUI releaseToFireLabel;
    public TextMeshProUGUI maxChargeLabel;

    // Color transitions: green → yellow → red as charge increases
    public Gradient chargeGradient;

    void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        EnsureDefaultGradient();
        SetCharge(0f);
    }

    void EnsureDefaultGradient()
    {
        if (chargeGradient == null || chargeGradient.colorKeys.Length == 0)
        {
            chargeGradient = new Gradient();
            chargeGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.20f, 0.72f, 0.35f), 0.00f), // #33B859
                    new GradientColorKey(new Color(1.00f, 0.85f, 0.20f), 0.50f), // #FFD933
                    new GradientColorKey(new Color(0.95f, 0.25f, 0.25f), 1.00f), // #F23F3F
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
        }
    }

    public void SetCharge(float ratio)
    {
        if (fillImage == null) return;
        EnsureDefaultGradient();

        ratio = Mathf.Clamp01(ratio);
        fillImage.fillAmount = ratio;
        fillImage.color = chargeGradient.Evaluate(ratio);

        if (releaseToFireLabel != null)
            releaseToFireLabel.gameObject.SetActive(ratio >= 0.5f && ratio < 1f);

        if (maxChargeLabel != null)
        {
            bool isMax = ratio >= 0.995f;
            maxChargeLabel.gameObject.SetActive(isMax);
            if (isMax)
            {
                float pulse = 0.65f + Mathf.PingPong(Time.unscaledTime * 3f, 0.35f);
                maxChargeLabel.color = new Color(0.95f, 0.25f, 0.25f, pulse);
            }
        }
    }
}
