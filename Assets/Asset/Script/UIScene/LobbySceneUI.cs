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
        // RefreshLobby();
        // if (LobbyManager.Instance != null)
        //     LobbyManager.Instance.OnLobbyUpdated += RefreshLobby;
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
        foreach (Transform child in playerListContainer)
            Destroy(child.gameObject);

        var lobby = LobbyManager.Instance.joinLobby;
        foreach (ulong id in playerIds)
        {
            var slot = Instantiate(playerSlotPrefab, playerListContainer);
            var isLocal = id == NetworkManager.Singleton.LocalClientId;
            string name = LobbyManager.Instance.GetPlayerName();

            int skin = lobby.Data.ContainsKey("Skin") ? int.Parse(lobby.Data["Skin"].Value) : 0;
            

            slot.GetComponent<PlayerSlotUI>().Setup(name, isLocal, skin);
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
