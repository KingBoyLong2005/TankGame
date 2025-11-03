using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using System;

public class LobbyPlayersManager : NetworkBehaviour
{
    public static LobbyPlayersManager Instance;

    // Lưu thông tin player: NetworkClientId -> (LobbyPlayerId, SkinIndex, PlayerName)
    private NetworkList<PlayerData> playersInLobby;
    
    // ✅ Lưu thông tin bot riêng
    private NetworkList<BotData> botsInLobby;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        playersInLobby = new NetworkList<PlayerData>();
        botsInLobby = new NetworkList<BotData>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientLeft;
        }

        // Client lắng nghe thay đổi danh sách
        playersInLobby.OnListChanged += OnPlayerListChanged;
        botsInLobby.OnListChanged += OnBotListChanged;

        // Nếu là client vừa join, gửi thông tin của mình lên server
        if (IsClient)
        {
            // Đợi một chút để LobbyManager khởi tạo xong
            Invoke(nameof(SendPlayerInfoToServer), 0.5f);
        }
    }

    private void SendPlayerInfoToServer()
    {
        if (!IsClient) return;

        string playerName = LobbyManager.Instance?.GetPlayerName() ?? $"Player{NetworkManager.Singleton.LocalClientId}";
        string lobbyPlayerId = LobbyManager.Instance?.GetPlayerId() ?? "";
        int skinIndex = LobbyManager.Instance?.selectedSkinIndex ?? 0;

        RequestAddSelfServerRpc(playerName, lobbyPlayerId, skinIndex);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientLeft;
        }
        playersInLobby.OnListChanged -= OnPlayerListChanged;
        botsInLobby.OnListChanged -= OnBotListChanged;
    }

    private void OnClientJoined(ulong clientId)
    {
        Debug.Log($"[Server] Client {clientId} connected to lobby");
    }

    private void OnClientLeft(ulong clientId)
    {
        Debug.Log($"[Server] Client {clientId} disconnected from lobby");
        RemovePlayerFromList(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddSelfServerRpc(string playerName, string lobbyPlayerId, int skinIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        AddPlayerToList(senderClientId, lobbyPlayerId, playerName, skinIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerSkinServerRpc(int newSkinIndex, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        for (int i = 0; i < playersInLobby.Count; i++)
        {
            if (playersInLobby[i].networkClientId == senderClientId)
            {
                var playerData = playersInLobby[i];
                playerData.skinIndex = newSkinIndex;
                playersInLobby[i] = playerData;
                Debug.Log($"[Server] Updated skin for client {senderClientId} to {newSkinIndex}");
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerNameServerRpc(string newName, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        for (int i = 0; i < playersInLobby.Count; i++)
        {
            if (playersInLobby[i].networkClientId == senderClientId)
            {
                var playerData = playersInLobby[i];
                playerData.playerName = new FixedString64Bytes(newName);
                playersInLobby[i] = playerData;
                Debug.Log($"[Server] Updated name for client {senderClientId} to {newName}");
                break;
            }
        }
    }

    private void AddPlayerToList(ulong networkClientId, string lobbyPlayerId, string playerName, int skinIndex)
    {
        // Kiểm tra xem player đã tồn tại chưa
        bool exists = false;
        for (int i = 0; i < playersInLobby.Count; i++)
        {
            if (playersInLobby[i].networkClientId == networkClientId)
            {
                exists = true;
                Debug.LogWarning($"[Server] Player {playerName} already exists in list");
                break;
            }
        }

        if (!exists)
        {
            playersInLobby.Add(new PlayerData
            {
                networkClientId = networkClientId,
                lobbyPlayerId = new FixedString64Bytes(lobbyPlayerId),
                playerName = new FixedString64Bytes(playerName),
                skinIndex = skinIndex
            });
            Debug.Log($"[Server] Added player {playerName} (NetID: {networkClientId}, Skin: {skinIndex})");
        }
    }

    private void RemovePlayerFromList(ulong networkClientId)
    {
        for (int i = playersInLobby.Count - 1; i >= 0; i--)
        {
            if (playersInLobby[i].networkClientId == networkClientId)
            {
                Debug.Log($"[Server] Removed player {playersInLobby[i].playerName}");
                playersInLobby.RemoveAt(i);
                break;
            }
        }
    }

    // Callback khi list thay đổi (gọi trên tất cả clients)
    private void OnPlayerListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        Debug.Log($"[Client] Player list changed! Event: {changeEvent.Type}, Total players: {playersInLobby.Count}");
        RefreshUI();
    }

    // ✅ Callback khi bot list thay đổi
    private void OnBotListChanged(NetworkListEvent<BotData> changeEvent)
    {
        Debug.Log($"[Client] Bot list changed! Event: {changeEvent.Type}, Total bots: {botsInLobby.Count}");
        RefreshUI();
    }

    private void RefreshUI()
    {
        // Tìm UI trong scene hiện tại
        var lobbyUI = FindFirstObjectByType<LobbySceneUI>();
        if (lobbyUI != null)
        {
            // ✅ Convert NetworkList sang List thủ công
            lobbyUI.RefreshLobbyFromNetcode(ConvertToList(playersInLobby), ConvertBotToList(botsInLobby));
        }
        else
        {
            Debug.LogWarning("[LobbyPlayersManager] Không tìm thấy LobbySceneUI trong scene");
        }
    }

    // ✅ Helper method để convert NetworkList sang List
    private List<PlayerData> ConvertToList(NetworkList<PlayerData> networkList)
    {
        List<PlayerData> list = new List<PlayerData>();
        foreach (var item in networkList)
        {
            list.Add(item);
        }
        return list;
    }

    // ✅ Helper method để convert bot NetworkList sang List
    private List<BotData> ConvertBotToList(NetworkList<BotData> networkList)
    {
        List<BotData> list = new List<BotData>();
        foreach (var item in networkList)
        {
            list.Add(item);
        }
        return list;
    }

    public List<PlayerData> GetAllPlayers()
    {
        // ✅ Sử dụng helper method
        return ConvertToList(playersInLobby);
    }

    public List<BotData> GetAllBots()
    {
        return ConvertBotToList(botsInLobby);
    }

    public PlayerData? GetPlayerData(ulong networkClientId)
    {
        foreach (var player in playersInLobby)
        {
            if (player.networkClientId == networkClientId)
                return player;
        }
        return null;
    }

    // ✅ ServerRpc để thêm bot
    [ServerRpc(RequireOwnership = false)]
    public void AddBotServerRpc(string botName, int skinIndex)
    {
        if (!IsServer) return;

        // Kiểm tra bot đã tồn tại chưa
        foreach (var bot in botsInLobby)
        {
            if (bot.botName.ToString() == botName)
            {
                Debug.LogWarning($"[Server] Bot {botName} đã tồn tại!");
                return;
            }
        }

        botsInLobby.Add(new BotData
        {
            botName = new FixedString64Bytes(botName),
            skinIndex = skinIndex
        });

        Debug.Log($"[Server] Added bot {botName} with skin {skinIndex}");
    }

    // ✅ ServerRpc để xóa bot
    [ServerRpc(RequireOwnership = false)]
    public void RemoveBotServerRpc(string botName)
    {
        if (!IsServer) return;

        for (int i = botsInLobby.Count - 1; i >= 0; i--)
        {
            if (botsInLobby[i].botName.ToString() == botName)
            {
                Debug.Log($"[Server] Removed bot {botName}");
                botsInLobby.RemoveAt(i);
                break;
            }
        }
    }
}

// Struct để lưu thông tin player
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public ulong networkClientId;
    public FixedString64Bytes lobbyPlayerId;
    public FixedString64Bytes playerName;
    public int skinIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref networkClientId);
        serializer.SerializeValue(ref lobbyPlayerId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref skinIndex);
    }

    // Bắt buộc cho NetworkList<T>
    public bool Equals(PlayerData other)
    {
        return networkClientId == other.networkClientId &&
               lobbyPlayerId.Equals(other.lobbyPlayerId) &&
               playerName.Equals(other.playerName) &&
               skinIndex == other.skinIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is PlayerData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(networkClientId, lobbyPlayerId, playerName, skinIndex);
    }
}