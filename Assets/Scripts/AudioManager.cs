using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("SFX (auto-generated if null)")]
    public AudioClip bowDraw;
    public AudioClip arrowFire;
    public AudioClip arrowHit;
    public AudioClip pointScored;
    public AudioClip matchWin;
    public AudioClip matchLose;

    [Header("Music (optional)")]
    public AudioClip backgroundMusic;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    // Persistent settings
    public float SFXVolume   { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 0.4f;
    public bool  Muted       { get; private set; } = false;

    const string KEY_SFX   = "vol_sfx";
    const string KEY_MUSIC = "vol_music";
    const string KEY_MUTED = "muted";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource   = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        // Load saved settings
        SFXVolume   = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 0.4f);
        Muted       = PlayerPrefs.GetInt(KEY_MUTED, 0) == 1;
        ApplyVolumes();

        // Auto-generate SFX if not assigned
        if (bowDraw     == null) bowDraw     = ProceduralAudio.Tone(180f, 380f, 0.30f, waveform: WaveForm.Sine);
        if (arrowFire   == null) arrowFire   = ProceduralAudio.Tone(900f, 220f, 0.18f, waveform: WaveForm.Triangle);
        if (arrowHit    == null) arrowHit    = ProceduralAudio.Thud(0.20f);
        if (pointScored == null) pointScored = ProceduralAudio.Chord(new[] {523f, 659f, 784f}, 0.45f); // C5-E5-G5
        if (matchWin    == null) matchWin    = ProceduralAudio.Arpeggio(new[] {523f, 659f, 784f, 1046f}, 0.18f);
        if (matchLose   == null) matchLose   = ProceduralAudio.Arpeggio(new[] {523f, 466f, 392f, 311f}, 0.20f);
    }

    void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    void ApplyVolumes()
    {
        if (sfxSource   != null) sfxSource.volume   = Muted ? 0f : SFXVolume;
        if (musicSource != null) musicSource.volume = Muted ? 0f : MusicVolume;
    }

    // ── Public API for settings UI ───────────────────────────────
    public void SetSFXVolume(float v)
    {
        SFXVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_SFX, SFXVolume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float v)
    {
        MusicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, MusicVolume);
        ApplyVolumes();
    }

    public void SetMuted(bool m)
    {
        Muted = m;
        PlayerPrefs.SetInt(KEY_MUTED, m ? 1 : 0);
        ApplyVolumes();
    }

    public void ToggleMute() => SetMuted(!Muted);

    // ── SFX trigger methods ──────────────────────────────────────
    public void PlayBowDraw()     { if (bowDraw     != null) sfxSource.PlayOneShot(bowDraw); }
    public void PlayArrowFire()   { if (arrowFire   != null) sfxSource.PlayOneShot(arrowFire); }
    public void PlayArrowHit()    { if (arrowHit    != null) sfxSource.PlayOneShot(arrowHit); }
    public void PlayPointScored() { if (pointScored != null) sfxSource.PlayOneShot(pointScored); }
    public void PlayWin()         { if (matchWin    != null) sfxSource.PlayOneShot(matchWin); }
    public void PlayLose()        { if (matchLose   != null) sfxSource.PlayOneShot(matchLose); }
}

public enum WaveForm { Sine, Square, Triangle, Saw }

/// <summary>
/// Generates AudioClips procedurally so we don't need any audio asset files.
/// </summary>
public static class ProceduralAudio
{
    const int SAMPLE_RATE = 44100;

    public static AudioClip Tone(float freqStart, float freqEnd, float duration,
        float volume = 0.45f, WaveForm waveform = WaveForm.Sine)
    {
        int samples = (int)(duration * SAMPLE_RATE);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(freqStart, freqEnd, t);
            float phase = 2f * Mathf.PI * freq * i / SAMPLE_RATE;
            float wave = Sample(waveform, phase);
            float env  = Envelope(t);     // attack + decay shape
            data[i]    = wave * env * volume;
        }
        var clip = AudioClip.Create("Tone_" + freqStart + "_" + freqEnd, samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Thud(float duration, float volume = 0.55f)
    {
        int samples = (int)(duration * SAMPLE_RATE);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = Mathf.Exp(-t * 10f);   // sharp decay
            float sine = Mathf.Sin(2f * Mathf.PI * 90f * i / SAMPLE_RATE);
            float noise = Random.value * 2f - 1f;
            data[i] = (sine * 0.65f + noise * 0.35f) * env * volume;
        }
        var clip = AudioClip.Create("Thud", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Chord(float[] freqs, float duration, float volume = 0.35f)
    {
        int samples = (int)(duration * SAMPLE_RATE);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = Mathf.Sin(t * Mathf.PI); // bell envelope
            float sum = 0;
            foreach (var f in freqs)
                sum += Mathf.Sin(2f * Mathf.PI * f * i / SAMPLE_RATE);
            data[i] = (sum / freqs.Length) * env * volume;
        }
        var clip = AudioClip.Create("Chord", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Arpeggio(float[] freqs, float noteDuration, float volume = 0.40f)
    {
        int notesSamples = (int)(noteDuration * SAMPLE_RATE);
        int totalSamples = notesSamples * freqs.Length;
        float[] data = new float[totalSamples];
        for (int n = 0; n < freqs.Length; n++)
        {
            float f = freqs[n];
            for (int i = 0; i < notesSamples; i++)
            {
                float t = (float)i / notesSamples;
                float env = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin(2f * Mathf.PI * f * i / SAMPLE_RATE);
                data[n * notesSamples + i] = wave * env * volume;
            }
        }
        var clip = AudioClip.Create("Arpeggio", totalSamples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Sample(WaveForm w, float phase)
    {
        switch (w)
        {
            case WaveForm.Square:   return Mathf.Sin(phase) >= 0 ? 1f : -1f;
            case WaveForm.Triangle: return 2f * Mathf.Abs(2f * (phase / (2f * Mathf.PI) % 1f) - 1f) - 1f;
            case WaveForm.Saw:      return 2f * (phase / (2f * Mathf.PI) % 1f) - 1f;
            default:                return Mathf.Sin(phase);
        }
    }

    static float Envelope(float t)
    {
        const float attack  = 0.05f;
        const float release = 0.20f;
        if (t < attack)        return t / attack;
        if (t > 1f - release)  return (1f - t) / release;
        return 1f;
    }
}
