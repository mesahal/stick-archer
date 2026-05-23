using UnityEngine;
using TMPro;
using Photon.Pun;

// Shows a countdown timer shared between both players during a match
public class MatchmakingTimer : MonoBehaviourPun, IPunObservable
{
    public TextMeshProUGUI timerText;
    public float matchDurationSeconds = 180f; // 3 minutes per match

    private float timeRemaining;
    private bool matchRunning = false;

    void Start()
    {
        timeRemaining = matchDurationSeconds;
    }

    public void StartMatch()
    {
        matchRunning = true;
    }

    void Update()
    {
        if (!matchRunning) return;
        if (!PhotonNetwork.IsMasterClient) return; // only master ticks time

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            matchRunning = false;
            GameManager.Instance?.OnTimeUp();
        }
    }

    void LateUpdate()
    {
        int mins = Mathf.FloorToInt(timeRemaining / 60f);
        int secs = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{mins:00}:{secs:00}";
    }

    // Photon syncs timeRemaining from master to other client
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(timeRemaining);
        else
            timeRemaining = (float)stream.ReceiveNext();
    }
}
