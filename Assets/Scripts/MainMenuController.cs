using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using StickArcher.Analytics;
using StickArcher.UI;

/// <summary>
/// Attached to the MainMenu scene's Canvas.
/// Handles button clicks, Photon connection status, and difficulty selection.
/// 
/// SETUP: In the Unity Editor, build the MainMenu UI visually:
///   1. Create a Canvas with CanvasScaler (Scale With Screen Size, 1920x1080, match 0.5)
///   2. Add title text, "PLAY ONLINE" button, "VS COMPUTER" button, difficulty dropdown
///   3. Drag-assign the references in the Inspector below
/// </summary>
public class MainMenuController : MonoBehaviourPunCallbacks
{
    [Header("UI References (assign in Inspector)")]
    [Tooltip("Button that starts online matchmaking")]
    public Button playOnlineButton;

    [Tooltip("Button that starts practice vs AI")]
    public Button practiceButton;

    [Tooltip("Text that shows connection status (Connecting... / Finding opponent...)")]
    public TextMeshProUGUI statusText;

    [Tooltip("Optional character select flow shown before Online or Practice starts.")]
    public CharacterSelectUI characterSelectUI;

    [Header("Practice Difficulty")]
    [Tooltip("Dropdown with options: Easy / Normal / Hard (in that order)")]
    public TMP_Dropdown difficultyDropdown;

    void Start()
    {
        // Wire button clicks
        if (playOnlineButton != null)
            playOnlineButton.onClick.AddListener(OnPlayOnlineClicked);

        if (practiceButton != null)
            practiceButton.onClick.AddListener(OnPracticeClicked);

        // Wire difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.value = (int)GameMode.Difficulty;
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }

        if (statusText != null)
            statusText.text = "";

        WireGearButton();
        EnsureProfileBadge();

        // Offer daily rewards on launch when something is claimable (design 12).
        DailyRewardsScreen.ShowOnLaunchIfAvailable();
    }

    /// <summary>
    /// Ensures a ProfileBadge exists (baked by v12 editor tool under Safe; runtime fallback for old scenes).
    /// </summary>
    void EnsureProfileBadge()
    {
        if (GetComponentInChildren<ProfileBadge>() != null) return;

        var safe = transform.Find("Safe");
        var parent = safe != null ? safe : (transform as RectTransform);
        if (parent == null) return;

        var go = new GameObject("ProfileBadge");
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        go.AddComponent<ProfileBadge>();
    }

    /// <summary>Wire the top-right gear to open the runtime settings overlay (the scene
    /// has a GearButton but no authored settings panel).</summary>
    void WireGearButton()
    {
        // Find the gear by name anywhere in the scene (it lives under Safe). It may be an
        // Image without a Button, so get-or-add the Button before wiring.
        GameObject gearGO = GameObject.Find("GearButton");
        if (gearGO == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "GearButton") { gearGO = t.gameObject; break; }
        }
        if (gearGO == null) return;

        // Make every graphic in the gear hierarchy catch clicks (the icon image often has
        // Raycast Target off, so taps land on nothing).
        foreach (var g in gearGO.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = true;

        // Wire any existing Button(s) in the hierarchy to open settings.
        bool wired = false;
        foreach (var b in gearGO.GetComponentsInChildren<Button>(true))
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => RuntimeSettings.Show());
            wired = true;
        }

        // None present — add one on the holder and give it a raycast graphic.
        if (!wired)
        {
            var btn = gearGO.AddComponent<Button>();
            var graphic = gearGO.GetComponent<Graphic>() ?? gearGO.GetComponentInChildren<Graphic>(true);
            if (graphic == null)
            {
                var img = gearGO.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                graphic = img;
            }
            btn.targetGraphic = graphic;
            btn.onClick.AddListener(() => RuntimeSettings.Show());
        }
    }

    // ── Button Callbacks ────────────────────────────────────────

    void OnPlayOnlineClicked()
    {
        GameMode.Current = GameMode.Mode.Online;
        Analytics.Log(GameEvents.MenuPlayOnline);

        if (characterSelectUI != null)
        {
            SetStatus("");
            characterSelectUI.ShowForMode(GameMode.Mode.Online);
            return;
        }

        // No authored character-select panel — show the runtime one (design 02).
        RuntimeCharacterSelect.Show(_ =>
        {
            if (playOnlineButton != null) playOnlineButton.interactable = false;
            UIManager.Instance?.BeginLobby(); // allow the lobby to show (clears prior cancel)
            SetStatus("Connecting...");
            NetworkManager.Instance?.ConnectAndPlay();
        });
    }

    void OnPracticeClicked()
    {
        GameMode.Current = GameMode.Mode.Practice;

        // Ask the player how tough the computer should be first, then continue into
        // character select / the match. The chosen difficulty drives the AIController.
        DifficultySelect.Show(diff =>
        {
            Analytics.Log(GameEvents.MenuPractice, EventParams.Difficulty, diff.ToString().ToLower());
            StartPracticeFlow();
        });
    }

    void StartPracticeFlow()
    {
        if (characterSelectUI != null)
        {
            SetStatus("");
            characterSelectUI.ShowForMode(GameMode.Mode.Practice);
            return;
        }

        // No authored character-select panel — show the runtime one (design 02).
        RuntimeCharacterSelect.Show(_ =>
        {
            if (practiceButton != null) practiceButton.interactable = false;
            SetStatus("Loading practice...");
            SceneManager.LoadScene("GameArena");
        });
    }

    void OnDifficultyChanged(int index)
    {
        GameMode.Difficulty = (GameMode.AIDifficulty)Mathf.Clamp(index, 0, 2);
        Analytics.Log(GameEvents.DifficultyChanged, EventParams.Difficulty, GameMode.Difficulty.ToString().ToLower());
    }

    // ── Photon Callbacks ────────────────────────────────────────

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

    // ── Helpers ─────────────────────────────────────────────────

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}
