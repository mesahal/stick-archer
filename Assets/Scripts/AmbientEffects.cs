using UnityEngine;

/// <summary>
/// Light ambient effects: dust particles, subtle wind.
/// Mobile-optimized: very low particle counts, long lifetimes.
/// </summary>
public class AmbientEffects : MonoBehaviour
{
    [Header("Dust Particles")]
    public bool enableDust = true;
    public int maxDustParticles = 15;
    public float dustSpawnRate = 0.5f;
    public float dustLifetime = 4f;
    
    [Header("Wind Strips")]
    public bool enableWind = true;
    public int maxWindParticles = 5;
    public float windSpawnRate = 2f;
    public float windSpeed = 3f;
    
    [Header("Area")]
    public Vector2 spawnArea = new Vector2(20f, 10f);
    public Vector2 cameraOffset = new Vector2(0f, 2f);
    
    private ParticleSystem dustSystem;
    private ParticleSystem windSystem;
    private float dustTimer;
    private float windTimer;
    private Transform cameraTransform;
    
    void Start()
    {
        cameraTransform = Camera.main?.transform;
        
        if (enableDust) SetupDustSystem();
        if (enableWind) SetupWindSystem();
    }
    
    void SetupDustSystem()
    {
        GameObject dustObj = new GameObject("AmbientDust");
        dustObj.transform.SetParent(transform);
        
        dustSystem = dustObj.AddComponent<ParticleSystem>();
        
        var main = dustSystem.main;
        main.startLifetime = dustLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(1f, 1f, 1f, 0.2f);
        main.maxParticles = maxDustParticles;
        main.gravityModifier = 0.1f;
        
        var emission = dustSystem.emission;
        emission.rateOverTime = 0; // We control manually
        
        var shape = dustSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnArea.x, spawnArea.y, 1f);
        
        var renderer = dustSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
    }
    
    void SetupWindSystem()
    {
        GameObject windObj = new GameObject("WindStrips");
        windObj.transform.SetParent(transform);
        
        windSystem = windObj.AddComponent<ParticleSystem>();
        
        var main = windSystem.main;
        main.startLifetime = 2f;
        main.startSpeed = windSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(1f, 1f, 1f, 0.15f);
        main.maxParticles = maxWindParticles;
        
        var emission = windSystem.emission;
        emission.rateOverTime = 0;
        
        var shape = windSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.5f, spawnArea.y, 1f);
        
        var velocityOverLifetime = windSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = windSpeed;
        
        var renderer = windSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.lengthScale = 10f; // Stretched particles for wind streak look
    }
    
    void Update()
    {
        // Follow camera
        if (cameraTransform != null)
        {
            Vector3 followPos = cameraTransform.position + (Vector3)cameraOffset;
            followPos.z = 10f; // Behind gameplay
            
            if (dustSystem != null)
                dustSystem.transform.position = followPos;
            if (windSystem != null)
                windSystem.transform.position = followPos + Vector3.left * spawnArea.x * 0.4f;
        }
        
        // Manual spawn for controlled rates
        if (enableDust && dustSystem != null)
        {
            dustTimer += Time.deltaTime;
            if (dustTimer >= 1f / dustSpawnRate)
            {
                dustTimer = 0;
                if (dustSystem.particleCount < maxDustParticles)
                {
                    dustSystem.Emit(1);
                }
            }
        }
        
        if (enableWind && windSystem != null)
        {
            windTimer += Time.deltaTime;
            if (windTimer >= 1f / windSpawnRate)
            {
                windTimer = 0;
                if (windSystem.particleCount < maxWindParticles)
                {
                    windSystem.Emit(1);
                }
            }
        }
    }
}
