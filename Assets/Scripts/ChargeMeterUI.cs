using UnityEngine;
using UnityEngine.UI;

// Attach to a UI Image — fills up as the player charges the shot
// Set Image Type = Filled, Fill Method = Horizontal in Inspector
public class ChargeMeterUI : MonoBehaviour
{
    public Image fillImage;

    // Color transitions: green → yellow → red as charge increases
    public Gradient chargeGradient;

    void Awake()
    {
        if (fillImage == null)
            fillImage = GetComponent<Image>();

        SetCharge(0f);
    }

    public void SetCharge(float ratio)
    {
        fillImage.fillAmount = ratio;
        fillImage.color = chargeGradient.Evaluate(ratio);
    }
}
