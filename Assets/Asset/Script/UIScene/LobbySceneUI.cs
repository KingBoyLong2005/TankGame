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
    [SerializeField] private TMP_Text CodeLobby;

    private List<GameObject> slots = new List<GameObject>();

    private Dictionary<string, int> botSkins = new();


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
        CodeLobby.text = LobbyManager.Instance.GetCodeLobby();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            // Khi client vào scene Lobby
            if (LobbyPlayersManager.Instance != null)
            {
                LobbyPlayersManager.Instance.RegisterMapping(
                    NetworkManager.Singleton.LocalClientId,
                    LobbyManager.Instance.GetPlayerId()
                );
            }
        }
    }
    
    // private void OnDestroy()
    // {
    //     if (LobbyManager.Instance != null)
    //         LobbyManager.Instance.OnLobbyUpdated -= RefreshLobby;
    // }

    private void Update()
    {
        if (LobbyManager.Instance.hostLobby == null) return;
        LobbyGameFlow.Instance.TryCheckAllReady();
    }

    public void RefreshLobby(List<ulong> playerIds)
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.joinLobby == null)
            return;

        var lobby = LobbyManager.Instance.joinLobby;

        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        slots.Clear();

        // --- Hiển thị bot ---
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
        {
            string botData = lobby.Data["Bots"].Value;
            string[] botNames = botData.Split(';');

            foreach (string botName in botNames)
            {
                if (string.IsNullOrEmpty(botName)) continue;
                if (!botSkins.ContainsKey(botName))
                    botSkins[botName] = Random.Range(0, 3);

                var botSlot = Instantiate(playerSlotPrefab, playerListContainer);
                botSlot.GetComponent<PlayerSlotUI>().Setup(botName, false, botSkins[botName]);
                slots.Add(botSlot);
            }
        }

        // --- Hiển thị người chơi thực ---
        foreach (ulong id in playerIds)
        {
            string name = "Unknown";
            int skin = 0;
            bool isLocal = id == NetworkManager.Singleton.LocalClientId;

            if (LobbyPlayersManager.Instance.networkToLobbyId.TryGetValue(id, out string lobbyId))
            {
                var p = lobby.Players.Find(pl => pl.Id == lobbyId);
                if (p != null)
                {
                    name = p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : "Unknown";
                    skin = p.Data.ContainsKey("Skin") ? int.Parse(p.Data["Skin"].Value) : 0;
                }
            }

            var playerSlot = Instantiate(playerSlotPrefab, playerListContainer);
            playerSlot.GetComponent<PlayerSlotUI>().Setup(name, isLocal, skin);
            slots.Add(playerSlot);
        }
    }
    private async void AddBot()
    {
        if (slots.Count >= 4) return;

        string botName = $"Bot {slots.Count}";
        await AddBotToLobby(botName);
    }
    private async Task AddBotToLobby(string newBotName)
    {
        var lobby = LobbyManager.Instance.joinLobby;
        if (lobby == null) return;

        string oldValue = "";
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
            oldValue = lobby.Data["Bots"].Value;

        // Ghép thêm bot mới
        string newValue = string.IsNullOrEmpty(oldValue)
            ? newBotName
            : $"{oldValue};{newBotName}";

        try
        {
            lobby = await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "Bots", new DataObject(DataObject.VisibilityOptions.Public, newValue) }
                }
            });

            Debug.Log($"✅ Bot '{newBotName}' added to lobby data!");
            LobbyManager.Instance.joinLobby = lobby; // update bản nhớ cục bộ
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Không thể thêm bot: " + e.Message);
        }
    }
}