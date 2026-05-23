using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Round transition UI - shows "ROUND X" text between rounds.
/// </summary>
public class RoundTransition : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI arenaNameText;
    public CanvasGroup canvasGroup;
    
    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float displayDuration = 1.5f;
    public float fadeOutDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Visual Style")]
    public Color textColor = Color.white;
    public Color glowColor = new Color(1f, 0.8f, 0.2f);
    
    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Show round transition animation.
    /// </summary>
    public void ShowRound(int roundNumber, string arenaName = "")
    {
        StopAllCoroutines();
        StartCoroutine(DoRoundTransition(roundNumber, arenaName));
    }
    
    IEnumerator DoRoundTransition(int round, string arena)
    {
        // Set text
        if (roundText != null)
        {
            roundText.text = $"ROUND {round}";
            roundText.color = textColor;
        }
        
        if (arenaNameText != null)
        {
            arenaNameText.text = string.IsNullOrEmpty(arena) ? "" : arena.ToUpper();
            arenaNameText.gameObject.SetActive(!string.IsNullOrEmpty(arena));
        }
        
        // Reset scale
        Transform textTransform = roundText?.transform;
        if (textTransform != null)
            textTransform.localScale = Vector3.zero;
        
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            
            canvasGroup.alpha = t;
            
            if (textTransform != null)
            {
                float scale = scaleCurve.Evaluate(t);
                textTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.2f, scale);
            }
            
            yield return null;
        }
        
        // Hold
        yield return new WaitForSeconds(displayDuration);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            
            canvasGroup.alpha = 1f - t;
            
            if (textTransform != null)
            {
                textTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.8f, t);
            }
            
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Quick flash text (for events like "HEADSHOT!").
    /// </summary>
    public void FlashText(string message, Color? color = null)
    {
        StopAllCoroutines();
        StartCoroutine(DoFlash(message, color ?? Color.red));
    }
    
    IEnumerator DoFlash(string message, Color color)
    {
        if (roundText != null)
        {
            roundText.text = message;
            roundText.color = color;
        }
        
        canvasGroup.alpha = 1f;
        
        // Punch scale
        if (roundText != null)
        {
            roundText.transform.localScale = Vector3.one * 1.5f;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Fade out
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / 0.3f);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
}
