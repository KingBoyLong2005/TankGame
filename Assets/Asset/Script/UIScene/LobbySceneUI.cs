using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using TMPro;
using Unity.Services.Lobbies;
using System.Threading.Tasks;

public class LobbySceneUI : MonoBehaviour
{
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button Ready;
    [SerializeField] private Button CancelReady;
    [SerializeField] private Button Refresh;
    [SerializeField] private TMP_Text CodeLobby;

    private List<GameObject> slots = new List<GameObject>();

    private Dictionary<string, int> botSkins = new();

    private void OnEnable()
    {
        LobbyManager.Instance.OnLobbyUpdated += RefreshLobby;
    }

    private void OnDisable()
    {
        LobbyManager.Instance.OnLobbyUpdated -= RefreshLobby;
    }

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
        Refresh.onClick.AddListener(() =>
        {
            RefreshLobby();
        });

        addBotButton.onClick.AddListener(AddBot);
        CodeLobby.text = LobbyManager.Instance.GetCodeLobby();
        RefreshLobby();
    }

    private void Update()
    {
        if (LobbyManager.Instance.hostLobby == null) return;
        LobbyGameFlow.Instance.TryCheckAllReady();
    }

    private void RefreshLobby()
    {
        foreach (var s in slots) Destroy(s);
        slots.Clear();

        var lobby = LobbyManager.Instance.joinLobby;
        if (lobby == null) return;

        // --- Hiển thị bots ---
        if (lobby.Data != null && lobby.Data.ContainsKey("Bots"))
        {
            string botData = lobby.Data["Bots"].Value;
            string[] botNames = botData.Split(';');

            foreach (string botName in botNames)
            {
                if (string.IsNullOrEmpty(botName)) continue;
                
                if (!botSkins.ContainsKey(botName))
                    botSkins[botName] = Random.Range(0, 3); // chỉ random 1 lần
                
                var slot = Instantiate(playerSlotPrefab, playerListContainer);
                slot.GetComponent<PlayerSlotUI>().Setup(botName, false, botSkins[botName]);
                slots.Add(slot);
            }
        }

        // --- Hiển thị người chơi ---
        foreach (var p in lobby.Players)
        {
            string name = p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : "Unknown";
            bool isLocal = p.Id == LobbyManager.Instance.GetPlayerId();
            int skin = p.Data.ContainsKey("Skin") ? int.Parse(p.Data["Skin"].Value) : 0;

            var slot = Instantiate(playerSlotPrefab, playerListContainer);
            slot.GetComponent<PlayerSlotUI>().Setup(name, isLocal, skin);
            slots.Add(slot);
            slot.transform.SetAsLastSibling();
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
            LobbyManager.Instance.OnLobbyUpdated?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Không thể thêm bot: " + e.Message);
        }
    }
}
