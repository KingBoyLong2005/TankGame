using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using TMPro;
using Unity.Services.Lobbies;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Collections;

public class LobbySceneUI : MonoBehaviour
{
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button Ready;
    [SerializeField] private Button CancelReady;
    [SerializeField] private TMP_Text CodeLobby;

    private List<GameObject> slots = new List<GameObject>();
    private Dictionary<string, int> botSkins = new();
    [SerializeField] private TMP_Text TextStatus;

    // NetworkVariable lưu trữ nội dung text cần đồng bộ
    // private NetworkVariable<FixedString128Bytes> statusText =
    //     new NetworkVariable<FixedString128Bytes>("Waiting for players...", 
    //     NetworkVariableReadPermission.Everyone, 
    //     NetworkVariableWritePermission.Server);    
    private string lastLobbyState = "";

    private void Start()
    {
        Ready.onClick.AddListener(() =>
        {
            LobbyGameFlow.Instance.SetReady(true);
        });
        
        CancelReady.onClick.AddListener(() =>
        {
            LobbyGameFlow.Instance.SetReady(false);
        });

        addBotButton.onClick.AddListener(AddBot);
        
        if (LobbyManager.Instance != null && LobbyManager.Instance.joinLobby != null)
        {
            CodeLobby.text = LobbyManager.Instance.GetCodeLobby();
        }

        // Đăng ký lắng nghe sự kiện cập nhật lobby từ Lobby Service
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyUpdated += RefreshLobbyFromLobbyData;
        }

        // Refresh UI lần đầu sau 1s để đảm bảo Netcode đã sync xong
        Invoke(nameof(InitialRefresh), 1f);
    }

    private void InitialRefresh()
    {
        if (LobbyPlayersManager.Instance != null)
        {
            RefreshLobbyFromNetcode(
                LobbyPlayersManager.Instance.GetAllPlayers(),
                LobbyPlayersManager.Instance.GetAllBots()
            );
        }
        else
        {
            RefreshLobbyFromLobbyData();
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnLobbyUpdated -= RefreshLobbyFromLobbyData;
    }

    private void Update()
    {
        if (LobbyManager.Instance != null && LobbyManager.Instance.hostLobby != null)
        {
            LobbyGameFlow.Instance?.TryCheckAllReady();
        }
    }

    // ✅ Phương thức refresh UI dựa trên dữ liệu từ Netcode (ưu tiên)
    public void RefreshLobbyFromNetcode(List<PlayerData> players, List<BotData> bots)
    {
        Debug.Log($"[LobbySceneUI] Refreshing from Netcode with {players.Count} players and {bots.Count} bots");

        // Xóa các slot cũ
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        slots.Clear();

        // --- 1. Hiển thị BOT trước (từ Netcode) ---
        foreach (var bot in bots)
        {
            string botName = bot.botName.ToString();
            int skin = bot.skinIndex;

            var botSlot = Instantiate(playerSlotPrefab, playerListContainer);
            botSlot.GetComponent<PlayerSlotUI>().Setup(botName, false, skin);
            slots.Add(botSlot);
        }

        // --- 2. Hiển thị PLAYER từ Netcode ---
        foreach (var playerData in players)
        {
            string name = playerData.playerName.ToString();
            if (string.IsNullOrEmpty(name))
                name = $"Player{playerData.networkClientId}";

            int skin = playerData.skinIndex;
            bool isLocal = NetworkManager.Singleton != null && 
                          playerData.networkClientId == NetworkManager.Singleton.LocalClientId;

            var playerSlot = Instantiate(playerSlotPrefab, playerListContainer);
            playerSlot.GetComponent<PlayerSlotUI>().Setup(name, isLocal, skin);
            slots.Add(playerSlot);
        }

        Debug.Log($"✅ Refreshed Lobby UI from Netcode: {bots.Count} bots + {players.Count} players = {slots.Count} total slots");
    }

    // Phương thức refresh UI dựa trên dữ liệu Lobby Service (fallback)
    private void RefreshLobbyFromLobbyData()
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.joinLobby == null)
        {
            Debug.LogWarning("⚠️ LobbyManager hoặc joinLobby chưa sẵn sàng");
            return;
        }

        // Nếu có LobbyPlayersManager, ưu tiên dùng Netcode
        if (LobbyPlayersManager.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            RefreshLobbyFromNetcode(
                LobbyPlayersManager.Instance.GetAllPlayers(),
                LobbyPlayersManager.Instance.GetAllBots()
            );
            return;
        }

        var lobby = LobbyManager.Instance.joinLobby;

        // Tạo hash để kiểm tra thay đổi
        string currentState = $"Players:{lobby.Players.Count}";
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
            currentState += $";Bots:{lobby.Data["Bots"].Value}";

        if (currentState == lastLobbyState && slots.Count > 0)
            return;

        lastLobbyState = currentState;

        // Xóa các slot cũ
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        slots.Clear();

        // --- 1. Hiển thị BOT trước ---
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
        {
            string botData = lobby.Data["Bots"].Value;
            if (!string.IsNullOrEmpty(botData))
            {
                string[] botEntries = botData.Split(';');

                foreach (string botEntry in botEntries)
                {
                    if (string.IsNullOrEmpty(botEntry)) continue;
                    
                    string[] parts = botEntry.Split(':');
                    string botName = parts[0];
                    int skin = parts.Length > 1 && int.TryParse(parts[1], out int s) ? s : Random.Range(0, 3);

                    botSkins[botName] = skin;

                    var botSlot = Instantiate(playerSlotPrefab, playerListContainer);
                    botSlot.GetComponent<PlayerSlotUI>().Setup(botName, false, skin);
                    slots.Add(botSlot);
                }
            }
        }

        // --- 2. Hiển thị PLAYER ---
        foreach (var player in lobby.Players)
        {
            string name = player.Data != null && player.Data.ContainsKey("PlayerName") 
                ? player.Data["PlayerName"].Value 
                : "Unknown";
            
            int skin = 0;
            if (player.Data != null && player.Data.ContainsKey("Skin"))
                int.TryParse(player.Data["Skin"].Value, out skin);

            bool isLocal = player.Id == LobbyManager.Instance.GetPlayerId();

            var playerSlot = Instantiate(playerSlotPrefab, playerListContainer);
            playerSlot.GetComponent<PlayerSlotUI>().Setup(name, isLocal, skin);
            slots.Add(playerSlot);
        }

        Debug.Log($"✅ Refreshed Lobby UI from Lobby Service: {lobby.Players.Count} players + {botSkins.Count} bots");
    }

    // Giữ lại để tương thích (deprecated)
    public void RefreshLobby(List<ulong> playerIds)
    {
        if (LobbyPlayersManager.Instance != null)
        {
            RefreshLobbyFromNetcode(
                LobbyPlayersManager.Instance.GetAllPlayers(),
                LobbyPlayersManager.Instance.GetAllBots()
            );
        }
        else
        {
            RefreshLobbyFromLobbyData();
        }
    }

    private async void AddBot()
    {
        if (slots.Count >= 4)
        {
            Debug.LogWarning("⚠️ Lobby đã đầy (4 người)");
            return;
        }

        string botName = $"Bot{Random.Range(1000, 9999)}";
        int randomSkin = Random.Range(0, 3);

        // ✅ Sử dụng Netcode để đồng bộ real-time
        if (LobbyPlayersManager.Instance != null)
        {
            LobbyPlayersManager.Instance.AddBotServerRpc(botName, randomSkin);
            Debug.Log($"✅ Sent add bot request to server: {botName} (Skin {randomSkin})");
        }

        // Vẫn cập nhật Lobby Service để persist data
        await AddBotToLobby(botName, randomSkin);
    }

    private async Task AddBotToLobby(string newBotName, int skinIndex)
    {
        var lobby = LobbyManager.Instance.joinLobby;
        if (lobby == null) return;

        string oldValue = "";
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
            oldValue = lobby.Data["Bots"].Value;

        string botEntry = $"{newBotName}:{skinIndex}";

        string newValue = string.IsNullOrEmpty(oldValue)
            ? botEntry
            : $"{oldValue};{botEntry}";

        try
        {
            lobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "Bots", new DataObject(DataObject.VisibilityOptions.Public, newValue) }
                }
            });

            Debug.Log($"✅ Bot '{newBotName}' (Skin {skinIndex}) added to Lobby Service!");
            UpdateStatusServerRpc("Bot '{newBotName}' (Skin {skinIndex}) added to Lobby Service!");
            LobbyManager.Instance.joinLobby = lobby;
            if (LobbyManager.Instance.hostLobby != null)
                LobbyManager.Instance.hostLobby = lobby;

            botSkins[newBotName] = skinIndex;
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Không thể thêm bot vào Lobby Service: " + e.Message);
            UpdateStatusServerRpc("Không thể thêm bot vào Lobby Service: " + e.Message);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void UpdateStatusServerRpc(string newText)
    {
        TextStatus.text = newText;
    }
}