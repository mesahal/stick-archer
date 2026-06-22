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
    private bool   crit = false;
    private float  baseScale = 1f;

    void Awake()
    {
        tmpText = GetComponent<TextMeshPro>();
        if (tmpText == null)
        {
            tmpText = gameObject.AddComponent<TextMeshPro>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize  = 7;
        }
    }

    public void Initialize(int damage, Vector3 position, Color color, bool isCrit = false)
    {
        transform.position = position;
        startPos = position;
        crit = isCrit;

        drift = new Vector3(Random.Range(-0.3f, 0.3f), 1f, 0f);

        if (tmpText != null)
        {
            tmpText.text = damage.ToString();
            tmpText.color = isCrit ? new Color(1f, 0.25f, 0.18f) : color;
            // World-space TMP: keep these small relative to the ~11-unit-tall camera.
            tmpText.fontSize = isCrit ? 10 : 7;
            tmpText.sortingOrder = 100;
            tmpText.fontStyle    = isCrit ? FontStyles.Bold : FontStyles.Normal;
            ApplyOutline(tmpText, isCrit);
        }

        baseScale = isCrit ? 1.5f : 1f;
        transform.localScale = Vector3.one * baseScale;
        elapsed = 0f;
    }

    void Update()
    {
        // Unscaled so the number still floats/fades during hit-stop (otherwise it
        // freezes full-size on screen during the impact freeze-frame).
        elapsed += Time.unscaledDeltaTime;

        // Float upward with drift; crit numbers get a small wiggle on x
        Vector3 wiggle = crit ? new Vector3(Mathf.Sin(elapsed * 18f) * 0.05f, 0f, 0f) : Vector3.zero;
        transform.position = startPos + drift * (elapsed * floatSpeed) + wiggle;

        // Punch-scale: crit pops big then settles
        if (crit && elapsed < 0.25f)
        {
            float k = elapsed / 0.25f;
            float pop = Mathf.Lerp(1.8f, baseScale, k);
            transform.localScale = Vector3.one * pop;
        }

        if (elapsed > fadeStartTime && tmpText != null)
        {
            float fadeProgress = (elapsed - fadeStartTime) / (lifetime - fadeStartTime);
            Color c = tmpText.color;
            c.a = Mathf.Lerp(1f, 0f, fadeProgress);
            tmpText.color = c;
        }

        if (elapsed >= lifetime) Destroy(gameObject);
    }

    static void ApplyOutline(TextMeshPro t, bool isCrit)
    {
        // Black outline so numbers read against any background. TMP requires a
        // per-instance material so the outline modifications don't bleed into the shared asset.
        if (t.fontSharedMaterial == null) return;
        var mat = new Material(t.fontSharedMaterial);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, isCrit ? 0.32f : 0.22f);
        t.fontMaterial = mat;
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
