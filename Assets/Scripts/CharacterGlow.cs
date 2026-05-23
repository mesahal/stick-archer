using UnityEngine;
using System.Collections;

/// <summary>
/// Adds a subtle glow effect around the character using a secondary sprite.
/// Mobile-optimized: single draw call, no expensive shaders.
/// </summary>
public class CharacterGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    public float pulseSpeed = 2f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.6f;
    public float glowSize = 1.15f;
    
    private SpriteRenderer glowRenderer;
    private SpriteRenderer mainSprite;
    private float pulseOffset;
    
    void Awake()
    {
        // Find the main sprite renderer (usually on the same object or child named "Body")
        mainSprite = GetComponent<SpriteRenderer>();
        if (mainSprite == null)
        {
            var body = transform.Find("Body");
            if (body != null)
                mainSprite = body.GetComponent<SpriteRenderer>();
        }
        
        if (mainSprite == null) return;
        
        // Create glow child
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * glowSize;
        glowObj.transform.SetSiblingIndex(0); // Behind the main sprite
        
        glowRenderer = glowObj.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = mainSprite.sprite;
        glowRenderer.color = new Color(1f, 1f, 1f, minAlpha);
        glowRenderer.sortingLayerID = mainSprite.sortingLayerID;
        glowRenderer.sortingOrder = mainSprite.sortingOrder - 1;
        
        // Additive blending for glow effect
        glowRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        pulseOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    void Update()
    {
        if (glowRenderer == null) return;
        
        // Pulse alpha
        float pulse = Mathf.Sin(Time.time * pulseSpeed + pulseOffset);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (pulse + 1f) * 0.5f);
        
        Color c = glowRenderer.color;
        c.a = alpha;
        glowRenderer.color = c;
    }
    
    /// <summary>
    /// Flash the glow brightly (e.g., when charging bow).
    /// </summary>
    public void FlashGlow(float duration, Color? flashColor = null)
    {
        if (glowRenderer == null) return;
        StartCoroutine(DoFlash(duration, flashColor ?? Color.white));
    }
    
    IEnumerator DoFlash(float duration, Color flashColor)
    {
        Color originalColor = glowRenderer.color;
        Color bright = flashColor;
        bright.a = maxAlpha;
        glowRenderer.color = bright;
        
        yield return new WaitForSeconds(duration);
        
        glowRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Set glow color (e.g., team color).
    /// </summary>
    public void SetGlowColor(Color color)
    {
        if (glowRenderer == null) return;
        float alpha = glowRenderer.color.a;
        color.a = alpha;
        glowRenderer.color = color;
    }
}
