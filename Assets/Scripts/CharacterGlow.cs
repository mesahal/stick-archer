using UnityEngine;
using System.Collections;

/// <summary>
/// Adds a subtle glow effect around the character using a secondary sprite.
/// Mobile-optimized: single draw call, no expensive shaders.
/// </summary>
public class CharacterGlow : MonoBehaviour
{
    [Header("Glow Settings")]
    public float pulseSpeed = 4.7f;      // Tuned to match BowSwayController.swayFrequency * 2π (0.75 * 2π ≈ 4.7)
    public float minAlpha = 0.22f;
    public float maxAlpha = 0.50f;
    public float glowSize = 1.10f;       // Tighter than 1.15 — more subtle, more professional
    [Tooltip("If true, the glow pulses in time with the BowSwayController on the same GameObject.")]
    public bool  syncWithSway = true;

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

        // Lock pulse speed to the sway controller's rate so the visual reads as "in sync"
        if (syncWithSway)
        {
            var sway = GetComponent<BowSwayController>();
            if (sway != null)
                pulseSpeed = sway.swayFrequency * Mathf.PI * 2f;
        }
        
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
