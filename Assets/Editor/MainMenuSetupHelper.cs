#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unity Editor menu: Tools → Stick Archers → Setup MainMenu Scene
/// Creates a ready-to-use MainMenu scene with NetworkManager + Play Online button.
/// </summary>
public static class MainMenuSetupHelper
{
    [MenuItem("Tools/Stick Archers/Setup MainMenu Scene")]
    static void SetupMainMenuScene()
    {
        // Make sure we save the current scene first
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Create or open MainMenu scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Camera
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;

        // NetworkManager (persists across scenes)
        GameObject nm = new GameObject("NetworkManager");
        nm.AddComponent<NetworkManager>();

        // Canvas
        GameObject canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background panel
        GameObject bg = CreatePanel(canvasGO.transform, "Background",
            Vector2.zero, Vector2.one, new Color(0.12f, 0.12f, 0.25f));

        // Title text
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0, 150);
        titleRT.sizeDelta = new Vector2(800, 120);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "STICK ARCHERS";
        titleTMP.fontSize = 80;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;
        titleTMP.fontStyle = FontStyles.Bold;

        // Sub-title
        GameObject subGO = new GameObject("SubTitleText");
        subGO.transform.SetParent(canvasGO.transform, false);
        var subRT = subGO.AddComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0, 70);
        subRT.sizeDelta = new Vector2(600, 60);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "BATTLE";
        subTMP.fontSize = 48;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(1f, 0.8f, 0.2f);

        // Play Online Button
        GameObject btnGO = new GameObject("PlayOnlineButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchoredPosition = new Vector2(0, -80);
        btnRT.sizeDelta = new Vector2(400, 80);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f);
        var btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.9f, 0.4f);
        cb.pressedColor = new Color(0.1f, 0.5f, 0.2f);
        btn.colors = cb;

        // Button label
        GameObject btnLabelGO = new GameObject("Label");
        btnLabelGO.transform.SetParent(btnGO.transform, false);
        var lblRT = btnLabelGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP = btnLabelGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text = "PLAY ONLINE";
        lblTMP.fontSize = 36;
        lblTMP.alignment = TextAlignmentOptions.Center;
        lblTMP.color = Color.white;
        lblTMP.fontStyle = FontStyles.Bold;

        // Status text
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(canvasGO.transform, false);
        var statusRT = statusGO.AddComponent<RectTransform>();
        statusRT.anchoredPosition = new Vector2(0, -200);
        statusRT.sizeDelta = new Vector2(600, 50);
        var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text = "";
        statusTMP.fontSize = 28;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = new Color(1f, 1f, 0.5f);

        // MainMenuController
        var ctrl = canvasGO.AddComponent<MainMenuController>();
        ctrl.playOnlineButton = btn;
        ctrl.statusText = statusTMP;

        // Save scene
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log("[MainMenuSetupHelper] MainMenu scene created at " + scenePath);
        EditorUtility.DisplayDialog("MainMenu Created",
            "MainMenu scene saved to Assets/Scenes/MainMenu.unity\n\n" +
            "Add both scenes to Build Settings:\n" +
            "File → Build Settings → Add Open Scenes\n" +
            "(MainMenu = index 0, GameArena = index 1)", "OK");
    }

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }
}
#endif
