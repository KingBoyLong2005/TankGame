using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using Unity.VisualScripting;

public class PlayerSetup : NetworkBehaviour
{
    [SerializeField] private GameObject cameraRigPrefab; // assign prefab CameraRig trong Inspector

    private GameObject localCameraRig;
    [HideInInspector] public Camera LocalCamera; // lưu cho các script khác dùng

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Spawn camera rig local
            localCameraRig = Instantiate(cameraRigPrefab);
            DontDestroyOnLoad(localCameraRig);

            Camera cam = localCameraRig.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                LocalCamera = cam;
            }
            // Tìm Cinemachine camera trong prefab vừa spawn
            var cc = localCameraRig.GetComponentInChildren<CinemachineCamera>();
            if (cc != null)
            {
                cc.Follow = transform;
                cc.LookAt = transform;
                cc.Priority = 1;
            }

            // Bật AudioListener (chỉ cho local client)
            var listener = localCameraRig.GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = true;
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
}
