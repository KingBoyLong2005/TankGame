using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class LobbyGameFlow : MonoBehaviour
{
    public static LobbyGameFlow Instance { get; private set; }
    [Header("Lobby Settings")]
    // [SerializeField] private int maxPlayers = 4;
    [SerializeField] private List<string> maps = new List<string> { "Map1", "Map2", "Map3" };
    [SerializeField] private int countdownSeconds = 5;

    private bool countdownRunning = false;

    private LobbyManager LM => LobbyManager.Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public string GetPlayerSkin(string playerId)
    {
        if (LM.joinLobby == null) return null;
        var p = LM.joinLobby.Players.Find(x => x.Id == playerId);
        if (p != null && p.Data != null && p.Data.TryGetValue("Skin", out var d))
            return d.Value;
        return null;
    }

    // public async void SelectSkin(int skinIndex)
    // {
    //     try
    //     {
    //         if (LM.joinLobby == null) return;

    //         var update = new UpdatePlayerOptions
    //         {
    //             Data = new Dictionary<string, PlayerDataObject>
    //             {
    //                 { "Skin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, skinIndex.ToString()) },
    //                 { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") }
    //             }
    //         };

    //         // sau khi joinLobby được update
    //         var myPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
    //         if (myPlayer != null && myPlayer.TryGetComponent(out PlayerSetup setup))
    //         {
    //             setup.CmdRequestChangeSkinServerRpc(skinIndex);
    //         }

    //         LM.joinLobby = await LobbyService.Instance.UpdatePlayerAsync(LM.joinLobby.Id, AuthenticationService.Instance.PlayerId, update);
    //     }
    //     catch (Exception e) { Debug.LogError("SelectSkin error: " + e); }
    // }

    public async void SetReady(bool ready)
    {
        if (LM.joinLobby == null) return;

        var update = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ready ? "true" : "false") }
            }
        };

        LM.joinLobby = await LobbyService.Instance.UpdatePlayerAsync(LM.joinLobby.Id, AuthenticationService.Instance.PlayerId, update);

        TryCheckAllReady();
    }

    public void TryCheckAllReady()
    {
        if (LM.hostLobby == null) return;
        if (LM.hostLobby.HostId != AuthenticationService.Instance.PlayerId) return;

        bool allReady = true;
        foreach (var p in LM.hostLobby.Players)
        {
            if (p.Data == null || !p.Data.TryGetValue("IsReady", out var rd) || rd.Value != "true")
            {
                allReady = false;
                break;
            }
        }

        if (allReady && !countdownRunning)
            StartCoroutine(StartCountdown());
    }

    private System.Collections.IEnumerator StartCountdown()
    {
        countdownRunning = true;
        int t = countdownSeconds;

        while (t > 0)
        {
            Debug.Log("Game starting in " + t);
            yield return new WaitForSeconds(1f);
            t--;
        }

        string map = maps[UnityEngine.Random.Range(0, maps.Count)];
        Debug.Log("Loading " + map);

        NetworkManager.Singleton.SceneManager.LoadScene(map, UnityEngine.SceneManagement.LoadSceneMode.Single);
        countdownRunning = false;
    }
    public int GetMySkinIndex()
    {
        var myId = LM.GetPlayerId();
        var skinStr = GetPlayerSkin(myId);

        if (string.IsNullOrEmpty(skinStr)) return -1;
        if (int.TryParse(skinStr, out int idx)) return idx;

        return -1;
    }

    // public async void SetSkin(int skinIndex)
    // {
    //     try
    //     {
    //         if (LM.joinLobby == null) return;

    //         var update = new UpdatePlayerOptions
    //         {
    //             Data = new Dictionary<string, PlayerDataObject>
    //             {
    //                 { "Skin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, skinIndex.ToString()) }
    //             }
    //         };

    //         LM.joinLobby = await LobbyService.Instance.UpdatePlayerAsync(
    //             LM.joinLobby.Id,
    //             AuthenticationService.Instance.PlayerId,
    //             update
    //         );

    //         Debug.Log($"✅ Updated skin for player {AuthenticationService.Instance.PlayerId} = {skinIndex}");
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError("SetSkin error: " + e);
    //     }
    // }

}
