using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider2D))]
public class Bullet : NetworkBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private int damage = 10;           // Sát thương
    [SerializeField] private float lifetime = 4f;       // Thời gian tồn tại (giây)
    [SerializeField] private LayerMask collisionMask;   // Các layer có thể va chạm (ví dụ: Player, Bot, Wall)

    private NetworkObject owner; // Đối tượng đã bắn ra viên đạn

    private void Start()
    {
        if (IsServer)
        {
            // Chỉ server xử lý việc tự hủy
            Invoke(nameof(DespawnBullet), lifetime);
        }
    }

    public void SetOwner(NetworkObject ownerNetObj)
    {
        owner = ownerNetObj;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return; // Chỉ server xử lý logic

        // --- Bỏ qua va chạm với owner ---
        NetworkObject hitNetObj = other.GetComponent<NetworkObject>();
        if (hitNetObj != null && hitNetObj == owner)
            return;

        // --- Kiểm tra xem va vào thứ gì ---
        int layerMask = 1 << other.gameObject.layer;
        if ((collisionMask.value & layerMask) != 0)
        {
            // Nếu trúng Player hoặc Bot
            if (other.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                playerHealth.TakeDamageServerRpc(damage);
            }
            else if (other.TryGetComponent<BotHealth>(out var enemyHealth))
            {
                enemyHealth.TakeDamageServerRpc(damage);
            }

            // Nếu trúng tường hoặc bất kỳ vật thể trong mask
            DespawnBullet();
        }
    }

    private void DespawnBullet()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
