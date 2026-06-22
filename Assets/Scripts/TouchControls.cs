using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile tap-to-charge touch controls for Stick Archers Battle.
/// Tap and HOLD to charge the bow, RELEASE to fire.
/// Aim direction is driven entirely by BowSwayController (sine wave) —
/// no drag direction is needed or read here.
/// </summary>
public class TouchControls : MonoBehaviour
{
    private int  activeTouchId = -1;
    private bool isHolding     = false;

    const float HUD_TOP_FRACTION = 0.12f; // top 12% of screen reserved for HUD

    private Archer      onlineArcher;
    private ArcherLocal localArcher;

    void Update()
    {
        FindArcher();
        HandleTouches();
    }

    void FindArcher()
    {
        if (GameMode.IsPractice)
        {
            if (localArcher == null)
                foreach (var a in FindObjectsOfType<ArcherLocal>())
                    if (a.isPlayerControlled) { localArcher = a; break; }
        }
        else
        {
            if (onlineArcher == null)
                foreach (var a in FindObjectsOfType<Archer>())
                    if (a.photonView.IsMine) { onlineArcher = a; break; }
        }
    }

    void SetHold(bool hold)
    {
        if (localArcher  != null) localArcher.SetHoldInput(hold);
        if (onlineArcher != null) onlineArcher.SetHoldInput(hold);
    }

    void HandleTouches()
    {
        float hudBottom = Screen.height * (1f - HUD_TOP_FRACTION);

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            switch (t.phase)
            {
                case TouchPhase.Began:
                    // Ignore touches in HUD area
                    if (IsPointerOverUI(t.fingerId)) break;
                    if (t.position.y > hudBottom) break;
                    if (activeTouchId == -1)
                    {
                        activeTouchId = t.fingerId;
                        isHolding     = true;
                        SetHold(true);
                        TouchFeedback.Instance?.ShowTouch(t.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (t.fingerId == activeTouchId)
                    {
                        activeTouchId = -1;
                        isHolding     = false;
                        SetHold(false);
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && activeTouchId == -1)
        {
            if (IsPointerOverUI()) return;
            if ((Input.mousePosition.y) <= hudBottom)
            {
                isHolding = true;
                SetHold(true);
            }
        }
        if (Input.GetMouseButtonUp(0) && isHolding)
        {
            isHolding = false;
            SetHold(false);
        }
#endif
    }

    bool IsPointerOverUI(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;
        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
