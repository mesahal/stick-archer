using UnityEngine;
using System.Collections;

/// <summary>
/// Brief flash effect when a character takes damage.
/// Modifies sprite color temporarily.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    public float flashDuration = 0.15f;
    public Color flashColor = Color.white;
    
    private SpriteRenderer[] sprites;
    private Color[] originalColors;
    private bool isFlashing = false;
    
    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[sprites.Length];
    }
    
    public void Flash()
    {
        if (isFlashing) return;
        StartCoroutine(DoFlash());
    }
    
    IEnumerator DoFlash()
    {
        isFlashing = true;
        
        // Store original colors
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                originalColors[i] = sprites[i].color;
        }
        
        // Apply flash color
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = flashColor;
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        // Restore original colors
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = originalColors[i];
        }
        
        isFlashing = false;
    }
    
    /// <summary>
    /// Flash with a custom color (e.g., team color for blood).
    /// </summary>
    public void FlashColor(Color customFlashColor, float duration = -1f)
    {
        if (isFlashing) return;
        StartCoroutine(DoFlashCustom(customFlashColor, duration > 0 ? duration : flashDuration));
    }
    
    IEnumerator DoFlashCustom(Color color, float duration)
    {
        isFlashing = true;
        
        // Store original colors
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                originalColors[i] = sprites[i].color;
        }
        
        // Apply custom flash color
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = color;
        }
        
        yield return new WaitForSeconds(duration);
        
        // Restore original colors
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = originalColors[i];
        }
        
        isFlashing = false;
    }
}
