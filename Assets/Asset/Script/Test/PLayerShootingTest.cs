using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShootingTest : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float bulletSpeed = 10f;

    [Header("Input Actions")]
    [SerializeField] private InputAction fireAction;

    private float nextFireTime = 0f;
    private bool Fire = false;

    private void Start()
    {
        fireAction.Enable();
    }

    private void Update()
    {
        if (Time.time >= nextFireTime)
        {
            if (Fire || fireAction.IsPressed())
            {
                nextFireTime = Time.time + fireRate;
                Shoot();
                Fire = false;
            }
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.linearVelocity = firePoint.up * bulletSpeed;
        }
    }
    public void ShootTrue()
    {
        Fire = true;
    }
}
