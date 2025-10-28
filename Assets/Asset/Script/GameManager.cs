using Unity.Netcode;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Bot Settings")]
    [SerializeField] private GameObject botPrefab;

    private NetworkVariable<int> continueVotes = new(0);
    private NetworkVariable<int> exitVotes = new(0);

    private HashSet<int> usedSpawnIndices = new();

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            SpawnPointManager.Instance.ResetSpawnCycle();
            usedSpawnIndices.Clear();

            RegisterUsedSpawnPositionsByPlayers();
            SpawnLobbyBots();
        }
    }

    // ✅ Ghi nhận các spawn point đã có người chơi
    private void RegisterUsedSpawnPositionsByPlayers()
    {
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < SpawnPointManager.Instance.GetTotalSpawnPoints(); i++)
        {
            Vector3 spawnPos = SpawnPointManager.Instance.GetSpawnPosition(i);

            foreach (var player in players)
            {
                float dist = Vector3.Distance(player.transform.position, spawnPos);
                if (dist < 1.5f)
                {
                    usedSpawnIndices.Add(i);
                    break;
                }
            }
        }
    }

    // ✅ Kiểm tra còn ai sống
    public void CheckAliveEntities()
    {
        if (!IsServer) return;

        var alivePlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)
            .Where(p => p.IsAlive.Value)
            .ToList();

        var aliveBots = FindObjectsByType<BotHealth>(FindObjectsSortMode.None)
            .Where(b => b.IsAlive.Value)
            .ToList();

        int totalAlive = alivePlayers.Count + aliveBots.Count;

        if (totalAlive <= 0)
        {
            Debug.Log("💀 Tất cả đều chết → reset round");
            RestartRound();
            return;
        }

        // Nếu chỉ còn 1 phe sống sót
        bool playersAlive = alivePlayers.Count > 0;
        bool botsAlive = aliveBots.Count > 0;

        if (playersAlive && !botsAlive)
        {
            Debug.Log("✅ Người chơi thắng → hiển thị menu tiếp tục");
            ShowContinueMenuClientRpc();
        }
        else if (!playersAlive && botsAlive)
        {
            Debug.Log("💀 Bot thắng → reset round");
            RestartRound();
        }
        else
        {
            // Cả hai phe còn → tiếp tục chơi
        }
    }

    [ClientRpc]
    private void ShowContinueMenuClientRpc()
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
            RespawnAllEntities();
        }
    }

    // ✅ Respawn tất cả (player + bot)
    private void RespawnAllEntities()
    {
        continueVotes.Value = 0;
        exitVotes.Value = 0;

        usedSpawnIndices.Clear();
        SpawnPointManager.Instance.ResetSpawnCycle();

        // --- Respawn players ---
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var health = playerObj.GetComponent<PlayerHealth>();
            if (health == null) continue;

            Vector3 spawnPos = GetUniqueSpawnPosition();
            health.Respawn(spawnPos);
        }

        // --- Respawn bots ---
        var bots = FindObjectsByType<BotHealth>(FindObjectsSortMode.None);
        foreach (var bot in bots)
        {
            Vector3 spawnPos = GetUniqueSpawnPosition();
            bot.Respawn(spawnPos);
        }

        HideContinueMenuClientRpc();
    }

    [ClientRpc]
    private void HideContinueMenuClientRpc()
    {
        UIManager.Instance.HideContinueMenu();
    }

    private void RestartRound()
    {
        Debug.Log("🔄 Restarting round...");
        RespawnAllEntities();
    }

    private void ReturnToLobby()
    {
        continueVotes.Value = 0;
        exitVotes.Value = 0;

        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // ✅ Spawn bot khi bắt đầu
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
        if (string.IsNullOrEmpty(botData)) return;

        string[] botEntries = botData.Split(';');

        foreach (string botEntry in botEntries)
        {
            if (string.IsNullOrEmpty(botEntry)) continue;

            string[] parts = botEntry.Split(':');
            string botName = parts[0];
            int skin = parts.Length > 1 && int.TryParse(parts[1], out int s) ? s : Random.Range(0, 3);

            if (botPrefab == null)
            {
                Debug.LogError("❌ Bot Prefab chưa được gán!");
                return;
            }

            Vector3 spawnPos = GetUniqueSpawnPosition();
            GameObject botObj = Instantiate(botPrefab, spawnPos, Quaternion.identity);

            var netObj = botObj.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("❌ Bot prefab thiếu NetworkObject!");
                Destroy(botObj);
                continue;
            }

            netObj.Spawn();

            var botSetup = botObj.GetComponent<BotSetup>();
            if (botSetup != null)
            {
                botSetup.InitBot(botName, skin);
                Debug.Log($"🤖 Spawned bot: {botName} at {spawnPos}");
            }
        }
    }

    // ✅ Spawn point an toàn
    private Vector3 GetUniqueSpawnPosition()
    {
        int totalSpawnPoints = SpawnPointManager.Instance.GetTotalSpawnPoints();
        if (usedSpawnIndices.Count >= totalSpawnPoints)
        {
            usedSpawnIndices.Clear();
        }

        int spawnIndex;
        int attempts = 0;
        int maxAttempts = totalSpawnPoints * 2;

        do
        {
            spawnIndex = SpawnPointManager.Instance.GetCurrentSpawnIndex();
            attempts++;

            if (attempts > maxAttempts)
            {
                Debug.LogWarning("⚠️ Không tìm được spawn point trống, dùng vị trí ngẫu nhiên");
                break;
            }

            if (!usedSpawnIndices.Contains(spawnIndex))
                break;

            SpawnPointManager.Instance.AdvanceSpawnIndex();
        }
        while (true);

        usedSpawnIndices.Add(spawnIndex);
        Vector3 position = SpawnPointManager.Instance.GetSpawnPosition(spawnIndex);
        SpawnPointManager.Instance.AdvanceSpawnIndex();

        return position;
    }
}
