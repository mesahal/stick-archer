#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Headless entry-point for batch-mode scene/prefab creation.
/// Run via:
///   Unity -batchmode -quit -projectPath <path> -executeMethod BatchSetup.RunAll -logFile /tmp/unity_batch.log
/// </summary>
public static class BatchSetup
{
    static readonly string ScenesDir   = "Assets/Scenes";
    static readonly string ResourceDir = "Assets/Resources";

    // ----------------------------------------------------------------
    //  MAIN ENTRY POINT
    // ----------------------------------------------------------------
    public static void RunAll()
    {
        Debug.Log("=== BatchSetup.RunAll started ===");

        try { BuildMainMenuScene(); }
        catch (System.Exception e) { Debug.LogError("MainMenu failed: " + e); }

        try { BuildGameArenaScene(); }
        catch (System.Exception e) { Debug.LogError("GameArena failed: " + e); }

        try { CreatePrefabs(); }
        catch (System.Exception e) { Debug.LogError("Prefabs failed: " + e); }

        try { ConfigureBuildSettings(); }
        catch (System.Exception e) { Debug.LogError("BuildSettings failed: " + e); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("=== BatchSetup.RunAll complete ===");
    }

    // ----------------------------------------------------------------
    //  SCENE: MainMenu
    // ----------------------------------------------------------------
    static void BuildMainMenuScene()
    {
        EnsureFolder(ScenesDir);
        string scenePath = ScenesDir + "/MainMenu.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // NetworkManager (DontDestroyOnLoad — persists to GameArena)
        var nm = new GameObject("NetworkManager");
        nm.AddComponent<NetworkManager>();

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler   = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background
        CreatePanel(canvasGO.transform, "Background",
            Vector2.zero, Vector2.one, new Color(0.12f, 0.12f, 0.25f));

        // Title
        CreateTMPLabel(canvasGO.transform, "TitleText", "STICK ARCHERS",
            new Vector2(0, 150), new Vector2(800, 120), 80, Color.white, FontStyles.Bold);

        CreateTMPLabel(canvasGO.transform, "SubTitle", "BATTLE",
            new Vector2(0, 60), new Vector2(600, 70), 48,
            new Color(1f, 0.8f, 0.2f), FontStyles.Bold);

        // Play Online button
        var (btn, _) = CreateButton(canvasGO.transform, "PlayOnlineButton",
            "PLAY ONLINE", new Vector2(0, -80), new Vector2(400, 80),
            new Color(0.2f, 0.7f, 0.3f));

        // Status text
        var statusTMP = CreateTMPLabel(canvasGO.transform, "StatusText", "",
            new Vector2(0, -200), new Vector2(600, 50), 28,
            new Color(1f, 1f, 0.5f), FontStyles.Normal);

        // MainMenuController
        var ctrl = canvasGO.AddComponent<MainMenuController>();
        ctrl.playOnlineButton = btn;
        ctrl.statusText       = statusTMP;

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[BatchSetup] MainMenu scene saved → " + scenePath);
    }

    // ----------------------------------------------------------------
    //  SCENE: GameArena
    // ----------------------------------------------------------------
    static void BuildGameArenaScene()
    {
        EnsureFolder(ScenesDir);
        string scenePath = ScenesDir + "/GameArena.unity";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera
        Camera.main.orthographicSize = 6f;
        Camera.main.transform.position = new Vector3(0, 2, -10);
        Camera.main.backgroundColor = new Color(0.53f, 0.81f, 0.98f);

        // Ground
        var ground = CreateSpriteObj("Ground", new Vector3(0, -3.5f, 0),
            new Vector3(16, 1, 1), new Color(0.4f, 0.3f, 0.2f));
        ground.AddComponent<BoxCollider2D>();

        // Platforms
        var lp = CreateSpriteObj("Platform_Left", new Vector3(-4f, -1f, 0),
            new Vector3(4, 0.5f, 1), new Color(0.4f, 0.3f, 0.2f));
        lp.AddComponent<BoxCollider2D>();

        var rp = CreateSpriteObj("Platform_Right", new Vector3(4f, -1f, 0),
            new Vector3(4, 0.5f, 1), new Color(0.4f, 0.3f, 0.2f));
        rp.AddComponent<BoxCollider2D>();

        // Spawn points
        var p1Spawn = new GameObject("Player1Spawn");
        p1Spawn.transform.position = new Vector3(-5f, -1.5f, 0);
        var p2Spawn = new GameObject("Player2Spawn");
        p2Spawn.transform.position = new Vector3(5f, -1.5f, 0);

        // NetworkManager
        var nmGO    = new GameObject("NetworkManager");
        var netMgr  = nmGO.AddComponent<NetworkManager>();
        netMgr.player1SpawnPoint = p1Spawn.transform;
        netMgr.player2SpawnPoint = p2Spawn.transform;

        // GameManager
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        gmGO.AddComponent<Photon.Pun.PhotonView>();

        // ArenaManager
        var amGO = new GameObject("ArenaManager");
        amGO.AddComponent<ArenaManager>();
        amGO.AddComponent<Photon.Pun.PhotonView>();

        // AudioManager
        var audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();

        // GameArenaBootstrap
        var bootGO = new GameObject("GameArenaBootstrap");
        bootGO.AddComponent<GameArenaBootstrap>();

        // Canvas / HUD
        var canvasGO = new GameObject("Canvas");
        var c        = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler   = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var ui = canvasGO.AddComponent<UIManager>();

        // Score panel (top bar)
        var scorePanel = CreatePanel(canvasGO.transform, "ScorePanel",
            new Vector2(0, 0.88f), new Vector2(1, 1f), new Color(0, 0, 0, 0.5f));

        ui.player1ScoreText = CreateTMPLabel(scorePanel.transform, "P1Score", "0",
            new Vector2(-300, 0), new Vector2(200, 80), 64, Color.white, FontStyles.Bold);
        ui.player2ScoreText = CreateTMPLabel(scorePanel.transform, "P2Score", "0",
            new Vector2(300, 0), new Vector2(200, 80), 64, Color.white, FontStyles.Bold);

        // VS label
        CreateTMPLabel(scorePanel.transform, "VSLabel", "VS",
            new Vector2(0, 0), new Vector2(100, 80), 40,
            new Color(1f, 0.8f, 0.2f), FontStyles.Bold);

        // Charge meter (bottom center)
        var chargePanelGO = CreatePanel(canvasGO.transform, "ChargeMeterPanel",
            new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), new Color(0, 0, 0, 0.4f));
        var slider = chargePanelGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 0f;
        ui.chargeMeter  = slider;

        // Lobby panel
        var lobbyGO = CreatePanel(canvasGO.transform, "LobbyPanel",
            new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.7f), new Color(0, 0, 0, 0.8f));
        lobbyGO.SetActive(false);
        ui.lobbyPanel = lobbyGO;
        ui.lobbyStatusText = CreateTMPLabel(lobbyGO.transform, "LobbyStatus",
            "Finding opponent...", Vector2.zero, new Vector2(600, 80), 36,
            Color.white, FontStyles.Normal);

        // Result panel
        var resultGO = CreatePanel(canvasGO.transform, "ResultPanel",
            new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.75f), new Color(0, 0, 0, 0.85f));
        resultGO.SetActive(false);
        ui.resultPanel = resultGO;
        ui.resultTitleText = CreateTMPLabel(resultGO.transform, "ResultTitle",
            "You Win!", new Vector2(0, 60), new Vector2(500, 100), 56,
            Color.yellow, FontStyles.Bold);
        var (rematchBtn, _) = CreateButton(resultGO.transform, "RematchButton",
            "REMATCH", new Vector2(0, -40), new Vector2(300, 70),
            new Color(0.2f, 0.7f, 0.3f));
        var (menuBtn, _) = CreateButton(resultGO.transform, "MenuButton",
            "MENU", new Vector2(0, -130), new Vector2(300, 70),
            new Color(0.5f, 0.5f, 0.5f));

        // Opponent left panel
        var oppGO = CreatePanel(canvasGO.transform, "OpponentLeftPanel",
            new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.65f), new Color(0, 0, 0, 0.85f));
        oppGO.SetActive(false);
        ui.opponentLeftPanel = oppGO;
        CreateTMPLabel(oppGO.transform, "OppLeftMsg", "Opponent left the match",
            new Vector2(0, 30), new Vector2(500, 80), 32, Color.white, FontStyles.Normal);
        var (backBtn, _) = CreateButton(oppGO.transform, "BackToMenuBtn",
            "BACK TO MENU", new Vector2(0, -60), new Vector2(320, 70),
            new Color(0.5f, 0.5f, 0.5f));

        // GameHUD panel reference (the score panel serves as the HUD)
        ui.gameHUDPanel = scorePanel;

        // Touch controls
        var touchGO = new GameObject("TouchControls");
        touchGO.transform.SetParent(canvasGO.transform, false);
        var touchRT = touchGO.AddComponent<RectTransform>();
        touchRT.anchorMin = Vector2.zero;
        touchRT.anchorMax = Vector2.one;
        touchRT.offsetMin = touchRT.offsetMax = Vector2.zero;
        touchGO.AddComponent<TouchControls>();

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[BatchSetup] GameArena scene saved → " + scenePath);
    }

    // ----------------------------------------------------------------
    //  PREFABS
    // ----------------------------------------------------------------
    static void CreatePrefabs()
    {
        EnsureFolder(ResourceDir);
        CreateArcherPrefab();
        CreateArrowPrefab();
    }

    static void CreateArcherPrefab()
    {
        string path = ResourceDir + "/Archer.prefab";
        var root = new GameObject("Archer");

        root.AddComponent<SpriteRenderer>().sortingOrder = 1;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale   = 2f;

        var col  = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.2f);

        root.AddComponent<Animator>();

        var pv  = root.AddComponent<Photon.Pun.PhotonView>();
        var ptv = root.AddComponent<Photon.Pun.PhotonTransformView>();
        pv.ObservedComponents = new List<Component> { ptv };
        ptv.m_SynchronizePosition = true;
        ptv.m_SynchronizeRotation = false;
        ptv.m_SynchronizeScale    = false;

        var archerScript = root.AddComponent<Archer>();

        var spawnPt = new GameObject("ArrowSpawnPoint");
        spawnPt.transform.SetParent(root.transform, false);
        spawnPt.transform.localPosition = new Vector3(0.6f, 0.3f, 0f);
        archerScript.arrowSpawnPoint = spawnPt.transform;

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[BatchSetup] Archer.prefab created" : "[BatchSetup] Archer.prefab FAILED");
    }

    static void CreateArrowPrefab()
    {
        string path = ResourceDir + "/Arrow.prefab";
        var root = new GameObject("Arrow");

        root.AddComponent<SpriteRenderer>().sortingOrder = 2;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale           = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col       = root.AddComponent<CapsuleCollider2D>();
        col.size      = new Vector2(0.5f, 0.1f);
        col.direction = CapsuleDirection2D.Horizontal;
        col.isTrigger = true;

        root.AddComponent<Photon.Pun.PhotonView>();
        root.AddComponent<Arrow>();

        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        Object.DestroyImmediate(root);
        Debug.Log(ok ? "[BatchSetup] Arrow.prefab created" : "[BatchSetup] Arrow.prefab FAILED");
    }

    // ----------------------------------------------------------------
    //  BUILD SETTINGS
    // ----------------------------------------------------------------
    static void ConfigureBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene(ScenesDir + "/MainMenu.unity",  true),
            new EditorBuildSettingsScene(ScenesDir + "/GameArena.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[BatchSetup] Build Settings configured: MainMenu(0) + GameArena(1)");
    }

    // ----------------------------------------------------------------
    //  HELPERS
    // ----------------------------------------------------------------
    static GameObject CreateSpriteObj(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = new GameObject(name);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color  = color;
        return go;
    }

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.offsetMin   = rt.offsetMax = Vector2.zero;
        var img        = go.AddComponent<Image>();
        img.color      = color;
        return go;
    }

    static TextMeshProUGUI CreateTMPLabel(Transform parent, string name, string text,
        Vector2 anchoredPos, Vector2 sizeDelta, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt             = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        var tmp             = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.color           = color;
        tmp.fontStyle       = style;
        tmp.alignment       = TextAlignmentOptions.Center;
        return tmp;
    }

    static (Button btn, TextMeshProUGUI label) CreateButton(Transform parent, string name,
        string labelText, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt              = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        var img             = go.AddComponent<Image>();
        img.color           = bgColor;
        var btn             = go.AddComponent<Button>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var lrt         = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin   = Vector2.zero;
        lrt.anchorMax   = Vector2.one;
        lrt.offsetMin   = lrt.offsetMax = Vector2.zero;
        var tmp         = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text        = labelText;
        tmp.fontSize    = 30;
        tmp.color       = Color.white;
        tmp.fontStyle   = FontStyles.Bold;
        tmp.alignment   = TextAlignmentOptions.Center;

        return (btn, tmp);
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "Assets";
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
