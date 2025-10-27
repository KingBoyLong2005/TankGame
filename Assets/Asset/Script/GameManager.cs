using Unity.Netcode;
using System.Linq;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Bot Settings")]
    [SerializeField] private GameObject botPrefab; // ✅ Gán trong Inspector thay vì dùng Resources

    private NetworkVariable<int> continueVotes = new(0);
    private NetworkVariable<int> exitVotes = new(0);

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ✅ Spawn bot ngay khi GameManager được spawn (chỉ host)
        if (IsServer)
        {
            SpawnLobbyBots();
        }
    }

    public void CheckAlivePlayers()
    {
        if (!IsServer) return;

        var alivePlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)
            .Where(p => p.IsAlive.Value)
            .ToList();

        if (alivePlayers.Count == 1)
        {
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

        if (exitVotes.Value > 0)
        {
            ReturnToLobby();
        }
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

            if (playerObj == null)
            {
                GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                GameObject newPlayer = Instantiate(playerPrefab);

                var netObj = newPlayer.GetComponent<NetworkObject>();
                netObj.SpawnAsPlayerObject(client.ClientId);

                playerObj = netObj;
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

        var lobby = LobbyManager.Instance?.joinLobby;
        if (lobby == null)
        {
            Debug.LogWarning("⚠️ Không tìm thấy lobby data");
            return;
        }

        if (lobby.Data == null || !lobby.Data.ContainsKey("Bots"))
        {
            Debug.Log("✅ Không có bot nào để spawn");
            return;
        }

        string botData = lobby.Data["Bots"].Value;
        if (string.IsNullOrEmpty(botData))
        {
            Debug.Log("✅ Bot data trống");
            return;
        }

        string[] botEntries = botData.Split(';');

        foreach (string botEntry in botEntries)
        {
            if (string.IsNullOrEmpty(botEntry)) continue;

            // Parse "BotName:SkinIndex"
            string[] parts = botEntry.Split(':');
            string botName = parts[0];
            int skin = parts.Length > 1 && int.TryParse(parts[1], out int s) ? s : Random.Range(0, 3);

            // ✅ Kiểm tra botPrefab
            if (botPrefab == null)
            {
                Debug.LogError("❌ Bot Prefab chưa được gán trong GameManager!");
                return;
            }

            // Spawn bot
            Vector3 spawnPos = SpawnPointManager.Instance.GetNextSpawnPosition();
            GameObject botObj = Instantiate(botPrefab, spawnPos, Quaternion.identity);
            
            var netObj = botObj.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("❌ Bot prefab không có NetworkObject component!");
                Destroy(botObj);
                continue;
            }

            netObj.Spawn();

            // Khởi tạo bot
            var botSetup = botObj.GetComponent<BotSetup>();
            if (botSetup != null)
            {
                botSetup.InitBotServerRpc(botName, skin);
                Debug.Log($"✅ Spawned bot: {botName} with skin {skin}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Bot prefab không có BotSetup component!");
            }
        }
    }
}