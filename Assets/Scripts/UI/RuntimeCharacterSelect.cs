using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StickArcher.Progression;

/// <summary>
/// Runtime-built character select (design 02). Shown when the scene has no authored
/// CharacterSelectUI panel. Two cards (Adventurer / Soldier) with art, taglines and
/// Speed/Power stat bars, a gold VS divider, and a Confirm button. The Soldier is
/// locked until owned (requires level + coins, per the design's lock overlay).
/// </summary>
public class RuntimeCharacterSelect : MonoBehaviour
{
    public const int SoldierUnlockLevel = 5;
    public const int SoldierUnlockCost = 500;

    static readonly string[] Names    = { "ADVENTURER", "SOLDIER" };
    static readonly string[] Taglines = { "QUICK · NIMBLE · LIGHT", "STURDY · DEFENSIVE · TANKY" };
    static readonly string[] Art      = { "Characters/Player1/archer_idle", "Characters/Player2/archer_idle" };
    static readonly int[] Speed       = { 9, 5 };
    static readonly int[] Power       = { 6, 9 };

    int _selected;
    Action<int> _onConfirm;
    GameObject _root;

    readonly Image[] _border = new Image[2];
    readonly GameObject[] _check = new GameObject[2];
    readonly GameObject[] _lock = new GameObject[2];
    readonly CanvasGroup[] _cardGroup = new CanvasGroup[2];

    public static void Show(Action<int> onConfirm)
    {
        var go = new GameObject("RuntimeCharacterSelectCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f; // match height so the cards always fit vertically

        var ctrl = go.AddComponent<RuntimeCharacterSelect>();
        ctrl._onConfirm = onConfirm;
        ctrl._root = go;
        ctrl.Build(go.transform);
    }

    void Build(Transform canvas)
    {
        _selected = Mathf.Clamp(CharacterSelectUI.SelectedCharacter, 0, 1);
        if (!IsAvailable(_selected)) _selected = 0; // never start on a locked card

        // Background
        var bg = NewImage(canvas, "BG", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white, false, false);
        var brt = bg.rectTransform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        bg.sprite = UIArtProvider.BgSkyMenu;
        if (bg.sprite == null) bg.color = UIDesignSystem.HexColor("#0F1A38");

        // Title + subtitle
        var title = NewText(canvas, "Title", new Vector2(0, 410), new Vector2(1500, 90), "CHOOSE YOUR ARCHER",
            56f, UIFontProvider.ExtraBold, Color.white, TextAlignmentOptions.Center);
        title.characterSpacing = 6f;
        NewText(canvas, "Subtitle", new Vector2(0, 358), new Vector2(1400, 40),
            "Each archer plays differently — pick the one that suits you",
            22f, UIFontProvider.Medium, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Center);

        // Back button (top-left)
        var back = NewButton(canvas, "BackBtn", new Vector2(110, -90), new Vector2(96, 96), "‹",
            UIDesignSystem.HexColor("#1F2438", 0.85f), OnBack);
        back.GetComponent<RectTransform>().anchorMin = back.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
        UIArtProvider.ApplySliced(back.GetComponent<Image>(), UIArtProvider.Circle128);

        // Cards
        BuildCard(canvas, 0, new Vector2(-430, -30));
        BuildCard(canvas, 1, new Vector2(430, -30));

        // VS divider
        var vs = NewImage(canvas, "VsCircle", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(130, 130),
            UIDesignSystem.HexColor("#0F1421"), false, false);
        UIArtProvider.ApplySliced(vs, UIArtProvider.Circle128);
        var vsB = NewImage(vs.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130),
            new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 0.6f), false, false);
        var vsbrt = vsB.rectTransform; vsbrt.anchorMin = Vector2.zero; vsbrt.anchorMax = Vector2.one;
        vsbrt.offsetMin = new Vector2(4, 4); vsbrt.offsetMax = new Vector2(-4, -4);
        UIArtProvider.ApplySliced(vsB, UIArtProvider.Circle128);
        var vsTxt = NewText(vs.transform, "VS", Vector2.zero, new Vector2(130, 80), "VS", 50f,
            UIFontProvider.Black, UIDesignSystem.Gold, TextAlignmentOptions.Center);

        // Confirm
        NewButton(canvas, "ConfirmBtn", new Vector2(0, -440), new Vector2(560, 96), "CONFIRM SELECTION",
            UIDesignSystem.Primary, OnConfirm);

        UpdateVisuals();
    }

    void BuildCard(Transform canvas, int index, Vector2 pos)
    {
        var card = NewImage(canvas, "Card" + index, new Vector2(0.5f, 0.5f), pos, new Vector2(620, 680),
            UIDesignSystem.HexColor("#1B2038"), false, true);
        UIArtProvider.ApplySliced(card, UIArtProvider.Rounded32);

        // Border (recolored on selection)
        _border[index] = NewImage(card.transform, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            new Color(1, 1, 1, 0.08f), false, false);
        var bRt = _border[index].rectTransform; bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
        bRt.offsetMin = bRt.offsetMax = Vector2.zero;
        UIArtProvider.ApplySliced(_border[index], UIArtProvider.Rounded32);

        // Inner fill (leaves a rim for the border)
        var inner = NewImage(card.transform, "Inner", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            UIDesignSystem.HexColor("#161B30"), false, false);
        var iRt = inner.rectTransform; iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one;
        iRt.offsetMin = new Vector2(4, 4); iRt.offsetMax = new Vector2(-4, -4);
        UIArtProvider.ApplySliced(inner, UIArtProvider.Rounded32);

        _cardGroup[index] = card.gameObject.AddComponent<CanvasGroup>();

        // Character art
        var art = NewImage(inner.transform, "Art", new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(300, 300),
            Color.white, false, false);
        var sprite = Resources.Load<Sprite>(Art[index]);
        if (sprite != null) { art.sprite = sprite; art.preserveAspect = true; }
        else art.color = new Color(1, 1, 1, 0.12f);

        // Name + tagline
        var name = NewText(inner.transform, "Name", new Vector2(0, -90), new Vector2(560, 70), Names[index],
            52f, UIFontProvider.ExtraBold, index == _selected ? UIDesignSystem.Gold : Color.white, TextAlignmentOptions.Center);
        name.characterSpacing = 3f;
        NewText(inner.transform, "Tagline", new Vector2(0, -140), new Vector2(560, 36), Taglines[index],
            20f, UIFontProvider.Medium, new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Center);

        // Stat bars
        BuildStat(inner.transform, "SPEED", Speed[index], new Vector2(0, -200), UIDesignSystem.Gold);
        BuildStat(inner.transform, "POWER", Power[index], new Vector2(0, -250), UIDesignSystem.Success);

        // Check badge (selected)
        _check[index] = NewImage(card.transform, "Check", new Vector2(1, 1), new Vector2(-44, -44), new Vector2(64, 64),
            UIDesignSystem.Gold, false, false).gameObject;
        UIArtProvider.ApplySliced(_check[index].GetComponent<Image>(), UIArtProvider.Circle128);
        if (UIArtProvider.IconCheck != null)
        {
            var chk = NewImage(_check[index].transform, "Mark", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38, 38),
                UIDesignSystem.HexColor("#141A29"), false, false);
            chk.sprite = UIArtProvider.IconCheck; chk.type = Image.Type.Simple;
        }

        // Lock overlay (if not available)
        if (!IsAvailable(index))
            _lock[index] = BuildLockOverlay(card.transform, index);

        // Make the whole card a button
        var btn = card.gameObject.AddComponent<Button>();
        btn.targetGraphic = card;
        int idx = index;
        btn.onClick.AddListener(() => OnCardClicked(idx));
    }

    void BuildStat(Transform parent, string label, int value, Vector2 pos, Color fillColor)
    {
        NewText(parent, label + "Lbl", new Vector2(-200, pos.y), new Vector2(120, 30), label, 18f,
            UIFontProvider.Medium, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Left);
        var bg = NewImage(parent, label + "Bg", new Vector2(0.5f, 0.5f), new Vector2(20, pos.y), new Vector2(280, 16),
            UIDesignSystem.HexColor("#0F1421"), false, false);
        UIArtProvider.ApplySliced(bg, UIArtProvider.PillBar);
        var fill = NewImage(bg.transform, "Fill", new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, fillColor, false, false);
        var frt = fill.rectTransform;
        frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(Mathf.Clamp01(value / 10f), 1);
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        UIArtProvider.ApplySliced(fill, UIArtProvider.PillBar);
        NewText(parent, label + "Val", new Vector2(195, pos.y), new Vector2(80, 30), $"{value} / 10", 18f,
            UIFontProvider.Medium, new Color(1, 1, 1, 0.7f), TextAlignmentOptions.Right);
    }

    GameObject BuildLockOverlay(Transform card, int index)
    {
        var overlay = NewImage(card, "Lock", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            UIDesignSystem.HexColor("#0A0E1C", 0.72f), false, false);
        var ort = overlay.rectTransform; ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;
        UIArtProvider.ApplySliced(overlay, UIArtProvider.Rounded32);

        var prof = ProfileManager.Instance?.Profile;
        int level = prof != null ? prof.level : 1;
        bool levelMet = level >= SoldierUnlockLevel;

        NewText(overlay.transform, "Locked", new Vector2(0, 40), new Vector2(400, 50), "LOCKED", 28f,
            UIFontProvider.ExtraBold, Color.white, TextAlignmentOptions.Center).characterSpacing = 3f;
        NewText(overlay.transform, "Req", new Vector2(0, -10), new Vector2(460, 40),
            levelMet ? $"TAP TO UNLOCK · {SoldierUnlockCost} COINS" : $"REQUIRES LV {SoldierUnlockLevel}",
            22f, UIFontProvider.Bold, levelMet ? UIDesignSystem.Gold : new Color(1, 1, 1, 0.6f), TextAlignmentOptions.Center);
        return overlay.gameObject;
    }

    bool IsAvailable(int index)
    {
        if (index == 0) return true;
        var pm = ProfileManager.Instance;
        return pm != null && pm.Profile != null && pm.Profile.OwnsCharacter(index);
    }

    void OnCardClicked(int index)
    {
        if (IsAvailable(index))
        {
            _selected = index;
            UpdateVisuals();
            return;
        }

        // Locked Soldier: try to unlock if requirements met.
        var pm = ProfileManager.Instance;
        if (pm == null || pm.Profile == null) return;
        if (pm.Profile.level < SoldierUnlockLevel) return; // level gate not met
        if (pm.TrySpendCoins(SoldierUnlockCost, "unlock_character"))
        {
            pm.UnlockCharacter(index);
            if (_lock[index] != null) Destroy(_lock[index]);
            _selected = index;
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < 2; i++)
        {
            bool sel = i == _selected;
            if (_border[i] != null)
                _border[i].color = sel
                    ? new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 1f)
                    : new Color(1, 1, 1, 0.08f);
            if (_check[i] != null) _check[i].SetActive(sel);
            if (_cardGroup[i] != null) _cardGroup[i].alpha = (sel || !IsAvailable(i)) ? 1f : 0.9f;
        }
    }

    void OnConfirm()
    {
        CharacterSelectUI.SelectedCharacter = _selected;
        var cb = _onConfirm;
        Destroy(_root);
        cb?.Invoke(_selected);
    }

    void OnBack() => Destroy(_root);

    // ── Tiny UI builders ───────────────────────────────────────
    Image NewImage(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, bool simple, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color; img.raycastTarget = raycast;
        if (simple) img.type = Image.Type.Simple;
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
        var t = NewText(go.transform, "Label", Vector2.zero, size, label, label.Length <= 2 ? 44f : 30f,
            UIFontProvider.Bold, Color.white, TextAlignmentOptions.Center);
        t.characterSpacing = 2f;
        return btn;
    }
}
