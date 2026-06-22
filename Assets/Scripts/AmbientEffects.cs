using UnityEngine;

/// <summary>
/// Light ambient effects: dust particles, subtle wind.
/// Mobile-optimized: very low particle counts, long lifetimes.
/// </summary>
public class AmbientEffects : MonoBehaviour
{
    [Header("Dust Particles")]
    public bool enableDust = true;
    public int maxDustParticles = 25;
    public float dustSpawnRate = 0.8f;
    public float dustLifetime = 5f;

    [Header("Wind Strips")]
    public bool enableWind = true;
    public int maxWindParticles = 5;
    public float windSpawnRate = 2f;
    public float windSpeed = 3f;

    [Header("Floating Leaves")]
    public bool enableLeaves = true;
    public int  maxLeafParticles = 5;
    public float leafSpawnRate  = 0.25f;
    public Color leafColor      = new Color(0.85f, 0.55f, 0.20f, 0.85f);
    
    [Header("Area")]
    public Vector2 spawnArea = new Vector2(20f, 10f);
    public Vector2 cameraOffset = new Vector2(0f, 2f);
    
    private ParticleSystem dustSystem;
    private ParticleSystem windSystem;
    private ParticleSystem leafSystem;
    private float dustTimer;
    private float windTimer;
    private float leafTimer;
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main?.transform;

        if (enableDust)   SetupDustSystem();
        if (enableWind)   SetupWindSystem();
        if (enableLeaves) SetupLeafSystem();
    }

    void SetupLeafSystem()
    {
        var leafObj = new GameObject("FloatingLeaves");
        leafObj.transform.SetParent(transform);

        leafSystem = leafObj.AddComponent<ParticleSystem>();

        var main = leafSystem.main;
        main.startLifetime    = 8f;
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.05f, 0.10f);
        main.startColor       = leafColor;
        main.startRotation    = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles     = maxLeafParticles;
        main.gravityModifier  = 0.06f;

        var emission = leafSystem.emission;
        emission.rateOverTime = 0;

        var shape = leafSystem.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(spawnArea.x, 0.5f, 1f);
        // Spawn at the top of the visible area and let them drift down
        shape.position  = new Vector3(0f, spawnArea.y * 0.5f, 0f);

        // Side-to-side drift via animated velocity
        var vel = leafSystem.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

        // Slow rotation as they fall
        var rot = leafSystem.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

        var r = leafSystem.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) r.material = new Material(shader);
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

        // Follow camera & emit leaves
        if (enableLeaves && leafSystem != null)
        {
            if (cameraTransform != null)
            {
                Vector3 followPos = cameraTransform.position + (Vector3)cameraOffset;
                followPos.z = 10f;
                leafSystem.transform.position = followPos;
            }
            leafTimer += Time.deltaTime;
            if (leafTimer >= 1f / leafSpawnRate)
            {
                leafTimer = 0;
                if (leafSystem.particleCount < maxLeafParticles)
                    leafSystem.Emit(1);
            }
        }
    }
}
