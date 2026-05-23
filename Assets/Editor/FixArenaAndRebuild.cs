#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-shot fix:
///  1. Sets camera to orthographic in GameArena scene
///  2. Adds CameraController to the camera
///  3. Gives Archer prefab a visible colored sprite
///  4. Gives Arrow prefab a visible colored sprite
///  5. Rebuilds the APK
/// </summary>
[InitializeOnLoad]
public static class FixArenaAndRebuild
{
    const string DoneKey = "FixArenaAndRebuild_v1";

    static FixArenaAndRebuild()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        EditorApplication.delayCall -= Run;

        FixGameArenaScene();
        FixArcherPrefab();
        FixArrowPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorPrefs.SetBool(DoneKey, true);
        Debug.Log("[FixArena] Scene + prefabs fixed. Building APK...");

        EditorApplication.delayCall += BuildAPK;
    }

    // ── FIX GAME ARENA SCENE ────────────────────────────────────
    static void FixGameArenaScene()
    {
        string path = "Assets/Scenes/GameArena.unity";
        var scene   = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // 1. Camera → orthographic 2D
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic     = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0, 1f, -10f);
            cam.backgroundColor    = new Color(0.48f, 0.75f, 0.95f);
            cam.clearFlags         = CameraClearFlags.SolidColor;

            // Add CameraController if not present
            if (cam.GetComponent<CameraController>() == null)
                cam.gameObject.AddComponent<CameraController>();
        }

        // 2. Remove Directional Light (not needed for 2D flat look)
        var dirLight = GameObject.Find("Directional Light");
        if (dirLight != null)
        {
            dirLight.GetComponent<Light>().intensity = 0.5f; // dim rather than delete
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixArena] GameArena camera fixed → orthographic 2D");
    }

    // ── FIX ARCHER PREFAB ────────────────────────────────────────
    static void FixArcherPrefab()
    {
        string prefabPath = "Assets/Resources/Archer.prefab";
        var prefabAsset   = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null) { Debug.LogError("[FixArena] Archer.prefab not found!"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            // Assign idle sprite (Player1 by default; we tint at runtime)
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Try to load the archer idle sprite
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Sprites/Player1_Adventurer/archer_idle.png");

                if (sprite != null)
                {
                    sr.sprite = sprite;
                    sr.color  = Color.white;
                }
                else
                {
                    // Fallback: use Unity's built-in white square
                    sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                    sr.color  = new Color(0.3f, 0.8f, 0.3f); // green box
                }

                sr.drawMode = SpriteDrawMode.Simple;
            }

            // Make sure the RigidBody is correct
            var rb = root.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 3f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }

            // Make sure Box Collider is right size
            var col = root.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size   = new Vector2(0.5f, 1f);
                col.offset = new Vector2(0f, 0f);
            }

            // Correct ArrowSpawnPoint position
            var spawnPt = root.transform.Find("ArrowSpawnPoint");
            if (spawnPt != null)
                spawnPt.localPosition = new Vector3(0.5f, 0.2f, 0);
        }

        Debug.Log("[FixArena] Archer.prefab sprite assigned");
    }

    // ── FIX ARROW PREFAB ─────────────────────────────────────────
    static void FixArrowPrefab()
    {
        string prefabPath = "Assets/Resources/Arrow.prefab";
        var prefabAsset   = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null) { Debug.LogError("[FixArena] Arrow.prefab not found!"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;
            var sr   = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.color  = new Color(0.9f, 0.7f, 0.1f);   // yellow arrow
                root.transform.localScale = new Vector3(0.4f, 0.08f, 1f);
            }
        }

        Debug.Log("[FixArena] Arrow.prefab sprite assigned");
    }

    // ── BUILD APK ────────────────────────────────────────────────
    static void BuildAPK()
    {
        EditorApplication.delayCall -= BuildAPK;

        string outputPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameArena.unity" },
            outputPath,
            BuildTarget.Android,
            BuildOptions.None);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[FixArena] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[FixArena] ❌ Build failed: " + report.summary.result);
    }
}
#endif
