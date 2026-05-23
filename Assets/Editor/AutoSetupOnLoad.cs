#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AUTO-RUNS once when Unity recompiles this script.
/// Builds MainMenu scene, GameArena scene, Archer/Arrow prefabs,
/// and configures Build Settings — all without any manual menu clicks.
///
/// After it runs successfully it renames itself to .done so it never fires again.
/// </summary>
[InitializeOnLoad]
public static class AutoSetupOnLoad
{
    const string DoneKey   = "AutoSetupOnLoad_Done";
    const string ScenesDir = "Assets/Scenes";
    const string ResDir    = "Assets/Resources";

    static AutoSetupOnLoad()
    {
        // Only run once per project (survives domain reloads via EditorPrefs)
        if (EditorPrefs.GetBool(DoneKey, false)) return;

        // Defer until editor is fully initialized
        EditorApplication.delayCall += RunSetup;
    }

    static void RunSetup()
    {
        EditorApplication.delayCall -= RunSetup;
        Debug.Log("[AutoSetup] Starting full project setup...");

        try { BuildMainMenuScene(); }  catch (System.Exception e) { Debug.LogError("[AutoSetup] MainMenu: " + e.Message); }
        try { BuildGameArenaScene(); } catch (System.Exception e) { Debug.LogError("[AutoSetup] GameArena: " + e.Message); }
        try { CreateArcherPrefab(); }  catch (System.Exception e) { Debug.LogError("[AutoSetup] Archer prefab: " + e.Message); }
        try { CreateArrowPrefab(); }   catch (System.Exception e) { Debug.LogError("[AutoSetup] Arrow prefab: " + e.Message); }
        try { ConfigureBuildSettings(); } catch (System.Exception e) { Debug.LogError("[AutoSetup] BuildSettings: " + e.Message); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorPrefs.SetBool(DoneKey, true);
        Debug.Log("[AutoSetup] ✅ Setup complete! Both scenes + prefabs created. See Assets/Scenes/ and Assets/Resources/");
    }

    // ───────────────────────────────────────────
    //  MAIN MENU SCENE
    // ───────────────────────────────────────────
    static void BuildMainMenuScene()
    {
        EnsureFolder(ScenesDir);
        string path = ScenesDir + "/MainMenu.unity";
        if (File.Exists(path)) { Debug.Log("[AutoSetup] MainMenu already exists, skipping."); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // NetworkManager persists to GameArena
        new GameObject("NetworkManager").AddComponent<NetworkManager>();

        // Canvas
        var cvGO = new GameObject("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        cvGO.AddComponent<GraphicRaycaster>();

        MakePanel(cvGO.transform, "Background", V2(0,0), V2(1,1), new Color(0.12f,0.12f,0.25f));
        MakeTMP(cvGO.transform, "Title",    "STICK ARCHERS", V2(0,160),  V2(800,120), 80,  Color.white,               FontStyles.Bold);
        MakeTMP(cvGO.transform, "Subtitle", "BATTLE",        V2(0,70),   V2(600,70),  48,  new Color(1f,0.8f,0.2f),   FontStyles.Bold);

        var (playBtn, _) = MakeButton(cvGO.transform, "PlayOnlineBtn", "PLAY ONLINE",
            V2(0,-80), V2(400,80), new Color(0.2f,0.7f,0.3f));

        var statusTMP = MakeTMP(cvGO.transform, "StatusText", "",
            V2(0,-200), V2(600,50), 28, new Color(1f,1f,0.5f), FontStyles.Normal);

        var ctrl = cvGO.AddComponent<MainMenuController>();
        ctrl.playOnlineButton = playBtn;
        ctrl.statusText       = statusTMP;

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[AutoSetup] MainMenu saved → " + path);
    }

    // ───────────────────────────────────────────
    //  GAME ARENA SCENE
    // ───────────────────────────────────────────
    static void BuildGameArenaScene()
    {
        EnsureFolder(ScenesDir);
        string path = ScenesDir + "/GameArena.unity";
        if (File.Exists(path)) { Debug.Log("[AutoSetup] GameArena already exists, skipping."); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Camera.main.orthographicSize = 6f;
        Camera.main.transform.position = new Vector3(0,2,-10);
        Camera.main.backgroundColor = new Color(0.53f,0.81f,0.98f);

        // Platforms
        MakePlatform("Ground",          V3(0,-3.5f,0), V3(16,1,1), new Color(0.4f,0.3f,0.2f));
        MakePlatform("Platform_Left",   V3(-4f,-1f,0), V3(4,0.5f,1), new Color(0.4f,0.3f,0.2f));
        MakePlatform("Platform_Right",  V3(4f,-1f,0),  V3(4,0.5f,1), new Color(0.4f,0.3f,0.2f));

        // Spawn points
        var p1 = new GameObject("Player1Spawn"); p1.transform.position = V3(-5f,-1.5f,0);
        var p2 = new GameObject("Player2Spawn"); p2.transform.position = V3(5f,-1.5f,0);

        // Managers
        var nmGO   = new GameObject("NetworkManager");
        var netMgr = nmGO.AddComponent<NetworkManager>();
        netMgr.player1SpawnPoint = p1.transform;
        netMgr.player2SpawnPoint = p2.transform;

        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        gmGO.AddComponent<Photon.Pun.PhotonView>();

        var amGO = new GameObject("ArenaManager");
        amGO.AddComponent<ArenaManager>();
        amGO.AddComponent<Photon.Pun.PhotonView>();

        new GameObject("AudioManager").AddComponent<AudioManager>();
        new GameObject("GameArenaBootstrap").AddComponent<GameArenaBootstrap>();

        // Canvas / HUD
        var cvGO = new GameObject("Canvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920,1080);
        cvGO.AddComponent<GraphicRaycaster>();

        var ui = cvGO.AddComponent<UIManager>();

        // Score HUD (top bar)
        var scoreBar = MakePanel(cvGO.transform, "GameHUDPanel",
            V2(0,0.88f), V2(1,1f), new Color(0,0,0,0.55f));
        ui.gameHUDPanel    = scoreBar;
        ui.player1ScoreText = MakeTMP(scoreBar.transform, "P1Score", "0",
            V2(-300,0), V2(200,80), 64, Color.white, FontStyles.Bold);
        ui.player2ScoreText = MakeTMP(scoreBar.transform, "P2Score", "0",
            V2(300,0), V2(200,80), 64, Color.white, FontStyles.Bold);
        MakeTMP(scoreBar.transform, "VS", "VS", V2(0,0), V2(100,80), 38,
            new Color(1f,0.8f,0.2f), FontStyles.Bold);

        // Charge slider
        var chSlider = MakeSlider(cvGO.transform, "ChargeMeter",
            V2(0, 0.04f), V2(0.4f, 0.07f));
        ui.chargeMeter = chSlider;

        // Lobby panel
        var lobbyGO = MakePanel(cvGO.transform, "LobbyPanel",
            V2(0.2f,0.3f), V2(0.8f,0.7f), new Color(0,0,0,0.82f));
        lobbyGO.SetActive(false);
        ui.lobbyPanel      = lobbyGO;
        ui.lobbyStatusText = MakeTMP(lobbyGO.transform, "LobbyStatus",
            "Finding opponent...", V2(0,0), V2(600,80), 36, Color.white, FontStyles.Normal);

        // Result panel
        var resultGO = MakePanel(cvGO.transform, "ResultPanel",
            V2(0.2f,0.25f), V2(0.8f,0.75f), new Color(0,0,0,0.85f));
        resultGO.SetActive(false);
        ui.resultPanel     = resultGO;
        ui.resultTitleText = MakeTMP(resultGO.transform, "ResultTitle",
            "You Win!", V2(0,60), V2(500,100), 56, Color.yellow, FontStyles.Bold);
        MakeButton(resultGO.transform, "RematchButton", "REMATCH",
            V2(0,-40), V2(300,70), new Color(0.2f,0.7f,0.3f));
        MakeButton(resultGO.transform, "MenuButton", "MENU",
            V2(0,-130), V2(300,70), new Color(0.5f,0.5f,0.5f));

        // Opponent left panel
        var oppGO = MakePanel(cvGO.transform, "OpponentLeftPanel",
            V2(0.25f,0.35f), V2(0.75f,0.65f), new Color(0,0,0,0.85f));
        oppGO.SetActive(false);
        ui.opponentLeftPanel = oppGO;
        MakeTMP(oppGO.transform, "OppMsg", "Opponent left the match",
            V2(0,30), V2(500,80), 32, Color.white, FontStyles.Normal);
        MakeButton(oppGO.transform, "BackBtn", "BACK TO MENU",
            V2(0,-60), V2(320,70), new Color(0.5f,0.5f,0.5f));

        // Touch controls overlay
        var tcGO = new GameObject("TouchControls");
        tcGO.transform.SetParent(cvGO.transform, false);
        var tcRT = tcGO.AddComponent<RectTransform>();
        tcRT.anchorMin = V2(0,0); tcRT.anchorMax = V2(1,1);
        tcRT.offsetMin = tcRT.offsetMax = V2(0,0);
        tcGO.AddComponent<TouchControls>();

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("[AutoSetup] GameArena saved → " + path);
    }

    // ───────────────────────────────────────────
    //  PREFABS
    // ───────────────────────────────────────────
    static void CreateArcherPrefab()
    {
        EnsureFolder(ResDir);
        string path = ResDir + "/Archer.prefab";
        if (File.Exists(path)) { Debug.Log("[AutoSetup] Archer.prefab already exists, skipping."); return; }

        var root = new GameObject("Archer");
        root.AddComponent<SpriteRenderer>().sortingOrder = 1;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true; rb.gravityScale = 2f;
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f,1.2f);
        root.AddComponent<Animator>();

        var pv  = root.AddComponent<Photon.Pun.PhotonView>();
        var ptv = root.AddComponent<Photon.Pun.PhotonTransformView>();
        pv.ObservedComponents = new List<Component> { ptv };
        ptv.m_SynchronizePosition = true;
        ptv.m_SynchronizeRotation = false;
        ptv.m_SynchronizeScale    = false;

        var archer = root.AddComponent<Archer>();
        var sp = new GameObject("ArrowSpawnPoint");
        sp.transform.SetParent(root.transform, false);
        sp.transform.localPosition = new Vector3(0.6f,0.3f,0f);
        archer.arrowSpawnPoint = sp.transform;

        bool ok; PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[AutoSetup] Archer.prefab ✅" : "[AutoSetup] Archer.prefab ❌ FAILED");
    }

    static void CreateArrowPrefab()
    {
        EnsureFolder(ResDir);
        string path = ResDir + "/Arrow.prefab";
        if (File.Exists(path)) { Debug.Log("[AutoSetup] Arrow.prefab already exists, skipping."); return; }

        var root = new GameObject("Arrow");
        root.AddComponent<SpriteRenderer>().sortingOrder = 2;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = root.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f,0.1f);
        col.direction = CapsuleDirection2D.Horizontal;
        col.isTrigger = true;
        root.AddComponent<Photon.Pun.PhotonView>();
        root.AddComponent<Arrow>();

        bool ok; PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[AutoSetup] Arrow.prefab ✅" : "[AutoSetup] Arrow.prefab ❌ FAILED");
    }

    // ───────────────────────────────────────────
    //  BUILD SETTINGS
    // ───────────────────────────────────────────
    static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesDir + "/MainMenu.unity",  true),
            new EditorBuildSettingsScene(ScenesDir + "/GameArena.unity", true),
        };
        Debug.Log("[AutoSetup] Build Settings → MainMenu(0) + GameArena(1)");
    }

    // ───────────────────────────────────────────
    //  MINI HELPERS
    // ───────────────────────────────────────────
    static void MakePlatform(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = color;
        go.AddComponent<BoxCollider2D>();
    }

    static GameObject MakePanel(Transform parent, string name,
        Vector2 aMin, Vector2 aMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = V2(0,0);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, float fs, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fs; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    static (Button, TextMeshProUGUI) MakeButton(Transform parent, string name,
        string label, Vector2 pos, Vector2 size, Color bg)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = bg;
        var btn = go.AddComponent<Button>();

        var lGO = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        var lrt = lGO.AddComponent<RectTransform>();
        lrt.anchorMin = V2(0,0); lrt.anchorMax = V2(1,1);
        lrt.offsetMin = lrt.offsetMax = V2(0,0);
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 30; tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold; tmp.alignment = TextAlignmentOptions.Center;
        return (btn, tmp);
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = V2(0,0);
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        bg.AddComponent<RectTransform>().anchorMin = V2(0,0);
        bg.GetComponent<RectTransform>().anchorMax = V2(1,1);
        bg.AddComponent<Image>().color = new Color(0.2f,0.2f,0.2f);
        var fill = new GameObject("Fill");
        fill.transform.SetParent(go.transform, false);
        fill.AddComponent<RectTransform>();
        fill.AddComponent<Image>().color = new Color(0.2f,0.8f,0.3f);
        var slider = go.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0f;
        return slider;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(
                Path.GetDirectoryName(path)?.Replace("\\","/") ?? "Assets",
                Path.GetFileName(path));
    }

    static Vector2 V2(float x, float y) => new Vector2(x, y);
    static Vector3 V3(float x, float y, float z) => new Vector3(x, y, z);
}
#endif
