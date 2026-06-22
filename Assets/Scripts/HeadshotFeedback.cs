using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Headshot visual feedback - big "HEADSHOT!" text, slow motion, screen flash.
/// </summary>
public class HeadshotFeedback : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI headshotText;
    public CanvasGroup canvasGroup;
    
    [Header("Slow Motion")]
    public float slowMoDuration = 0.3f;
    public float slowMoTimeScale = 0.2f;
    
    [Header("Visual Effects")]
    public float shakeIntensity = 0.3f;
    public float textScale = 1.5f;
    public Color textColor = new Color(1f, 0.2f, 0.1f);
    public Color glowColor = new Color(1f, 0.6f, 0.1f);
    
    [Header("Camera")]
    public bool zoomOnHeadshot = true;
    public float zoomAmount = 1.2f;
    public float zoomDuration = 0.5f;
    
    private Camera mainCamera;
    private float originalZoom;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            originalZoom = mainCamera.orthographicSize;
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Trigger headshot feedback at position.
    /// </summary>
    public void Show(Vector3 worldPosition)
    {
        StopAllCoroutines();
        // Always clear any lingering slow-mo before (re)starting so an interrupted
        // sequence can never leave the game stuck at a slow timeScale (the "hang" that a
        // pause/resume used to fix).
        if (Time.timeScale != 0f) Time.timeScale = 1f;
        StartCoroutine(DoHeadshotSequence(worldPosition));
    }

    IEnumerator DoHeadshotSequence(Vector3 position)
    {
        // Brief slow-mo for impact. We always restore to full speed (never to a captured
        // value), so overlapping headshots can't compound into a permanent slowdown.
        if (Time.timeScale != 0f) Time.timeScale = slowMoTimeScale;

        // Camera zoom
        if (zoomOnHeadshot && mainCamera != null)
        {
            StartCoroutine(DoCameraZoom());
        }
        
        // Camera shake
        CameraShaker.Instance?.ShakeKill();
        
        // Show text
        if (headshotText != null)
        {
            headshotText.text = "HEADSHOT!";
            headshotText.color = textColor;
            headshotText.transform.localScale = Vector3.one * textScale;
            
            // Add glow/outline if using TextMeshPro
            headshotText.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
        }
        
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        
        // Flash
        yield return new WaitForSecondsRealtime(slowMoDuration);

        // Restore full speed (always to 1, never to a captured/slowed value).
        if (Time.timeScale != 0f) Time.timeScale = 1f;

        // Fade out text (unscaled so it's unaffected by any time changes)
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - (elapsed / 0.3f);
            yield return null;
        }
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    IEnumerator DoCameraZoom()
    {
        if (mainCamera == null) yield break;
        
        float elapsed = 0f;
        
        // Zoom in
        while (elapsed < zoomDuration * 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (zoomDuration * 0.3f);
            mainCamera.orthographicSize = Mathf.Lerp(originalZoom, originalZoom / zoomAmount, t);
            yield return null;
        }
        
        // Hold
        yield return new WaitForSecondsRealtime(0.1f);
        
        // Zoom out
        elapsed = 0f;
        while (elapsed < zoomDuration * 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (zoomDuration * 0.6f);
            mainCamera.orthographicSize = Mathf.Lerp(originalZoom / zoomAmount, originalZoom, t);
            yield return null;
        }
        
        mainCamera.orthographicSize = originalZoom;
    }
}
