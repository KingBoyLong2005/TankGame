using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using TMPro;
using Unity.Services.Lobbies;
using System.Threading.Tasks;
using Unity.Netcode;

public class LobbySceneUI : MonoBehaviour
{
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button Ready;
    [SerializeField] private Button CancelReady;
    // [SerializeField] private Button RefreshButton;
    [SerializeField] private TMP_Text CodeLobby;

    private List<GameObject> slots = new List<GameObject>();
    private Dictionary<string, int> botSkins = new();
    
    private string lastLobbyState = "";
    private float refreshTimer = 0f;
    private const float REFRESH_INTERVAL = 0.5f; // Refresh mỗi 0.5s

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
        // RefreshButton.onClick.AddListener(() => { RefreshLobbyFromLobbyData(); });
        CodeLobby.text = LobbyManager.Instance.GetCodeLobby();
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            if (LobbyPlayersManager.Instance != null)
            {
                LobbyPlayersManager.Instance.RegisterMapping(
                    NetworkManager.Singleton.LocalClientId,
                    LobbyManager.Instance.GetPlayerId()
                );
            }
        }

        // Đăng ký lắng nghe sự kiện cập nhật lobby
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyUpdated += RefreshLobbyFromLobbyData;
        }

        // Refresh UI lần đầu
        Invoke(nameof(RefreshLobbyFromLobbyData), 0.5f);
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnLobbyUpdated -= RefreshLobbyFromLobbyData;
    }

    private void Update()
    {
        if (LobbyManager.Instance.hostLobby == null) return;
        LobbyGameFlow.Instance.TryCheckAllReady();

        // ✅ Thêm auto-refresh để đảm bảo UI luôn sync
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= REFRESH_INTERVAL)
        {
            refreshTimer = 0f;
            RefreshLobbyFromLobbyData();
        }
    }

    private void RefreshLobbyFromLobbyData()
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.joinLobby == null)
        {
            Debug.LogWarning("⚠️ LobbyManager hoặc joinLobby chưa sẵn sàng");
            return;
        }

        var lobby = LobbyManager.Instance.joinLobby;

        // ✅ Tạo hash để kiểm tra thay đổi (bao gồm cả số lượng player)
        string currentState = $"Players:{lobby.Players.Count}";
        foreach (var p in lobby.Players)
        {
            currentState += $"|{p.Id}";
        }
        
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
            currentState += $";Bots:{lobby.Data["Bots"].Value}";

        // Nếu không thay đổi thì không cần refresh
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

        Debug.Log($"✅ Refreshed Lobby UI: {lobby.Players.Count} players + {botSkins.Count} bots = {slots.Count} total slots");
    }

    public void RefreshLobby(List<ulong> playerIds)
    {
        RefreshLobbyFromLobbyData();
    }

    private async void AddBot()
    {
        if (slots.Count >= 4)
        {
            Debug.LogWarning("⚠️ Lobby đã đầy (4 người)");
            return;
        }

        string botName = $"Bot{Random.Range(1000, 9999)}";
        await AddBotToLobby(botName);
    }

    private async Task AddBotToLobby(string newBotName)
    {
        var lobby = LobbyManager.Instance.joinLobby;
        if (lobby == null) return;

        string oldValue = "";
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
            oldValue = lobby.Data["Bots"].Value;

        int randomSkin = Random.Range(0, 3);
        string botEntry = $"{newBotName}:{randomSkin}";
        
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

            Debug.Log($"✅ Bot '{newBotName}' (Skin {randomSkin}) added to lobby!");
            
            LobbyManager.Instance.joinLobby = lobby;
            if (LobbyManager.Instance.hostLobby != null)
                LobbyManager.Instance.hostLobby = lobby;

            botSkins[newBotName] = randomSkin;

            // ✅ Force refresh ngay lập tức
            lastLobbyState = ""; // Reset để force update
            RefreshLobbyFromLobbyData();
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Không thể thêm bot: " + e.Message);
        }
    }
}