using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private TMP_InputField codeLobby;
    [SerializeField] private TMP_Text Error;
    [SerializeField] private TMP_InputField PlayerNameInput;

    [Header("Scene Settings")]
    public string lobbySceneName = "LobbyScene";

    public Unity.Services.Lobbies.Models.Lobby hostLobby;
    public Unity.Services.Lobbies.Models.Lobby joinLobby;

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

    public int selectedSkinIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private async void Start()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // playerName = "Player" + UnityEngine.Random.Range(0, 100);
        // PlayerNameInput.text = playerName;

        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(CreateLobby);

        if (joinLobbyButton != null)
        {
            joinLobbyButton.onClick.AddListener(() =>
            {
                string code = codeLobby.text.Trim().ToUpper();
                JoinLobby(code);
            });
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

    #region Heartbeat & Poll
    private async void HandleLobbyHeartBeat()
    {
        if (!IsLobbyHost()) return;

        hearBeatLobbyTimer -= Time.deltaTime;
        if (hearBeatLobbyTimer <= 0f)
        {
            hearBeatLobbyTimer = HEARTBEAT_INTERVAL;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
            catch (Exception e) { Debug.LogWarning("Heartbeat failed: " + e.Message); }
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
            catch { updateLobbyPollTimer = LOBBY_POLL_INTERVAL + 5f; }
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
            catch { }
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
                            await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, pl.Id);
                    }
                }
            }
            catch { }
        }
    }
    #endregion

    #region Create / Join
    public async void CreateLobby()
    {
        try
        {
            CheckName(PlayerNameInput);
            string lobbyName = "MyLobby";
            int maxPlayer = 4;

            var createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer()
            };

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptions);
            hostLobby = lobby;
            joinLobby = lobby;

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayer - 1);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> { { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, relayCode) } }
            });

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            
            // Đảm bảo scene LobbyScene được tải trước khi start host
            // if (SceneManager.GetActiveScene().name != lobbySceneName)
            {
                SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
                // Đợi scene tải hoàn tất
                await System.Threading.Tasks.Task.Run(() =>
                {
                    // while (SceneManager.GetActiveScene().name != lobbySceneName)
                    // {
                        System.Threading.Thread.Sleep(100);
                    // }
                });
            }

            NetworkManager.Singleton.StartHost();
            // NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
            Debug.Log("Code join: " + lobby.LobbyCode);
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
            CheckName(PlayerNameInput);
            joinLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions { Player = GetPlayer() });

            string relayCode = joinLobby.Data["RelayCode"].Value;
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(relayCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, "wss"));

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
    #endregion

    public Player GetPlayer()
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
    public string GetPlayerId()
    {
        return AuthenticationService.Instance.IsSignedIn
            ? AuthenticationService.Instance.PlayerId
            : null;
    }
    public string GetPlayerName()
    {
        return playerName;
    }
    private void CheckName(TMP_InputField playerNameCheck)
    {
        if (playerNameCheck.text == "")
        {
            playerName = "Player" + UnityEngine.Random.Range(0, 100);
        }
        else
        {
            playerName = PlayerNameInput.text;
        }
    }

    public void SetSelectedSkin(int index)
    {
        selectedSkinIndex = index;
        Debug.Log("Đã chọn skin: " + index);
    }
}
