using UnityEngine;
using Photon.Pun;

/// <summary>
/// Legacy arena prefab loader — currently a no-op. Player spawn and arena generation
/// are handled by GameArenaBootstrap + ArenaGenerator. Kept for GameArena scene compatibility.
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
