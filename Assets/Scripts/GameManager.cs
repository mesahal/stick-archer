using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public int scoreToWin = 5;

    private int player1Score = 0;
    private int player2Score = 0;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Master client awards a kill point.</summary>
    public void RecordKill(int shooterActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("RPC_AddScore", RpcTarget.All, shooterActorNumber);
    }

    [PunRPC]
    public void RPC_AddScore(int actorNumber)
    {
        int victimActor = actorNumber == 1 ? 2 : 1;
        
        if (actorNumber == 1) player1Score++;
        else                  player2Score++;

        UIManager.Instance?.UpdateScore(player1Score, player2Score);
        AudioManager.Instance?.PlayPointScored();
        
        // Visual effects
        CameraShaker.Instance?.ShakeKill();
        KillFeed.Instance?.ShowKill(actorNumber, victimActor);
        
        // Score pop animation
        if (UIManager.Instance != null)
        {
            var p1Text = UIManager.Instance.player1ScoreText?.GetComponent<RectTransform>();
            var p2Text = UIManager.Instance.player2ScoreText?.GetComponent<RectTransform>();
            if (actorNumber == 1 && p1Text != null)
                ButtonAnimator.PopText(p1Text, 1.4f);
            else if (actorNumber == 2 && p2Text != null)
                ButtonAnimator.PopText(p2Text, 1.4f);
        }

        if (player1Score >= scoreToWin || player2Score >= scoreToWin)
        {
            int winnerActor = player1Score >= scoreToWin ? 1 : 2;
            bool localWon = PhotonNetwork.LocalPlayer.ActorNumber == winnerActor;
            UIManager.Instance?.ShowResult(localWon);
            PhotonNetwork.CurrentRoom.IsOpen = false;
            if (localWon) AudioManager.Instance?.PlayWin();
            else          AudioManager.Instance?.PlayLose();
            
            CameraShaker.Instance?.ShakeGameOver();
        }
        else
        {
            Invoke(nameof(ResetRound), 2f);
        }
    }

    void ResetRound()
    {
        foreach (var archer in FindObjectsOfType<Archer>())
            archer.Respawn();
    }

    public void OnTimeUp()
    {
        int winnerActor = player1Score > player2Score ? 1 :
                          player2Score > player1Score ? 2 : 0;

        if (winnerActor == 0)
        {
            UIManager.Instance?.ShowResult(false);
        }
        else
        {
            bool localWon = PhotonNetwork.LocalPlayer.ActorNumber == winnerActor;
            UIManager.Instance?.ShowResult(localWon);
            if (localWon) AudioManager.Instance?.PlayWin();
            else          AudioManager.Instance?.PlayLose();
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
    }
}
