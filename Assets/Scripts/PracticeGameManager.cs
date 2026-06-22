using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using StickArcher.Analytics;
using StickArcher.Progression;

/// <summary>
/// Manages scoring and rounds for Practice (vs AI) mode.
/// Works entirely locally - no Photon required.
/// </summary>
public class PracticeGameManager : MonoBehaviour
{
    public static PracticeGameManager Instance;

    public int scoreToWin = 5;

    private int player1Score = 0;
    private int player2Score = 0;
    private bool _resettingRound = false;
    private bool _gameEnded = false;
    private Coroutine _rebuildCo;

    void Awake()
    {
        Instance = this;
        scoreToWin = RemoteConfig.GetInt("score_to_win", scoreToWin);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Called by ArcherLocal when a player is killed.</summary>
    public void RecordKill(int shooterPlayerIndex)
    {
        if (_gameEnded) return;

        int victimPlayerIndex = shooterPlayerIndex == 1 ? 2 : 1;
        ArcherLocal victim = FindArcher(victimPlayerIndex);
        if (victim != null && !victim.isDead && victim.currentHealth > 0f)
            return;

        if (shooterPlayerIndex == 1) player1Score++;
        else                         player2Score++;

        Analytics.KillRecorded(shooterPlayerIndex, victimPlayerIndex, player1Score, player2Score);

        // Player is index 1; only their kills count toward lifetime stats.
        if (shooterPlayerIndex == 1)
            ProfileManager.Instance?.RecordKill();

        UIManager.Instance?.UpdateScore(player1Score, player2Score);
        AudioManager.Instance?.PlayPointScored();

        CameraShaker.Instance?.ShakeKill();
        KillFeed.Instance?.ShowKill(shooterPlayerIndex, victimPlayerIndex);

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
            EndMatch(player1Score >= scoreToWin);
        }
        else if (!_resettingRound)
        {
            _resettingRound = true;
            Invoke(nameof(ResetRound), 2f);
        }
    }

    ArcherLocal FindArcher(int playerIndex)
    {
        foreach (var archer in FindObjectsOfType<ArcherLocal>(true))
        {
            if (archer != null && archer.playerIndex == playerIndex)
                return archer;
        }

        return null;
    }

    void ResetRound()
    {
        if (_gameEnded) return;

        // Clear the guard BEFORE running the rebuild - if anything below throws we must
        // not leave RecordKill() permanently blocked. The coroutine is the only thing
        // that should be in flight at a time; track it explicitly.
        _resettingRound = false;

        if (_rebuildCo != null) StopCoroutine(_rebuildCo);
        _rebuildCo = StartCoroutine(RebuildAndRespawn());
    }

    IEnumerator RebuildAndRespawn()
    {
        // Clear any lingering hit-stop / slow-mo so the round flow runs at full speed
        // (a stuck timeScale would stretch/halt these waits — the "hang after a kill").
        if (Time.timeScale != 0f) Time.timeScale = 1f;

        // Delay before respawn so death animation reads
        yield return new WaitForSecondsRealtime(1f);

        if (_gameEnded)
        {
            _rebuildCo = null;
            yield break;
        }

        // Announce the upcoming round (number = duels completed + 1) with current score.
        var transition = FindObjectOfType<RoundTransition>(true);
        if (transition != null)
        {
            int upcomingRound = player1Score + player2Score + 1;
            transition.ShowRound(upcomingRound, player1Score, player2Score, scoreToWin);
        }

        // Hold while the transition plays
        yield return new WaitForSecondsRealtime(1f);

        if (_gameEnded)
        {
            _rebuildCo = null;
            yield break;
        }

        // Rebuild the arena with a fresh layout so the spawn-platform and centre-blocker
        // vertical positions change every round (matches the reference game).
        RebuildArena();
        yield return null;   // let the destroys/spawns settle a frame
        yield return null;

        // Respawn all archers onto the new platforms - include inactive in case a GO was disabled
        foreach (var archer in FindObjectsOfType<ArcherLocal>(true))
            if (archer != null) archer.Respawn();

        // Randomize wind for new round
        WindSystem.Instance?.RandomizeConditions();

        _rebuildCo = null;
    }

    /// <summary>Tear down the current arena pieces and generate a fresh randomized layout.</summary>
    void RebuildArena()
    {
        var gen = FindObjectOfType<ArenaGenerator>();
        if (gen == null) return;

        // Destroy everything tagged "Arena" (ground, platforms, crates, plank).
        // Spawn-point markers are untagged, so they survive and get repositioned by
        // the new platforms (and archers realign their feet to them on Respawn).
        GameObject[] pieces;
        try { pieces = GameObject.FindGameObjectsWithTag("Arena"); }
        catch { pieces = new GameObject[0]; }
        foreach (var g in pieces)
            if (g != null) Destroy(g);

        gen.GenerateArena(Random.Range(0, 3), Random.Range(int.MinValue, int.MaxValue));
    }

    void EndMatch(bool playerWon)
    {
        _gameEnded = true;
        _resettingRound = false;
        if (Time.timeScale != 0f) Time.timeScale = 1f;

        Analytics.MatchEnded(playerWon ? 1 : 2, playerWon, player1Score, player2Score);
        ProfileManager.Instance?.GrantMatchRewards(playerWon, "practice");

        CancelInvoke(nameof(ResetRound));
        if (_rebuildCo != null)
        {
            StopCoroutine(_rebuildCo);
            _rebuildCo = null;
        }

        StopPracticeGameplay();
        UIManager.Instance?.ShowResult(playerWon);

        if (playerWon) AudioManager.Instance?.PlayWin();
        else           AudioManager.Instance?.PlayLose();
        CameraShaker.Instance?.ShakeGameOver();
    }

    void StopPracticeGameplay()
    {
        foreach (AIController ai in FindObjectsOfType<AIController>(true))
        {
            if (ai != null)
                ai.enabled = false;
        }

        foreach (TouchControls touch in FindObjectsOfType<TouchControls>(true))
        {
            if (touch != null)
                touch.enabled = false;
        }

        foreach (ArcherLocal archer in FindObjectsOfType<ArcherLocal>(true))
        {
            if (archer != null)
                archer.SetHoldInput(false);
        }
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
