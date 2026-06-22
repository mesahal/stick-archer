#if CLOUD_SAVE_ENABLED
using UnityEngine;

namespace StickArcher.Progression
{
    /// <summary>
    /// Cloud-backed store seam. Compiled only when <c>CLOUD_SAVE_ENABLED</c> is defined,
    /// so the project builds offline-only by default.
    ///
    /// Strategy: keep the <see cref="LocalProfileStore"/> as an offline cache (instant,
    /// always available) and sync to the backend in the background. This avoids blocking
    /// the boot path on a network round-trip and gives correct behaviour offline.
    ///
    /// TO IMPLEMENT (your side — needs a backend account):
    ///   - PlayFab: GetUserData / UpdateUserData, or
    ///   - Firebase: Firestore/Realtime DB document per auth uid, or
    ///   - Unity Gaming Services: Cloud Save key/value.
    ///   Reconcile by schemaVersion + a server timestamp on conflict.
    /// Define CLOUD_SAVE_ENABLED in Player Settings ▸ Scripting Define Symbols and
    /// register this in ProfileManager.CreateStore().
    /// </summary>
    public class CloudProfileStore : IProfileStore
    {
        readonly LocalProfileStore _cache = new LocalProfileStore();

        public PlayerProfile Load()
        {
            // Return the local cache immediately; kick off an async cloud pull that
            // reconciles and re-saves via ProfileManager when it returns.
            // TODO: backend.FetchAsync(...) -> on success, merge + ProfileManager.ReplaceProfile(...)
            return _cache.Load();
        }

        public void Save(PlayerProfile profile)
        {
            _cache.Save(profile);
            // TODO: backend.UploadAsync(JsonUtility.ToJson(profile))
        }
    }
}
#endif
