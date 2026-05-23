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
    public bool spawnDeathParticles = true;
    public int particleCount = 12;
    
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
        if (spawnDeathParticles)
        {
            SpawnDeathParticles();
        }
        
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
        // Simple particle burst at death position
        GameObject psObj = new GameObject("DeathParticles");
        psObj.transform.position = transform.position;
        
        var ps = psObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor = new Color(0.9f, 0.2f, 0.1f, 0.8f);
        main.maxParticles = particleCount;
        main.gravityModifier = 1f;
        
        var emission = ps.emission;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)particleCount));
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        
        ps.Play();
        Destroy(psObj, 1f);
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
