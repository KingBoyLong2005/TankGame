using Unity.Netcode;
using System.Linq;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    private NetworkVariable<int> continueVotes = new(0);
    private NetworkVariable<int> exitVotes = new(0);

    private void Awake() => Instance = this;

    public void CheckAlivePlayers()
    {
        if (!IsServer) return;

        var alivePlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)
            .Where(p => p.IsAlive.Value)
            .ToList();

        if (alivePlayers.Count == 1)
        {
            // Khi chỉ còn một người sống → mở menu cho tất cả client
            ShowContinueMenuClientRpc();
        }
    }

    [ClientRpc]
    void ShowContinueMenuClientRpc()
    {
        UIManager.Instance.ShowContinueMenu();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerVoteServerRpc(bool continueGame, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (continueGame)
            continueVotes.Value++;
        else
            exitVotes.Value++;

        int totalPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Nếu có người chọn "Exit" → tất cả về Lobby
        if (exitVotes.Value > 0)
        {
            ReturnToLobby();
        }
        // Nếu tất cả đều chọn "Continue" → Respawn lại
        else if (continueVotes.Value >= totalPlayers)
        {
            RespawnAllPlayers();
        }
    }

    private void RespawnAllPlayers()
    {
        continueVotes.Value = 0;
        exitVotes.Value = 0;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;

            // Nếu player đã bị despawn (bị xoá mất NetworkObject)
            if (playerObj == null)
            {
                // Spawn lại prefab player mới cho client này
                GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                GameObject newPlayer = Instantiate(playerPrefab);

                var netObj = newPlayer.GetComponent<NetworkObject>();
                netObj.SpawnAsPlayerObject(client.ClientId);

                playerObj = netObj; // Cập nhật player object để Respawn
            }

            if (playerObj != null)
            {
                var health = playerObj.GetComponent<PlayerHealth>();
                Vector3 spawnPos = SpawnPointManager.Instance.GetNextSpawnPosition();
                health.Respawn(spawnPos);
            }
        }

        HideContinueMenuClientRpc();
    }


    [ClientRpc]
    private void HideContinueMenuClientRpc()
    {
        UIManager.Instance.HideContinueMenu();
    }

    private void ReturnToLobby()
    {
        continueVotes.Value = 0;
        exitVotes.Value = 0;

        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    public void SpawnLobbyBots()
    {
        if (!IsServer) return;
        var lobby = LobbyManager.Instance.joinLobby;
        if (lobby == null || lobby.Data == null || !lobby.Data.ContainsKey("Bots")) return;

        string botData = lobby.Data["Bots"].Value;
        string[] botNames = botData.Split(';');

        foreach (string botName in botNames)
        {
            if (string.IsNullOrEmpty(botName)) continue;

            // Chọn skin ngẫu nhiên
            int skin = Random.Range(0, 3);

            // Spawn bot prefab
            GameObject botPrefab = Resources.Load<GameObject>("Bot"); // đặt prefab trong Resources/Bot.prefab
            GameObject botObj = Instantiate(botPrefab, SpawnPointManager.Instance.GetNextSpawnPosition(), Quaternion.identity);
            var netObj = botObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            botObj.GetComponent<BotSetup>().InitBotServerRpc(botName, skin);
        }
    }
}
