using UnityEngine;
using System.Collections;

/// <summary>
/// Polished death effect: fade out + optional particle burst.
/// Much cleaner than instant ragdoll disappearance.
/// </summary>
public class DeathEffect : MonoBehaviour
{
    [Header("Death Fade")]
    public float fadeDuration = 0.8f;
    public bool  spawnDeathParticles = true;
    public int   particleCount = 12;

    [Header("Feather Burst")]
    public bool  spawnFeathers = true;
    public int   featherCount  = 10;
    public Color featherColor  = new Color(0.95f, 0.92f, 0.85f, 0.95f);

    [Header("Slow-Motion on Death")]
    public bool  slowMoOnDeath     = true;
    public float slowMoTimeScale   = 0.45f;
    public float slowMoDuration    = 0.4f;
    
    private SpriteRenderer[] sprites;
    private bool isDying = false;
    
    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();
    }
    
    /// <summary>
    /// Call this instead of Destroy() for a polished death effect.
    /// </summary>
    public void Die()
    {
        if (isDying) return;
        isDying = true;
        
        StartCoroutine(DoDeathSequence());
    }
    
    IEnumerator DoDeathSequence()
    {
        // Spawn death particles
        if (spawnDeathParticles) SpawnDeathParticles();
        if (spawnFeathers)       SpawnFeatherBurst();
        if (slowMoOnDeath)       StartCoroutine(DoSlowMo());

        // Fade out all sprites
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);
            
            foreach (var sr in sprites)
            {
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }
            
            yield return null;
        }
        
        // Fully invisible, can now destroy or disable
        // Keep GameObject but disable rendering for potential respawn
        foreach (var sr in sprites)
        {
            if (sr != null)
                sr.enabled = false;
        }
    }
    
    void SpawnDeathParticles()
    {
        GameObject psObj = new GameObject("DeathParticles");
        psObj.transform.position = transform.position;

        var ps = psObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime   = 0.6f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor      = new Color(0.9f, 0.2f, 0.1f, 0.8f);
        main.maxParticles    = particleCount;
        main.gravityModifier = 1f;

        var emission = ps.emission;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.2f;

        ps.Play();
        Destroy(psObj, 1f);
    }

    /// <summary>
    /// Light feather-like particles drift up and away. Reads as "spirit leaving the body" —
    /// more cinematic than just red blobs falling.
    /// </summary>
    void SpawnFeatherBurst()
    {
        var go = new GameObject("DeathFeathers");
        go.transform.position = transform.position + Vector3.up * 0.4f;

        var ps = go.AddComponent<ParticleSystem>();
        var m  = ps.main;
        m.startLifetime    = 1.5f;
        m.startSpeed       = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        m.startSize        = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        m.startColor       = featherColor;
        m.gravityModifier  = -0.08f;   // negative so they drift upward
        m.maxParticles     = featherCount;
        m.startRotation    = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var e = ps.emission;
        e.SetBurst(0, new ParticleSystem.Burst(0f, (short)featherCount));

        var s = ps.shape;
        s.shapeType = ParticleSystemShapeType.Cone;
        s.angle     = 35f;
        s.rotation  = new Vector3(-90f, 0f, 0f); // point cone upward

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        Color faded = featherColor; faded.a = 0f;
        g.SetKeys(
            new[] { new GradientColorKey(featherColor, 0f), new GradientColorKey(faded, 1f) },
            new[] { new GradientAlphaKey(featherColor.a, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ps.Play();
        Destroy(go, 2f);
    }

    /// <summary>
    /// Brief slow-motion to emphasize the kill, then restores normal time scale.
    /// Uses unscaled real time for the timer so it works even mid-slowmo.
    /// </summary>
    IEnumerator DoSlowMo()
    {
        float restoreScale = Time.timeScale;
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(slowMoDuration);
        Time.timeScale      = restoreScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
    
    /// <summary>
    /// Reset for respawn - restore visibility.
    /// </summary>
    public void Respawn()
    {
        isDying = false;
        StopAllCoroutines();
        
        foreach (var sr in sprites)
        {
            if (sr != null)
            {
                sr.enabled = true;
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }
    }
    
    /// <summary>
    /// Flash white briefly on respawn.
    /// </summary>
    public void RespawnFlash()
    {
        StartCoroutine(DoRespawnFlash());
    }
    
    IEnumerator DoRespawnFlash()
    {
        // White flash
        foreach (var sr in sprites)
        {
            if (sr != null)
                sr.color = Color.white;
        }
        
        yield return new WaitForSeconds(0.15f);
        
        // Back to normal (colors will be restored by Archer respawn)
    }
}
