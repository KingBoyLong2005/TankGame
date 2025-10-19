using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LobbyPlayersManager : NetworkBehaviour
{
    public static LobbyPlayersManager Instance;

    private List<ulong> playerIds = new List<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientLeft;
        }
    }

    private void OnClientJoined(ulong clientId)
    {
        AddPlayerServerRpc(clientId);
    }

    private void OnClientLeft(ulong clientId)
    {
        playerIds.Remove(clientId);
        UpdatePlayerListClientRpc(playerIds.ToArray());
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerServerRpc(ulong clientId)
    {
        if (!playerIds.Contains(clientId))
            playerIds.Add(clientId);

        UpdatePlayerListClientRpc(playerIds.ToArray());
    }

    [ClientRpc]
    private void UpdatePlayerListClientRpc(ulong[] ids)
    {
        playerIds = ids.ToList();
        FindFirstObjectByType<LobbySceneUI>().RefreshLobby(ids.ToList());
    }
}
