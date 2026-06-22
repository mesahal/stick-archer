using UnityEngine;
using System.Collections;
#if URP_INSTALLED
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Drives short, code-triggered post-process effects:
///   • OnHit       — brief chromatic-aberration pulse
///   • OnHeadshot  — chromatic-aberration spike + lens distortion punch
///   • OnRoundEnd  — vignette fades in to focus on the winner
///
/// Attach to any GameObject in the scene that ALSO has a Volume component pointing
/// at the GlobalVolumeProfile (created by SetupURP). The script grabs the profile
/// from the same GameObject's Volume on Awake.
///
/// Falls back to a no-op silently if URP isn't installed (compile guard).
/// </summary>
public class PostFXTriggers : MonoBehaviour
{
    public static PostFXTriggers Instance { get; private set; }

    [Header("Chromatic Aberration on Hit")]
    public float hitCAPeak     = 0.40f;
    public float hitCABase     = 0.05f;
    public float hitCADuration = 0.15f;

    [Header("Headshot Lens Distortion")]
    public float headshotLensPunch    = -0.20f;
    public float headshotLensDuration = 0.30f;

    [Header("Round-End Vignette")]
    public float roundEndVignette = 0.50f;
    public float roundEndFadeIn   = 0.40f;

#if URP_INSTALLED
    Volume              _volume;
    ChromaticAberration _ca;
    LensDistortion      _lens;
    Vignette            _vignette;
    float               _baseVignetteIntensity;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

#if URP_INSTALLED
        _volume = GetComponent<Volume>();
        if (_volume == null) _volume = FindObjectOfType<Volume>();
        if (_volume == null || _volume.profile == null)
        {
            Debug.LogWarning("[PostFXTriggers] No Volume with profile found. Post-FX triggers will be inactive.");
            return;
        }
        _volume.profile.TryGet(out _ca);
        _volume.profile.TryGet(out _lens);
        _volume.profile.TryGet(out _vignette);
        if (_vignette != null) _baseVignetteIntensity = _vignette.intensity.value;
#endif
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void OnHit()
    {
#if URP_INSTALLED
        if (_ca == null) return;
        StopCoroutine(nameof(CAPulse));
        StartCoroutine(nameof(CAPulse));
#endif
    }

    public void OnHeadshot()
    {
#if URP_INSTALLED
        OnHit();
        if (_lens == null) return;
        StopCoroutine(nameof(LensPunch));
        StartCoroutine(nameof(LensPunch));
#endif
    }

    public void OnRoundEnd()
    {
#if URP_INSTALLED
        if (_vignette == null) return;
        StopCoroutine(nameof(VignetteFadeIn));
        StartCoroutine(nameof(VignetteFadeIn));
#endif
    }

    public void OnRoundStart()
    {
#if URP_INSTALLED
        if (_vignette == null) return;
        _vignette.intensity.value = _baseVignetteIntensity;
#endif
    }

#if URP_INSTALLED
    IEnumerator CAPulse()
    {
        _ca.intensity.value = hitCAPeak;
        float t = 0f;
        while (t < hitCADuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / hitCADuration;
            _ca.intensity.value = Mathf.Lerp(hitCAPeak, hitCABase, k);
            yield return null;
        }
        _ca.intensity.value = hitCABase;
    }

    IEnumerator LensPunch()
    {
        _lens.intensity.value = headshotLensPunch;
        float t = 0f;
        while (t < headshotLensDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / headshotLensDuration;
            _lens.intensity.value = Mathf.Lerp(headshotLensPunch, 0f, k);
            yield return null;
        }
        _lens.intensity.value = 0f;
    }

    IEnumerator VignetteFadeIn()
    {
        float start = _vignette.intensity.value;
        float t = 0f;
        while (t < roundEndFadeIn)
        {
            t += Time.unscaledDeltaTime;
            float k = t / roundEndFadeIn;
            _vignette.intensity.value = Mathf.Lerp(start, roundEndVignette, k);
            yield return null;
        }
    }
#endif
}
