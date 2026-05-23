#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// VisualOverhaul v4 — fixes the "everything is tiny" bug from v3.
/// The built-in UISprite has a native size of only ~0.16 world units,
/// making all our scaled objects come out 6× too small.
///
/// Fix: generate a proper 32×32 white-square sprite asset with PPU=32
/// so that scale.x = world units exactly.
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v4
{
    const string DoneKey       = "VisualOverhaul_v4_Done";
    const string WhiteSquarePath = "Assets/Art/_WhiteSquare.png";

    static VisualOverhaul_v4()
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

    static Sprite _whiteSquare;
    static Sprite WhiteSquare => _whiteSquare ??= LoadWhiteSquare();

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;
        Debug.Log("[VisualOverhaul_v4] Starting...");

        EnsureWhiteSquareAsset();
        BuildArcherPrefab();
        BuildArrowPrefab();
        RebuildGameArena();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[VisualOverhaul_v4] Done. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════
    //  WHITE SQUARE SPRITE ASSET
    // ══════════════════════════════════════════════════════════
    static void EnsureWhiteSquareAsset()
    {
        if (!Directory.Exists("Assets/Art")) Directory.CreateDirectory("Assets/Art");

        if (!File.Exists(WhiteSquarePath))
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color32[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255,255,255,255);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(WhiteSquarePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(WhiteSquarePath);
        }

        var imp = (TextureImporter)AssetImporter.GetAtPath(WhiteSquarePath);
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 32f;   // 32px sprite → 1 world unit
        imp.filterMode          = FilterMode.Point;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled       = false;
        AssetDatabase.ImportAsset(WhiteSquarePath, ImportAssetOptions.ForceUpdate);

        _whiteSquare = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);
        Debug.Log("[VisualOverhaul_v4] _WhiteSquare.png created (32×32, PPU=32)");
    }

    static Sprite LoadWhiteSquare() => AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);

    // ══════════════════════════════════════════════════════════
    //  ARCHER PREFAB
    // ══════════════════════════════════════════════════════════
    static void BuildArcherPrefab()
    {
        string path = "Assets/Resources/Archer.prefab";

        var root = new GameObject("Archer");

        var rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true; rb.gravityScale = 3.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.45f, 1.2f);
        col.offset = new Vector2(0f, -0.05f);

        root.AddComponent<Animator>();

        var pv  = root.AddComponent<Photon.Pun.PhotonView>();
        var ptv = root.AddComponent<Photon.Pun.PhotonTransformView>();
        pv.ObservedComponents = new System.Collections.Generic.List<Component> { ptv };
        ptv.m_SynchronizePosition = true;
        ptv.m_SynchronizeRotation = false;
        ptv.m_SynchronizeScale    = false;

        var archerScript = root.AddComponent<Archer>();

        // ── Stickman body parts ────────────────────────────────
        MakeChild(root.transform, "Body",  new Vector3(0,  0.10f, 0), new Vector3(0.24f, 0.45f, 1), new Color(0.20f,0.40f,0.85f), 5);
        MakeChild(root.transform, "Pants", new Vector3(0, -0.27f, 0), new Vector3(0.24f, 0.30f, 1), new Color(0.15f,0.28f,0.60f), 5);
        MakeChild(root.transform, "LegL",  new Vector3(-0.07f,-0.50f,0), new Vector3(0.08f,0.20f,1), new Color(0.15f,0.28f,0.60f), 5);
        MakeChild(root.transform, "LegR",  new Vector3( 0.07f,-0.50f,0), new Vector3(0.08f,0.20f,1), new Color(0.15f,0.28f,0.60f), 5);
        MakeChild(root.transform, "Head",  new Vector3(0,  0.45f, 0), new Vector3(0.30f, 0.30f, 1), new Color(0.95f,0.78f,0.62f), 7);
        MakeChild(root.transform, "Hair",  new Vector3(0,  0.58f, 0), new Vector3(0.32f, 0.10f, 1), new Color(0.30f,0.18f,0.08f), 8);
        // Arms
        MakeChild(root.transform, "ArmBack",  new Vector3(-0.10f, 0.15f, -0.01f), new Vector3(0.08f, 0.25f, 1), new Color(0.95f,0.78f,0.62f), 6);
        MakeChild(root.transform, "ArmFront", new Vector3( 0.15f, 0.10f,  0.01f), new Vector3(0.08f, 0.20f, 1), new Color(0.95f,0.78f,0.62f), 7);

        // ── ArrowSpawnPoint pivot (bow rotates with this) ──────
        var spawnPt = new GameObject("ArrowSpawnPoint");
        spawnPt.transform.SetParent(root.transform, false);
        spawnPt.transform.localPosition = new Vector3(0.05f, 0.10f, 0);
        archerScript.arrowSpawnPoint = spawnPt.transform;

        // Bow children of pivot
        MakeChild(spawnPt.transform, "BowShaft", new Vector3(0.30f, 0, 0), new Vector3(0.06f, 0.65f, 1), new Color(0.55f,0.32f,0.10f), 6);
        MakeChild(spawnPt.transform, "BowGrip",  new Vector3(0.30f, 0, 0), new Vector3(0.10f, 0.12f, 1), new Color(0.35f,0.20f,0.05f), 7);
        MakeChild(spawnPt.transform, "BowString",new Vector3(0.25f, 0, -0.01f), new Vector3(0.015f, 0.55f, 1), new Color(0.95f,0.95f,0.95f,0.9f), 7);
        MakeChild(spawnPt.transform, "BowTip",   new Vector3(0.50f, 0, 0), new Vector3(0.08f, 0.08f, 1), new Color(0.95f,0.75f,0.15f), 8);

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[v4] Archer.prefab built" : "[v4] Archer.prefab FAILED");
    }

    // ══════════════════════════════════════════════════════════
    //  ARROW PREFAB
    // ══════════════════════════════════════════════════════════
    static void BuildArrowPrefab()
    {
        string path = "Assets/Resources/Arrow.prefab";
        var root = new GameObject("Arrow");

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.mass = 0.5f;

        var col = root.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.7f, 0.2f);
        col.direction = CapsuleDirection2D.Horizontal;
        col.isTrigger = true;

        root.AddComponent<Photon.Pun.PhotonView>();
        root.AddComponent<Arrow>();

        MakeChild(root.transform, "Shaft",  new Vector3(0,    0, 0), new Vector3(0.7f,  0.10f, 1), new Color(0.55f,0.35f,0.10f), 8);
        MakeChild(root.transform, "Tip",    new Vector3(0.38f, 0, 0), new Vector3(0.22f, 0.20f, 1), new Color(0.95f,0.75f,0.15f), 9);
        MakeChild(root.transform, "Fletch", new Vector3(-0.32f,0, 0), new Vector3(0.15f, 0.24f, 1), new Color(0.95f,0.20f,0.20f), 8);

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[v4] Arrow.prefab built" : "[v4] Arrow.prefab FAILED");
    }

    // ══════════════════════════════════════════════════════════
    //  GAME ARENA
    // ══════════════════════════════════════════════════════════
    static void RebuildGameArena()
    {
        string path = "Assets/Scenes/GameArena.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // Wipe all old environment objects
        foreach (var n in new[] {
            "Ground","Platform_Left","Platform_Right","Platform_Centre",
            "Player1Spawn","Player2Spawn",
            "Building_Left","Building_Right",
            "BG_Bldg_Far_L","BG_Bldg_Far_R","BG_Bldg_Mid_L","BG_Bldg_Mid_R",
            "BG_City","CloudParent"
        })
        {
            GameObject go;
            while ((go = GameObject.Find(n)) != null) Object.DestroyImmediate(go);
        }

        // ── CAMERA ──────────────────────────────────────────────
        var cam = Camera.main;
        cam.orthographic     = true;
        cam.orthographicSize = 3.5f;          // shows 7u height, ~16u width on phone
        cam.transform.position = new Vector3(0, 0f, -10f);
        cam.backgroundColor    = new Color(0.46f, 0.78f, 0.94f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        var cc = cam.GetComponent<CameraController>() ?? cam.gameObject.AddComponent<CameraController>();
        cc.fixedY      = 0f;
        cc.fixedZ      = -10f;
        cc.smoothSpeed = 4f;
        cc.minX        = -1.5f;
        cc.maxX        =  1.5f;

        // ── BACKGROUND CITY (far layer) ─────────────────────────
        var city = new GameObject("BG_City");
        AddBgSilhouette(city.transform, -7.5f, 4f,   1.8f, new Color(0.62f,0.82f,0.96f));
        AddBgSilhouette(city.transform, -5.0f, 5.5f, 2.2f, new Color(0.58f,0.80f,0.95f));
        AddBgSilhouette(city.transform, -2.5f, 4.5f, 2.0f, new Color(0.55f,0.78f,0.94f));
        AddBgSilhouette(city.transform,  0.0f, 5.0f, 2.3f, new Color(0.55f,0.78f,0.94f));
        AddBgSilhouette(city.transform,  2.5f, 4.0f, 2.0f, new Color(0.58f,0.80f,0.95f));
        AddBgSilhouette(city.transform,  5.0f, 5.5f, 2.2f, new Color(0.55f,0.78f,0.94f));
        AddBgSilhouette(city.transform,  7.5f, 4f,   1.8f, new Color(0.62f,0.82f,0.96f));

        // ── STREET (dark band at bottom) ────────────────────────
        var ground = MakeWorldSprite("Ground",
            new Vector3(0, -3.8f, 0), new Vector3(30, 1.2f, 1),
            new Color(0.28f, 0.28f, 0.32f), 0);
        ground.AddComponent<BoxCollider2D>();

        // ── LEFT BUILDING ───────────────────────────────────────
        BuildBuilding("Building_Left", -3.5f, -1.7f, 2.6f, 3.4f,
            new Color(0.85f, 0.60f, 0.40f), new Color(0.55f, 0.35f, 0.20f));
        // Roof platform
        var roofL = MakeWorldSprite("Platform_Left",
            new Vector3(-3.5f, 0.1f, 0), new Vector3(2.8f, 0.30f, 1),
            new Color(0.45f, 0.45f, 0.48f), 2);
        roofL.AddComponent<BoxCollider2D>();
        // Roof trim
        MakeChild(roofL.transform, "RoofTrim", new Vector3(0, -0.45f, 0), new Vector3(1f, 0.4f, 1), new Color(0.55f,0.35f,0.20f), 1);

        // ── RIGHT BUILDING ──────────────────────────────────────
        BuildBuilding("Building_Right", 3.5f, -1.7f, 2.6f, 3.4f,
            new Color(0.82f, 0.56f, 0.38f), new Color(0.55f, 0.35f, 0.20f));
        var roofR = MakeWorldSprite("Platform_Right",
            new Vector3(3.5f, 0.1f, 0), new Vector3(2.8f, 0.30f, 1),
            new Color(0.45f, 0.45f, 0.48f), 2);
        roofR.AddComponent<BoxCollider2D>();
        MakeChild(roofR.transform, "RoofTrim", new Vector3(0, -0.45f, 0), new Vector3(1f, 0.4f, 1), new Color(0.55f,0.35f,0.20f), 1);

        // ── SPAWN POINTS ────────────────────────────────────────
        var p1 = new GameObject("Player1Spawn"); p1.transform.position = new Vector3(-3.5f, 1.0f, 0);
        var p2 = new GameObject("Player2Spawn"); p2.transform.position = new Vector3(3.5f, 1.0f, 0);

        // ── Wire NetworkManager ─────────────────────────────────
        var nm = Object.FindObjectOfType<NetworkManager>();
        if (nm != null) { nm.player1SpawnPoint = p1.transform; nm.player2SpawnPoint = p2.transform; }

        // ── Remove directional light (looks bad in 2D) ──────────
        var dl = GameObject.Find("Directional Light");
        if (dl != null) Object.DestroyImmediate(dl);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[v4] GameArena rebuilt with PROPER scale");
    }

    // ══════════════════════════════════════════════════════════
    //  BUILDING WITH WINDOWS
    // ══════════════════════════════════════════════════════════
    static void BuildBuilding(string name, float x, float y, float w, float h,
                              Color wallColor, Color trimColor)
    {
        var body = MakeWorldSprite(name, new Vector3(x, y, 0), new Vector3(w, h, 1), wallColor, 1);

        // Window grid: 3 cols × 3 rows
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                float lx = (c - 1) * 0.30f;
                float ly = (r - 1) * 0.28f;
                // Window frame (dark border)
                MakeChild(body.transform, "WinFrame_" + r + "_" + c,
                    new Vector3(lx, ly, -0.01f),
                    new Vector3(0.18f / w, 0.23f / h, 1),
                    trimColor, 2);
                // Window glass (light blue)
                MakeChild(body.transform, "Win_" + r + "_" + c,
                    new Vector3(lx, ly, -0.02f),
                    new Vector3(0.14f / w, 0.18f / h, 1),
                    new Color(0.62f, 0.85f, 0.97f), 3);
            }

        // Vertical accent stripes (between window columns)
        for (int c = 0; c < 2; c++)
        {
            float lx = (c - 0.5f) * 0.30f;
            MakeChild(body.transform, "Stripe_" + c,
                new Vector3(lx, 0, -0.005f),
                new Vector3(0.02f / w, 0.95f, 1),
                trimColor, 2);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  BACKGROUND SILHOUETTE
    // ══════════════════════════════════════════════════════════
    static void AddBgSilhouette(Transform parent, float x, float h, float w, Color color)
    {
        var bldg = new GameObject("BgB");
        bldg.transform.SetParent(parent, false);
        bldg.transform.position   = new Vector3(x, -2f + h * 0.5f, 5);
        bldg.transform.localScale = new Vector3(w, h, 1);
        var sr = bldg.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSquare;
        sr.color  = color;
        sr.sortingOrder = -5;
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════
    static GameObject MakeWorldSprite(string name, Vector3 pos, Vector3 scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSquare;
        sr.color  = color;
        sr.sortingOrder = order;
        return go;
    }

    static GameObject MakeChild(Transform parent, string name,
        Vector3 localPos, Vector3 localScale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSquare;
        sr.color  = color;
        sr.sortingOrder = order;
        return go;
    }

    // ══════════════════════════════════════════════════════════
    //  BUILD APK
    // ══════════════════════════════════════════════════════════
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
            Debug.Log("[v4] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[v4] ❌ Build failed: " + report.summary.result);
    }
}
#endif
