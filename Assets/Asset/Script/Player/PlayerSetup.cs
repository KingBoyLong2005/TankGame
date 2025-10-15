using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using Unity.Collections;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject cameraRigPrefab;

    private GameObject localCameraRig;
    [HideInInspector] public Camera LocalCamera;

    [Header("Skin visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer turretRenderer;
    [SerializeField] private TankSkinDatabase skinDatabase;

    private NetworkVariable<int> skinIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> playerName =
        new NetworkVariable<FixedString64Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "LobbyScene") return;
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

    public override void OnNetworkSpawn()
    {
        // Áp dụng skin hiện có
        ApplySkin(skinIndex.Value);

        // Theo dõi thay đổi skin từ server
        skinIndex.OnValueChanged += (oldV, newV) => ApplySkin(newV);

        // Gán vị trí spawn
        int playerIndex = (int)OwnerClientId;
        Vector3 spawnPosition = SpawnPointManager.Instance
            ? SpawnPointManager.Instance.GetSpawnPosition(playerIndex)
            : Vector3.zero;

        transform.position = spawnPosition;

        if (IsOwner)
            StartCoroutine(SendSkinDataToServer());
    }

    private System.Collections.IEnumerator SendSkinDataToServer()
    {
        // Chờ LobbyManager chắc chắn tồn tại
        yield return new WaitUntil(() => LobbyManager.Instance != null);

        string myName = LobbyManager.Instance.GetPlayerName();
        int mySkin = LobbyManager.Instance.selectedSkinIndex;

        Debug.Log($"🟢 {myName} đang spawn với skin index: {mySkin}");

        playerName.Value = new FixedString64Bytes(myName);
        SetSkinServerRpc(mySkin);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && localCameraRig != null)
        {
            Debug.Log("Destroy camera rig của player local");
            Destroy(localCameraRig);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void SetSkinServerRpc(int idx, ServerRpcParams rpcParams = default)
    {
        if (idx < 0 || idx >= skinDatabase.skins.Count) idx = 0;
        Debug.Log($"📡 Server nhận SetSkinServerRpc({idx}) từ client {OwnerClientId}");
        skinIndex.Value = idx;
    }

    private void ApplySkin(int idx)
    {
        var skin = skinDatabase.GetSkinByIndex(idx);
        if (skin == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy skin index {idx} trong database!");
            return;
        }

        if (bodyRenderer != null) bodyRenderer.sprite = skin.bodySprite;
        if (turretRenderer != null) turretRenderer.sprite = skin.turretSprite;

        Debug.Log($"🎨 Player {OwnerClientId} áp dụng skin: {skin.displayName} (index {idx})");
    }

    public string GetPlayerName()
    {
        return playerName.Value.ToString();
    }
}
