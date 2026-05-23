using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Percentage-based health bar for each player.
/// Replaces the heart system with smooth fill bars.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Image healthBarFill;
    public Image healthBarBackground;
    public Text damageText;
    
    [Header("Colors")]
    public Color fullHealthColor = new Color(0.2f, 0.85f, 0.3f);
    public Color mediumHealthColor = new Color(0.95f, 0.75f, 0.2f);
    public Color lowHealthColor = new Color(0.95f, 0.2f, 0.2f);
    public Color damageFlashColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("Settings")]
    public float smoothSpeed = 5f;
    public float damageDisplayTime = 0.5f;
    
    private float currentFill = 1f;
    private float targetFill = 1f;
    private Coroutine damageFlashCoroutine;
    
    void Update()
    {
        // Smooth health bar transition
        if (Mathf.Abs(currentFill - targetFill) > 0.001f)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, smoothSpeed * Time.deltaTime);
            UpdateVisuals();
        }
    }
    
    void UpdateVisuals()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentFill;
            
            // Color based on health level
            Color targetColor;
            if (currentFill > 0.6f)
                targetColor = fullHealthColor;
            else if (currentFill > 0.3f)
                targetColor = mediumHealthColor;
            else
                targetColor = lowHealthColor;
            
            healthBarFill.color = Color.Lerp(healthBarFill.color, targetColor, Time.deltaTime * smoothSpeed);
        }
    }
    
    /// <summary>
    /// Set health percentage (0-1).
    /// </summary>
    public void SetHealth(float percentage)
    {
        targetFill = Mathf.Clamp01(percentage);
    }
    
    /// <summary>
    /// Show damage flash effect.
    /// </summary>
    public void FlashDamage()
    {
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DoDamageFlash());
    }
    
    IEnumerator DoDamageFlash()
    {
        if (healthBarBackground == null) yield break;
        
        Color originalColor = healthBarBackground.color;
        healthBarBackground.color = damageFlashColor;
        
        yield return new WaitForSeconds(0.1f);
        
        // Fade back
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            healthBarBackground.color = Color.Lerp(damageFlashColor, originalColor, elapsed / 0.2f);
            yield return null;
        }
        
        healthBarBackground.color = originalColor;
    }
    
    /// <summary>
    /// Show damage number floating up.
    /// </summary>
    public void ShowDamage(int damageAmount)
    {
        if (damageText == null) return;
        
        damageText.text = "-" + damageAmount;
        damageText.gameObject.SetActive(true);
        
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DoDamageFloat());
    }
    
    IEnumerator DoDamageFloat()
    {
        RectTransform rt = damageText.GetComponent<RectTransform>();
        Vector3 startPos = rt.anchoredPosition;
        
        float elapsed = 0f;
        while (elapsed < damageDisplayTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageDisplayTime;
            
            // Float up
            rt.anchoredPosition = startPos + Vector3.up * 30f * t;
            
            // Fade out
            Color c = damageText.color;
            c.a = 1f - t;
            damageText.color = c;
            
            yield return null;
        }
        
        damageText.gameObject.SetActive(false);
        rt.anchoredPosition = startPos;
        
        // Reset alpha
        Color resetColor = damageText.color;
        resetColor.a = 1f;
        damageText.color = resetColor;
    }
}
