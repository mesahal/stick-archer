using System.IO;
using UnityEngine;

namespace StickArcher.Progression
{
    /// <summary>
    /// Default store: a single JSON file under <see cref="Application.persistentDataPath"/>.
    /// Survives app updates, is wiped on uninstall. Writes atomically (temp file + replace)
    /// so a crash mid-write can't corrupt the existing save.
    /// </summary>
    public class LocalProfileStore : IProfileStore
    {
        const string FileName = "profile.json";

        string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public PlayerProfile Load()
        {
            try
            {
                if (!File.Exists(Path)) return null;
                string json = File.ReadAllText(Path);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonUtility.FromJson<PlayerProfile>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ProfileStore] load failed, starting fresh: {e.Message}");
                return null;
            }
        }

        public void Save(PlayerProfile profile)
        {
            if (profile == null) return;
            try
            {
                string json = JsonUtility.ToJson(profile);
                string tmp = Path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(Path)) File.Delete(Path);
                File.Move(tmp, Path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ProfileStore] save failed: {e.Message}");
            }
        }
    }
}
