using UnityEngine;
using Unity.Netcode;

public class BotHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(true);

    public System.Action<BotHealth> OnBotDied; // callback để AI Manager biết bot chết

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            IsAlive.Value = true;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (IsOwner == false && IsClient)
        {
            // client-side: có thể hiển thị máu hoặc hiệu ứng
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer || !IsAlive.Value) return;

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

        // Gọi callback cho AI Manager (nếu có)
        OnBotDied?.Invoke(this);

        // Ẩn bot, tạm thời chưa despawn để có thể respawn lại
        DisableBot();

        // Nếu bạn có hệ thống quản lý bot (BotManager)
        // GameManager.Instance.CheckAliveEntities();
    }

    private void DisableBot()
    {
        // Ẩn sprite và tắt va chạm
        foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            r.enabled = false;

        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        var ai = GetComponent<AITest>();
        if (ai != null) ai.enabled = false;
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;
        currentHealth.Value = maxHealth;
        IsAlive.Value = true;

        foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            r.enabled = true;

        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = true;

        var ai = GetComponent<AITest>();
        if (ai != null) ai.enabled = true;
    }

    public int GetCurrentHealth() => currentHealth.Value;
}
