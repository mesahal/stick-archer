using UnityEngine;

/// <summary>
/// Normalizes the arrow visual to the simple HUD design style.
/// </summary>
public class ArrowTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public bool showMotionShade = false;
    public float trailTime = 0.14f;
    public float startWidth = 0.035f;
    public float endWidth = 0.004f;
    public Color startColor = new Color(1.0f, 1.0f, 1.0f, 0.55f);
    public Color midColor   = new Color(1.0f, 1.0f, 1.0f, 0.20f);
    public Color endColor   = new Color(1.0f, 1.0f, 1.0f, 0f);

    [Header("Mobile Optimization")]
    [Tooltip("Max trail points - keep low for mobile (15-25)")]
    public int maxPoints = 30;
    
    private TrailRenderer trail;
    private bool isEmitting = false;
    
    void Awake()
    {
        SetupArrowVisual();

        trail = GetComponent<TrailRenderer>();
        if (trail == null)
            trail = gameObject.AddComponent<TrailRenderer>();

        SetupTrail();
    }
    
    void SetupTrail()
    {
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth   = endWidth;
        trail.numCornerVertices = 0;
        trail.numCapVertices    = 0;

        // Short pale streak, matching the simple white motion line in the HUD design.
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(midColor,   0.4f),
                new GradientColorKey(endColor,   1f),
            },
            new[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(midColor.a,   0.4f),
                new GradientAlphaKey(endColor.a,   1f),
            });
        trail.colorGradient = grad;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            trail.material = new Material(shader);
            trail.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trail.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        trail.widthCurve = curve;

        trail.enabled = false;
    }

    void SetupArrowVisual()
    {
        Sprite sprite = WhiteSquareSpriteCache.Get();

        SpriteRenderer rootSprite = GetComponent<SpriteRenderer>();
        if (rootSprite != null)
            rootSprite.enabled = false;

        ConfigurePart("Shaft", sprite,
            new Vector3(0f, 0f, 0f),
            Quaternion.identity,
            new Vector3(0.72f, 0.035f, 1f),
            new Color(0.18f, 0.10f, 0.03f, 1f),
            12);

        ConfigurePart("Tip", sprite,
            new Vector3(0.38f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 45f),
            new Vector3(0.11f, 0.11f, 1f),
            new Color(0.10f, 0.07f, 0.03f, 1f),
            13);

        ConfigurePart("FletchTop", sprite,
            new Vector3(-0.40f, 0.045f, 0f),
            Quaternion.Euler(0f, 0f, -35f),
            new Vector3(0.18f, 0.018f, 1f),
            new Color(0.93f, 0.97f, 1f, 1f),
            13);

        ConfigurePart("FletchBottom", sprite,
            new Vector3(-0.40f, -0.045f, 0f),
            Quaternion.Euler(0f, 0f, 35f),
            new Vector3(0.18f, 0.018f, 1f),
            new Color(0.93f, 0.97f, 1f, 1f),
            13);

        Transform oldFletch = transform.Find("Fletch");
        if (oldFletch != null)
        {
            SpriteRenderer oldRenderer = oldFletch.GetComponent<SpriteRenderer>();
            if (oldRenderer != null)
                oldRenderer.enabled = false;
        }
    }

    void ConfigurePart(string partName, Sprite sprite, Vector3 localPosition,
        Quaternion localRotation, Vector3 localScale, Color color, int sortingOrder)
    {
        Transform part = transform.Find(partName);
        if (part == null)
        {
            GameObject go = new GameObject(partName);
            go.transform.SetParent(transform, false);
            part = go.transform;
        }

        part.localPosition = localPosition;
        part.localRotation = localRotation;
        part.localScale = localScale;

        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = part.gameObject.AddComponent<SpriteRenderer>();

        renderer.enabled = true;
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = 0;
        renderer.sortingOrder = sortingOrder;
    }
    
    public void StartTrail()
    {
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = showMotionShade;
            isEmitting = showMotionShade;
        }
    }
    
    public void StopTrail()
    {
        if (trail != null)
        {
            isEmitting = false;
            // Trail will fade naturally based on trailTime
            Invoke(nameof(DisableTrail), trailTime);
        }
    }
    
    void DisableTrail()
    {
        if (!isEmitting && trail != null)
            trail.enabled = false;
    }
    
    void OnDestroy()
    {
        CancelInvoke();
    }
}
