using System.Collections.Generic;
using UnityEngine;

namespace StickArcher.Analytics
{
    /// <summary>
    /// Provider-agnostic analytics + crash hub. Self-bootstraps before the first scene
    /// loads, so <see cref="Analytics"/> calls anywhere are always safe. Fans every event
    /// out to all registered <see cref="IAnalyticsBackend"/>s (Debug console by default;
    /// Firebase/UGS drop in behind compile flags without touching call sites).
    ///
    /// Owns: install id + session id, session start/end + duration, per-match context
    /// (mode/difficulty/duration/kills), and global error capture.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        const string KEY_INSTALL_ID = "analytics_install_id";

        readonly List<IAnalyticsBackend> _backends = new List<IAnalyticsBackend>();

        string _installId;
        string _sessionId;
        float _sessionStartTime;

        // Current-match context (set by MatchStarted, consumed by MatchEnded).
        bool _matchActive;
        float _matchStartTime;
        int _matchKills;

        /// <summary>Creates the manager before any scene loads — no prefab/scene setup needed.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("AnalyticsManager");
            go.AddComponent<AnalyticsManager>();
        }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Stable per-install id (anonymous), fresh id per launch.
            _installId = PlayerPrefs.GetString(KEY_INSTALL_ID, "");
            if (string.IsNullOrEmpty(_installId))
            {
                _installId = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(KEY_INSTALL_ID, _installId);
            }
            RegisterBackends();
            foreach (var b in _backends) b.Initialize();

            SetUserProperty("install_id", _installId);
            SetUserProperty("platform", Application.platform.ToString());
            SetUserProperty("app_version", Application.version);

            Application.logMessageReceived += HandleLog;

            StartSession();
        }

        /// <summary>Register the active sinks. Add Firebase/UGS here behind compile flags.</summary>
        void RegisterBackends()
        {
            _backends.Add(new DebugAnalyticsBackend());
#if FIREBASE_ENABLED
            _backends.Add(new FirebaseAnalyticsBackend());
#endif
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Application.logMessageReceived -= HandleLog;
            Instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) EndSession();
            else        StartSession();   // resumed from background → new session
        }

        void OnApplicationQuit() => EndSession();

        bool _sessionEnded;

        void StartSession()
        {
            _sessionId = System.Guid.NewGuid().ToString("N");
            _sessionStartTime = Time.realtimeSinceStartup;
            _sessionEnded = false;
            LogEvent(GameEvents.SessionStart, "session_id", _sessionId);
        }

        void EndSession()
        {
            if (_sessionEnded) return;
            _sessionEnded = true;
            LogEvent(GameEvents.SessionEnd,
                "session_id", _sessionId,
                EventParams.SessionSec, Mathf.RoundToInt(Time.realtimeSinceStartup - _sessionStartTime));
            foreach (var b in _backends) b.Flush();
        }

        // ── Generic API ──────────────────────────────────────────────

        /// <summary>Log an event. Pass alternating key/value pairs, e.g.
        /// LogEvent("kill", "shooter_slot", 1, "victim_slot", 2).</summary>
        public void LogEvent(string eventName, params object[] keyValues)
        {
            var dict = BuildParams(keyValues);
            for (int i = 0; i < _backends.Count; i++)
                _backends[i].LogEvent(eventName, dict);
        }

        public void SetUserProperty(string key, string value)
        {
            for (int i = 0; i < _backends.Count; i++)
                _backends[i].SetUserProperty(key, value);
        }

        // ── Semantic helpers (keep call sites tidy) ──────────────────

        public void MatchStarted(string mode, string difficulty, int character)
        {
            _matchActive = true;
            _matchStartTime = Time.realtimeSinceStartup;
            _matchKills = 0;
            LogEvent(GameEvents.MatchStart,
                EventParams.Mode, mode,
                EventParams.Difficulty, difficulty,
                EventParams.Character, character);
        }

        public void KillRecorded(int shooterSlot, int victimSlot, int p1Score, int p2Score)
        {
            _matchKills++;
            LogEvent(GameEvents.Kill,
                EventParams.ShooterSlot, shooterSlot,
                EventParams.VictimSlot, victimSlot,
                EventParams.P1Score, p1Score,
                EventParams.P2Score, p2Score);
        }

        public void MatchEnded(int winnerSlot, bool localWon, int p1Score, int p2Score)
        {
            int duration = _matchActive ? Mathf.RoundToInt(Time.realtimeSinceStartup - _matchStartTime) : 0;
            LogEvent(GameEvents.MatchEnd,
                EventParams.WinnerSlot, winnerSlot,
                EventParams.LocalWon, localWon,
                EventParams.P1Score, p1Score,
                EventParams.P2Score, p2Score,
                EventParams.Kills, _matchKills,
                EventParams.DurationSec, duration);
            _matchActive = false;
        }

        // ── Crash / error capture ────────────────────────────────────

        void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            bool isException = type == LogType.Exception;
            for (int i = 0; i < _backends.Count; i++)
                _backends[i].LogError(condition, stackTrace, isException);
            LogEvent(GameEvents.AppError,
                EventParams.ErrorType, type.ToString(),
                EventParams.Message, Truncate(condition, 200));
        }

        // ── Helpers ──────────────────────────────────────────────────

        static Dictionary<string, object> BuildParams(object[] keyValues)
        {
            var dict = new Dictionary<string, object>();
            if (keyValues == null) return dict;
            for (int i = 0; i + 1 < keyValues.Length; i += 2)
            {
                var key = keyValues[i] as string;
                if (!string.IsNullOrEmpty(key)) dict[key] = keyValues[i + 1];
            }
            return dict;
        }

        static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }
}
