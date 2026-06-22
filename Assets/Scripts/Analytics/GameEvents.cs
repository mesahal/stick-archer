namespace StickArcher.Analytics
{
    /// <summary>
    /// Canonical event + parameter names for analytics. Centralised so call sites
    /// never hand-type strings (typos would silently fork a metric on the dashboard).
    ///
    /// Keep names snake_case and stable — once a dashboard chart references a name,
    /// renaming it splits the historical series.
    /// </summary>
    public static class GameEvents
    {
        // ── Session / lifecycle ──────────────────────────────────────
        public const string SessionStart = "session_start";
        public const string SessionEnd   = "session_end";
        public const string AppError     = "app_error";

        // ── Menu / funnel ────────────────────────────────────────────
        public const string MenuPlayOnline    = "menu_play_online";
        public const string MenuPractice       = "menu_practice";
        public const string DifficultyChanged  = "difficulty_changed";
        public const string CharacterSelected  = "character_selected";

        // ── Match flow ───────────────────────────────────────────────
        public const string MatchStart = "match_start";
        public const string MatchEnd   = "match_end";
        public const string Kill       = "kill";

        // ── Economy / progression ────────────────────────────────────
        public const string CurrencyEarned = "currency_earned";
        public const string CurrencySpent   = "currency_spent";
        public const string LevelUp         = "level_up";
    }

    /// <summary>Canonical parameter keys for the events above.</summary>
    public static class EventParams
    {
        public const string Mode        = "mode";          // "online" | "practice"
        public const string Difficulty  = "difficulty";    // "easy" | "normal" | "hard"
        public const string Character   = "character";     // selected character index
        public const string ShooterSlot = "shooter_slot";
        public const string VictimSlot  = "victim_slot";
        public const string P1Score     = "p1_score";
        public const string P2Score     = "p2_score";
        public const string WinnerSlot  = "winner_slot";
        public const string LocalWon    = "local_won";
        public const string DurationSec = "duration_sec";
        public const string SessionSec  = "session_sec";
        public const string Kills       = "kills";
        public const string ErrorType   = "error_type";
        public const string Message     = "message";
        public const string Amount      = "amount";      // currency/xp delta
        public const string Balance     = "balance";     // resulting balance
        public const string Level       = "level";
        public const string Reason      = "reason";       // why currency moved (e.g. "match_win")
    }
}
