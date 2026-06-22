using System.Collections.Generic;

namespace StickArcher.Analytics
{
    /// <summary>
    /// A pluggable analytics sink. The game talks only to the provider-agnostic
    /// <see cref="AnalyticsManager"/>; concrete SDKs (Firebase, Unity GS, GameAnalytics)
    /// implement this interface so they can be swapped without touching call sites.
    /// </summary>
    public interface IAnalyticsBackend
    {
        /// <summary>Human-readable name for logs (e.g. "Debug", "Firebase").</summary>
        string Name { get; }

        /// <summary>Called once at startup. Do SDK init / consent setup here.</summary>
        void Initialize();

        /// <summary>Record a named event with optional flat parameters.</summary>
        void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters);

        /// <summary>Attach a sticky property to all subsequent events (e.g. install_id).</summary>
        void SetUserProperty(string key, string value);

        /// <summary>Report a captured error / exception for crash dashboards.</summary>
        void LogError(string message, string stackTrace, bool isException);

        /// <summary>Best-effort flush of buffered events (e.g. on quit/pause).</summary>
        void Flush();
    }
}
