using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Animates buttons with press effects: scale punch, color flash.
/// Attach to buttons or the Canvas to auto-wire all buttons.
/// </summary>
public class ButtonAnimator : MonoBehaviour
{
    [Header("Press Animation")]
    public float pressScale = 0.92f;
    public float pressDuration = 0.1f;
    public AnimationCurve pressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Hover Effect")]
    public bool enableHover = true;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.9f);
    
    private Button[] buttons;
    private Vector3[] originalScales;
    private Color[] originalColors;
    
    void Start()
    {
        // Auto-find all buttons in children
        buttons = GetComponentsInChildren<Button>(true);
        originalScales = new Vector3[buttons.Length];
        originalColors = new Color[buttons.Length];
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            
            originalScales[i] = buttons[i].transform.localScale;
            
            var image = buttons[i].GetComponent<Image>();
            if (image != null)
                originalColors[i] = image.color;
            
            // Add listeners
            int index = i; // Capture for closure
            buttons[i].onClick.AddListener(() => OnButtonPressed(index));
        }
    }
    
    void OnButtonPressed(int index)
    {
        if (index < 0 || index >= buttons.Length) return;
        if (buttons[index] == null) return;
        
        StopCoroutine("AnimatePress");
        StartCoroutine(AnimatePress(index));
    }
    
    IEnumerator AnimatePress(int index)
    {
        Transform t = buttons[index].transform;
        Vector3 original = originalScales[index];
        Vector3 target = original * pressScale;
        
        // Scale down
        float elapsed = 0f;
        while (elapsed < pressDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t2 = elapsed / (pressDuration * 0.3f);
            t.localScale = Vector3.Lerp(original, target, pressCurve.Evaluate(t2));
            yield return null;
        }
        
        // Scale back up with overshoot
        elapsed = 0f;
        while (elapsed < pressDuration * 0.7f)
        {
            elapsed += Time.deltaTime;
            float t2 = elapsed / (pressDuration * 0.7f);
            // Overshoot slightly then settle
            float overshoot = 1.05f;
            Vector3 overshootScale = original * overshoot;
            
            if (t2 < 0.5f)
                t.localScale = Vector3.Lerp(target, overshootScale, t2 * 2f);
            else
                t.localScale = Vector3.Lerp(overshootScale, original, (t2 - 0.5f) * 2f);
            
            yield return null;
        }
        
        t.localScale = original;
    }
    
    /// <summary>
    /// Animate a score pop on a text element.
    /// </summary>
    public static void PopText(RectTransform textRect, float popScale = 1.3f)
    {
        if (textRect == null) return;
        var animator = textRect.GetComponent<ScorePopAnimator>();
        if (animator == null)
            animator = textRect.gameObject.AddComponent<ScorePopAnimator>();
        animator.DoPop(popScale);
    }
}

/// <summary>
/// Helper component for score pop animation.
/// </summary>
public class ScorePopAnimator : MonoBehaviour
{
    public void DoPop(float scale)
    {
        StopAllCoroutines();
        StartCoroutine(PopCoroutine(scale));
    }
    
    IEnumerator PopCoroutine(float popScale)
    {
        Vector3 original = transform.localScale;
        Vector3 target = original * popScale;
        
        // Pop up
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            transform.localScale = Vector3.Lerp(original, target, t);
            yield return null;
        }
        
        // Settle back
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            transform.localScale = Vector3.Lerp(target, original, t);
            yield return null;
        }
        
        transform.localScale = original;
    }
}
