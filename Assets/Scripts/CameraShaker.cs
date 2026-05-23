using UnityEngine;
using System.Collections;

/// <summary>
/// Lightweight camera shake for impacts and kills.
/// Mobile-friendly: uses perlin noise, no heavy physics.
/// </summary>
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;
    
    [Header("Shake Profiles")]
    public ShakeProfile hitShake = new ShakeProfile { duration = 0.15f, magnitude = 0.08f, frequency = 15f };
    public ShakeProfile killShake = new ShakeProfile { duration = 0.3f, magnitude = 0.15f, frequency = 12f };
    public ShakeProfile gameOverShake = new ShakeProfile { duration = 0.5f, magnitude = 0.25f, frequency = 8f };
    
    [Header("References")]
    public Transform cameraTransform;
    
    private Vector3 originalPos;
    private bool isShaking = false;
    private float shakeTimeRemaining = 0f;
    private float currentMagnitude = 0f;
    private float currentFrequency = 0f;
    private float seed;
    
    [System.Serializable]
    public struct ShakeProfile
    {
        public float duration;
        public float magnitude;
        public float frequency;
    }
    
    void Awake()
    {
        Instance = this;
        seed = Random.Range(0f, 100f);
    }
    
    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
        
        if (cameraTransform != null)
            originalPos = cameraTransform.localPosition;
    }
    
    void Update()
    {
        if (!isShaking || cameraTransform == null) return;
        
        shakeTimeRemaining -= Time.deltaTime;
        
        if (shakeTimeRemaining <= 0f)
        {
            isShaking = false;
            cameraTransform.localPosition = originalPos;
            return;
        }
        
        // Decay magnitude over time
        float progress = 1f - (shakeTimeRemaining / currentDuration);
        float decayedMag = currentMagnitude * (1f - progress * progress);
        
        // Perlin noise for smooth random shake
        float x = (Mathf.PerlinNoise(seed, Time.time * currentFrequency) - 0.5f) * 2f * decayedMag;
        float y = (Mathf.PerlinNoise(seed + 1f, Time.time * currentFrequency) - 0.5f) * 2f * decayedMag;
        
        cameraTransform.localPosition = originalPos + new Vector3(x, y, 0f);
    }
    
    private float currentDuration;
    
    /// <summary>
    /// Trigger a shake with the hit profile (light impact).
    /// </summary>
    public void ShakeHit()
    {
        TriggerShake(hitShake);
    }
    
    /// <summary>
    /// Trigger a shake with the kill profile (strong impact).
    /// </summary>
    public void ShakeKill()
    {
        TriggerShake(killShake);
    }
    
    /// <summary>
    /// Trigger a shake with the game over profile (victory/defeat).
    /// </summary>
    public void ShakeGameOver()
    {
        TriggerShake(gameOverShake);
    }
    
    /// <summary>
    /// Trigger a custom shake.
    /// </summary>
    public void Shake(float duration, float magnitude, float frequency)
    {
        TriggerShake(new ShakeProfile { duration = duration, magnitude = magnitude, frequency = frequency });
    }
    
    void TriggerShake(ShakeProfile profile)
    {
        isShaking = true;
        shakeTimeRemaining = profile.duration;
        currentMagnitude = profile.magnitude;
        currentFrequency = profile.frequency;
        currentDuration = profile.duration;
        seed = Random.Range(0f, 100f);
        
        if (cameraTransform != null && !isShaking)
            originalPos = cameraTransform.localPosition;
    }
    
    /// <summary>
    /// Stop any active shake and reset camera.
    /// </summary>
    public void StopShake()
    {
        isShaking = false;
        if (cameraTransform != null)
            cameraTransform.localPosition = originalPos;
    }
}
