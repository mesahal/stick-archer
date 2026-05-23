#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full visual overhaul to match the Stick Archers Battle reference:
///  - Tall building platforms (not tiny lines)
///  - City skyline background
///  - Proper archer sprite size
///  - Correct camera zoom and position
///  - Arrow looks like an arrow
///  - Player tints: Blue (P1) vs Red (P2) applied at spawn
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul
{
    const string DoneKey = "VisualOverhaul_v2";

    static VisualOverhaul()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;

        // If already in play mode, wait until it stops
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

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return; // safety guard
        Debug.Log("[VisualOverhaul] Starting...");

        FixSpriteImports();
        RebuildGameArenaScene();
        FixArcherPrefab();
        FixArrowPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[VisualOverhaul] Scene rebuilt. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ── 1. SPRITE IMPORT SETTINGS ───────────────────────────────
    static void FixSpriteImports()
    {
        string[] spritePaths = {
            "Assets/Art/Sprites/Player1_Adventurer/archer_idle.png",
            "Assets/Art/Sprites/Player1_Adventurer/archer_charge.png",
            "Assets/Art/Sprites/Player1_Adventurer/archer_fire.png",
            "Assets/Art/Sprites/Player1_Adventurer/archer_ragdoll.png",
            "Assets/Art/Sprites/Player2_Soldier/archer_idle.png",
            "Assets/Art/Sprites/Player2_Soldier/archer_charge.png",
            "Assets/Art/Sprites/Player2_Soldier/archer_ragdoll.png",
        };

        foreach (var path in spritePaths)
        {
            if (!File.Exists(path)) continue;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 32;     // 32px sprite → 1 unit; scale prefab ×2 = 2u tall
            imp.filterMode          = FilterMode.Point; // crisp pixel art
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log("[VisualOverhaul] Sprite imports fixed (PPU=32, Point filter)");
    }

    // ── 2. REBUILD GAME ARENA SCENE ─────────────────────────────
    static void RebuildGameArenaScene()
    {
        string path = "Assets/Scenes/GameArena.unity";
        var scene   = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // Delete old platforms / ground (we'll recreate them)
        foreach (var name in new[]{"Ground","Platform_Left","Platform_Right","Player1Spawn","Player2Spawn"})
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        // ── CAMERA ──────────────────────────────────────────────
        var cam = Camera.main;
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0, 1.5f, -10f);
        cam.backgroundColor    = new Color(0.47f, 0.74f, 0.95f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        if (cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();
        var cc = cam.GetComponent<CameraController>();
        cc.fixedY    = 1.5f;
        cc.fixedZ    = -10f;
        cc.smoothSpeed = 4f;

        // ── BACKGROUND CITY SILHOUETTE ───────────────────────────
        CreateBgBuilding("BG_Bldg_Far_L",  new Vector3(-7f, -1.5f, 1f),  new Vector3(3f, 5f, 1f),  new Color(0.55f, 0.75f, 0.88f));
        CreateBgBuilding("BG_Bldg_Far_R",  new Vector3(7f, -2f, 1f),     new Vector3(2.5f,4f,1f),  new Color(0.55f, 0.75f, 0.88f));
        CreateBgBuilding("BG_Bldg_Mid_L",  new Vector3(-4.5f,-1f,0.5f),  new Vector3(2f, 3.5f,1f), new Color(0.58f, 0.70f, 0.82f));
        CreateBgBuilding("BG_Bldg_Mid_R",  new Vector3(4.5f,-1f, 0.5f),  new Vector3(2f, 3.5f,1f), new Color(0.58f, 0.70f, 0.82f));

        // ── GROUND (hidden below view) ───────────────────────────
        var ground = CreatePlatform("Ground", new Vector3(0,-5.5f,0), new Vector3(20,1,1), new Color(0.35f,0.25f,0.18f));

        // ── LEFT BUILDING (Player 1 stands on top) ───────────────
        // Building body
        var leftBody = CreatePlatform("Building_Left", new Vector3(-4f,-2.5f,0), new Vector3(3f,6f,1f), new Color(0.72f,0.58f,0.42f));
        AddWindowsToBuilding(leftBody, 3, 4);
        // Roof / top platform
        var leftRoof = CreatePlatform("Platform_Left", new Vector3(-4f,0.6f,0), new Vector3(3.2f,0.3f,1f), new Color(0.50f,0.38f,0.25f));
        leftRoof.AddComponent<BoxCollider2D>();

        // ── RIGHT BUILDING (Player 2 stands on top) ──────────────
        var rightBody = CreatePlatform("Building_Right", new Vector3(4f,-2.5f,0), new Vector3(3f,6f,1f), new Color(0.68f,0.52f,0.38f));
        AddWindowsToBuilding(rightBody, 3, 4);
        var rightRoof = CreatePlatform("Platform_Right", new Vector3(4f,0.6f,0), new Vector3(3.2f,0.3f,1f), new Color(0.48f,0.35f,0.22f));
        rightRoof.AddComponent<BoxCollider2D>();

        // ── CENTRE LOWER PLATFORM ────────────────────────────────
        var centre = CreatePlatform("Platform_Centre", new Vector3(0,-1.5f,0), new Vector3(2f,0.3f,1f), new Color(0.50f,0.38f,0.25f));
        centre.AddComponent<BoxCollider2D>();

        // ── SPAWN POINTS (top of buildings) ──────────────────────
        var p1Spawn = new GameObject("Player1Spawn");
        p1Spawn.transform.position = new Vector3(-4f, 1.5f, 0);
        var p2Spawn = new GameObject("Player2Spawn");
        p2Spawn.transform.position = new Vector3(4f, 1.5f, 0);

        // Re-wire NetworkManager spawn points
        var nm = Object.FindObjectOfType<NetworkManager>();
        if (nm != null)
        {
            nm.player1SpawnPoint = p1Spawn.transform;
            nm.player2SpawnPoint = p2Spawn.transform;
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[VisualOverhaul] GameArena scene rebuilt with buildings");
    }

    // ── 3. ARCHER PREFAB ────────────────────────────────────────
    static void FixArcherPrefab()
    {
        string prefabPath = "Assets/Resources/Archer.prefab";
        if (!File.Exists(prefabPath)) { Debug.LogError("Archer.prefab missing"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            // Scale up so archer is roughly 2 units tall (visible on screen)
            root.transform.localScale = new Vector3(2f, 2f, 1f);

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Sprites/Player1_Adventurer/archer_idle.png");
                sr.sprite    = sprite != null ? sprite
                    : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.color     = Color.white;
                sr.sortingOrder = 5;
            }

            // Collider sized for a person
            var col = root.GetComponent<BoxCollider2D>();
            if (col != null) { col.size = new Vector2(0.45f, 0.9f); col.offset = new Vector2(0,0); }

            // Rigidbody
            var rb = root.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.gravityScale = 3f; rb.constraints = RigidbodyConstraints2D.FreezeRotation; }

            // Arrow spawn point
            var sp = root.transform.Find("ArrowSpawnPoint");
            if (sp != null) sp.localPosition = new Vector3(0.5f, 0.1f, 0);
        }

        Debug.Log("[VisualOverhaul] Archer.prefab updated");
    }

    // ── 4. ARROW PREFAB ─────────────────────────────────────────
    static void FixArrowPrefab()
    {
        string prefabPath = "Assets/Resources/Arrow.prefab";
        if (!File.Exists(prefabPath)) { Debug.LogError("Arrow.prefab missing"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            // Arrow shaft: thin elongated sprite
            root.transform.localScale = new Vector3(0.6f, 0.06f, 1f);

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.color      = new Color(0.6f, 0.4f, 0.15f);  // brown wood shaft
                sr.sortingOrder = 4;
            }

            // Tip collider
            var col = root.GetComponent<CapsuleCollider2D>();
            if (col != null) { col.size = new Vector2(0.9f, 0.9f); }
        }

        Debug.Log("[VisualOverhaul] Arrow.prefab updated");
    }

    // ── BUILD APK ───────────────────────────────────────────────
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
            Debug.Log("[VisualOverhaul] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[VisualOverhaul] ❌ Build failed: " + report.summary.result);
    }

    // ── HELPERS ─────────────────────────────────────────────────
    static GameObject CreatePlatform(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = color;
        return go;
    }

    static GameObject CreateBgBuilding(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = CreatePlatform(name, pos, scale, color);
        go.GetComponent<SpriteRenderer>().sortingOrder = -5;
        return go;
    }

    /// <summary>Adds simple white square "windows" as child sprites.</summary>
    static void AddWindowsToBuilding(GameObject building, int cols, int rows)
    {
        float bw = building.transform.localScale.x;
        float bh = building.transform.localScale.y;

        float winW = 0.18f;
        float winH = 0.22f;
        float padX = bw / (cols + 1) / bw;
        float padY = bh / (rows + 1) / bh;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float lx = Mathf.Lerp(-0.4f,  0.4f,  (c + 1f) / (cols + 1f));
                float ly = Mathf.Lerp(-0.45f, 0.35f, (r + 1f) / (rows + 1f));

                var win = new GameObject("Window");
                win.transform.SetParent(building.transform, false);
                win.transform.localPosition = new Vector3(lx, ly, -0.1f);
                win.transform.localScale    = new Vector3(winW, winH, 1f);
                var sr = win.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sr.color  = new Color(0.6f, 0.85f, 1f, 0.85f);   // light blue windows
                sr.sortingOrder = 2;
            }
        }
    }
}
#endif
