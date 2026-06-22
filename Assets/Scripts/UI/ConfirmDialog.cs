using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minimal runtime confirmation modal: title, message, a danger "confirm" button and a
/// "cancel" button, over a dim overlay. Self-contained so any screen can request a
/// confirmation without scene wiring. Used by the Settings "Reset Progress" action.
/// </summary>
public static class ConfirmDialog
{
    public static void Show(string title, string message, string confirmLabel, Action onConfirm)
    {
        Canvas canvas = ResolveTopCanvas();
        if (canvas == null) { onConfirm?.Invoke(); return; }

        // Root overlay (dim) — blocks input, closes on background tap (= cancel).
        var root = new GameObject("ConfirmDialog", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rrt = root.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        root.transform.SetAsLastSibling();

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);

        // Modal card
        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(root.transform, false);
        var crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(820, 460);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = UIDesignSystem.HexColor("#1F2438", 0.98f);
        UIArtProvider.ApplySliced(cardImg, UIArtProvider.Rounded32);

        // Red top accent
        var accent = new GameObject("Accent", typeof(RectTransform));
        accent.transform.SetParent(card.transform, false);
        var art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 1f); art.anchorMax = new Vector2(1f, 1f);
        art.pivot = new Vector2(0.5f, 1f);
        art.sizeDelta = new Vector2(0, 8); art.anchoredPosition = Vector2.zero;
        var ai = accent.AddComponent<Image>();
        ai.color = UIDesignSystem.Danger;

        MakeText(card.transform, title, 44f, UIFontProvider.ExtraBold, Color.white,
            new Vector2(0, 150), new Vector2(740, 70), TextAlignmentOptions.Center);
        MakeText(card.transform, message, 26f, UIFontProvider.Medium, new Color(1, 1, 1, 0.75f),
            new Vector2(0, 30), new Vector2(720, 160), TextAlignmentOptions.Top);

        // Buttons
        MakeButton(card.transform, confirmLabel, new Vector2(-190, -150), UIDesignSystem.Danger, () =>
        {
            onConfirm?.Invoke();
            UnityEngine.Object.Destroy(root);
        });
        MakeButton(card.transform, "CANCEL", new Vector2(190, -150), new Color(0.30f, 0.32f, 0.40f, 1f),
            () => UnityEngine.Object.Destroy(root));

        // Tap dim background to cancel.
        var bgBtn = root.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(root));
    }

    static Canvas ResolveTopCanvas()
    {
        Canvas best = null;
        foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
        {
            if (!c.isActiveAndEnabled) continue;
            if (best == null || c.sortingOrder >= best.sortingOrder) best = c;
        }
        return best;
    }

    static void MakeText(Transform parent, string text, float size, TMP_FontAsset font,
        Color color, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta; rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
        UIFontProvider.Apply(tmp, font);
    }

    static void MakeButton(Transform parent, string label, Vector2 pos, Color color, Action onClick)
    {
        var go = new GameObject(label + "Btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320, 96); rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        UIArtProvider.ApplySliced(img, UIArtProvider.Pill128);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 30f; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        UIFontProvider.Apply(tmp, UIFontProvider.Bold);
    }
}
