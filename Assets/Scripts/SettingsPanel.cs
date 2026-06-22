using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Lightweight settings panel: SFX + Music sliders + Mute toggle.
/// Drives AudioManager directly. Persists via PlayerPrefs (handled in AudioManager).
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;            // root panel (toggled on/off)
    public Button     openButton;       // gear icon
    public Button     closeButton;
    public Slider     sfxSlider;
    public Slider     musicSlider;
    public Toggle     muteToggle;
    public TextMeshProUGUI sfxValueText;
    public TextMeshProUGUI musicValueText;

    Image muteToggleBackground;
    RectTransform muteToggleKnob;
    bool controlsWired;

    // Procedurally-built extras (design 03): CONTROLS section + Aim Assist + Reset link.
    bool extrasBuilt;
    Toggle aimAssistToggle;
    Image aimToggleBackground;
    RectTransform aimToggleKnob;

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        ResolveReferences();
        WireControls();

        if (panel != null)
            panel.SetActive(false);
    }

    void OnDestroy()
    {
        UnwireControls();
    }

    void WireControls()
    {
        UnwireControls();
        var am = AudioManager.Instance;

        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (sfxSlider != null)
        {
            if (am != null)
                sfxSlider.value = am.SFXVolume;
            sfxSlider.onValueChanged.AddListener(OnSfx);
        }

        if (musicSlider != null)
        {
            if (am != null)
                musicSlider.value = am.MusicVolume;
            musicSlider.onValueChanged.AddListener(OnMusic);
        }

        CacheMuteToggleVisuals();

        if (muteToggle != null)
        {
            if (am != null)
                muteToggle.isOn = am.Muted;
            muteToggle.onValueChanged.AddListener(OnMute);
        }

        controlsWired = true;
        BuildExtras();
        UpdateLabels();
        UpdateMuteToggleVisual();
        UpdateAimToggleVisual();
    }

    /// <summary>Adds the CONTROLS section (Aim Assist toggle) and a Reset Progress link
    /// below the audio rows. Cloned from the mute row so styling matches the scene exactly.</summary>
    void BuildExtras()
    {
        if (extrasBuilt) return;
        if (muteToggle == null) return;

        RectTransform muteRow = muteToggle.transform.parent as RectTransform;
        if (muteRow == null) return;
        Transform rowsParent = muteRow.parent;
        if (rowsParent == null) return;
        extrasBuilt = true;

        // Row spacing: derive from music→mute gap if available, else a sensible default.
        float spacing = 150f;
        if (musicSlider != null)
        {
            RectTransform musicRow = musicSlider.transform.parent as RectTransform;
            if (musicRow != null)
                spacing = Mathf.Abs(muteRow.anchoredPosition.y - musicRow.anchoredPosition.y);
            if (spacing < 40f) spacing = 150f;
        }

        // ── CONTROLS section header (clone the AUDIO label for identical style) ──
        TextMeshProUGUI audioHeader = FindHeader(rowsParent, "AUDIO");
        float headerY = muteRow.anchoredPosition.y - spacing * 0.75f;
        if (audioHeader != null)
        {
            var hdr = Instantiate(audioHeader.gameObject, rowsParent);
            hdr.name = "ControlsHeader";
            var hrt = hdr.GetComponent<RectTransform>();
            hrt.anchoredPosition = new Vector2(audioHeader.rectTransform.anchoredPosition.x, headerY);
            var htmp = hdr.GetComponent<TextMeshProUGUI>();
            if (htmp != null) htmp.text = "CONTROLS";
        }

        // ── Aim Assist row (clone the mute row) ──
        var aimRowGO = Instantiate(muteRow.gameObject, rowsParent);
        aimRowGO.name = "AimAssistRow";
        var aimRow = aimRowGO.GetComponent<RectTransform>();
        aimRow.anchoredPosition = new Vector2(muteRow.anchoredPosition.x, muteRow.anchoredPosition.y - spacing);

        // Relabel: find the first text in the cloned row that isn't inside the toggle.
        foreach (var t in aimRowGO.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.GetComponentInParent<Toggle>() == null) { t.text = "Aim Assist"; break; }
        }

        aimAssistToggle = aimRowGO.GetComponentInChildren<Toggle>(true);
        if (aimAssistToggle != null)
        {
            aimAssistToggle.onValueChanged.RemoveAllListeners();
            aimToggleBackground = aimAssistToggle.GetComponent<Image>();
            aimAssistToggle.graphic = null;
            var knob = aimAssistToggle.transform.Find("Checkmark");
            if (knob != null)
            {
                aimToggleKnob = knob as RectTransform;
                var ki = knob.GetComponent<Image>();
                if (ki != null) { ki.color = Color.white; ki.CrossFadeAlpha(1f, 0f, true); }
            }
            aimAssistToggle.isOn = GameSettings.AimAssist;
            aimAssistToggle.onValueChanged.AddListener(OnAimAssist);
        }

        // ── Reset Progress link (red, centered, below the controls section) ──
        var linkGO = new GameObject("ResetProgressLink", typeof(RectTransform));
        linkGO.transform.SetParent(rowsParent, false);
        var lrt = linkGO.GetComponent<RectTransform>();
        lrt.anchorMin = muteRow.anchorMin;
        lrt.anchorMax = muteRow.anchorMax;
        lrt.pivot = muteRow.pivot;
        lrt.sizeDelta = new Vector2(muteRow.sizeDelta.x, 40f);
        lrt.anchoredPosition = new Vector2(muteRow.anchoredPosition.x, muteRow.anchoredPosition.y - spacing * 2f);
        var linkTMP = linkGO.AddComponent<TextMeshProUGUI>();
        linkTMP.text = "<u>Reset Progress</u>";
        linkTMP.fontSize = 26f;
        linkTMP.alignment = TextAlignmentOptions.Center;
        linkTMP.color = UIDesignSystem.Danger;
        UIFontProvider.Apply(linkTMP, UIFontProvider.Medium);
        var linkBtn = linkGO.AddComponent<Button>();
        linkBtn.transition = Selectable.Transition.None;
        linkBtn.onClick.AddListener(OnResetProgressClicked);
    }

    TextMeshProUGUI FindHeader(Transform root, string text)
    {
        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.text != null && t.text.Trim().ToUpper() == text) return t;
        return null;
    }

    void OnAimAssist(bool on)
    {
        GameSettings.AimAssist = on;
        UpdateAimToggleVisual();
    }

    void UpdateAimToggleVisual()
    {
        if (aimAssistToggle == null) return;
        bool isOn = aimAssistToggle.isOn;
        if (aimToggleBackground != null)
            aimToggleBackground.color = isOn ? UIDesignSystem.Success : new Color(0.227f, 0.247f, 0.333f, 1f);
        if (aimToggleKnob != null)
        {
            float width = ((RectTransform)aimAssistToggle.transform).rect.width;
            float knobWidth = aimToggleKnob.rect.width;
            float travel = Mathf.Max(0f, (width - knobWidth) * 0.5f - 4f);
            aimToggleKnob.anchoredPosition = new Vector2(isOn ? travel : -travel, 0f);
        }
    }

    void OnResetProgressClicked()
    {
        ConfirmDialog.Show(
            "RESET PROGRESS?",
            "This permanently erases your coins, level, and stats. This cannot be undone.",
            "RESET",
            () =>
            {
                StickArcher.Progression.ProfileManager.Instance?.ResetProgress();
            });
    }

    void UnwireControls()
    {
        if (!controlsWired) return;

        if (openButton != null)
            openButton.onClick.RemoveListener(Open);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfx);
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusic);
        if (muteToggle != null)
            muteToggle.onValueChanged.RemoveListener(OnMute);

        controlsWired = false;
    }

    public void Toggle(bool show)
    {
        ResolveReferences();
        WireControls();

        if (panel == null)
        {
            Debug.LogWarning("[SettingsPanel] Toggle requested, but no panel GameObject is assigned or found.");
            return;
        }

        if (show)
        {
            PlacePanelOnTop();
            UpdateLabels();
            UpdateMuteToggleVisual();
        }

        panel.SetActive(show);
    }

    void Open() => Toggle(true);

    void Close() => Toggle(false);

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
        UpdateMuteToggleVisual();
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (sfxValueText   != null && AudioManager.Instance != null)
            sfxValueText.text   = Mathf.RoundToInt(AudioManager.Instance.SFXVolume   * 100) + "%";
        if (musicValueText != null && AudioManager.Instance != null)
            musicValueText.text = Mathf.RoundToInt(AudioManager.Instance.MusicVolume * 100) + "%";
    }

    void CacheMuteToggleVisuals()
    {
        if (muteToggle == null) return;

        muteToggleBackground = muteToggle.GetComponent<Image>();
        muteToggle.graphic = null;

        Transform knob = muteToggle.transform.Find("Checkmark");
        if (knob != null)
        {
            muteToggleKnob = knob.GetComponent<RectTransform>();
            Image knobImage = knob.GetComponent<Image>();
            if (knobImage != null)
            {
                knobImage.color = Color.white;
                knobImage.CrossFadeAlpha(1f, 0f, true);
            }
        }
    }

    void UpdateMuteToggleVisual()
    {
        if (muteToggle == null) return;

        bool isOn = muteToggle.isOn;
        if (muteToggleBackground != null)
            muteToggleBackground.color = isOn ? UIDesignSystem.Success : new Color(0.227f, 0.247f, 0.333f, 1f);

        if (muteToggleKnob != null)
        {
            float width = ((RectTransform)muteToggle.transform).rect.width;
            float knobWidth = muteToggleKnob.rect.width;
            float travel = Mathf.Max(0f, (width - knobWidth) * 0.5f - 4f);
            muteToggleKnob.anchoredPosition = new Vector2(isOn ? travel : -travel, 0f);
        }
    }

    void ResolveReferences()
    {
        if (panel == null)
        {
            Transform found = FindDeep(transform, "SettingsPanel");
            if (found == null && transform.name == "SettingsPanel")
                found = transform;
            if (found != null)
                panel = found.gameObject;
        }

        Transform searchRoot = panel != null ? panel.transform : transform;

        if (closeButton == null)
            closeButton = FindDeep(searchRoot, "CloseBtn")?.GetComponent<Button>()
                ?? FindDeep(searchRoot, "CloseButton")?.GetComponent<Button>();

        if (sfxSlider == null)
            sfxSlider = FindDeep(searchRoot, "SfxRow")?.GetComponentInChildren<Slider>(true)
                ?? FindDeep(searchRoot, "SFXRow")?.GetComponentInChildren<Slider>(true);

        if (musicSlider == null)
            musicSlider = FindDeep(searchRoot, "MusicRow")?.GetComponentInChildren<Slider>(true);

        if (muteToggle == null)
            muteToggle = FindDeep(searchRoot, "MuteRow")?.GetComponentInChildren<Toggle>(true);

        if (sfxValueText == null)
            sfxValueText = FindDeep(searchRoot, "SfxRow")?.Find("Value")?.GetComponent<TextMeshProUGUI>()
                ?? FindDeep(searchRoot, "SFXRow")?.Find("Value")?.GetComponent<TextMeshProUGUI>();

        if (musicValueText == null)
            musicValueText = FindDeep(searchRoot, "MusicRow")?.Find("Value")?.GetComponent<TextMeshProUGUI>();
    }

    void PlacePanelOnTop()
    {
        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas != null && panel.transform.parent != canvas.transform)
        {
            panel.transform.SetParent(canvas.transform, false);
            StretchToParent(panel.GetComponent<RectTransform>());
        }

        panel.transform.SetAsLastSibling();
    }

    void StretchToParent(RectTransform rectTransform)
    {
        if (rectTransform == null) return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    Transform FindDeep(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
