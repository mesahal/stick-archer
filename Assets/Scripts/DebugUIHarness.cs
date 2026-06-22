#if UNITY_EDITOR
using UnityEngine;
using StickArcher.Progression;

/// <summary>
/// EDITOR-ONLY QA helper. Lets a developer trigger transient UI states with
/// number keys during Play mode without grinding through a full match, so each
/// screen can be visually checked against its design spec.
///
///   1 → Round transition (cycles round/score: green → gold → red states)
///   2 → Victory result screen
///   3 → Defeat result screen
///   4 → Pause menu toggle
///   0 → Hide result panel
///
/// Compiled out of player builds via UNITY_EDITOR.
/// </summary>
public class DebugUIHarness : MonoBehaviour
{
    int _roundDemo = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            var rt = FindObjectOfType<RoundTransition>(true);
            if (rt != null)
            {
                // Cycle through round/score combos to preview colour states.
                int round, p1, p2;
                switch (_roundDemo % 3)
                {
                    case 0:  round = 2; p1 = 1; p2 = 0; break; // green
                    case 1:  round = 3; p1 = 2; p2 = 1; break; // gold
                    default: round = 5; p1 = 4; p2 = 3; break; // red / match point
                }
                _roundDemo++;
                rt.ShowRound(round, p1, p2, 5);
                Debug.Log($"[DebugUIHarness] Round transition: ROUND {round}, score {p1}-{p2}");
            }
            else Debug.LogWarning("[DebugUIHarness] No RoundTransition found.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UIManager.Instance?.ShowResult(true);
            Debug.Log("[DebugUIHarness] Victory result.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            UIManager.Instance?.ShowResult(false);
            Debug.Log("[DebugUIHarness] Defeat result.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            var pause = FindObjectOfType<PauseMenuUI>(true);
            if (pause != null) pause.TogglePause();
            Debug.Log("[DebugUIHarness] Toggle pause.");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ProfileManager.Instance?.AddXp(1000); // force a level-up to preview the modal
            Debug.Log("[DebugUIHarness] Granted 1000 XP (level-up).");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            // Force-damage Player 1 to preview the hit reaction / death ragdoll.
            foreach (var a in FindObjectsOfType<ArcherLocal>(true))
            {
                if (a != null && a.playerIndex == 1 && !a.isDead)
                {
                    a.SetLastHit(new Vector3(9f, 3f, 0f), a.transform.position + Vector3.up * 0.8f);
                    a.OnHitReceived(2, 40f);
                    Debug.Log("[DebugUIHarness] Dealt 40 dmg to Player 1.");
                    break;
                }
            }
        }
    }
}
#endif
