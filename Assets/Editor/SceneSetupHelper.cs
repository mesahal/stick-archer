#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

// Unity Editor menu: Tools → Stick Archers → Setup GameArena Scene
// Run this once after opening the project to scaffold the arena scene hierarchy
public static class SceneSetupHelper
{
    [MenuItem("Tools/Stick Archers/Setup GameArena Scene")]
    static void SetupGameArenaScene()
    {
        // --- Camera ---
        Camera.main.orthographicSize = 6f;
        Camera.main.transform.position = new Vector3(0, 2, -10);

        // --- Ground platform ---
        GameObject ground = CreateSprite("Ground", new Vector3(0, -3.5f, 0), new Vector3(16, 1, 1), Color.gray);
        ground.AddComponent<BoxCollider2D>();

        // --- Left platform ---
        GameObject leftPlat = CreateSprite("Platform_Left", new Vector3(-4f, -1f, 0), new Vector3(4, 0.5f, 1), Color.gray);
        leftPlat.AddComponent<BoxCollider2D>();

        // --- Right platform ---
        GameObject rightPlat = CreateSprite("Platform_Right", new Vector3(4f, -1f, 0), new Vector3(4, 0.5f, 1), Color.gray);
        rightPlat.AddComponent<BoxCollider2D>();

        // --- Spawn Points ---
        GameObject p1Spawn = new GameObject("Player1Spawn");
        p1Spawn.transform.position = new Vector3(-5f, -1.5f, 0);
        GameObject p2Spawn = new GameObject("Player2Spawn");
        p2Spawn.transform.position = new Vector3(5f, -1.5f, 0);

        // --- NetworkManager ---
        GameObject nm = new GameObject("NetworkManager");
        var netMgr = nm.AddComponent<NetworkManager>();
        netMgr.player1SpawnPoint = p1Spawn.transform;
        netMgr.player2SpawnPoint = p2Spawn.transform;

        // --- GameManager ---
        GameObject gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();
        gm.AddComponent<Photon.Pun.PhotonView>();

        // --- ArenaManager ---
        GameObject am = new GameObject("ArenaManager");
        am.AddComponent<ArenaManager>();
        am.AddComponent<Photon.Pun.PhotonView>();

        // --- AudioManager ---
        GameObject audio = new GameObject("AudioManager");
        audio.AddComponent<AudioManager>();

        // --- GameArenaBootstrap (spawns local player after scene loads) ---
        GameObject bootstrap = new GameObject("GameArenaBootstrap");
        bootstrap.AddComponent<GameArenaBootstrap>();

        // --- Canvas / HUD ---
        GameObject canvas = new GameObject("Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var ui = canvas.AddComponent<UIManager>();

        // Score display
        GameObject scorePanel = CreateUIPanel(canvas.transform, "ScorePanel",
            new Vector2(0, 0.9f), new Vector2(1, 1f));
        ui.player1ScoreText = CreateTMPText(scorePanel.transform, "P1Score", "0", TextAnchor.MiddleLeft);
        ui.player2ScoreText = CreateTMPText(scorePanel.transform, "P2Score", "0", TextAnchor.MiddleRight);

        Debug.Log("[SceneSetupHelper] GameArena scene scaffolded. Assign prefabs and wire remaining UI references.");
        EditorUtility.DisplayDialog("Setup Complete",
            "GameArena scene scaffolded!\n\nNext: Assign Archer and Arrow prefabs to NetworkManager, then wire UI panels.", "OK");
    }

    static GameObject CreateSprite(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.color = color;
        return go;
    }

    static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, TextAnchor anchor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 48;
        tmp.alignment = anchor == TextAnchor.MiddleLeft
            ? TextAlignmentOptions.Left
            : TextAlignmentOptions.Right;
        return tmp;
    }
}
#endif
