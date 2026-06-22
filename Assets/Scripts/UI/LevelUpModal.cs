using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StickArcher.Progression;

/// <summary>
/// Self-bootstrapping presenter that shows a runtime "LEVEL UP!" modal (design 11)
/// whenever the player advances a level. Subscribes to ProfileManager.OnLevelUp,
/// queues level-ups, and shows them one at a time on its own overlay canvas.
/// </summary>
public class LevelUpModal : MonoBehaviour
{
    static LevelUpModal _instance;

    readonly Queue<int> _pending = new Queue<int>();
    bool _subscribed;
    bool _showing;
    GameObject _current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("LevelUpModal");
        _instance = go.AddComponent<LevelUpModal>();
        DontDestroyOnLoad(go);
    }

    void Update()
    {
        if (!_subscribed && ProfileManager.Instance != null)
        {
            ProfileManager.Instance.OnLevelUp += OnLevelUp;
            _subscribed = true;
        }
        if (!_showing && _pending.Count > 0)
            ShowNext();
    }

    void OnDestroy()
    {
        if (_subscribed && ProfileManager.Instance != null)
            ProfileManager.Instance.OnLevelUp -= OnLevelUp;
    }

    void OnLevelUp(int newLevel) => _pending.Enqueue(newLevel);

    void ShowNext()
    {
        if (_pending.Count == 0) return;
        int level = _pending.Dequeue();
        _showing = true;
        _current = Build(level, ProfileManager.LevelUpCoins(level));
    }

    void OnContinue()
    {
        if (_current != null) Destroy(_current);
        _current = null;
        _showing = false;
    }

    GameObject Build(int level, int coins)
    {
        var go = new GameObject("LevelUpCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600; // above result screen
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Dim overlay
        var dim = NewImage(go.transform, "Dim", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.65f), true);
        var drt = dim.rectTransform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;

        // Panel
        var panel = NewImage(go.transform, "Panel", Vector2.zero, new Vector2(800, 680), UIDesignSystem.HexColor("#202540", 0.99f), false);
        UIArtProvider.ApplySliced(panel, UIArtProvider.Rounded32);
        // gold border
        var border = NewImage(panel.transform, "Border", Vector2.zero, Vector2.zero, new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 0.4f), false);
        var bRt = border.rectTransform; bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one; bRt.offsetMin = bRt.offsetMax = Vector2.zero;
        UIArtProvider.ApplySliced(border, UIArtProvider.Rounded32);
        var inner = NewImage(panel.transform, "Inner", Vector2.zero, Vector2.zero, UIDesignSystem.HexColor("#202540"), false);
        var iRt = inner.rectTransform; iRt.anchorMin = Vector2.zero; iRt.anchorMax = Vector2.one; iRt.offsetMin = new Vector2(3, 3); iRt.offsetMax = new Vector2(-3, -3);
        UIArtProvider.ApplySliced(inner, UIArtProvider.Rounded32);

        // gold top accent
        var accent = NewImage(inner.transform, "Accent", Vector2.zero, Vector2.zero, UIDesignSystem.Gold, false);
        var art = accent.rectTransform; art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1); art.pivot = new Vector2(0.5f, 1);
        art.sizeDelta = new Vector2(-12, 8); art.anchoredPosition = new Vector2(0, -4);

        // Title
        var title = NewText(inner.transform, "Title", new Vector2(0, 250), new Vector2(720, 100), "LEVEL UP!", 72f,
            UIFontProvider.Black, UIDesignSystem.GoldTitle, TextAlignmentOptions.Center);
        title.characterSpacing = 8f;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(UIDesignSystem.GoldTitle, UIDesignSystem.GoldTitle, UIDesignSystem.GoldStroke, UIDesignSystem.GoldStroke);
        UIFontProvider.ApplyTitleDropShadow(title);

        // Big level number
        NewText(inner.transform, "Level", new Vector2(0, 90), new Vector2(400, 200), level.ToString(), 150f,
            UIFontProvider.ExtraBold, Color.white, TextAlignmentOptions.Center);
        NewText(inner.transform, "Sub", new Vector2(0, -10), new Vector2(600, 40), "NEW LEVEL REACHED", 22f,
            UIFontProvider.Medium, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Center).characterSpacing = 6f;

        // Unlock note (only when this level unlocks something)
        string unlock = UnlockFor(level);
        if (!string.IsNullOrEmpty(unlock))
        {
            NewText(inner.transform, "UnlockHdr", new Vector2(0, -70), new Vector2(620, 32), "UNLOCKED", 20f,
                UIFontProvider.Bold, UIDesignSystem.Gold, TextAlignmentOptions.Center).characterSpacing = 4f;
            NewText(inner.transform, "UnlockTxt", new Vector2(0, -108), new Vector2(640, 36), unlock, 24f,
                UIFontProvider.Medium, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Center);
        }

        // Coin reward pill
        var pill = NewImage(inner.transform, "RewardPill", new Vector2(0, -170), new Vector2(360, 60), new Color(UIDesignSystem.Gold.r, UIDesignSystem.Gold.g, UIDesignSystem.Gold.b, 0.14f), false);
        UIArtProvider.ApplySliced(pill, UIArtProvider.Pill128);
        var coinIcon = NewImage(pill.transform, "Coin", new Vector2(-120, 0), new Vector2(34, 34), UIDesignSystem.Gold, false);
        if (UIArtProvider.IconCoin != null) coinIcon.sprite = UIArtProvider.IconCoin;
        NewText(pill.transform, "RewardTxt", new Vector2(20, 0), new Vector2(280, 50), $"+{coins} COINS", 26f,
            UIFontProvider.ExtraBold, UIDesignSystem.Gold, TextAlignmentOptions.Center);

        // Continue button (raycast = true so it's clickable)
        var btn = NewImage(inner.transform, "Continue", new Vector2(0, -250), new Vector2(560, 80), UIDesignSystem.Primary, true);
        UIArtProvider.ApplySliced(btn, UIArtProvider.Pill128);
        var b = btn.gameObject.AddComponent<Button>();
        b.targetGraphic = btn;
        b.onClick.AddListener(OnContinue);
        NewText(btn.transform, "Label", Vector2.zero, new Vector2(560, 80), "CONTINUE", 32f,
            UIFontProvider.Bold, Color.white, TextAlignmentOptions.Center).characterSpacing = 4f;

        return go;
    }

    static string UnlockFor(int level)
    {
        if (level == RuntimeCharacterSelect.SoldierUnlockLevel) return "Soldier archer available";
        return null;
    }

    // ── builders ──
    Image NewImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
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
}
