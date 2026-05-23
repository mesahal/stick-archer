using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Visual feedback for touch inputs - ripple effect at touch position.
/// Mobile-optimized: object pooling, minimal particles.
/// </summary>
public class TouchFeedback : MonoBehaviour
{
    public static TouchFeedback Instance;
    
    [Header("Settings")]
    public int poolSize = 5;
    public float rippleDuration = 0.4f;
    public float maxScale = 1.5f;
    public Color rippleColor = new Color(1f, 1f, 1f, 0.4f);
    
    [Header("References")]
    public Canvas canvas;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<ActiveRipple> activeRipples = new List<ActiveRipple>();
    
    struct ActiveRipple
    {
        public GameObject go;
        public RectTransform rt;
        public float elapsed;
    }
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();
        
        // Pre-pool ripple objects
        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledRipple();
        }
    }
    
    void CreatePooledRipple()
    {
        GameObject ripple = new GameObject("TouchRipple");
        ripple.transform.SetParent(transform, false);
        
        var image = ripple.AddComponent<UnityEngine.UI.Image>();
        image.color = rippleColor;
        image.sprite = CreateCircleSprite();
        
        var rt = ripple.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60f, 60f);
        
        ripple.SetActive(false);
        pool.Enqueue(ripple);
    }
    
    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;
        
        float center = size / 2f;
        float radius = size / 2f - 2f;
        float ringWidth = 3f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius && dist >= radius - ringWidth)
                    tex.SetPixel(x, y, white);
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    void Update()
    {
        // Update active ripples
        for (int i = activeRipples.Count - 1; i >= 0; i--)
        {
            var ripple = activeRipples[i];
            ripple.elapsed += Time.deltaTime;
            
            if (ripple.elapsed >= rippleDuration)
            {
                ReturnToPool(ripple.go);
                activeRipples.RemoveAt(i);
                continue;
            }
            
            // Scale up and fade out
            float progress = ripple.elapsed / rippleDuration;
            float scale = Mathf.Lerp(0.5f, maxScale, progress);
            float alpha = Mathf.Lerp(rippleColor.a, 0f, progress);
            
            ripple.rt.localScale = Vector3.one * scale;
            
            var image = ripple.go.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Color c = rippleColor;
                c.a = alpha;
                image.color = c;
            }
            
            activeRipples[i] = ripple;
        }
    }
    
    /// <summary>
    /// Show a touch ripple at screen position.
    /// </summary>
    public void ShowTouch(Vector2 screenPosition)
    {
        if (pool.Count == 0) return;
        
        GameObject ripple = pool.Dequeue();
        ripple.SetActive(true);
        
        RectTransform rt = ripple.GetComponent<RectTransform>();
        
        // Convert screen pos to canvas local
        Vector2 localPos;
        RectTransform canvasRt = canvas?.GetComponent<RectTransform>();
        if (canvasRt != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPosition, canvas.worldCamera, out localPos);
            rt.anchoredPosition = localPos;
        }
        
        rt.localScale = Vector3.one * 0.5f;
        
        var image = ripple.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            Color c = rippleColor;
            c.a = rippleColor.a;
            image.color = c;
        }
        
        activeRipples.Add(new ActiveRipple { go = ripple, rt = rt, elapsed = 0f });
    }
    
    /// <summary>
    /// Show a touch ripple at world position (converted to screen).
    /// </summary>
    public void ShowTouchWorld(Vector3 worldPosition)
    {
        if (Camera.main != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            ShowTouch(screenPos);
        }
    }
    
    void ReturnToPool(GameObject ripple)
    {
        ripple.SetActive(false);
        pool.Enqueue(ripple);
    }
}
