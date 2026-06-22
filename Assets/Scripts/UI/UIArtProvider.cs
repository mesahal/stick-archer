using UnityEngine;

/// <summary>
/// Lazy-loads design-system sprites from Resources/UI/ at runtime.
/// Assets are in Assets/Resources/UI/{Shapes,Gradients,Icons}/ so they're
/// available in Android builds without Addressables.
/// </summary>
public static class UIArtProvider
{
    // ── Shapes ──────────────────────────────────────────────
    public static Sprite Rounded16 => Get(ref _rounded16, "UI/Shapes/rounded_16");
    public static Sprite Rounded24 => Get(ref _rounded24, "UI/Shapes/rounded_24");
    public static Sprite Rounded32 => Get(ref _rounded32, "UI/Shapes/rounded_32");
    public static Sprite Pill128   => Get(ref _pill128,   "UI/Shapes/pill_128");
    public static Sprite PillBar   => Get(ref _pillBar,   "UI/Shapes/pill_bar");
    public static Sprite Circle128 => Get(ref _circle128, "UI/Shapes/circle_128");

    // ── Gradients ────────────────────────────────────────────
    public static Sprite BgSkyMenu  => Get(ref _bgSkyMenu,  "UI/Gradients/bg_sky_menu");
    public static Sprite MenuBg     => Get(ref _menuBg,     "UI/Backgrounds/menu_bg");
    public static Sprite PanelBg    => Get(ref _panelBg,    "UI/Gradients/panel_bg");
    public static Sprite PanelDark  => Get(ref _panelDark,  "UI/Gradients/panel_dark");
    public static Sprite BtnPrimary => Get(ref _btnPrimary, "UI/Gradients/btn_primary");
    public static Sprite BtnSuccess => Get(ref _btnSuccess, "UI/Gradients/btn_success");
    public static Sprite BtnGold    => Get(ref _btnGold,    "UI/Gradients/btn_gold");
    public static Sprite BtnDanger  => Get(ref _btnDanger,  "UI/Gradients/btn_danger");
    public static Sprite BtnWarning => Get(ref _btnWarning, "UI/Gradients/btn_warning");
    public static Sprite HpFull     => Get(ref _hpFull,     "UI/Gradients/hp_full");
    public static Sprite HpLow      => Get(ref _hpLow,      "UI/Gradients/hp_low");
    public static Sprite ChargeMeter=> Get(ref _chargeMeter,"UI/Gradients/charge_meter");
    public static Sprite TitleGold  => Get(ref _titleGold,  "UI/Gradients/title_gold");

    // ── Icons ────────────────────────────────────────────────
    public static Sprite IconCoin   => Get(ref _iconCoin,   "UI/Icons/coin");
    public static Sprite IconXp     => Get(ref _iconXp,     "UI/Icons/xp");
    public static Sprite IconTrophy => Get(ref _iconTrophy, "UI/Icons/trophy");
    public static Sprite IconStar   => Get(ref _iconStar,   "UI/Icons/star");
    public static Sprite IconHeart  => Get(ref _iconHeart,  "UI/Icons/heart");
    public static Sprite IconTarget => Get(ref _iconTarget, "UI/Icons/target");
    public static Sprite IconBow    => Get(ref _iconBow,    "UI/Icons/bow");
    public static Sprite IconGear   => Get(ref _iconGear,   "UI/Icons/gear");
    public static Sprite IconGlobe  => Get(ref _iconGlobe,  "UI/Icons/globe");
    public static Sprite IconRobot  => Get(ref _iconRobot,  "UI/Icons/robot");
    public static Sprite IconPause  => Get(ref _iconPause,  "UI/Icons/pause");
    public static Sprite IconPlay   => Get(ref _iconPlay,   "UI/Icons/play");
    public static Sprite IconBack   => Get(ref _iconBack,   "UI/Icons/back");
    public static Sprite IconCheck  => Get(ref _iconCheck,  "UI/Icons/check");
    public static Sprite IconHome   => Get(ref _iconHome,   "UI/Icons/home");
    public static Sprite IconRetry  => Get(ref _iconRetry,  "UI/Icons/retry");
    public static Sprite IconSound  => Get(ref _iconSound,  "UI/Icons/sound");
    public static Sprite IconClose  => Get(ref _iconClose,  "UI/Icons/close");

    // ── Backing fields ───────────────────────────────────────
    static Sprite _rounded16, _rounded24, _rounded32, _pill128, _pillBar, _circle128;
    static Sprite _bgSkyMenu, _menuBg, _panelBg, _panelDark, _btnPrimary, _btnSuccess, _btnGold;
    static Sprite _btnDanger, _btnWarning, _hpFull, _hpLow, _chargeMeter, _titleGold;
    static Sprite _iconCoin, _iconXp, _iconTrophy, _iconStar, _iconHeart, _iconTarget, _iconBow;
    static Sprite _iconGear, _iconGlobe, _iconRobot, _iconPause, _iconPlay, _iconBack;
    static Sprite _iconCheck, _iconHome, _iconRetry, _iconSound, _iconClose;

    static Sprite Get(ref Sprite field, string path)
    {
        if (field == null)
            field = Resources.Load<Sprite>(path);
        return field;
    }

    /// <summary>
    /// Applies a sliced 9-patch shape sprite to an existing Image, preserving its color.
    /// No-ops gracefully if the sprite hasn't loaded.
    /// </summary>
    public static void ApplySliced(UnityEngine.UI.Image img, Sprite shape)
    {
        if (img == null || shape == null) return;
        img.sprite = shape;
        img.type = UnityEngine.UI.Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
    }

    /// <summary>
    /// Adds a Simple gradient Image as a child of <paramref name="parent"/>, stretched to fill.
    /// Returns the created Image, or null if the sprite hasn't loaded.
    /// </summary>
    public static UnityEngine.UI.Image AddGradientOverlay(Transform parent, Sprite gradient, string name = "Gradient")
    {
        if (parent == null || gradient == null) return null;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.sprite = gradient;
        img.type = UnityEngine.UI.Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }
}
