using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using Unity.VisualScripting;

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

    public override void OnNetworkSpawn()
    {
        // --- Áp dụng skin ban đầu (từ network var) ---
        ApplySkin(skinIndex.Value);

        // --- Subscribe event khi skinIndex thay đổi ---
        skinIndex.OnValueChanged += (oldV, newV) =>
        {
            ApplySkin(newV);
        };

        // --- Nếu là owner local thì báo server biết skin mình đã chọn ---
        if (IsOwner)
        {
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

            // --- Camera rig spawn ---
            if (cameraRigPrefab != null)
            {
                localCameraRig = Instantiate(cameraRigPrefab);
                DontDestroyOnLoad(localCameraRig);
                LocalCamera = localCameraRig.GetComponentInChildren<Camera>();

                var cc = localCameraRig.GetComponentInChildren<CinemachineCamera>();
                if (cc != null)
                {
                    cc.Follow = transform;
                    cc.LookAt = transform;
                }

                var listener = localCameraRig.GetComponentInChildren<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
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
}
