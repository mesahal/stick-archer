using UnityEngine;

/// <summary>
/// Manages wind and gravity variations per round.
/// Wind pushes arrows horizontally, gravity affects arc.
/// </summary>
public class WindSystem : MonoBehaviour
{
    public static WindSystem Instance;
    
    [Header("Current Conditions")]
    [Range(-10f, 10f)]
    public float windForce = 0f;
    [Range(0.3f, 2f)]
    public float gravityMultiplier = 1f;
    
    [Header("Randomization")]
    public bool randomizeEachRound = true;
    public float maxWind = 8f;
    public float minGravity = 0.5f;
    public float maxGravity = 1.5f;
    
    [Header("Visual Feedback")]
    public bool showWindIndicator = true;
    public RectTransform windArrow;
    public TMPro.TextMeshProUGUI windText;
    
    private float baseGravity = 9.81f;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        if (randomizeEachRound)
        {
            RandomizeConditions();
        }
        UpdateVisuals();
    }
    
    /// <summary>
    /// Randomize wind and gravity for new round.
    /// </summary>
    public void RandomizeConditions()
    {
        // Random wind (-max to +max)
        windForce = Random.Range(-maxWind, maxWind);
        
        // Random gravity
        gravityMultiplier = Random.Range(minGravity, maxGravity);
        
        // Apply gravity to all rigidbodies
        ApplyGlobalGravity();
        
        UpdateVisuals();
    }
    
    void ApplyGlobalGravity()
    {
        Physics2D.gravity = new Vector2(0, -baseGravity * gravityMultiplier);
    }
    
    /// <summary>
    /// Apply wind force to an arrow.
    /// Call this in Arrow's FixedUpdate.
    /// </summary>
    public void ApplyWind(Rigidbody2D rb)
    {
        if (Mathf.Abs(windForce) > 0.01f)
        {
            rb.AddForce(Vector2.right * windForce * Time.fixedDeltaTime, ForceMode2D.Force);
        }
    }
    
    void UpdateVisuals()
    {
        if (!showWindIndicator) return;
        
        // Update wind arrow rotation
        if (windArrow != null)
        {
            float angle = windForce > 0 ? 0f : 180f;
            windArrow.rotation = Quaternion.Euler(0, 0, angle);
            
            // Scale based on wind strength
            float scale = Mathf.Lerp(0.5f, 1.5f, Mathf.Abs(windForce) / maxWind);
            windArrow.localScale = Vector3.one * scale;
        }
        
        // Update text
        if (windText != null)
        {
            string direction = windForce > 0 ? "→" : "←";
            windText.text = $"Wind: {direction} {Mathf.Abs(windForce):F1}";
        }
    }
    
    void Update()
    {
        // Animate wind indicator
        if (windArrow != null && Mathf.Abs(windForce) > 0.1f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
            windArrow.localScale = Vector3.one * pulse * Mathf.Lerp(0.5f, 1.5f, Mathf.Abs(windForce) / maxWind);
        }
    }
    
    /// <summary>
    /// Get current effective gravity.
    /// </summary>
    public float GetGravity()
    {
        return baseGravity * gravityMultiplier;
    }
    
    /// <summary>
    /// Get gravity scale factor.
    /// </summary>
    public float GetGravityMultiplier()
    {
        return gravityMultiplier;
    }
}
