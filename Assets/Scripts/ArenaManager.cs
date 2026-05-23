using UnityEngine;
using Photon.Pun;

/// <summary>
/// Arena management — currently a no-op because we use a single hand-built arena.
/// (Earlier versions spawned players from here; that's now solely done by
/// GameArenaBootstrap so we don't double-spawn.)
/// </summary>
public class ArenaManager : MonoBehaviourPunCallbacks
{
    [Header("Level Prefabs (optional)")]
    public GameObject[] arenaPrefabs;

    void Start()
    {
        // If multiple arena prefabs are configured, master picks one and syncs
        if (arenaPrefabs != null && arenaPrefabs.Length > 0 && PhotonNetwork.IsMasterClient)
        {
            int arenaIndex = Random.Range(0, arenaPrefabs.Length);
            photonView.RPC(nameof(RPC_LoadArena), RpcTarget.All, arenaIndex);
        }
        // DO NOT call SpawnLocalPlayer here — that's GameArenaBootstrap's job.
    }

    [PunRPC]
    void RPC_LoadArena(int index)
    {
        if (arenaPrefabs == null || arenaPrefabs.Length == 0) return;
        Instantiate(arenaPrefabs[index], Vector3.zero, Quaternion.identity);
    }
}
