using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mobile drag-to-aim touch controls for Stick Archers Battle.
/// Drag anywhere on screen (Angry Birds style): drag away from target to aim,
/// release to fire. No buttons — pure touch interaction.
/// </summary>
public class TouchControls : MonoBehaviour
{
    private int   activeTouchId = -1;
    private bool  isHolding     = false;
    private Vector2 dragStart   = Vector2.zero;

    const float DragFullChargeFraction = 0.35f;
    const float HUD_TOP_FRACTION       = 0.12f; // top 12% reserved for HUD

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
            {
                foreach (var a in FindObjectsOfType<ArcherLocal>())
                    if (a.isPlayerControlled) { localArcher = a; break; }
            }
        }
        else
        {
            if (onlineArcher == null)
            {
                foreach (var a in FindObjectsOfType<Archer>())
                    if (a.photonView.IsMine) { onlineArcher = a; break; }
            }
        }
    }

    int LocalPlayerIndex()
    {
        if (localArcher  != null) return localArcher.playerIndex;
        if (onlineArcher != null) return onlineArcher.playerIndex;
        return 1;
    }

    void SetHold(bool hold)
    {
        if (localArcher  != null) localArcher.SetHoldInput(hold);
        if (onlineArcher != null) onlineArcher.SetHoldInput(hold);
    }

    void SetAimAndCharge(Vector2 aimDir, float chargeRatio)
    {
        if (localArcher  != null) localArcher.SetAimAndCharge(aimDir, chargeRatio);
        if (onlineArcher != null) onlineArcher.SetAimAndCharge(aimDir, chargeRatio);
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
                    if (t.position.y > hudBottom && activeTouchId == -1)
                        break;
                    if (activeTouchId == -1)
                    {
                        activeTouchId = t.fingerId;
                        dragStart     = t.position;
                        isHolding     = true;
                        SetHold(true);
                        TouchFeedback.Instance?.ShowTouch(t.position);
                    }
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (t.fingerId == activeTouchId)
                        UpdateDrag(t.position);
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

        // Editor mouse fallback
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && activeTouchId == -1)
        {
            Vector2 pos = Input.mousePosition;
            if (pos.y <= hudBottom)
            {
                dragStart = pos;
                isHolding = true;
                SetHold(true);
            }
        }
        if (Input.GetMouseButton(0) && isHolding)
        {
            UpdateDrag(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(0) && isHolding)
        {
            isHolding = false;
            SetHold(false);
        }
#endif
    }

    void UpdateDrag(Vector2 currentPos)
    {
        Vector2 delta = dragStart - currentPos;
        int pDir = LocalPlayerIndex() == 2 ? -1 : 1;

        Vector2 aimDir = new Vector2(delta.x * pDir, delta.y).normalized;
        if (aimDir == Vector2.zero)
            aimDir = new Vector2(pDir, 0.5f);

        float chargeRatio = Mathf.Clamp01(delta.magnitude / (Screen.height * DragFullChargeFraction));
        SetAimAndCharge(aimDir, chargeRatio);
    }
}
