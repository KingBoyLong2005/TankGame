using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Camera mainCamera;
    public LayerMask wallLayer;

    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f; // Tốc độ tối đa
    [SerializeField] private float rotationSpeed = 150f; // Tốc độ quay
    [SerializeField] private float acceleration = 5f; // Gia tốc khi bắt đầu di chuyển
    [SerializeField] private float deceleration = 3f; // Giảm tốc khi dừng

    [Header("Input Actions")]
    [SerializeField] private InputAction moveAction; // Action for movement
    [SerializeField] private InputAction mousePositionAction; // Action for mouse position

    [Header("Turret")]
    [SerializeField] private GameObject turretTransform; // Transform của turret

    [Header("Rotation Offsets")]
    public float bodyRotationOffset = -90f;   // offset cho Player
    public float turretRotationOffset = -90f; // offset cho Turret

    private PlayerSetup playerSetup;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private Collider2D col;
    private float targetBodyAngle;
    private float currentBodyAngle;
    private bool isInputEnabled = false; // Kiểm soát trạng thái input

    private void OnEnable()
    {
        // Chỉ kích hoạt input nếu không ở Lobby Scene
        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            moveAction.Enable();
            mousePositionAction.Enable();
            isInputEnabled = true;
        }
    }

    private void OnDisable()
    {
        moveAction.Disable();
        mousePositionAction.Disable();
        isInputEnabled = false;
    }

    private void Start()
    {   
        StartCoroutine(WaitForCamera());
        playerSetup = GetComponent<PlayerSetup>();
        if (playerSetup != null && playerSetup.IsOwner)
        {
            mainCamera = playerSetup.LocalCamera;
            if (mainCamera == null)
                mainCamera = Camera.main; // fallback
        }
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = false;
        col = GetComponent<Collider2D>();
    }
    private void Update()
    {
        // Chỉ xử lý input nếu được phép
        if (!isInputEnabled || playerSetup == null || !playerSetup.IsOwner)
            return;

        // Đọc đầu vào di chuyển
        moveInput = moveAction.ReadValue<Vector2>();

        // ✅ Xoay turret an toàn
        if (turretTransform != null )
        {
            Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
            Vector3 mousePos3D = new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane);
            Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mousePos3D);
            worldMousePos.z = 0f;

            Vector2 dir = (Vector2)(worldMousePos - turretTransform.transform.position);
            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + turretRotationOffset;
                turretTransform.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            // Debug
            Debug.DrawLine(turretTransform.transform.position, worldMousePos, Color.yellow);
        }
        // else if (mainCamera == null)
        // {
        //     Debug.LogWarning($"[{name}] mainCamera chưa được gán (turret không xoay).");
        // }
    }

    private void FixedUpdate()
    {
        // Chỉ xử lý di chuyển nếu được phép
        if (!isInputEnabled) return;

        // Xoay thân xe chỉ khi có đầu vào di chuyển
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

        // Movement direction (always along the tank's forward direction)
        Vector2 forward = transform.up;

        // Current speed along forward direction
        float currentSpeed = Vector2.Dot(rb.linearVelocity, forward);

        // Target speed based on input magnitude (clamped to maxSpeed)
        float targetSpeed = moveInput.magnitude * maxSpeed;

        // Choose acceleration or deceleration
        float accelRate = (moveInput.magnitude > 0.01f) ? acceleration : deceleration;

        // Move toward target speed
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

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

    // Phương thức để kích hoạt input khi chuyển sang scene chơi
    public void EnableInput()
    {
        if (!isInputEnabled)
        {
            moveAction.Enable();
            mousePositionAction.Enable();
            isInputEnabled = true;

            // Kích hoạt camera nếu cần
            if (playerSetup != null && playerSetup.LocalCamera != null)
            {
                playerSetup.LocalCamera.enabled = true;
                var cc = playerSetup.LocalCamera.GetComponentInParent<CinemachineCamera>();
                if (cc != null) cc.enabled = true;
                var listener = playerSetup.LocalCamera.GetComponentInParent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
        }
    }
    private IEnumerator WaitForCamera()
    {
        int safety = 0;
        while (mainCamera == null && safety < 120) // tối đa chờ 1 giây
        {
            if (playerSetup != null && playerSetup.IsOwner)
            {
                mainCamera = playerSetup.LocalCamera;
                if (mainCamera == null)
                    mainCamera = Camera.main;
            }
            safety++;
            yield return null; // chờ 1 frame
        }

        if (mainCamera == null)
            Debug.LogError($"[{name}] ❌ Không thể tìm thấy camera cho player owner sau 1 giây!");
        else
            Debug.Log($"[{name}] ✅ Camera đã gán thành công: {mainCamera.name}");
}
}