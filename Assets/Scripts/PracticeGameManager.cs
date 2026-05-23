using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scoring and rounds for Practice (vs AI) mode.
/// Works entirely locally — no Photon required.
/// </summary>
public class PracticeGameManager : MonoBehaviour
{
    public static PracticeGameManager Instance;

    public int scoreToWin = 5;

    private int player1Score = 0;
    private int player2Score = 0;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Called by ArcherLocal when a player is killed.</summary>
    public void RecordKill(int shooterPlayerIndex)
    {
        int victimPlayerIndex = shooterPlayerIndex == 1 ? 2 : 1;
        
        if (shooterPlayerIndex == 1) player1Score++;
        else                         player2Score++;

        UIManager.Instance?.UpdateScore(player1Score, player2Score);
        AudioManager.Instance?.PlayPointScored();
        
        // Visual effects
        CameraShaker.Instance?.ShakeKill();
        KillFeed.Instance?.ShowKill(shooterPlayerIndex, victimPlayerIndex);
        
        // Score pop animation
        if (UIManager.Instance != null)
        {
            var p1Text = UIManager.Instance.player1ScoreText?.GetComponent<RectTransform>();
            var p2Text = UIManager.Instance.player2ScoreText?.GetComponent<RectTransform>();
            if (shooterPlayerIndex == 1 && p1Text != null)
                ButtonAnimator.PopText(p1Text, 1.4f);
            else if (shooterPlayerIndex == 2 && p2Text != null)
                ButtonAnimator.PopText(p2Text, 1.4f);
        }

        if (player1Score >= scoreToWin || player2Score >= scoreToWin)
        {
            bool playerWon = player1Score >= scoreToWin;
            UIManager.Instance?.ShowResult(playerWon);
            if (playerWon) AudioManager.Instance?.PlayWin();
            else           AudioManager.Instance?.PlayLose();
            
            CameraShaker.Instance?.ShakeGameOver();
        }
        else
        {
            Invoke(nameof(ResetRound), 2f);
        }
    }

    void ResetRound()
    {
        foreach (var archer in FindObjectsOfType<ArcherLocal>())
            archer.Respawn();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
