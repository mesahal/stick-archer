#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Complete visual rebuild v3 — matches the reference Stick Archers Battle game.
/// Builds stickman archers from primitives, large visible arrows, proper buildings,
/// city skyline, and correct camera framing.
/// </summary>
[InitializeOnLoad]
public static class VisualOverhaul_v3
{
    const string DoneKey = "VisualOverhaul_v3_Done";

    static VisualOverhaul_v3()
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

    static Sprite WhiteSquare => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;

        Debug.Log("[VisualOverhaul_v3] Starting full rebuild...");

        BuildArcherPrefab();
        BuildArrowPrefab();
        RebuildGameArena();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log("[VisualOverhaul_v3] Done. Building APK...");
        EditorApplication.delayCall += BuildAPK;
    }

    // ══════════════════════════════════════════════════════════
    //  ARCHER PREFAB — stickman built from primitives
    // ══════════════════════════════════════════════════════════
    static void BuildArcherPrefab()
    {
        string path = "Assets/Resources/Archer.prefab";

        // Build in a scene root
        var root = new GameObject("Archer");
        root.transform.localScale = Vector3.one;

        // ── Components on root ─────────────────────────────────
        var rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3.5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.45f, 1.1f);
        col.offset = new Vector2(0f, 0f);

        root.AddComponent<Animator>();

        var pv = root.AddComponent<Photon.Pun.PhotonView>();
        var ptv = root.AddComponent<Photon.Pun.PhotonTransformView>();
        pv.ObservedComponents = new System.Collections.Generic.List<Component> { ptv };
        ptv.m_SynchronizePosition = true;
        ptv.m_SynchronizeRotation = false;
        ptv.m_SynchronizeScale    = false;

        var archerScript = root.AddComponent<Archer>();

        // ── BODY (torso) ───────────────────────────────────────
        var body = MakeChild(root.transform, "Body",
            new Vector3(0, 0.05f, 0), new Vector3(0.22f, 0.45f, 1),
            new Color(0.20f, 0.40f, 0.85f), 5);

        // ── PANTS ──────────────────────────────────────────────
        MakeChild(root.transform, "Pants",
            new Vector3(0, -0.32f, 0), new Vector3(0.22f, 0.30f, 1),
            new Color(0.15f, 0.28f, 0.60f), 5);

        // ── LEFT LEG ───────────────────────────────────────────
        MakeChild(root.transform, "LegL",
            new Vector3(-0.07f, -0.55f, 0), new Vector3(0.08f, 0.20f, 1),
            new Color(0.15f, 0.28f, 0.60f), 5);

        // ── RIGHT LEG ──────────────────────────────────────────
        MakeChild(root.transform, "LegR",
            new Vector3(0.07f, -0.55f, 0), new Vector3(0.08f, 0.20f, 1),
            new Color(0.15f, 0.28f, 0.60f), 5);

        // ── HEAD ───────────────────────────────────────────────
        MakeChild(root.transform, "Head",
            new Vector3(0, 0.43f, 0), new Vector3(0.30f, 0.30f, 1),
            new Color(0.95f, 0.78f, 0.62f), 7);

        // ── HAIR ───────────────────────────────────────────────
        MakeChild(root.transform, "Hair",
            new Vector3(0, 0.55f, 0), new Vector3(0.32f, 0.12f, 1),
            new Color(0.35f, 0.22f, 0.10f), 8);

        // ── ARROW SPAWN POINT (pivot rotated for aim) ──────────
        var spawnPt = new GameObject("ArrowSpawnPoint");
        spawnPt.transform.SetParent(root.transform, false);
        spawnPt.transform.localPosition = new Vector3(0, 0.05f, 0);
        archerScript.arrowSpawnPoint = spawnPt.transform;

        // ── BOW (child of spawn pt so it rotates with aim) ─────
        MakeChild(spawnPt.transform, "BowShaft",
            new Vector3(0.30f, 0, 0), new Vector3(0.08f, 0.60f, 1),
            new Color(0.55f, 0.32f, 0.10f), 6);
        // Bow string
        MakeChild(spawnPt.transform, "BowString",
            new Vector3(0.30f, 0, -0.01f), new Vector3(0.02f, 0.55f, 1),
            new Color(1f, 1f, 1f, 0.85f), 7);
        // Bow tip indicator (small dot at arrow tip)
        MakeChild(spawnPt.transform, "BowTip",
            new Vector3(0.55f, 0, 0), new Vector3(0.08f, 0.08f, 1),
            new Color(0.9f, 0.7f, 0.2f), 7);

        // ── Save as prefab ─────────────────────────────────────
        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[VisualOverhaul_v3] Archer.prefab built (stickman)" : "Archer.prefab FAILED");
    }

    // ══════════════════════════════════════════════════════════
    //  ARROW PREFAB — large, bright, clearly visible
    // ══════════════════════════════════════════════════════════
    static void BuildArrowPrefab()
    {
        string path = "Assets/Resources/Arrow.prefab";
        var root = new GameObject("Arrow");

        // ── Components ─────────────────────────────────────────
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.mass = 0.5f;

        var col = root.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.6f, 0.18f);
        col.direction = CapsuleDirection2D.Horizontal;
        col.isTrigger = true;

        root.AddComponent<Photon.Pun.PhotonView>();
        root.AddComponent<Arrow>();

        // ── Shaft (visible brown body) ─────────────────────────
        MakeChild(root.transform, "Shaft",
            new Vector3(0, 0, 0), new Vector3(0.55f, 0.10f, 1),
            new Color(0.55f, 0.35f, 0.10f), 8);

        // ── Tip (yellow arrowhead) ─────────────────────────────
        MakeChild(root.transform, "Tip",
            new Vector3(0.30f, 0, 0), new Vector3(0.18f, 0.18f, 1),
            new Color(0.95f, 0.75f, 0.15f), 9);

        // ── Fletching (back feathers) ──────────────────────────
        MakeChild(root.transform, "Fletch",
            new Vector3(-0.27f, 0, 0), new Vector3(0.14f, 0.22f, 1),
            new Color(1f, 0.2f, 0.2f), 8);

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[VisualOverhaul_v3] Arrow.prefab built (visible arrow shape)" : "Arrow.prefab FAILED");
    }

    // ══════════════════════════════════════════════════════════
    //  GAME ARENA — buildings, sky, spawn points
    // ══════════════════════════════════════════════════════════
    static void RebuildGameArena()
    {
        string path = "Assets/Scenes/GameArena.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // Delete old environment objects
        foreach (var n in new[] {
            "Ground","Platform_Left","Platform_Right","Platform_Centre",
            "Player1Spawn","Player2Spawn",
            "Building_Left","Building_Right",
            "BG_Bldg_Far_L","BG_Bldg_Far_R","BG_Bldg_Mid_L","BG_Bldg_Mid_R",
            "BG_City","CloudParent"
        })
        {
            var go = GameObject.Find(n);
            while (go != null) { Object.DestroyImmediate(go); go = GameObject.Find(n); }
        }

        // ── CAMERA ──────────────────────────────────────────────
        var cam = Camera.main;
        cam.orthographic     = true;
        cam.orthographicSize = 3.8f;
        cam.transform.position = new Vector3(0, 0.4f, -10f);
        cam.backgroundColor    = new Color(0.46f, 0.78f, 0.94f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        var cc = cam.GetComponent<CameraController>() ?? cam.gameObject.AddComponent<CameraController>();
        cc.fixedY    = 0.4f;
        cc.fixedZ    = -10f;
        cc.smoothSpeed = 4f;
        cc.minX = -2f;
        cc.maxX =  2f;

        // ── SKY (already via background color) ──────────────────

        // ── CITY SILHOUETTE (back layer, lighter blue) ──────────
        var cityParent = new GameObject("BG_City");
        cityParent.transform.position = new Vector3(0, 0, 5);
        AddBgSilhouette(cityParent.transform, -10f, 4f, new Color(0.66f, 0.84f, 0.97f, 0.9f));
        AddBgSilhouette(cityParent.transform,  -6f, 3f, new Color(0.62f, 0.80f, 0.95f, 0.9f));
        AddBgSilhouette(cityParent.transform,  -2f, 3.5f, new Color(0.58f, 0.77f, 0.94f, 0.9f));
        AddBgSilhouette(cityParent.transform,   2f, 3.2f, new Color(0.58f, 0.77f, 0.94f, 0.9f));
        AddBgSilhouette(cityParent.transform,   6f, 4f, new Color(0.62f, 0.80f, 0.95f, 0.9f));
        AddBgSilhouette(cityParent.transform,  10f, 3f, new Color(0.66f, 0.84f, 0.97f, 0.9f));

        // ── GROUND BAR (street, runs across bottom) ─────────────
        var ground = MakeWorldSprite("Ground",
            new Vector3(0, -4.0f, 0), new Vector3(30, 1.5f, 1),
            new Color(0.30f, 0.30f, 0.32f), 0);
        ground.AddComponent<BoxCollider2D>();

        // ── LEFT BUILDING (Player 1 stands on top) ──────────────
        BuildBuilding("Building_Left", -3.5f, -2.0f, 2.6f, 3.6f,
            new Color(0.80f, 0.55f, 0.38f), new Color(0.60f, 0.38f, 0.22f));

        // Roof platform (collider)
        var roofL = MakeWorldSprite("Platform_Left",
            new Vector3(-3.5f, 0.0f, 0), new Vector3(2.8f, 0.35f, 1),
            new Color(0.42f, 0.42f, 0.46f), 2);
        roofL.AddComponent<BoxCollider2D>();

        // ── RIGHT BUILDING ──────────────────────────────────────
        BuildBuilding("Building_Right", 3.5f, -2.0f, 2.6f, 3.6f,
            new Color(0.78f, 0.52f, 0.36f), new Color(0.58f, 0.36f, 0.20f));
        var roofR = MakeWorldSprite("Platform_Right",
            new Vector3(3.5f, 0.0f, 0), new Vector3(2.8f, 0.35f, 1),
            new Color(0.42f, 0.42f, 0.46f), 2);
        roofR.AddComponent<BoxCollider2D>();

        // ── SPAWN POINTS (top of buildings) ─────────────────────
        var p1Spawn = new GameObject("Player1Spawn");
        p1Spawn.transform.position = new Vector3(-3.5f, 0.75f, 0);
        var p2Spawn = new GameObject("Player2Spawn");
        p2Spawn.transform.position = new Vector3(3.5f, 0.75f, 0);

        // ── Re-wire NetworkManager ──────────────────────────────
        var nm = Object.FindObjectOfType<NetworkManager>();
        if (nm != null)
        {
            nm.player1SpawnPoint = p1Spawn.transform;
            nm.player2SpawnPoint = p2Spawn.transform;
        }

        // ── Dim the directional light (it makes 2D look weird) ─
        var dirLight = GameObject.Find("Directional Light");
        if (dirLight != null) Object.DestroyImmediate(dirLight);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[VisualOverhaul_v3] GameArena rebuilt with buildings + city skyline");
    }

    // ══════════════════════════════════════════════════════════
    //  BUILDING WITH WINDOWS
    // ══════════════════════════════════════════════════════════
    static void BuildBuilding(string name, float x, float y, float w, float h,
                              Color wallColor, Color trimColor)
    {
        // Body
        var body = MakeWorldSprite(name, new Vector3(x, y, 0),
            new Vector3(w, h, 1), wallColor, 1);

        // Top trim (cornice)
        MakeChild(body.transform,"Trim_Top",
            new Vector3(0, 0.48f, 0), new Vector3(1.05f, 0.06f, 1),
            trimColor, 2);

        // Window grid 3×3
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                float lx = (c - 1) * 0.28f;
                float ly = (r - 1) * 0.25f + 0.05f;
                var win = MakeChild(body.transform, "Win_" + r + "_" + c,
                    new Vector3(lx, ly, -0.05f),
                    new Vector3(0.13f, 0.18f, 1),
                    new Color(0.55f, 0.82f, 0.96f, 1f), 3);
                // Window frame (darker outline behind)
                MakeChild(body.transform, "WinFrame_" + r + "_" + c,
                    new Vector3(lx, ly, -0.04f),
                    new Vector3(0.16f, 0.21f, 1),
                    trimColor, 2);
            }
    }

    // ══════════════════════════════════════════════════════════
    //  BACKGROUND SILHOUETTE
    // ══════════════════════════════════════════════════════════
    static void AddBgSilhouette(Transform parent, float x, float h, Color color)
    {
        float w = Random.Range(1.2f, 2.4f);
        var bldg = new GameObject("BgB");
        bldg.transform.SetParent(parent, false);
        bldg.transform.position   = new Vector3(x, -3f + h * 0.5f, 5);
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
            Debug.Log("[VisualOverhaul_v3] ✅ APK rebuilt → " + outputPath);
            EditorUtility.RevealInFinder(outputPath);
        }
        else
            Debug.LogError("[VisualOverhaul_v3] ❌ Build failed: " + report.summary.result);
    }
}
#endif
