using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Self-contained, fully functional settings overlay (own canvas). Built at runtime so
/// it works regardless of scene authoring. Controls: SFX volume, Music volume, Mute,
/// Aim Assist, and Reset Progress — all wired to AudioManager / GameSettings /
/// ProfileManager and persisted there.
/// </summary>
public class RuntimeSettings : MonoBehaviour
{
    GameObject _root;
    TextMeshProUGUI _sfxVal, _musicVal;

    public static void Show()
    {
        var go = new GameObject("RuntimeSettingsCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 420;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f; // match height so the panel always fits vertically

        var ctrl = go.AddComponent<RuntimeSettings>();
        ctrl._root = go;
        ctrl.Build(go.transform);
    }

    void Build(Transform canvas)
    {
        // Dim background — tap outside the card to close.
        var dim = NewImage(canvas, "Dim", new Color(0, 0, 0, 0.92f), true);
        Stretch(dim.rectTransform);
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(Close);

        // Card
        var card = NewImage(canvas, "Card", UIDesignSystem.HexColor("#161B30"), true);
        var crt = card.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(760, 720);
        crt.anchoredPosition = Vector2.zero;
        UIArtProvider.ApplySliced(card, UIArtProvider.Rounded32);

        NewText(card.transform, "Title", new Vector2(0, 300), new Vector2(680, 70), "SETTINGS",
            48f, UIFontProvider.ExtraBold, Color.white, TextAlignmentOptions.Center).characterSpacing = 5f;

        // Close (X) — use a plain letter so it always renders (✕ glyph is missing in the font).
        var close = NewButton(card.transform, "CloseBtn", new Vector2(320, 300), new Vector2(64, 64), "X",
            UIDesignSystem.HexColor("#1F2438"), Close);
        UIArtProvider.ApplySliced(close.GetComponent<Image>(), UIArtProvider.Circle128);

        // ── AUDIO ──
        SectionHeader(card.transform, "AUDIO", 230);

        var am = AudioManager.Instance;
        float sfx = am != null ? am.SFXVolume : 1f;
        float music = am != null ? am.MusicVolume : 0.4f;

        NewText(card.transform, "SfxLbl", new Vector2(-300, 165), new Vector2(220, 40), "Sound FX",
            26f, UIFontProvider.Medium, Color.white, TextAlignmentOptions.Left);
        _sfxVal = NewText(card.transform, "SfxVal", new Vector2(300, 165), new Vector2(90, 40),
            Pct(sfx), 24f, UIFontProvider.Bold, UIDesignSystem.Gold, TextAlignmentOptions.Right);
        MakeSlider(card.transform, new Vector2(20, 120), 420f, sfx, v =>
        {
            AudioManager.Instance?.SetSFXVolume(v);
            if (_sfxVal != null) _sfxVal.text = Pct(v);
        });

        NewText(card.transform, "MusicLbl", new Vector2(-300, 60), new Vector2(220, 40), "Music",
            26f, UIFontProvider.Medium, Color.white, TextAlignmentOptions.Left);
        _musicVal = NewText(card.transform, "MusicVal", new Vector2(300, 60), new Vector2(90, 40),
            Pct(music), 24f, UIFontProvider.Bold, UIDesignSystem.Gold, TextAlignmentOptions.Right);
        MakeSlider(card.transform, new Vector2(20, 15), 420f, music, v =>
        {
            AudioManager.Instance?.SetMusicVolume(v);
            if (_musicVal != null) _musicVal.text = Pct(v);
        });

        ToggleRow(card.transform, new Vector2(0, -55), "Mute All",
            am != null && am.Muted, on => AudioManager.Instance?.SetMuted(on));

        // ── CONTROLS ──
        SectionHeader(card.transform, "CONTROLS", -130);
        ToggleRow(card.transform, new Vector2(0, -195), "Aim Assist",
            GameSettings.AimAssist, on => GameSettings.AimAssist = on);

        // Reset progress
        var reset = NewButton(card.transform, "ResetBtn", new Vector2(0, -285), new Vector2(360, 70),
            "RESET PROGRESS", UIDesignSystem.HexColor("#2A1722"), OnResetProgress);
        var resetTxt = reset.GetComponentInChildren<TextMeshProUGUI>();
        if (resetTxt != null) resetTxt.color = UIDesignSystem.Danger;
    }

    void SectionHeader(Transform parent, string text, float y)
    {
        NewText(parent, text + "Hdr", new Vector2(-300, y), new Vector2(400, 34), text,
            20f, UIFontProvider.Bold, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Left).characterSpacing = 4f;
    }

    void OnResetProgress()
    {
        ConfirmDialog.Show(
            "RESET PROGRESS?",
            "This permanently erases your coins, level, and stats. This cannot be undone.",
            "RESET",
            () => StickArcher.Progression.ProfileManager.Instance?.ResetProgress());
    }

    void Close() { if (_root != null) Destroy(_root); }

    static string Pct(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";

    // ── Builders ───────────────────────────────────────────────

    void ToggleRow(Transform parent, Vector2 pos, string label, bool initial, Action<bool> onChanged)
    {
        NewText(parent, label + "Lbl", new Vector2(-300, pos.y), new Vector2(280, 40), label,
            26f, UIFontProvider.Medium, Color.white, TextAlignmentOptions.Left);

        var pill = NewImage(parent, label + "Pill", UIDesignSystem.HexColor("#3A3F52"), true);
        var prt = pill.rectTransform;
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(96, 44);
        prt.anchoredPosition = new Vector2(276, pos.y);
        UIArtProvider.ApplySliced(pill, UIArtProvider.Pill128);

        var knob = NewImage(pill.transform, "Knob", Color.white, false);
        var krt = knob.rectTransform;
        krt.sizeDelta = new Vector2(34, 34);
        krt.anchorMin = krt.anchorMax = new Vector2(0.5f, 0.5f);
        UIArtProvider.ApplySliced(knob, UIArtProvider.Circle128);

        bool state = initial;
        Action apply = () =>
        {
            pill.color = state ? UIDesignSystem.Success : UIDesignSystem.HexColor("#3A3F52");
            krt.anchoredPosition = new Vector2(state ? 26 : -26, 0);
        };
        apply();

        var btn = pill.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() =>
        {
            state = !state;
            apply();
            onChanged?.Invoke(state);
        });
    }

    Slider MakeSlider(Transform parent, Vector2 pos, float width, float val, UnityAction<float> cb)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 30);
        rt.anchoredPosition = pos;
        var slider = go.GetComponent<Slider>();

        var bg = NewImage(go.transform, "Background", UIDesignSystem.HexColor("#0F1421"), true);
        var bgrt = bg.rectTransform;
        bgrt.anchorMin = new Vector2(0, 0.3f); bgrt.anchorMax = new Vector2(1, 0.7f);
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
        UIArtProvider.ApplySliced(bg, UIArtProvider.PillBar);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var fart = (RectTransform)fillArea.transform;
        fart.anchorMin = new Vector2(0, 0.3f); fart.anchorMax = new Vector2(1, 0.7f);
        fart.offsetMin = new Vector2(8, 0); fart.offsetMax = new Vector2(-8, 0);
        var fill = NewImage(fillArea.transform, "Fill", UIDesignSystem.Gold, true);
        var frt = fill.rectTransform;
        frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1);
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        frt.sizeDelta = new Vector2(10, 0);
        UIArtProvider.ApplySliced(fill, UIArtProvider.PillBar);

        var hsa = new GameObject("Handle Slide Area", typeof(RectTransform));
        hsa.transform.SetParent(go.transform, false);
        var hsart = (RectTransform)hsa.transform;
        hsart.anchorMin = new Vector2(0, 0); hsart.anchorMax = new Vector2(1, 1);
        hsart.offsetMin = new Vector2(10, 0); hsart.offsetMax = new Vector2(-10, 0);
        var handle = NewImage(hsa.transform, "Handle", Color.white, true);
        var hrt = handle.rectTransform;
        hrt.sizeDelta = new Vector2(28, 28);
        hrt.anchorMin = new Vector2(0, 0); hrt.anchorMax = new Vector2(0, 1);
        UIArtProvider.ApplySliced(handle, UIArtProvider.Circle128);

        slider.fillRect = frt;
        slider.handleRect = hrt;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.value = val;
        slider.onValueChanged.AddListener(cb);
        return slider;
    }

    Image NewImage(Transform parent, string name, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color; img.raycastTarget = raycast;
        return img;
    }

    TextMeshProUGUI NewText(Transform parent, string name, Vector2 pos, Vector2 size, string text,
        float fontSize, TMP_FontAsset font, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = align; tmp.raycastTarget = false; tmp.enableWordWrapping = false;
        UIFontProvider.Apply(tmp, font);
        return tmp;
    }

    Button NewButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        UIArtProvider.ApplySliced(img, UIArtProvider.Pill128);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());
        NewText(go.transform, "Label", Vector2.zero, size, label, label.Length <= 2 ? 32f : 26f,
            UIFontProvider.Bold, Color.white, TextAlignmentOptions.Center).characterSpacing = 1f;
        return btn;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
