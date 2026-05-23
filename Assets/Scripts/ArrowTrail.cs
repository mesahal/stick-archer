using UnityEngine;

/// <summary>
/// Lightweight trail effect for arrows. Mobile-optimized with minimal vertices.
/// </summary>
public class ArrowTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public float trailTime = 0.3f;
    public float startWidth = 0.15f;
    public float endWidth = 0.02f;
    public Color startColor = new Color(1f, 0.9f, 0.3f, 0.8f);
    public Color endColor = new Color(1f, 0.6f, 0.1f, 0f);
    
    [Header("Mobile Optimization")]
    [Tooltip("Max trail points - keep low for mobile (15-25)")]
    public int maxPoints = 20;
    
    private TrailRenderer trail;
    private bool isEmitting = false;
    
    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            SetupTrail();
        }
    }
    
    void SetupTrail()
    {
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.startColor = startColor;
        trail.endColor = endColor;
        trail.numCornerVertices = 0;
        trail.numCapVertices = 0;
        
        // Mobile-optimized material
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        trail.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        
        // Limit points for performance
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        trail.widthCurve = curve;
        
        trail.enabled = false;
    }
    
    public void StartTrail()
    {
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = true;
            isEmitting = true;
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
