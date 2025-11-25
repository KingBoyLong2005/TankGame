using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerControllerTest : MonoBehaviour
{
    private Camera mainCamera;
    public LayerMask wallLayer;

    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float rotationSpeed = 150f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 3f;

    [Header("Input Actions")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction mousePositionAction;

    [Header("Turret")]
    [SerializeField] private GameObject turretTransform;

    [Header("Rotation Offsets")]
    public float bodyRotationOffset = -90f;
    public float turretRotationOffset = -90f;

    private Vector2 moveInput;
    private Rigidbody2D rb;
    private Collider2D col;
    private float targetBodyAngle;
    private float currentBodyAngle;
    private bool useMobileInput = false;
    private Vector2 mobileInput = Vector2.zero;
    public void SetMobileInput(Vector2 input)
    {
        useMobileInput = true;
        mobileInput = input;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = false;

        mainCamera = Camera.main;
        moveAction.Enable();
        mousePositionAction.Enable();
    }

    private void Update()
    {
        // Kiểm tra các component cần thiết
        if (turretTransform == null || mainCamera == null || moveAction == null || mousePositionAction == null) return;

        // Đọc đầu vào di chuyển
        if (useMobileInput)
            moveInput = mobileInput;
        else
            moveInput = moveAction.ReadValue<Vector2>();

        // Xoay turret theo chuột
        if (turretTransform != null && mainCamera != null)
        {
            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mousePos);
            Vector2 direction = (Vector2)(worldMousePos - turretTransform.transform.position);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + turretRotationOffset;
            turretTransform.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        #if UNITY_ANDROID
        GameObject playerObj = GameObject.FindGameObjectWithTag("Bot");
        if (playerObj != null)
        {
            Transform target = playerObj.transform;
            RotateTurretTowards(target.position);
        }
        #endif
    }

    private void FixedUpdate()
    {
        if (moveInput.magnitude > 0.01f)
        {
            Vector2 targetDirection = moveInput.normalized;
            targetBodyAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg + bodyRotationOffset;
            currentBodyAngle = Mathf.MoveTowardsAngle(
                transform.eulerAngles.z,
                targetBodyAngle,
                rotationSpeed * Time.fixedDeltaTime
            );
            transform.rotation = Quaternion.Euler(0f, 0f, currentBodyAngle);
        }

        Vector2 forward = transform.up;
        float currentSpeed = Vector2.Dot(rb.linearVelocity, forward);
        float targetSpeed = moveInput.magnitude * maxSpeed;
        float accelRate = (moveInput.magnitude > 0.01f) ? acceleration : deceleration;
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

        // ======== 🧱 Kiểm tra va chạm tường bằng Raycast ========
        if (moveInput.magnitude > 0.01f)
        {
            float moveDistance = newSpeed * Time.fixedDeltaTime;
            Vector2 boxSize = col.bounds.size * 0.9f; // giảm nhẹ 10% để tránh kẹt mép

            RaycastHit2D hit = Physics2D.BoxCast(
                rb.position,
                boxSize,
                0f,
                forward,
                moveDistance,
                wallLayer
            );

            // Vẽ debug box để xem vùng kiểm tra
            Color rayColor = (hit.collider != null) ? Color.red : Color.green;
            Debug.DrawRay(rb.position, forward * moveDistance, rayColor, 0.1f);

            if (hit.collider == null)
            {
                rb.linearVelocity = forward * newSpeed; // Không có tường → di chuyển bình thường
            }
            else
            {
                rb.linearVelocity = Vector2.zero; // Có tường → dừng lại
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
    #if UNITY_ANDROID
    private void RotateTurretTowards(Vector2 targetPos)
    {
        if (turretTransform == null) return;

        Vector2 direction = targetPos - (Vector2)turretTransform.transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + turretRotationOffset;
        turretTransform.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
    #endif
}
