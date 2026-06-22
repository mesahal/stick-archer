using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StickArcher.Progression;

/// <summary>
/// Runtime daily-rewards screen (design 12): an HOURLY card (coins) and a 12-HOUR card
/// (coins + gems), each with a CLAIM button that becomes a live countdown while on
/// cooldown. Shown from the main menu on launch when a reward is claimable.
/// </summary>
public class DailyRewardsScreen : MonoBehaviour
{
    static bool _shownThisSession;

    GameObject _root;
    Button _hourlyBtn, _twelveBtn;
    TextMeshProUGUI _hourlyBtnLbl, _twelveBtnLbl;

    /// <summary>Show once per app session if any timed reward is currently claimable.</summary>
    public static void ShowOnLaunchIfAvailable()
    {
        if (_shownThisSession) return;
        var pm = ProfileManager.Instance;
        if (pm == null || !pm.HasClaimableReward()) return;
        _shownThisSession = true;
        Show();
    }

    public static void Show()
    {
        var go = new GameObject("DailyRewardsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var ctrl = go.AddComponent<DailyRewardsScreen>();
        ctrl._root = go;
        ctrl.Build(go.transform);
    }

    void Build(Transform canvas)
    {
        // Dim
        var dim = Img(canvas, "Dim", Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.65f), true);
        var drt = dim.rectTransform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;

        // Panel
        var panel = Img(canvas, "Panel", Vector2.zero, new Vector2(800, 600), UIDesignSystem.HexColor("#202540", 0.99f), true);
        UIArtProvider.ApplySliced(panel, UIArtProvider.Rounded32);
        var accent = Img(panel.transform, "Accent", Vector2.zero, Vector2.zero, UIDesignSystem.Gold, false);
        var art = accent.rectTransform; art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1); art.pivot = new Vector2(0.5f, 1);
        art.sizeDelta = new Vector2(-12, 8); art.anchoredPosition = new Vector2(0, -4);

        Txt(panel.transform, "Title", new Vector2(0, 250), new Vector2(720, 70), "DAILY REWARDS", 48f,
            UIFontProvider.Black, Color.white, TextAlignmentOptions.Center).characterSpacing = 4f;
        Txt(panel.transform, "Sub", new Vector2(0, 200), new Vector2(720, 36), "Claim free coins and gems", 22f,
            UIFontProvider.Medium, new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Center);

        // Close X
        var close = Img(panel.transform, "Close", new Vector2(350, 250), new Vector2(56, 56), UIDesignSystem.HexColor("#2A3050"), true);
        UIArtProvider.ApplySliced(close, UIArtProvider.Circle128);
        var cb = close.gameObject.AddComponent<Button>(); cb.targetGraphic = close;
        cb.onClick.AddListener(() => Destroy(_root));
        if (UIArtProvider.IconClose != null)
        {
            var x = Img(close.transform, "X", Vector2.zero, new Vector2(26, 26), Color.white, false);
            x.sprite = UIArtProvider.IconClose; x.type = Image.Type.Simple;
        }
        else Txt(close.transform, "X", Vector2.zero, new Vector2(56, 56), "X", 28f, UIFontProvider.Bold, Color.white, TextAlignmentOptions.Center);

        BuildCard(panel.transform, true, new Vector2(-170, -30));
        BuildCard(panel.transform, false, new Vector2(170, -30));

        Refresh();
    }

    void BuildCard(Transform parent, bool hourly, Vector2 pos)
    {
        Color accent = hourly ? UIDesignSystem.Gold : UIDesignSystem.HexColor("#6B8CFF");

        var card = Img(parent, hourly ? "HourlyCard" : "TwelveCard", pos, new Vector2(300, 340),
            UIDesignSystem.HexColor("#171C30"), true);
        UIArtProvider.ApplySliced(card, UIArtProvider.Rounded24);

        Txt(card.transform, "Hdr", new Vector2(0, 130), new Vector2(280, 36), hourly ? "HOURLY" : "12 HOUR", 22f,
            UIFontProvider.ExtraBold, accent, TextAlignmentOptions.Center).characterSpacing = 3f;

        // Chest (stylized rounded box)
        var chest = Img(card.transform, "Chest", new Vector2(0, 40), new Vector2(150, 96), accent, false);
        UIArtProvider.ApplySliced(chest, UIArtProvider.Rounded16);
        var lid = Img(chest.transform, "Lid", new Vector2(0, 36), new Vector2(150, 34),
            hourly ? UIDesignSystem.GoldLight : UIDesignSystem.HexColor("#8BA8FF"), false);
        UIArtProvider.ApplySliced(lid, UIArtProvider.Rounded16);
        var latch = Img(chest.transform, "Latch", new Vector2(0, 6), new Vector2(26, 26),
            hourly ? UIDesignSystem.GoldStroke : UIDesignSystem.HexColor("#4A6BE6"), false);

        // Reward line(s)
        string coinTxt = hourly ? $"+{ProfileManager.HourlyRewardCoins}" : $"+{ProfileManager.TwelveHourRewardCoins}";
        var coinIcon = Img(card.transform, "CoinIcon", new Vector2(hourly ? -50 : -78, -55), new Vector2(28, 28), UIDesignSystem.Gold, false);
        if (UIArtProvider.IconCoin != null) coinIcon.sprite = UIArtProvider.IconCoin;
        Txt(card.transform, "CoinTxt", new Vector2(hourly ? 10 : -18, -55), new Vector2(120, 36), coinTxt, 24f,
            UIFontProvider.ExtraBold, UIDesignSystem.Gold, TextAlignmentOptions.Left);
        if (!hourly)
        {
            var gemIcon = Img(card.transform, "GemIcon", new Vector2(58, -55), new Vector2(26, 26), UIDesignSystem.HexColor("#6B8CFF"), false);
            UIArtProvider.ApplySliced(gemIcon, UIArtProvider.Rounded16);
            Txt(card.transform, "GemTxt", new Vector2(98, -55), new Vector2(80, 36), $"+{ProfileManager.TwelveHourRewardGems}", 24f,
                UIFontProvider.ExtraBold, UIDesignSystem.HexColor("#8BA8FF"), TextAlignmentOptions.Left);
        }

        // Claim button
        var btnImg = Img(card.transform, "ClaimBtn", new Vector2(0, -120), new Vector2(220, 56), UIDesignSystem.Success, true);
        UIArtProvider.ApplySliced(btnImg, UIArtProvider.Pill128);
        var btn = btnImg.gameObject.AddComponent<Button>(); btn.targetGraphic = btnImg;
        var lbl = Txt(btnImg.transform, "Lbl", Vector2.zero, new Vector2(220, 56), "CLAIM", 22f,
            UIFontProvider.Bold, Color.white, TextAlignmentOptions.Center);

        if (hourly) { _hourlyBtn = btn; _hourlyBtnLbl = lbl; btn.onClick.AddListener(OnClaimHourly); }
        else        { _twelveBtn = btn; _twelveBtnLbl = lbl; btn.onClick.AddListener(OnClaim12h); }
    }

    void OnClaimHourly() { if (ProfileManager.Instance != null && ProfileManager.Instance.ClaimHourly()) Refresh(); }
    void OnClaim12h()    { if (ProfileManager.Instance != null && ProfileManager.Instance.Claim12h()) Refresh(); }

    void Update() => Refresh();

    void Refresh()
    {
        var pm = ProfileManager.Instance;
        if (pm == null) return;
        SetClaimState(_hourlyBtn, _hourlyBtnLbl, pm.CanClaimHourly(), pm.HourlyRemaining());
        SetClaimState(_twelveBtn, _twelveBtnLbl, pm.CanClaim12h(), pm.TwelveHourRemaining());
    }

    void SetClaimState(Button btn, TextMeshProUGUI lbl, bool canClaim, TimeSpan remaining)
    {
        if (btn == null || lbl == null) return;
        btn.interactable = canClaim;
        var img = btn.targetGraphic as Image;
        if (canClaim)
        {
            if (img != null) img.color = UIDesignSystem.Success;
            lbl.text = "CLAIM";
        }
        else
        {
            if (img != null) img.color = new Color(1, 1, 1, 0.12f);
            lbl.text = remaining.Hours > 0
                ? $"{remaining.Hours}h {remaining.Minutes}m"
                : $"{remaining.Minutes}m {remaining.Seconds}s";
        }
    }

    // ── builders ──
    Image Img(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool raycast)
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

    TextMeshProUGUI Txt(Transform parent, string name, Vector2 pos, Vector2 size, string text,
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
