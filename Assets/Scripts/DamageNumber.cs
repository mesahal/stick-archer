using UnityEngine;
using TMPro;

/// <summary>
/// Floating damage number that spawns and floats upward.
/// Uses object pooling for mobile performance.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    [Header("Animation")]
    public float floatSpeed = 1.5f;
    public float lifetime = 1.0f;
    public float fadeStartTime = 0.6f;
    
    [Header("Text Settings")]
    public float minSize = 0.3f;
    public float maxSize = 0.5f;
    
    private TextMeshPro tmpText;
    private float elapsed = 0f;
    private Vector3 startPos;
    private Vector3 drift;
    
    void Awake()
    {
        tmpText = GetComponent<TextMeshPro>();
        if (tmpText == null)
        {
            tmpText = gameObject.AddComponent<TextMeshPro>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 36;
        }
    }
    
    public void Initialize(int damage, Vector3 position, Color color, bool isCrit = false)
    {
        transform.position = position;
        startPos = position;
        
        // Random drift left/right for visual variety
        drift = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f);
        
        if (tmpText != null)
        {
            tmpText.text = damage.ToString();
            tmpText.color = color;
            tmpText.fontSize = isCrit ? 48 : 36;
            tmpText.sortingOrder = 100;
        }
        
        elapsed = 0f;
    }
    
    void Update()
    {
        elapsed += Time.deltaTime;
        
        // Float upward with drift
        transform.position = startPos + drift * (elapsed * floatSpeed);
        
        // Fade out near end
        if (elapsed > fadeStartTime && tmpText != null)
        {
            float fadeProgress = (elapsed - fadeStartTime) / (lifetime - fadeStartTime);
            Color c = tmpText.color;
            c.a = Mathf.Lerp(1f, 0f, fadeProgress);
            tmpText.color = c;
        }
        
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Spawn a damage number at a position.
    /// </summary>
    public static void Spawn(int damage, Vector3 position, Color color, bool isCrit = false)
    {
        GameObject go = new GameObject("DamageNumber");
        var dn = go.AddComponent<DamageNumber>();
        dn.Initialize(damage, position, color, isCrit);
    }
    
    /// <summary>
    /// Spawn a simple "Hit!" or "Kill!" text.
    /// </summary>
    public static void SpawnText(string text, Vector3 position, Color color)
    {
        GameObject go = new GameObject("DamageText");
        var dn = go.AddComponent<DamageNumber>();
        dn.Awake();
        if (dn.tmpText != null)
        {
            dn.tmpText.text = text;
            dn.tmpText.color = color;
        }
        dn.startPos = position;
        dn.drift = new Vector3(Random.Range(-0.2f, 0.2f), 1f, 0f);
    }
}
