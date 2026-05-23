#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// v9 — Completes Practice (vs AI) mode:
///   1. Creates ArcherLocal.prefab and ArrowLocal.prefab in Assets/Resources/
///   2. Adds PracticeGameManager to GameArena scene
///   3. Wires GameArenaBootstrap.archerLocalPrefab / arrowLocalPrefab
///   4. Rebuilds APK
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v9
{
    const string DoneKey = "VisualOverhaul_v9_Done";

    static VisualOverhaul_v9()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged += WaitForEditMode;
            return;
        }
        EditorApplication.delayCall += Run;
    }

    static void WaitForEditMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= WaitForEditMode;
            if (!EditorPrefs.GetBool(DoneKey, false))
                EditorApplication.delayCall += Run;
        }
    }

    static Sprite _ws;
    static Sprite WS => _ws ??= AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/_WhiteSquare.png");

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;

        EnsureLocalPrefabs();
        PatchGameArenaScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[v9] Practice mode setup complete. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════════
    //  CREATE ArcherLocal.prefab & ArrowLocal.prefab
    // ══════════════════════════════════════════════════════════════
    static void EnsureLocalPrefabs()
    {
        string archerPath = "Assets/Resources/ArcherLocal.prefab";
        string arrowPath  = "Assets/Resources/ArrowLocal.prefab";

        // ── ArcherLocal prefab ────────────────────────────────────
        if (AssetDatabase.LoadAssetAtPath<GameObject>(archerPath) == null)
        {
            var root = new GameObject("ArcherLocal");

            // Physics
            var rb = root.AddComponent<Rigidbody2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = 0f; // archers stand on rooftop, no falling needed

            var col = root.AddComponent<CapsuleCollider2D>();
            col.size   = new Vector2(0.45f, 0.90f);
            col.offset = new Vector2(0f, 0.10f);
            col.isTrigger = false;

            // ArcherLocal script
            var al = root.AddComponent<ArcherLocal>();

            // Body parts (same structure as online Archer built in v4)
            AddBodyPart(root.transform, "Body",    new Vector3(0,  0.18f, 0), new Vector3(0.42f, 0.52f, 1), WS);
            AddBodyPart(root.transform, "Pants",   new Vector3(0, -0.22f, 0), new Vector3(0.40f, 0.36f, 1), WS);
            AddBodyPart(root.transform, "Legs",    new Vector3(0, -0.55f, 0), new Vector3(0.18f, 0.28f, 1), WS);
            AddBodyPart(root.transform, "Head",    new Vector3(0,  0.60f, 0), new Vector3(0.44f, 0.44f, 1), WS);
            AddBodyPart(root.transform, "Hair",    new Vector3(0,  0.76f, 0), new Vector3(0.30f, 0.14f, 1), new Color(0.25f, 0.15f, 0.08f));

            // Bow
            var bowShaft = AddBodyPart(root.transform, "BowShaft",
                new Vector3(0.28f, 0.18f, 0), new Vector3(0.08f, 0.80f, 1),
                new Color(0.55f, 0.35f, 0.15f));
            AddBodyPart(bowShaft.transform, "BowTip_Top",
                new Vector3(0,  0.46f, 0), new Vector3(0.14f, 0.08f, 1),
                new Color(0.55f, 0.35f, 0.15f));
            AddBodyPart(bowShaft.transform, "BowTip_Bot",
                new Vector3(0, -0.46f, 0), new Vector3(0.14f, 0.08f, 1),
                new Color(0.55f, 0.35f, 0.15f));

            // Arrow spawn point
            var spawnGO = new GameObject("ArrowSpawnPoint");
            spawnGO.transform.SetParent(root.transform, false);
            spawnGO.transform.localPosition = new Vector3(0.45f, 0.18f, 0);
            al.arrowSpawnPoint = spawnGO.transform;

            // Save
            PrefabUtility.SaveAsPrefabAsset(root, archerPath);
            Object.DestroyImmediate(root);
            Debug.Log("[v9] Created ArcherLocal.prefab");
        }

        // ── ArrowLocal prefab ─────────────────────────────────────
        if (AssetDatabase.LoadAssetAtPath<GameObject>(arrowPath) == null)
        {
            var root = new GameObject("ArrowLocal");

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.2f;
            rb.mass         = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CapsuleCollider2D>();
            col.size      = new Vector2(0.60f, 0.10f);
            col.direction = CapsuleDirection2D.Horizontal;
            col.isTrigger = true;

            root.AddComponent<ArrowLocal>();

            // Visual shaft
            var shaft = new GameObject("Shaft");
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localScale = new Vector3(0.55f, 0.06f, 1);
            var sr = shaft.AddComponent<SpriteRenderer>();
            sr.sprite = WS;
            sr.color  = new Color(0.70f, 0.50f, 0.20f);
            sr.sortingOrder = 5;

            // Tip
            var tip = new GameObject("Tip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0.30f, 0, 0);
            tip.transform.localScale    = new Vector3(0.14f, 0.10f, 1);
            var sr2 = tip.AddComponent<SpriteRenderer>();
            sr2.sprite = WS; sr2.color = new Color(0.85f, 0.85f, 0.85f); sr2.sortingOrder = 5;

            PrefabUtility.SaveAsPrefabAsset(root, arrowPath);
            Object.DestroyImmediate(root);
            Debug.Log("[v9] Created ArrowLocal.prefab");
        }
    }

    static SpriteRenderer AddBodyPart(Transform parent, string name, Vector3 pos, Vector3 scale, Sprite sprite, int order = 1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    static SpriteRenderer AddBodyPart(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int order = 1)
    {
        var sr = AddBodyPart(parent, name, pos, scale, WS, order);
        sr.color = color;
        return sr;
    }

    // ══════════════════════════════════════════════════════════════
    //  PATCH GameArena SCENE
    // ══════════════════════════════════════════════════════════════
    static void PatchGameArenaScene()
    {
        string scenePath = "Assets/Scenes/GameArena.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Ensure PracticeGameManager exists
        var pgm = Object.FindObjectOfType<PracticeGameManager>();
        if (pgm == null)
        {
            var go = new GameObject("PracticeGameManager");
            go.AddComponent<PracticeGameManager>();
            Debug.Log("[v9] Added PracticeGameManager to GameArena");
        }

        // 2. Wire GameArenaBootstrap prefab refs
        var bootstrap = Object.FindObjectOfType<GameArenaBootstrap>();
        if (bootstrap != null)
        {
            var archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ArcherLocal.prefab");
            var arrowPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ArrowLocal.prefab");
            if (archerPrefab != null) bootstrap.archerLocalPrefab = archerPrefab;
            if (arrowPrefab  != null) bootstrap.arrowLocalPrefab  = arrowPrefab;
            EditorUtility.SetDirty(bootstrap);
            Debug.Log("[v9] Wired GameArenaBootstrap local prefabs");
        }
        else
        {
            Debug.LogWarning("[v9] GameArenaBootstrap not found in GameArena scene.");
        }

        EditorSceneManager.SaveScene(scene);
    }

    // ══════════════════════════════════════════════════════════════
    //  BUILD APK
    // ══════════════════════════════════════════════════════════════
    static void BuildAPK()
    {
        EditorApplication.delayCall -= BuildAPK;
        string outputPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            "StickArchers.apk");

        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameArena.unity" },
            outputPath, BuildTarget.Android, BuildOptions.None);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[v9] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v9] ❌ Build failed: " + report.summary.result);
    }
}
#endif
