using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace StickArcher.Analytics
{
    /// <summary>
    /// Default zero-dependency backend: prints events to the Unity console.
    /// Lets the whole funnel be wired and verified in-editor before any real SDK
    /// (with its dashboard account + credentials) is dropped in. Always present so
    /// analytics calls are never silently lost during development.
    /// </summary>
    public class DebugAnalyticsBackend : IAnalyticsBackend
    {
        public string Name => "Debug";

        // Set false to silence console spam while keeping other backends active.
        public bool verbose = true;

        public void Initialize()
        {
            Debug.Log("[Analytics:Debug] initialized");
        }

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (!verbose) return;

            var sb = new StringBuilder();
            sb.Append("[Analytics] ").Append(eventName);
            if (parameters != null && parameters.Count > 0)
            {
                sb.Append(" { ");
                bool first = true;
                foreach (var kv in parameters)
                {
                    if (!first) sb.Append(", ");
                    sb.Append(kv.Key).Append('=').Append(kv.Value);
                    first = false;
                }
                sb.Append(" }");
            }
            Debug.Log(sb.ToString());
        }

        public void SetUserProperty(string key, string value)
        {
            if (verbose) Debug.Log($"[Analytics] user_property {key}={value}");
        }

        public void LogError(string message, string stackTrace, bool isException)
        {
            Debug.Log($"[Analytics] {(isException ? "EXCEPTION" : "error")}: {message}");
        }

        public void Flush() { }
    }
}
