using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class PlayerShooting : NetworkBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab; // Prefab bullet
    [SerializeField] private Transform firePoint; // Điểm bắn (child của turret, ví dụ: đầu nòng súng)
    [SerializeField] private float fireRate = 0.5f; // Tốc độ bắn (giây/lần)
    [SerializeField] private float bulletSpeed = 10f; // Tốc độ đạn

    [Header("Input Actions")]
    [SerializeField] private InputAction fireAction; // Action cho bắn (Button)

    private float nextFireTime = 0f;
    private PlayerSetup playerSetup;
    private bool isInputEnabled = false;

    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            fireAction.Enable();
            isInputEnabled = true;
        }
    }

    private void OnDisable()
    {
        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            fireAction.Disable();
            isInputEnabled = false;
        }
    }

    private void Start()
    {
        playerSetup = GetComponent<PlayerSetup>();
    }

    private void Update()
    {
        if (!isInputEnabled && playerSetup != null && playerSetup.IsOwner && fireAction.IsPressed() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            ShootServerRpc();
        }
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        if (!IsServer) return;

        // Spawn bullet trên server
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.GetComponent<NetworkObject>().Spawn();

        // Đẩy đạn theo hướng turret
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.linearVelocity = firePoint.up * bulletSpeed; // Giả sử firePoint hướng lên

        // Gán owner để tránh tự bắn (trong Bullet script)
        bullet.GetComponent<Bullet>().SetOwner(NetworkObject);
    }
}