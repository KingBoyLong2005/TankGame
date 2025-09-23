using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Lobby : MonoBehaviour
{
    public static Lobby Instance { get; private set; }
    private Unity.Services.Lobbies.Models.Lobby hostLobby;
    private Unity.Services.Lobbies.Models.Lobby joinLobby;

    private float hearBeatLobbyTimer = 15f;
    private float updateLobbyPollTimer = 5f;
    private float sendLastSeenTimer = 10f;
    private float checkInactiveTimer = 10f;

    private const float HEARTBEAT_INTERVAL = 15f;
    private const float LOBBY_POLL_INTERVAL = 5f;
    private const float LASTSEEN_INTERVAL = 10f;
    private const float CHECK_INACTIVE_INTERVAL = 10f;
    private const int TIMEOUT_SECONDS = 30;

    private string playerName;

    [Header("UI References")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private TMP_InputField codeLobby;
    [SerializeField] private TMP_Text Error;

    [Header("Scene Settings")]
    public string gameSceneName = "LobbyScene";

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
        }
    }

    private async void Start()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            var options = new InitializationOptions();
            await UnityServices.InitializeAsync(options);
            Debug.Log("Unity Services Initialized!");
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously as: " + AuthenticationService.Instance.PlayerId);
        }

        playerName = "Player" + UnityEngine.Random.Range(0, 100);

        if (createLobbyButton != null)
        {
            createLobbyButton.onClick.AddListener(CreateLobby);
        }
        else
        {
            Debug.LogWarning("Chưa gán Create Lobby Button trong Inspector!");
        }


        if (joinLobbyButton != null)
        {
            joinLobbyButton.onClick.AddListener(() => 
            // codeLobby là TMP_InputField
            {
                string code = codeLobby.text.Trim().ToUpper();
                JoinLobby(code);
            });
        }
        else
        {
            Debug.LogWarning("Chưa gán các Join Lobby Button trong Inspector!");
        }
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyPollForUpdate();
        ClientSendLastSeen();
        HostCheckInactivePlayers();
    }

    private bool IsLobbyHost()
    {
        return joinLobby != null && joinLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void HandleLobbyHeartBeat()
    {
        if (IsLobbyHost())
        {
            hearBeatLobbyTimer -= Time.deltaTime;
            if (hearBeatLobbyTimer <= 0f)
            {
                hearBeatLobbyTimer = HEARTBEAT_INTERVAL;
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogError($"Heartbeat failed: {e.Message}");
                }
            }
        }
    }

    private async void HandleLobbyPollForUpdate()
    {
        if (joinLobby == null) return;

        updateLobbyPollTimer -= Time.deltaTime;
        if (updateLobbyPollTimer <= 0f)
        {
            updateLobbyPollTimer = LOBBY_POLL_INTERVAL;
            try
            {
                joinLobby = await LobbyService.Instance.GetLobbyAsync(joinLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"Lobby update failed: {e.Message}");
                updateLobbyPollTimer = LOBBY_POLL_INTERVAL + 5f;
            }
        }
    }

    private async void ClientSendLastSeen()
    {
        if (joinLobby == null || AuthenticationService.Instance.PlayerId == hostLobby?.HostId) return;

        sendLastSeenTimer -= Time.deltaTime;
        if (sendLastSeenTimer <= 0f)
        {
            sendLastSeenTimer = LASTSEEN_INTERVAL;
            try
            {
                long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await LobbyService.Instance.UpdatePlayerAsync(joinLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { "lastSeen", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, unix.ToString()) }
                        }
                    });
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning("Send lastSeen failed: " + e.Message);
            }
        }
    }

    private async void HostCheckInactivePlayers()
    {
        if (!IsLobbyHost() || hostLobby == null) return;

        checkInactiveTimer -= Time.deltaTime;
        if (checkInactiveTimer <= 0f)
        {
            checkInactiveTimer = CHECK_INACTIVE_INTERVAL;
            try
            {
                hostLobby = await LobbyService.Instance.GetLobbyAsync(hostLobby.Id);
                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                foreach (var pl in hostLobby.Players)
                {
                    if (pl.Id == AuthenticationService.Instance.PlayerId) continue;
                    if (pl.Data != null && pl.Data.TryGetValue("lastSeen", out var ds)
                        && long.TryParse(ds.Value, out var last))
                    {
                        if (now - last > TIMEOUT_SECONDS)
                        {
                            await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, pl.Id);
                        }
                    }
                    else
                    {
                        await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, pl.Id);
                    }
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning("Host check failed: " + e.Message);
            }
        }
    }

    public async void CreateLobby()
    {
        try
        {
            string lobbyName = "MyLobby";
            int maxPlayer = 4;

            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
            };

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptions);
            hostLobby = lobby;
            joinLobby = lobby;

            // Tạo Relay Allocation
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayer - 1);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Lưu RelayCode vào Lobby Data
            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                }
            });

            // Config UnityTransport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));

            Debug.Log("Relay Host setup complete. Code: " + relayCode);

            // Start Host + Load Scene
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
            Debug.Log("Code join: " + lobby.LobbyCode );
        }
        catch (LobbyServiceException e)
        {
            Error.text = $"[Lobby Error] {e.Reason}: {e.Message}";
        }
        catch (RelayServiceException e)
        {
            Error.text = $"[Relay Error] {e.Reason}: {e.Message}";
        }
        catch (NullReferenceException e)
        {
            Error.text = "[Setup Error] NetworkManager hoặc Transport chưa được gán: " + e.Message;
        }
        catch (Exception e)
        {
            Error.text = "[Unknown Error] " + e;
        }
    }

    public async void JoinLobby(string lobbyCode)
    {
        try
        {
            joinLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            });

            string relayCode = joinLobby.Data["RelayCode"].Value;

            // Join Relay
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(relayCode);

            // Config UnityTransport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, "wss"));

            Debug.Log("Relay Client setup complete. Joined with code: " + relayCode);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Error.text = $"[Relay Error] {e.Reason}: {e.Message}";
        }
        catch (NullReferenceException e)
        {
            Error.text = "[Setup Error] NetworkManager hoặc Transport chưa được gán: " + e.Message;
        }
        catch (LobbyServiceException e)
        {
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                Error.text = $"Mã lobby '{lobbyCode}' không tồn tại hoặc đã hết hạn.";
            else if (e.Reason == LobbyExceptionReason.ValidationError)
                Error.text = $"Mã lobby '{lobbyCode}' không hợp lệ.";
        }
        
        catch (Exception e)
        {
            Error.text = "[Unknown Error]: " + e;
        }
    }

    private Player GetPlayer()
    {
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { "lastSeen", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, unix.ToString()) }
            }
        };
    }
}
