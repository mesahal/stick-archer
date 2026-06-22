using UnityEngine;

/// <summary>
/// Lightweight persisted gameplay settings (PlayerPrefs-backed).
/// Audio settings live in AudioManager; this covers control-side options.
/// </summary>
public static class GameSettings
{
    const string AimAssistKey = "aim_assist";

    /// <summary>
    /// When enabled the human player's bow sways more slowly, making the
    /// release-timing window more forgiving. Off by default.
    /// </summary>
    public static bool AimAssist
    {
        get => PlayerPrefs.GetInt(AimAssistKey, 0) == 1;
        set { PlayerPrefs.SetInt(AimAssistKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>Sway-speed multiplier applied to the human archer when aim assist is on.</summary>
    public const float AimAssistSwayScale = 0.6f;
}
