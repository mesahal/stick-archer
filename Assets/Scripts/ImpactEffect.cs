using UnityEngine;

/// <summary>
/// Lightweight impact particle effect for arrow hits. Mobile-optimized, no textures needed.
/// </summary>
public class ImpactEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    public int particleCount = 16;
    public float particleLife = 0.45f;
    public float minSpeed = 2.5f;
    public float maxSpeed = 6.5f;
    public float minSize = 0.06f;
    public float maxSize = 0.16f;
    public float gravity = 9.81f;

    [Header("Colors")]
    public Color startColor = new Color(1.0f, 0.85f, 0.30f, 1f); // bright yellow-orange spark
    public Color endColor   = new Color(0.8f, 0.20f, 0.05f, 0f); // dark ember fade

    [Header("Secondary Spark Burst")]
    public int sparkCount   = 6;
    public Color sparkColor = new Color(1.0f, 1.0f, 0.65f, 1f);  // bright white-yellow sparks
    
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
        
        // Use real particle texture if available, otherwise default
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        var particleTex = Resources.Load<Texture2D>("Particles/particle_spark");
        if (particleTex != null) mat.mainTexture = particleTex;
        renderer.material = mat;
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
        SpawnSparkBurst(position);
        Destroy(gameObject, particleLife + 0.1f);
    }

    /// <summary>
    /// Quick burst of bright sparks layered on top of the main impact for extra punch.
    /// Each spark is a tiny additive-blended quad that flies outward and fades fast.
    /// </summary>
    void SpawnSparkBurst(Vector3 position)
    {
        if (sparkCount <= 0) return;

        var holder = new GameObject("SparkBurst");
        holder.transform.position = position;

        var sparkPs = holder.AddComponent<ParticleSystem>();
        var m = sparkPs.main;
        m.startLifetime  = 0.25f;
        m.startSpeed     = new ParticleSystem.MinMaxCurve(4f, 8f);
        m.startSize      = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        m.startColor     = new ParticleSystem.MinMaxGradient(sparkColor);
        m.gravityModifier = 0.4f;
        m.maxParticles   = sparkCount;
        m.playOnAwake    = false;

        var e = sparkPs.emission;
        e.enabled = false;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)sparkCount) });

        var s = sparkPs.shape;
        s.shapeType = ParticleSystemShapeType.Sphere;
        s.radius    = 0.05f;

        var col = sparkPs.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        Color faded = sparkColor; faded.a = 0f;
        g.SetKeys(
            new[] { new GradientColorKey(sparkColor, 0f), new GradientColorKey(faded, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var r = sparkPs.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            r.material = new Material(shader);
            r.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            r.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // additive
        }
        r.renderMode = ParticleSystemRenderMode.Billboard;

        sparkPs.Play();
        Destroy(holder, 0.4f);
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
