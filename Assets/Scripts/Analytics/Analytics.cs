namespace StickArcher.Analytics
{
    /// <summary>
    /// Static, null-safe facade over <see cref="AnalyticsManager"/>. Call sites use this
    /// so they never have to null-check the singleton or know which backends exist:
    ///     Analytics.Log(GameEvents.MenuPractice);
    ///     Analytics.MatchStarted("practice", "hard", 0);
    /// </summary>
    public static class Analytics
    {
        public static void Log(string eventName, params object[] keyValues)
            => AnalyticsManager.Instance?.LogEvent(eventName, keyValues);

        public static void SetUserProperty(string key, string value)
            => AnalyticsManager.Instance?.SetUserProperty(key, value);

        public static void MatchStarted(string mode, string difficulty, int character)
            => AnalyticsManager.Instance?.MatchStarted(mode, difficulty, character);

        public static void KillRecorded(int shooterSlot, int victimSlot, int p1Score, int p2Score)
            => AnalyticsManager.Instance?.KillRecorded(shooterSlot, victimSlot, p1Score, p2Score);

        public static void MatchEnded(int winnerSlot, bool localWon, int p1Score, int p2Score)
            => AnalyticsManager.Instance?.MatchEnded(winnerSlot, localWon, p1Score, p2Score);
    }
}
