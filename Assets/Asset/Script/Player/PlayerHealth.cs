using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100; // Máu tối đa
    // [SerializeField] private GameObject deathEffect; // Hiệu ứng chết (nếu có, prefab particle)
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true);
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            currentHealth.Value = maxHealth; // reset chỉ trên server
        }
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
        if (!IsServer) return;

        IsAlive.Value = false;

        // Ẩn tạm player
        DisablePlayerClientRpc();

        GameManager.Instance.CheckAliveEntities();
    }
    [ClientRpc]
    private void DisablePlayerClientRpc()
    {
        // Chạy trên TẤT CẢ clients
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.enabled = false;

        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var c in colliders) c.enabled = false;

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
    }
    // private void DisablePlayer()
    // {
    //     // Tắt collider và renderer (không despawn)
    //     var renderers = GetComponentsInChildren<SpriteRenderer>();
    //     foreach (var r in renderers) r.enabled = false;

    //     var colliders = GetComponentsInChildren<Collider2D>();
    //     foreach (var c in colliders) c.enabled = false;

    //     var controller = GetComponent<PlayerController>();
    //     if (controller != null) controller.enabled = false;
    // }

    public void Respawn(Vector3 position)
    {
        if (!IsServer) return;
        
        transform.position = position;
        currentHealth.Value = maxHealth;
        IsAlive.Value = true;
        
        // Gọi ClientRpc để enable trên tất cả clients
        EnablePlayerClientRpc(position);
    }

    [ClientRpc]
    private void EnablePlayerClientRpc(Vector3 position)
    {
        transform.position = position;
        
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.enabled = true;

        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var c in colliders) c.enabled = true;

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;
    }

    public int GetCurrentHealth()
    {
        return currentHealth.Value;
    }
    public void ResetHealth()
    {
        currentHealth.Value = 50;
    }
}