using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lightweight settings panel: SFX + Music sliders + Mute toggle.
/// Drives AudioManager directly. Persists via PlayerPrefs (handled in AudioManager).
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Wired automatically by VisualOverhaul_v8")]
    public GameObject panel;            // root panel (toggled on/off)
    public Button     openButton;       // gear icon
    public Button     closeButton;
    public Slider     sfxSlider;
    public Slider     musicSlider;
    public Toggle     muteToggle;
    public TextMeshProUGUI sfxValueText;
    public TextMeshProUGUI musicValueText;

    void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (openButton  != null) openButton.onClick.AddListener(() => Toggle(true));
        if (closeButton != null) closeButton.onClick.AddListener(() => Toggle(false));

        var am = AudioManager.Instance;
        if (am == null) return;

        if (sfxSlider   != null) { sfxSlider.value   = am.SFXVolume;   sfxSlider.onValueChanged.AddListener(OnSfx); }
        if (musicSlider != null) { musicSlider.value = am.MusicVolume; musicSlider.onValueChanged.AddListener(OnMusic); }
        if (muteToggle  != null) { muteToggle.isOn   = am.Muted;       muteToggle.onValueChanged.AddListener(OnMute); }

        UpdateLabels();
    }

    public void Toggle(bool show)
    {
        if (panel != null) panel.SetActive(show);
    }

    void OnSfx(float v)
    {
        AudioManager.Instance?.SetSFXVolume(v);
        UpdateLabels();
    }

    void OnMusic(float v)
    {
        AudioManager.Instance?.SetMusicVolume(v);
        UpdateLabels();
    }

    void OnMute(bool m)
    {
        AudioManager.Instance?.SetMuted(m);
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (sfxValueText   != null && AudioManager.Instance != null)
            sfxValueText.text   = Mathf.RoundToInt(AudioManager.Instance.SFXVolume   * 100) + "%";
        if (musicValueText != null && AudioManager.Instance != null)
            musicValueText.text = Mathf.RoundToInt(AudioManager.Instance.MusicVolume * 100) + "%";
    }
}
