using UnityEngine;
using UnityEngine.UI;   // để dùng Button
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "GameScene"; // tên scene muốn load sau khi tạo phòng

    [Header("UI References")]
    [SerializeField] private Button createLobbyButton; // nút tạo phòng (gán trong Inspector)

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("Không tìm thấy NetworkManager trong scene!");
            return;
        }

        // Subcribe sự kiện Netcode
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Gán sự kiện cho nút UI
        if (createLobbyButton != null)
        {
            createLobbyButton.onClick.AddListener(StartHost);
        }
        else
        {
            Debug.LogWarning("Chưa gán Create Lobby Button trong Inspector!");
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host đã bắt đầu. Loading game scene...");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client đã kết nối...");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} đã kết nối.");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} đã rời.");
    }
}
