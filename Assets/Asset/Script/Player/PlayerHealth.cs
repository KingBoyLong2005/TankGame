using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100; // Máu tối đa
    // [SerializeField] private GameObject deathEffect; // Hiệu ứng chết (nếu có, prefab particle)

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentHealth.Value = maxHealth; // Reset health khi spawn
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int previous, int newValue)
    {
        // Cập nhật UI hoặc hiệu ứng (ví dụ: hiển thị máu cho local player)
        if (IsOwner)
        {
            Debug.Log($"Health: {newValue}/{maxHealth}");
        }
    }

    [ServerRpc(RequireOwnership = false)] // Bất kỳ ai cũng gọi được, nhưng server xử lý
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;
        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Xử lý chết: Hủy player hoặc respawn
        // if (deathEffect != null)
        // {
        //     Instantiate(deathEffect, transform.position, Quaternion.identity);
        // }
        NetworkObject.Despawn(); // Hủy trên mạng
    }

    public int GetCurrentHealth()
    {
        return currentHealth.Value;
    }
}