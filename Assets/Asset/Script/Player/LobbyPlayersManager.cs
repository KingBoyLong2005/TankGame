using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LobbyPlayersManager : NetworkBehaviour
{
    public static LobbyPlayersManager Instance;

    private readonly List<ulong> playerIds = new();
    public Dictionary<ulong, string> networkToLobbyId = new();

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
        if (!IsServer) return;
        if (!playerIds.Contains(clientId))
            playerIds.Add(clientId);

        Debug.Log($"[Server] Client joined: {clientId}");
        UpdateAllClientsUI();
    }

    private void OnClientLeft(ulong clientId)
    {
        if (!IsServer) return;

        playerIds.Remove(clientId);
        networkToLobbyId.Remove(clientId);
        UpdateAllClientsUI();
    }

    private void UpdateAllClientsUI()
    {
        UpdatePlayerListClientRpc(playerIds.ToArray());
    }

    [ClientRpc]
    private void UpdatePlayerListClientRpc(ulong[] ids)
    {
        Debug.Log($"[Client] Refreshing UI: {ids.Length} players connected.");

        var ui = FindFirstObjectByType<LobbySceneUI>();
        if (ui != null)
            ui.RefreshLobbyFromNetcode(ids);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterMappingServerRpc(ulong networkId, string lobbyId)
    {
        if (!networkToLobbyId.ContainsKey(networkId))
        {
            networkToLobbyId[networkId] = lobbyId;
            Debug.Log($"[Server] Mapping added: NetID {networkId} → LobbyID {lobbyId}");
        }

        // Đồng bộ lại danh sách để UI hiển thị đúng tên
        UpdatePlayerListClientRpc(playerIds.ToArray());
    }
    public void RefreshUIForServer()
    {
        UpdatePlayerListClientRpc(playerIds.ToArray());
    }
}
