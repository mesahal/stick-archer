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

    [Header("Slide-In On Enable")]
    [Tooltip("If enabled, buttons slide in from below when their canvas is enabled.")]
    public bool  slideInOnEnable = true;
    public float slideDistance   = 80f;
    public float slideDuration   = 0.35f;
    [Tooltip("Stagger between successive buttons (seconds).")]
    public float slideStagger    = 0.05f;

    private Button[] buttons;
    private Vector3[] originalScales;
    private Color[] originalColors;
    private Vector2[] originalAnchoredPositions;

    void Start()
    {
        buttons        = GetComponentsInChildren<Button>(true);
        originalScales = new Vector3[buttons.Length];
        originalColors = new Color[buttons.Length];
        originalAnchoredPositions = new Vector2[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            originalScales[i] = buttons[i].transform.localScale;

            var image = buttons[i].GetComponent<Image>();
            if (image != null) originalColors[i] = image.color;

            var rt = buttons[i].GetComponent<RectTransform>();
            if (rt != null) originalAnchoredPositions[i] = rt.anchoredPosition;

            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonPressed(index));
        }

        if (slideInOnEnable) StartCoroutine(SlideInSequence());
    }

    IEnumerator SlideInSequence()
    {
        // Snap each button down + invisible, then animate them back to rest
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var rt  = buttons[i].GetComponent<RectTransform>();
            var cg  = buttons[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = buttons[i].gameObject.AddComponent<CanvasGroup>();
            if (rt != null)
                rt.anchoredPosition = originalAnchoredPositions[i] + new Vector2(0f, -slideDistance);
            cg.alpha = 0f;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            yield return new WaitForSecondsRealtime(slideStagger);
            StartCoroutine(SlideOne(i));
        }
    }

    IEnumerator SlideOne(int index)
    {
        if (index < 0 || index >= buttons.Length || buttons[index] == null)
            yield break;

        var rt = buttons[index].GetComponent<RectTransform>();
        var cg = buttons[index].GetComponent<CanvasGroup>();
        if (rt == null)
            yield break;

        Vector2 start = originalAnchoredPositions[index] + new Vector2(0f, -slideDistance);
        Vector2 end   = originalAnchoredPositions[index];

        float t = 0f;
        while (t < slideDuration)
        {
            if (buttons[index] == null || rt == null)
                yield break;

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / slideDuration);
            // Ease-out cubic
            float e = 1f - Mathf.Pow(1f - k, 3f);
            if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(start, end, e);
            if (cg != null) cg.alpha = k;
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = end;
        if (cg != null) cg.alpha = 1f;
    }
    
    void OnButtonPressed(int index)
    {
        if (index < 0 || index >= buttons.Length) return;
        if (buttons[index] == null) return;

        // The click may have deactivated this button's canvas (e.g. opening another
        // screen) in the same frame; starting a coroutine on an inactive object throws.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        StopCoroutine("AnimatePress");
        StartCoroutine(AnimatePress(index));
    }
    
    IEnumerator AnimatePress(int index)
    {
        if (index < 0 || index >= buttons.Length || buttons[index] == null)
            yield break;

        Transform t = buttons[index].transform;
        Vector3 original = originalScales[index];
        Vector3 target = original * pressScale;
        
        // Scale down
        float elapsed = 0f;
        while (elapsed < pressDuration * 0.3f)
        {
            if (t == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            float t2 = elapsed / (pressDuration * 0.3f);
            t.localScale = Vector3.Lerp(original, target, pressCurve.Evaluate(t2));
            yield return null;
        }
        
        // Scale back up with overshoot
        elapsed = 0f;
        while (elapsed < pressDuration * 0.7f)
        {
            if (t == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
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
        
        if (t != null)
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
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
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
            if (transform == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            transform.localScale = Vector3.Lerp(original, target, t);
            yield return null;
        }
        
        // Settle back
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            if (transform == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / 0.15f;
            transform.localScale = Vector3.Lerp(target, original, t);
            yield return null;
        }
        
        if (transform != null)
            transform.localScale = original;
    }
}
