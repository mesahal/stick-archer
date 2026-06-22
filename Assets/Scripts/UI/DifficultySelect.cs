using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime difficulty picker shown after the player chooses VS Computer. Three options
/// — Easy / Medium / Hard — set GameMode.Difficulty, then invoke a callback to continue
/// into character select / the match. Self-contained overlay (own canvas), matching the
/// other runtime menus' style.
/// </summary>
public class DifficultySelect : MonoBehaviour
{
    Action<GameMode.AIDifficulty> _onPick;
    GameObject _root;

    public static void Show(Action<GameMode.AIDifficulty> onPick)
    {
        var go = new GameObject("DifficultySelectCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 410;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f; // match height: keeps the landscape layout fitting vertically on any device

        var ctrl = go.AddComponent<DifficultySelect>();
        ctrl._onPick = onPick;
        ctrl._root = go;
        ctrl.Build(go.transform);
    }

    void Build(Transform canvas)
    {
        // Dim background that also blocks clicks behind the modal.
        NewImage(canvas, "Dim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.92f), true);

        // Solid card behind the content so the menu never bleeds through.
        var card = NewImage(canvas, "Card", Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.zero, new Vector2(820, 760), UIDesignSystem.HexColor("#161B30"), true);
        UIArtProvider.ApplySliced(card, UIArtProvider.Rounded32);

        // Title + subtitle
        var title = NewText(canvas, "Title", new Vector2(0, 300), new Vector2(1400, 90), "SELECT DIFFICULTY",
            56f, UIFontProvider.ExtraBold, Color.white, TextAlignmentOptions.Center);
        title.characterSpacing = 6f;
        NewText(canvas, "Subtitle", new Vector2(0, 246), new Vector2(1200, 40),
            "How tough should the computer be?",
            24f, UIFontProvider.Medium, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Center);

        // Option buttons
        BuildOption(canvas, new Vector2(0, 110), "EASY", "Relaxed · the computer misses a lot",
            UIDesignSystem.Success, GameMode.AIDifficulty.Easy);
        BuildOption(canvas, new Vector2(0, -20), "MEDIUM", "Balanced · a fair fight",
            UIDesignSystem.Gold, GameMode.AIDifficulty.Normal);
        BuildOption(canvas, new Vector2(0, -150), "HARD", "Ruthless · sharp aim, fast shots",
            UIDesignSystem.Danger, GameMode.AIDifficulty.Hard);

        // Back
        var back = NewButton(canvas, "BackBtn", new Vector2(0, -300), new Vector2(260, 80), "BACK",
            UIDesignSystem.HexColor("#1F2438", 0.95f), OnBack);
    }

    void BuildOption(Transform canvas, Vector2 pos, string label, string desc, Color accent, GameMode.AIDifficulty diff)
    {
        var btnGo = new GameObject(label + "Btn", typeof(RectTransform));
        btnGo.transform.SetParent(canvas, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(620, 110);
        rt.anchoredPosition = pos;

        var img = btnGo.AddComponent<Image>();
        img.color = UIDesignSystem.HexColor("#1B2038");
        UIArtProvider.ApplySliced(img, UIArtProvider.Rounded32);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => OnPick(diff));

        // Accent stripe on the left edge.
        var stripe = NewImage(btnGo.transform, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(26, 0), new Vector2(14, -24), accent, false);
        var srt = stripe.rectTransform;
        srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(0f, 1f);
        srt.offsetMin = new Vector2(14, 12); srt.offsetMax = new Vector2(28, -12);
        UIArtProvider.ApplySliced(stripe, UIArtProvider.Rounded32);

        var name = NewText(btnGo.transform, "Name", new Vector2(36, 22), new Vector2(560, 50), label,
            40f, UIFontProvider.ExtraBold, accent, TextAlignmentOptions.Left);
        name.characterSpacing = 3f;
        NewText(btnGo.transform, "Desc", new Vector2(36, -26), new Vector2(560, 36), desc,
            21f, UIFontProvider.Medium, new Color(1, 1, 1, 0.62f), TextAlignmentOptions.Left);
    }

    void OnPick(GameMode.AIDifficulty diff)
    {
        GameMode.Difficulty = diff;
        var cb = _onPick;
        Destroy(_root);
        cb?.Invoke(diff);
    }

    void OnBack() => Destroy(_root);

    // ── Tiny UI builders ───────────────────────────────────────
    Image NewImage(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (aMin == Vector2.zero && aMax == Vector2.one)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }
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
        var t = NewText(go.transform, "Label", Vector2.zero, size, label, 28f,
            UIFontProvider.Bold, Color.white, TextAlignmentOptions.Center);
        t.characterSpacing = 2f;
        return btn;
    }
}
