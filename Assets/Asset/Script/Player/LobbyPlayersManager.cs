using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LobbyPlayersManager : NetworkBehaviour
{
    public static LobbyPlayersManager Instance;

    private List<ulong> playerIds = new List<ulong>();

    // Ánh xạ giữa Network Client ID và Lobby Player ID
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
        // Server thêm player mới vào danh sách
        AddPlayerServerRpc(clientId);

        // Server không tự biết lobbyId của client, client sẽ tự báo lên sau
        Debug.Log($"[Server] Client joined: {clientId}");
    }

    private void OnClientLeft(ulong clientId)
    {
        playerIds.Remove(clientId);
        networkToLobbyId.Remove(clientId);
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
        Debug.Log($"[Client] Refresh UI with {playerIds.Count} players");

        var ui = FindFirstObjectByType<LobbySceneUI>();
        if (ui != null)
            ui.RefreshLobby(playerIds);
    }

    // Client gọi khi vừa join xong để đăng ký mapping
    public void RegisterMapping(ulong networkId, string lobbyId)
    {
        if (!networkToLobbyId.ContainsKey(networkId))
        {
            networkToLobbyId[networkId] = lobbyId;
            Debug.Log($"Mapping added: NetID {networkId} → LobbyID {lobbyId}");
        }
    }
}
