using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Attached to the MainMenu scene's Canvas.
/// Handles Photon status updates directly so the status text
/// updates through the whole connection → matchmaking flow.
/// </summary>
public class MainMenuController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public Button playOnlineButton;
    public Button practiceButton;
    public TextMeshProUGUI statusText;

    [Header("Practice Difficulty")]
    [Tooltip("Optional. Dropdown options should be Easy / Normal / Hard in that order.")]
    public TMP_Dropdown difficultyDropdown;

    void Start()
    {
        // Auto-create missing buttons
        AutoCreateButtons();
        
        if (playOnlineButton != null)
            playOnlineButton.onClick.AddListener(OnPlayOnlineClicked);

        if (practiceButton != null)
            practiceButton.onClick.AddListener(OnPracticeClicked);

        if (difficultyDropdown != null)
        {
            // Restore last selection
            difficultyDropdown.value = (int)GameMode.Difficulty;
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }

        if (statusText != null)
            statusText.text = "";
            
        // Setup button animations
        var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<ButtonAnimator>() == null)
            canvas.gameObject.AddComponent<ButtonAnimator>();
    }

    void OnDifficultyChanged(int index)
    {
        GameMode.Difficulty = (GameMode.AIDifficulty)Mathf.Clamp(index, 0, 2);
    }
    
    void AutoCreateButtons()
    {
        var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        if (canvas == null) return;
        
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        
        // Create Online button if missing
        if (playOnlineButton == null)
        {
            GameObject btnObj = CreateButton(canvasRt, "PlayOnlineButton",
                "PLAY ONLINE", new Vector2(0, 90), new Color(0.2f, 0.9f, 0.4f));
            playOnlineButton = btnObj.GetComponent<Button>();
            Debug.Log("[MainMenuController] Auto-created Online button");
        }
        
        // Create Practice button if missing
        if (practiceButton == null)
        {
            GameObject btnObj = CreateButton(canvasRt, "PracticeButton",
                "VS COMPUTER", new Vector2(0, -90), new Color(0.3f, 0.7f, 1f));
            practiceButton = btnObj.GetComponent<Button>();
            Debug.Log("[MainMenuController] Auto-created VS Computer button");
        }
    }
    
    GameObject CreateButton(RectTransform parent, string name, string text, Vector2 pos, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(320, 80);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        img.sprite = CreateRoundedRectSprite();
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;
        
        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 5);
        textRt.offsetMax = new Vector2(-10, -5);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 32;
        tmp.fontStyle = TMPro.FontStyles.Bold;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        return btnObj;
    }
    
    Sprite CreateRoundedRectSprite()
    {
        // Create a simple white texture
        Texture2D tex = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    void OnPlayOnlineClicked()
    {
        if (playOnlineButton != null)
            playOnlineButton.interactable = false;

        GameMode.Current = GameMode.Mode.Online;
        SetStatus("Connecting...");
        NetworkManager.Instance?.ConnectAndPlay();
    }

    void OnPracticeClicked()
    {
        if (practiceButton != null)
            practiceButton.interactable = false;

        GameMode.Current = GameMode.Mode.Practice;
        SetStatus("Loading practice...");
        SceneManager.LoadScene("GameArena");
    }

    // ── Photon callbacks to update status text ──────────────────
    public override void OnConnectedToMaster()
    {
        SetStatus("Finding opponent...");
    }

    public override void OnJoinedRoom()
    {
        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        SetStatus(count >= 2 ? "Starting game..." : "Waiting for opponent...");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus("Starting game...");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SetStatus("Disconnected: " + cause);
        if (playOnlineButton != null)
            playOnlineButton.interactable = true;
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        SetStatus("Finding opponent...");
    }

    // ────────────────────────────────────────────────────────────
    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}
