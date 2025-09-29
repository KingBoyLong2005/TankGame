using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using Unity.VisualScripting;
using Unity.Collections;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject cameraRigPrefab; // assign prefab CameraRig trong Inspector

    private GameObject localCameraRig;
    [HideInInspector] public Camera LocalCamera; // lưu cho các script khác dùng

    [Header("Skin visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer turretRenderer;
    [SerializeField] private TankSkinDatabase skinDatabase; // gán trong Prefab
    // networked skin index (server authoritative)
    private NetworkVariable<int> skinIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<FixedString64Bytes> playerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!IsOwner) return;

        if (scene.name != "LobbyScene" && localCameraRig == null)
        {
            SpawnCameraRig();
            GetComponent<PlayerController>()?.EnableInput();
        }
    }
    private void SpawnCameraRig()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LobbyScene")
        {
            if (cameraRigPrefab == null) return;

            localCameraRig = Instantiate(cameraRigPrefab);
            DontDestroyOnLoad(localCameraRig);
            LocalCamera = localCameraRig.GetComponentInChildren<Camera>();

            if (LocalCamera != null) LocalCamera.enabled = true;

            var cc = localCameraRig.GetComponentInChildren<CinemachineCamera>();
            if (cc != null)
            {
                cc.Follow = transform;
                cc.LookAt = transform;
                cc.enabled = true;
            }

            var listener = localCameraRig.GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = true;
        }
    }
    public override void OnNetworkSpawn()
    {
        // --- Áp dụng skin ban đầu (từ network var) ---
        ApplySkin(skinIndex.Value);

        // --- Subscribe event khi skinIndex thay đổi ---
        skinIndex.OnValueChanged += (oldV, newV) =>
        {
            ApplySkin(newV);
        };

        // --- Gán vị trí spawn ---
        // Chỉ server quyết định vị trí spawn
        int playerIndex = (int)OwnerClientId; // Dùng ClientId như chỉ số tạm thời
        Vector3 spawnPosition = Vector3.zero; // Giá trị mặc định
        if (SpawnPointManager.Instance != null)
        {
            spawnPosition = SpawnPointManager.Instance.GetSpawnPosition(playerIndex);
        }
        else
        {
            Debug.LogWarning("SpawnPointManager.Instance is null. Using default position (0,0,0).");
        }
        transform.position = spawnPosition;

        // --- Nếu là owner local thì báo server biết skin mình đã chọn ---
        if (IsOwner)
        {
            string myName = LobbyManager.Instance.GetPlayerName();
            playerName.Value = new FixedString64Bytes(myName);

            int mySkin = -1;

            // chỉ lấy từ LobbyGameFlow nếu đang ở lobby scene
            if (LobbyGameFlow.Instance != null)
            {
                mySkin = LobbyGameFlow.Instance.GetMySkinIndex();
            }

            if (mySkin >= 0)
            {
                SetSkinServerRpc(mySkin);
            }
            else
            {
                Debug.LogWarning("Không tìm thấy skin chọn trong Lobby → giữ mặc định.");
            }
            // if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LobbyScene")
            // {
            //     // // --- Camera rig spawn (tạm thời không kích hoạt) ---
            //     // if (cameraRigPrefab != null)
            //     // {
            //     //     localCameraRig = Instantiate(cameraRigPrefab);
            //     //     DontDestroyOnLoad(localCameraRig);
            //     //     LocalCamera = localCameraRig.GetComponentInChildren<Camera>();
            //     //     LocalCamera.enabled = false; // Vô hiệu hóa camera

            //     //     var cc = localCameraRig.GetComponentInChildren<CinemachineCamera>();
            //     //     if (cc != null)
            //     //     {
            //     //         cc.Follow = transform;
            //     //         cc.LookAt = transform;
            //     //         cc.enabled = false; // Vô hiệu hóa CinemachineCamera
            //     //     }

            //     //     var listener = localCameraRig.GetComponentInChildren<AudioListener>();
            //     //     if (listener != null) listener.enabled = false; // Vô hiệu hóa AudioListener
            //     // }
            //     SpawnCameraRig();
            // }
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsOwner && localCameraRig != null)
        {
            Debug.Log("Destroy camera");
            Destroy(localCameraRig); // Xóa camera khi player rời game
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SetSkinServerRpc(int idx, ServerRpcParams rpcParams = default)
    {
        // optional: validate idx range
        if (idx < 0 || idx >= skinDatabase.skins.Count) idx = 0;
        skinIndex.Value = idx;
    }
    private void ApplySkin(int idx)
    {
        var skin = skinDatabase.GetSkinByIndex(idx);
        if (skin == null) return;
        if (bodyRenderer != null) bodyRenderer.sprite = skin.bodySprite;
        if (turretRenderer != null) turretRenderer.sprite = skin.turretSprite;
    }

    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestChangeSkinServerRpc(int idx, ServerRpcParams rpcParams = default)
    {
        if (idx < 0 || idx >= skinDatabase.skins.Count) idx = 0;
        skinIndex.Value = idx;
    }

    public string GetPlayerName()
    {
        return playerName.Value.ToString();
    }
}
