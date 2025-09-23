using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int damage = 10; // Sát thương
    [SerializeField] private float lifetime = 5f; // Thời gian tồn tại (giây)

    private NetworkObject owner; // Owner để tránh tự bắn

    public void SetOwner(NetworkObject ownerNetObj)
    {
        owner = ownerNetObj;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            // Chỉ server chạy logic tự hủy sau lifetime
            Invoke(nameof(DespawnBullet), lifetime);
        }
    }

    private void DespawnBullet()
    {
        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Chỉ server xử lý va chạm

        NetworkObject hitNetObj = other.GetComponent<NetworkObject>();
        if (hitNetObj != null && hitNetObj != owner) // Không phải owner
        {
            PlayerHealth hitHealth = other.GetComponent<PlayerHealth>();
            if (hitHealth != null)
            {
                hitHealth.TakeDamageServerRpc(damage);
            }
            NetworkObject.Despawn(); // Hủy bullet trên mạng
        }
    }
}