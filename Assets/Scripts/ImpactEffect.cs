using UnityEngine;

/// <summary>
/// Lightweight impact particle effect for arrow hits. Mobile-optimized, no textures needed.
/// </summary>
public class ImpactEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    public int particleCount = 8;
    public float particleLife = 0.4f;
    public float minSpeed = 2f;
    public float maxSpeed = 5f;
    public float minSize = 0.08f;
    public float maxSize = 0.18f;
    public float gravity = 9.81f;
    
    [Header("Colors")]
    public Color startColor = new Color(1f, 0.3f, 0.2f, 1f);
    public Color endColor = new Color(0.6f, 0.1f, 0.05f, 0f);
    
    private ParticleSystem ps;
    
    void Awake()
    {
        CreateParticleSystem();
    }
    
    void CreateParticleSystem()
    {
        ps = gameObject.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startLifetime = particleLife;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(startColor);
        main.gravityModifier = gravity / 9.81f;
        main.maxParticles = particleCount;
        main.playOnAwake = false;
        
        var emission = ps.emission;
        emission.enabled = false;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)particleCount) });
        
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Mobile optimization: simple billboard, no texture
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
    
    public void Play(Vector3 position, Vector2 hitNormal, Color? customColor = null)
    {
        transform.position = position;
        
        // Rotate to face hit direction
        if (hitNormal != Vector2.zero)
        {
            float angle = Mathf.Atan2(hitNormal.y, hitNormal.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
        }
        
        // Apply custom color if provided
        if (customColor.HasValue)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(customColor.Value);
            
            var colorOverLifetime = ps.colorOverLifetime;
            Gradient gradient = new Gradient();
            Color faded = customColor.Value;
            faded.a = 0f;
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(customColor.Value, 0f), new GradientColorKey(faded, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }
        
        ps.Play();
        Destroy(gameObject, particleLife + 0.1f);
    }
    
    /// <summary>
    /// Static helper to spawn an impact effect at a position.
    /// </summary>
    public static void Spawn(Vector3 position, Vector2 hitNormal, Color? color = null, Transform parent = null)
    {
        GameObject go = new GameObject("ImpactEffect");
        if (parent != null) go.transform.SetParent(parent, true);
        var effect = go.AddComponent<ImpactEffect>();
        effect.Play(position, hitNormal, color);
    }
}
