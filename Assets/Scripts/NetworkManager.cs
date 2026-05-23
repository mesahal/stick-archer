using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [Header("Spawn Points (auto-found if null)")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;

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
        // CRITICAL: makes both clients auto-load the GameArena scene when master calls LoadLevel
        PhotonNetwork.AutomaticallySyncScene = true;
        UIManager.Instance?.ShowLobby("Connecting...");
        PhotonNetwork.GameVersion = "1.0";
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

    void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.LoadLevel("GameArena");
    }

    private bool _hasSpawnedLocal = false;

    /// <summary>Called by GameArenaBootstrap after the scene loads.</summary>
    public void SpawnLocalPlayer()
    {
        // Guard against double-spawn (could happen if scene loads twice)
        if (_hasSpawnedLocal) return;
        _hasSpawnedLocal = true;

        if (player1SpawnPoint == null || player2SpawnPoint == null)
            RefreshSpawnPoints();

        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        Vector3 spawnPos = actor == 1
            ? (player1SpawnPoint != null ? player1SpawnPoint.position : new Vector3(-3.5f, 1f, 0))
            : (player2SpawnPoint != null ? player2SpawnPoint.position : new Vector3( 3.5f, 1f, 0));

        // Send playerIndex via instantiation data so BOTH clients receive it
        GameObject archerObj = PhotonNetwork.Instantiate("Archer", spawnPos, Quaternion.identity,
            0, new object[] { actor });

        Archer archer = archerObj.GetComponent<Archer>();
        archer.spawnPosition = spawnPos;
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

    public void ReturnToMenu()
    {
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }
}
