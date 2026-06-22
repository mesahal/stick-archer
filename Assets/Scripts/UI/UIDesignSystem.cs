using UnityEngine;

/// <summary>
/// Centralized design tokens extracted from 00_design_system.svg.
/// All UI scripts should reference these instead of hardcoding colors.
/// Reference resolution: 1920×1080, CanvasScaler match = 0.5
/// </summary>
public static class UIDesignSystem
{
    // ── PALETTE: BACKGROUNDS ───────────────────────────
    public static readonly Color BgDark      = HexColor("#141A29");
    public static readonly Color BgPanel     = HexColor("#1F2438", 0.96f);
    public static readonly Color BgPanelDeep = HexColor("#0F1421", 0.98f);
    public static readonly Color BgSkyTop    = HexColor("#1A2552");
    public static readonly Color BgSkyMid    = HexColor("#0F1A38");
    public static readonly Color BgSkyBot    = HexColor("#0A0E1C");

    // ── PALETTE: ACTIONS ───────────────────────────────
    // Primary (blue)
    public static readonly Color Primary    = HexColor("#268CF2");
    public static readonly Color PrimaryTop = HexColor("#4DA3FF");
    public static readonly Color PrimaryBot = HexColor("#1F73D9");

    // Success (green)
    public static readonly Color Success    = HexColor("#33B859");
    public static readonly Color SuccessTop = HexColor("#5BD980");
    public static readonly Color SuccessBot = HexColor("#258F44");

    // Warning (orange)
    public static readonly Color Warning    = HexColor("#F28C1A");
    public static readonly Color WarningTop = HexColor("#FFB04D");
    public static readonly Color WarningBot = HexColor("#D67410");

    // Danger (red)
    public static readonly Color Danger     = HexColor("#F23F3F");
    public static readonly Color DangerTop  = HexColor("#FF7070");
    public static readonly Color DangerBot  = HexColor("#D63232");

    // Neutral
    public static readonly Color Neutral    = HexColor("#737380");

    // Gold
    public static readonly Color Gold       = HexColor("#FFD933");
    public static readonly Color GoldLight  = HexColor("#FFE066");
    public static readonly Color GoldDark   = HexColor("#E6B800");
    public static readonly Color GoldTitle  = HexColor("#FFF3A0");
    public static readonly Color GoldStroke = HexColor("#C9990A");

    // Panel gradients (top → bottom)
    public static readonly Color PanelTop   = HexColor("#252B45");
    public static readonly Color PanelBot   = HexColor("#181D30");

    // ── PALETTE: TEXT ──────────────────────────────────
    public static readonly Color TextPrimary   = Color.white;
    public static readonly Color TextSecondary  = new Color(1f, 1f, 1f, 0.85f);
    public static readonly Color TextTertiary   = new Color(1f, 1f, 1f, 0.6f);
    public static readonly Color TextHint       = new Color(1f, 1f, 1f, 0.55f);
    public static readonly Color TextDisabled   = new Color(1f, 1f, 1f, 0.35f);
    public static readonly Color TextVersion    = new Color(1f, 1f, 1f, 0.25f);

    // ── PALETTE: HEALTH BAR ────────────────────────────
    public static readonly Color HpFull      = HexColor("#33B859");
    public static readonly Color HpMedium    = HexColor("#F28C1A");
    public static readonly Color HpLow       = HexColor("#F23F3F");
    public static readonly Color HpBarBg     = HexColor("#0F1421");

    // ── TYPOGRAPHY (reference-resolution pixel sizes) ──
    public const float FontDisplay  = 120f; // game title, VICTORY/DEFEAT
    public const float FontH1       = 72f;  // screen titles
    public const float FontH2       = 52f;  // section headings
    public const float FontBody     = 36f;  // button labels, primary content
    public const float FontSmall    = 28f;  // sublabels, secondary text
    public const float FontCaption  = 22f;  // hints, status, version
    public const float FontHudLabel = 14f;  // HUD micro-labels

    // ── SPACING & LAYOUT (4pt grid) ────────────────────
    public const float Space4   = 4f;
    public const float Space8   = 8f;
    public const float Space16  = 16f;
    public const float Space24  = 24f;
    public const float Space32  = 32f;
    public const float Space48  = 48f;
    public const float Space64  = 64f;

    // ── CORNER RADII ───────────────────────────────────
    public const float CornerSmall  = 16f;
    public const float CornerMed    = 24f;
    public const float CornerLarge  = 32f;
    public const float CornerPill   = 60f;

    // ── TOUCH TARGETS ──────────────────────────────────
    public const float MinTouch = 96f; // minimum touch target (≥44dp)

    // ── BUTTON SIZES (reference-resolution) ────────────
    public static readonly Vector2 BtnPrimarySize   = new Vector2(640, 140);
    public static readonly Vector2 BtnSecondarySize = new Vector2(560, 90);
    public static readonly Vector2 BtnIconSize      = new Vector2(96, 96);

    // ── HUD PANEL SIZES ────────────────────────────────
    public static readonly Vector2 PlayerHudSize    = new Vector2(540, 120);
    public static readonly Vector2 RoundBadgeSize   = new Vector2(200, 64);
    public static readonly Vector2 WindBadgeSize    = new Vector2(180, 44);
    public static readonly Vector2 ChargeMeterSize  = new Vector2(800, 80);
    public static readonly Vector2 PauseBtnSize     = new Vector2(96, 96);

    // ── ANIMATION DURATIONS ────────────────────────────
    public const float FadeInDuration   = 0.3f;
    public const float FadeOutDuration  = 0.3f;
    public const float PanelSlideDuration = 0.35f;
    public const float ButtonPressScale = 0.92f;

    // ── DISABLED STATE ─────────────────────────────────
    public const float DisabledAlpha = 0.4f;

    // ── HELPERS ────────────────────────────────────────

    /// <summary>Parse a hex string to a Unity Color.</summary>
    public static Color HexColor(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alpha;
        return c;
    }

    /// <summary>
    /// Returns the appropriate health bar color for a given percentage (0-1).
    /// > 0.6 → green, > 0.3 → orange, otherwise → red
    /// </summary>
    public static Color GetHealthColor(float percentage)
    {
        if (percentage > 0.6f) return HpFull;
        if (percentage > 0.3f) return Color.Lerp(HpMedium, HpFull, (percentage - 0.3f) / 0.3f);
        return Color.Lerp(HpLow, HpMedium, percentage / 0.3f);
    }

    /// <summary>
    /// Returns charge meter color (green → yellow → red) for a given value (0-1).
    /// Matches the chargeGrad from the HUD design.
    /// </summary>
    public static Color GetChargeColor(float value)
    {
        if (value < 0.5f)
            return Color.Lerp(Success, Gold, value * 2f);
        else
            return Color.Lerp(Gold, Danger, (value - 0.5f) * 2f);
    }
}
