using UnityEngine;
using Unity.Netcode;

public class AIShooting : NetworkBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float fireCooldown = 1f;

    private float lastShootTime;

    // --- Hàm bắn cho AI ---
    public void TryShoot(Vector2 direction)
    {
        if (Time.time - lastShootTime < fireCooldown) return;

        if (IsServer)
        {
            SpawnBulletServerRpc(firePoint.position, direction);
        }
        else if (IsHost)
        {
            // host cũng có quyền điều khiển bot
            SpawnBulletServerRpc(firePoint.position, direction);
        }

        lastShootTime = Time.time;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnBulletServerRpc(Vector2 spawnPos, Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir * bulletSpeed;

        var netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            bullet.GetComponent<Bullet>()?.SetOwner(GetComponent<NetworkObject>());
            netObj.Spawn(true);
        }
    }
}
