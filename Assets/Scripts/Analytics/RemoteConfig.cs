using System;
using System.Collections.Generic;
using UnityEngine;

namespace StickArcher.Analytics
{
    /// <summary>
    /// Provider-agnostic remote config. Ships with safe local defaults so gameplay
    /// works offline / before any fetch; a backend (Firebase Remote Config, UGS, etc.)
    /// can later call <see cref="Apply"/> with fetched overrides.
    ///
    /// Wire gameplay tunables through this (see <c>score_to_win</c> used by the game
    /// managers) so balance can be changed server-side without shipping a build.
    /// </summary>
    public static class RemoteConfig
    {
        // Built-in defaults. Add a key here the moment gameplay reads it.
        static readonly Dictionary<string, object> _defaults = new Dictionary<string, object>
        {
            { "score_to_win",           5 },
            { "round_reset_delay_sec",  2f },
            // Progression rewards (Phase 2) — server-tunable economy balance:
            { "coins_per_match",        10 },
            { "coins_per_win",          25 },
            { "xp_per_match",           20 },
            { "xp_per_win",             50 },
            // Pre-seeded for later phases (ads) so the schema is stable:
            { "interstitial_frequency", 2 },
            { "rewarded_coins",         50 },
        };

        static readonly Dictionary<string, object> _remote = new Dictionary<string, object>();

        /// <summary>Raised after a backend applies fetched values (so live systems can refresh).</summary>
        public static event Action OnFetched;

        /// <summary>Called by a remote-config backend once values are fetched.</summary>
        public static void Apply(IReadOnlyDictionary<string, object> values)
        {
            if (values == null) return;
            foreach (var kv in values) _remote[kv.Key] = kv.Value;
            Debug.Log($"[RemoteConfig] applied {values.Count} remote value(s)");
            OnFetched?.Invoke();
        }

        public static int GetInt(string key, int fallback = 0)   => Get(key, fallback, Convert.ToInt32);
        public static float GetFloat(string key, float fallback = 0f) => Get(key, fallback, Convert.ToSingle);
        public static bool GetBool(string key, bool fallback = false) => Get(key, fallback, Convert.ToBoolean);
        public static string GetString(string key, string fallback = "") => Get(key, fallback, Convert.ToString);

        static T Get<T>(string key, T fallback, Func<object, T> convert)
        {
            object raw;
            if (_remote.TryGetValue(key, out raw) || _defaults.TryGetValue(key, out raw))
            {
                try { return convert(raw); }
                catch { return fallback; }
            }
            return fallback;
        }
    }
}
