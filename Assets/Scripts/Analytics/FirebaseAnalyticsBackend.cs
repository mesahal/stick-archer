#if FIREBASE_ENABLED
using System.Collections.Generic;
using Firebase.Analytics;
using Firebase.Crashlytics;
using UnityEngine;

namespace StickArcher.Analytics
{
    /// <summary>
    /// Firebase Analytics + Crashlytics sink. Compiled only when the scripting define
    /// symbol <c>FIREBASE_ENABLED</c> is set, so the project builds without the SDK.
    ///
    /// TO ENABLE (one-time, on your side — needs a Firebase project):
    ///   1. Create a Firebase project, add an Android app with package id
    ///      from ProjectSettings (currently com.yourcompany.stickarchers — change first).
    ///   2. Download google-services.json into Assets/.
    ///   3. Import the Firebase Unity SDK (FirebaseAnalytics.unitypackage,
    ///      FirebaseCrashlytics.unitypackage).
    ///   4. Player Settings ▸ Scripting Define Symbols (Android): add FIREBASE_ENABLED.
    ///   5. Register this backend in AnalyticsManager.RegisterBackends() (already gated
    ///      by the same #if).
    /// No call sites change — they talk to the Analytics facade only.
    /// </summary>
    public class FirebaseAnalyticsBackend : IAnalyticsBackend
    {
        public string Name => "Firebase";

        public void Initialize()
        {
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == Firebase.DependencyStatus.Available)
                {
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    Debug.Log("[Analytics:Firebase] ready");
                }
                else
                {
                    Debug.LogWarning($"[Analytics:Firebase] dependencies unavailable: {task.Result}");
                }
            });
        }

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                FirebaseAnalytics.LogEvent(eventName);
                return;
            }

            var list = new List<Parameter>(parameters.Count);
            foreach (var kv in parameters)
            {
                switch (kv.Value)
                {
                    case int i:    list.Add(new Parameter(kv.Key, i)); break;
                    case long l:   list.Add(new Parameter(kv.Key, l)); break;
                    case float f:  list.Add(new Parameter(kv.Key, f)); break;
                    case double d: list.Add(new Parameter(kv.Key, d)); break;
                    case bool b:   list.Add(new Parameter(kv.Key, b ? 1L : 0L)); break;
                    default:       list.Add(new Parameter(kv.Key, kv.Value?.ToString() ?? "")); break;
                }
            }
            FirebaseAnalytics.LogEvent(eventName, list.ToArray());
        }

        public void SetUserProperty(string key, string value)
            => FirebaseAnalytics.SetUserProperty(key, value);

        public void LogError(string message, string stackTrace, bool isException)
        {
            Crashlytics.Log(message);
            if (isException)
                Crashlytics.LogException(new System.Exception(message + "\n" + stackTrace));
        }

        public void Flush() { }
    }
}
#endif
