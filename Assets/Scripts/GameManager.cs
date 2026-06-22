using UnityEngine;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using StickArcher.Analytics;
using StickArcher.Progression;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public int scoreToWin = 5;

    private int player1Score = 0;
    private int player2Score = 0;
    private Coroutine _rebuildCo;
    private bool _gameEnded = false;

    void Awake()
    {
        Instance = this;
        // Allow server-side balance tuning without a new build.
        scoreToWin = RemoteConfig.GetInt("score_to_win", scoreToWin);
    }

    /// <summary>Master client awards a kill point.</summary>
    public void RecordKill(int shooterActorNumber)
    {
        if (_gameEnded) return;
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("RPC_AddScore", RpcTarget.All, shooterActorNumber);
    }

    [PunRPC]
    public void RPC_AddScore(int shooterActorNumber)
    {
        if (_gameEnded) return;

        int shooterSlot = GetPlayerSlot(shooterActorNumber);
        int victimSlot = shooterSlot == 1 ? 2 : 1;
        
        if (shooterSlot == 1) player1Score++;
        else                  player2Score++;

        Analytics.KillRecorded(shooterSlot, victimSlot, player1Score, player2Score);

        // Tally only the local player's kills into lifetime stats.
        if (shooterSlot == GetPlayerSlot(PhotonNetwork.LocalPlayer.ActorNumber))
            ProfileManager.Instance?.RecordKill();

        UIManager.Instance?.UpdateScore(player1Score, player2Score);
        AudioManager.Instance?.PlayPointScored();
        
        // Visual effects
        CameraShaker.Instance?.ShakeKill();
        KillFeed.Instance?.ShowKill(shooterSlot, victimSlot);
        
        // Score pop animation
        if (UIManager.Instance != null)
        {
            var p1Text = UIManager.Instance.player1ScoreText?.GetComponent<RectTransform>();
            var p2Text = UIManager.Instance.player2ScoreText?.GetComponent<RectTransform>();
            if (shooterSlot == 1 && p1Text != null)
                ButtonAnimator.PopText(p1Text, 1.4f);
            else if (shooterSlot == 2 && p2Text != null)
                ButtonAnimator.PopText(p2Text, 1.4f);
        }

        if (player1Score >= scoreToWin || player2Score >= scoreToWin)
        {
            int winnerSlot = player1Score >= scoreToWin ? 1 : 2;
            EndMatch(winnerSlot);
        }
        else
        {
            // Master picks the next arena layout/seed and broadcasts to everyone so
            // both devices rebuild the same buildings.
            if (PhotonNetwork.IsMasterClient)
            {
                int type = Random.Range(0, 3);
                int seed = Random.Range(int.MinValue, int.MaxValue);
                photonView.RPC("RPC_RebuildArena", RpcTarget.AllBuffered, type, seed);
            }
        }
    }

    [PunRPC]
    public void RPC_RebuildArena(int type, int seed)
    {
        if (_gameEnded) return;
        if (_rebuildCo != null) StopCoroutine(_rebuildCo);
        _rebuildCo = StartCoroutine(RebuildAndRespawnRoutine(type, seed));
    }

    IEnumerator RebuildAndRespawnRoutine(int type, int seed)
    {
        // Detach archers so destroying buildings can't take them along
        if (_gameEnded) yield break;

        // Clear any lingering hit-stop / slow-mo so the round flow runs at full speed.
        // (A stuck timeScale would stretch/halt the wait below — the "hang after a kill".)
        if (Time.timeScale != 0f) Time.timeScale = 1f;

        var archers = FindObjectsOfType<Archer>(true);
        foreach (var a in archers)
            if (a != null) a.transform.SetParent(null, true);

        // Delay the rebuild a bit so the death animation reads (realtime so it's immune to
        // any timeScale state).
        yield return new WaitForSecondsRealtime(2f);

        if (_gameEnded)
        {
            _rebuildCo = null;
            yield break;
        }

        string[] arenaNames = { "Ground", "Platform_Player1Spawn", "Platform_Player2Spawn", "Platform_Center", "ArenaGenerator", "Player1Spawn", "Player2Spawn" };
        foreach (string n in arenaNames)
        {
            var go = GameObject.Find(n);
            if (go != null) Destroy(go);
        }

        yield return null;
        yield return null;

        var genGO = new GameObject("ArenaGenerator");
        var gen = genGO.AddComponent<ArenaGenerator>();
        gen.generateOnStart = false;
        gen.GenerateArena(type, seed);

        yield return null;
        yield return null;

        // Announce the upcoming round with the current score.
        var transition = FindObjectOfType<RoundTransition>(true);
        if (transition != null)
            transition.ShowRound(player1Score + player2Score + 1, player1Score, player2Score, scoreToWin);

        foreach (var archer in FindObjectsOfType<Archer>(true))
            if (archer != null) archer.Respawn();

        WindSystem.Instance?.RandomizeConditions();
        _rebuildCo = null;
    }

    public void OnTimeUp()
    {
        if (_gameEnded) return;

        int winnerSlot = player1Score > player2Score ? 1 :
                          player2Score > player1Score ? 2 : 0;

        if (winnerSlot == 0)
        {
            Analytics.MatchEnded(0, false, player1Score, player2Score);
            ProfileManager.Instance?.GrantMatchRewards(false, "online");
            UIManager.Instance?.ShowResult(false);
        }
        else
        {
            bool localWon = GetPlayerSlot(PhotonNetwork.LocalPlayer.ActorNumber) == winnerSlot;
            Analytics.MatchEnded(winnerSlot, localWon, player1Score, player2Score);
            ProfileManager.Instance?.GrantMatchRewards(localWon, "online");
            UIManager.Instance?.ShowResult(localWon);
            if (localWon) AudioManager.Instance?.PlayWin();
            else          AudioManager.Instance?.PlayLose();
        }

        _gameEnded = true;
        if (PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.CurrentRoom.IsOpen = false;
    }

    void EndMatch(int winnerSlot)
    {
        _gameEnded = true;
        if (Time.timeScale != 0f) Time.timeScale = 1f;

        if (_rebuildCo != null)
        {
            StopCoroutine(_rebuildCo);
            _rebuildCo = null;
        }

        if (PhotonNetwork.CurrentRoom != null)
            PhotonNetwork.CurrentRoom.IsOpen = false;

        foreach (TouchControls touch in FindObjectsOfType<TouchControls>(true))
        {
            if (touch != null)
                touch.enabled = false;
        }

        bool localWon = GetPlayerSlot(PhotonNetwork.LocalPlayer.ActorNumber) == winnerSlot;
        Analytics.MatchEnded(winnerSlot, localWon, player1Score, player2Score);
        ProfileManager.Instance?.GrantMatchRewards(localWon, "online");
        UIManager.Instance?.ShowResult(localWon);
        if (localWon) AudioManager.Instance?.PlayWin();
        else          AudioManager.Instance?.PlayLose();

        CameraShaker.Instance?.ShakeGameOver();
    }

    int GetPlayerSlot(int actorNumber)
    {
        if (NetworkManager.Instance != null)
            return NetworkManager.Instance.GetPlayerSlot(actorNumber);
        return actorNumber == 2 ? 2 : 1;
    }
}
