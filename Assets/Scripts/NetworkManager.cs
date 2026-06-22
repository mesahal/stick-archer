using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [Header("Spawn Points (auto-found if null)")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;

    [Header("Bot Fallback")]
    [Tooltip("If no human opponent joins within this many seconds (or the connection fails), " +
             "start a match against the AI so Online is always playable, exactly like Computer mode.")]
    public float botFallbackSeconds = 6f;
    private bool _matchStarted = false;
    private Coroutine _botFallbackCo;

    void Awake()
    {
        // Singleton: destroy duplicate that gets created when GameArena scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Listen for scene loads so we can find spawn points after GameArena loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameArena")
            RefreshSpawnPoints();
    }

    /// <summary>Find spawn points by name in the current scene.</summary>
    void RefreshSpawnPoints()
    {
        var p1 = GameObject.Find("Player1Spawn");
        var p2 = GameObject.Find("Player2Spawn");
        if (p1 != null) player1SpawnPoint = p1.transform;
        if (p2 != null) player2SpawnPoint = p2.transform;
        Debug.Log("[NetworkManager] Spawn points refreshed.");
    }

    public void ConnectAndPlay()
    {
        _hasSpawnedLocal = false;
        suppressDisconnectError = false;
        _matchStarted = false;

        // Start the bot-fallback timer: if no real opponent is found (or we can't connect)
        // in time, drop into a Computer-mode match so Online is always playable.
        if (_botFallbackCo != null) StopCoroutine(_botFallbackCo);
        _botFallbackCo = StartCoroutine(BotFallbackTimer());

        // CRITICAL: makes both clients auto-load the GameArena scene when master calls LoadLevel
        PhotonNetwork.AutomaticallySyncScene = true;
        UIManager.Instance?.ShowLobby("Finding opponent...");
        PhotonNetwork.GameVersion = "1.0";

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
                StartGame();
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            UIManager.Instance?.ShowLobby("Finding opponent...");
            PhotonNetwork.JoinRandomRoom();
            return;
        }

        if (PhotonNetwork.IsConnected)
            return;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        UIManager.Instance?.ShowLobby("Finding opponent...");
        // Join ANY open room — if none exists, OnJoinRandomFailed will create one
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // No open room found — become the host and wait
        UIManager.Instance?.ShowLobby("Waiting for opponent...");
        RoomOptions options = new RoomOptions { MaxPlayers = 2, IsOpen = true, IsVisible = true };
        PhotonNetwork.CreateRoom(null, options); // null = auto-generated name
    }

    public override void OnJoinedRoom()
    {
        UIManager.Instance?.ShowLobby("Waiting for opponent...");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            StartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            StartGame();
    }

    // ── Bot fallback: keeps Online always playable (vs AI) when no human shows up ──

    IEnumerator BotFallbackTimer()
    {
        yield return new WaitForSecondsRealtime(botFallbackSeconds);
        if (!_matchStarted)
            StartBotMatch();
    }

    /// <summary>Tear down networking and start a single-player match vs the AI — identical
    /// to Computer mode (same arena, mechanics, feel).</summary>
    public void StartBotMatch()
    {
        if (_matchStarted) return;
        _matchStarted = true;
        if (_botFallbackCo != null) { StopCoroutine(_botFallbackCo); _botFallbackCo = null; }

        suppressDisconnectError = true;
        PhotonNetwork.AutomaticallySyncScene = false;
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        // Same flow as VS COMPUTER. (The lobby canvas lives in the MainMenu scene and is
        // destroyed by the scene load below.)
        GameMode.Current = GameMode.Mode.Practice;
        GameMode.Difficulty = GameMode.AIDifficulty.Normal; // fair bot
        SceneManager.LoadScene("GameArena");
    }

    void StartGame()
    {
        _matchStarted = true;
        if (_botFallbackCo != null) { StopCoroutine(_botFallbackCo); _botFallbackCo = null; }

        if (!PhotonNetwork.IsMasterClient) return;

        // Pick the initial arena layout/seed and stash on the room so the non-master
        // client builds the same buildings when the scene loads. Subsequent rebuilds
        // come through GameManager.RPC_RebuildArena.
        int type = Random.Range(0, 3);
        int seed = Random.Range(int.MinValue, int.MaxValue);
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { "_at", type },
            { "_as", seed }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        PhotonNetwork.LoadLevel("GameArena");
    }

    public int GetPlayerSlot(int actorNumber)
    {
        Player[] players = PhotonNetwork.PlayerList;
        if (players == null || players.Length == 0)
            return actorNumber == 2 ? 2 : 1;

        int firstActor = int.MaxValue;
        int secondActor = int.MaxValue;
        foreach (Player player in players)
        {
            if (player == null) continue;
            int actor = player.ActorNumber;
            if (actor < firstActor)
            {
                secondActor = firstActor;
                firstActor = actor;
            }
            else if (actor < secondActor)
            {
                secondActor = actor;
            }
        }

        return actorNumber == secondActor ? 2 : 1;
    }

    private bool _hasSpawnedLocal = false;
    private bool suppressDisconnectError = false;

    /// <summary>Called by GameArenaBootstrap after the scene loads.</summary>
    public void SpawnLocalPlayer()
    {
        // Guard against double-spawn (could happen if scene loads twice)
        if (_hasSpawnedLocal) return;
        _hasSpawnedLocal = true;

        if (player1SpawnPoint == null || player2SpawnPoint == null)
            RefreshSpawnPoints();

        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        int playerSlot = GetPlayerSlot(actor);
        int selectedCharacter = CharacterSelectUI.SelectedCharacter;
        Vector3 spawnPos = playerSlot == 1
            ? (player1SpawnPoint != null ? player1SpawnPoint.position : new Vector3(-3.5f, 1f, 0))
            : (player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3( 3.5f, 1f, 0));

        // Send playerIndex via instantiation data so BOTH clients receive it
        GameObject archerObj = PhotonNetwork.Instantiate("Archer", spawnPos, Quaternion.identity,
            0, new object[] { playerSlot, selectedCharacter });

        // Lift the archer so its feet sit on the spawn point, not floating above it.
        archerObj.transform.position = SpawnAlignment.AlignFeetTo(archerObj, spawnPos);

        Archer archer = archerObj.GetComponent<Archer>();
        archer.spawnPosition = spawnPos;
        archer.selectedCharacterIndex = selectedCharacter;
        // playerIndex is set via OnPhotonInstantiate on all clients
    }

    public override void OnLeftRoom()
    {
        _hasSpawnedLocal = false;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UIManager.Instance?.ShowOpponentLeft();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (suppressDisconnectError)
        {
            suppressDisconnectError = false;
            return;
        }

        // Couldn't connect / lost connection before a match started → fall back to a bot
        // match instead of an error, so Online is always playable.
        if (!_matchStarted)
        {
            StartBotMatch();
            return;
        }

        ErrorModalUI.Instance?.ShowConnectionError(cause.ToString());
    }

    public void ReturnToMenu()
    {
        suppressDisconnectError = true;
        _hasSpawnedLocal = false;

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        else if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

        SceneManager.LoadScene("MainMenu");
    }
}
