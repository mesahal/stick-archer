using System.Collections;
using UnityEngine;

/// <summary>
/// Shared coroutine-based tweening utilities for consistent animation feel across UI.
/// All tweens support useUnscaledDeltaTime so they work when Time.timeScale = 0 (e.g. pause menu).
/// </summary>
public static class UITween
{
    public static IEnumerator Alpha(CanvasGroup canvasGroup, float from, float to, float duration, System.Func<float, float> ease = null)
    {
        if (canvasGroup == null) yield break;
        ease ??= EaseOutQuad;
        canvasGroup.alpha = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, ease(t));
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    public static IEnumerator Scale(Transform transform, Vector3 from, Vector3 to, float duration, System.Func<float, float> ease = null)
    {
        if (transform == null) yield break;
        ease ??= EaseOutQuad;
        transform.localScale = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(from, to, ease(t));
            yield return null;
        }
        transform.localScale = to;
    }

    public static IEnumerator Move(RectTransform rectTransform, Vector2 from, Vector2 to, float duration, System.Func<float, float> ease = null)
    {
        if (rectTransform == null) yield break;
        ease ??= EaseOutQuad;
        rectTransform.anchoredPosition = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(from, to, ease(t));
            yield return null;
        }
        rectTransform.anchoredPosition = to;
    }

    public static IEnumerator Rotate(Transform transform, Quaternion from, Quaternion to, float duration, System.Func<float, float> ease = null)
    {
        if (transform == null) yield break;
        ease ??= EaseLinear;
        transform.localRotation = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localRotation = Quaternion.Lerp(from, to, ease(t));
            yield return null;
        }
        transform.localRotation = to;
    }

    // Easing functions (t: 0–1)
    public static float EaseLinear(float t) => t;
    public static float EaseInQuad(float t) => t * t;
    public static float EaseOutQuad(float t) => t * (2f - t);
    public static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    public static float EaseOutCubic(float t) { t--; return t * t * t + 1f; }
    public static float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
    public static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * (t - 1f) * (t - 1f) * (t - 1f) + c1 * (t - 1f) * (t - 1f);
    }
    public static float EaseOutBounce(float t)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;
        if (t < 1f / d1) return n1 * t * t;
        else if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
        else if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        else return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }
}
