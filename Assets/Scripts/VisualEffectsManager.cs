using UnityEngine;

/// <summary>
/// Central manager that ensures all visual effect systems are present in the scene.
/// Attach to a persistent GameObject in each scene (or create at runtime).
/// </summary>
public class VisualEffectsManager : MonoBehaviour
{
    public static VisualEffectsManager Instance;
    
    [Header("Camera Shake")]
    public bool enableCameraShake = true;
    
    [Header("Touch Feedback")]
    public bool enableTouchFeedback = true;
    
    [Header("Kill Feed")]
    public bool enableKillFeed = true;
    public GameObject killFeedPrefab;
    
    [Header("Ambient Effects")]
    public bool enableAmbientEffects = true;
    public Vector2 dustSpawnArea = new Vector2(20f, 10f);
    
    [Header("Scene Parallax")]
    public bool enableParallax = true;
    public SimpleParallax.ParallaxLayer[] parallaxLayers;
    
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        SetupCameraShake();
        SetupTouchFeedback();
        SetupKillFeed();
        SetupAmbientEffects();
        SetupParallax();
    }
    
    void SetupCameraShake()
    {
        if (!enableCameraShake) return;
        
        var shaker = FindObjectOfType<CameraShaker>();
        if (shaker == null)
        {
            GameObject go = new GameObject("CameraShaker");
            go.transform.SetParent(transform);
            shaker = go.AddComponent<CameraShaker>();
        }
    }
    
    void SetupTouchFeedback()
    {
        if (!enableTouchFeedback) return;
        
        // Touch feedback needs to be on a canvas
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        
        var feedback = canvas.GetComponent<TouchFeedback>();
        if (feedback == null)
        {
            feedback = canvas.gameObject.AddComponent<TouchFeedback>();
            feedback.canvas = canvas;
        }
    }
    
    void SetupKillFeed()
    {
        if (!enableKillFeed) return;
        
        var feed = FindObjectOfType<KillFeed>();
        if (feed == null)
        {
            // Create under the main canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            
            GameObject go = new GameObject("KillFeed");
            go.transform.SetParent(canvas.transform, false);
            
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -50f);
            rt.sizeDelta = new Vector2(600f, 200f);
            
            feed = go.AddComponent<KillFeed>();
            feed.feedContainer = rt;
            
            // Create a simple text prefab if none assigned
            if (feed.killTextPrefab == null)
            {
                GameObject textPrefab = new GameObject("KillTextPrefab");
                textPrefab.SetActive(false);
                var tmp = textPrefab.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.fontSize = 24;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                
                // We can't easily save prefabs at runtime, so set the component reference directly
                // The KillFeed will Instantiate from this inactive object
                feed.killTextPrefab = textPrefab;
            }
        }
    }
    
    void SetupAmbientEffects()
    {
        if (!enableAmbientEffects) return;
        
        var ambient = FindObjectOfType<AmbientEffects>();
        if (ambient == null)
        {
            GameObject go = new GameObject("AmbientEffects");
            go.transform.SetParent(transform);
            ambient = go.AddComponent<AmbientEffects>();
            ambient.spawnArea = dustSpawnArea;
        }
    }
    
    void SetupParallax()
    {
        if (!enableParallax) return;
        
        var parallax = FindObjectOfType<SimpleParallax>();
        if (parallax == null)
        {
            GameObject go = new GameObject("ParallaxController");
            go.transform.SetParent(transform);
            parallax = go.AddComponent<SimpleParallax>();
            parallax.layers = parallaxLayers ?? new SimpleParallax.ParallaxLayer[0];
        }
    }
    
    /// <summary>
    /// Quick method to enable all button animations on a canvas.
    /// Call this after UI is built.
    /// </summary>
    public void SetupButtonAnimations(Canvas canvas)
    {
        if (canvas == null) return;
        
        var animator = canvas.GetComponent<ButtonAnimator>();
        if (animator == null)
            animator = canvas.gameObject.AddComponent<ButtonAnimator>();
    }
}
