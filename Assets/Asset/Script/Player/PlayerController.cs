using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f; // Tốc độ tối đa
    [SerializeField] private float rotationSpeed = 150f; // Tốc độ quay
    [SerializeField] private float acceleration = 5f; // Gia tốc khi bắt đầu di chuyển
    [SerializeField] private float deceleration = 3f; // Giảm tốc khi dừng

    [Header("Input Actions")]
    [SerializeField] private InputAction moveAction; // Action for movement (WASD or Arrow keys as Vector2)
    [SerializeField] private InputAction mousePositionAction; // Action for mouse position (Pointer Position)


    [Header("Turret")]
    [SerializeField] private GameObject turretTransform; // Transform của turret (child object)

    [Header("Rotation Offsets")]
    public float bodyRotationOffset = -90f;   // offset cho Player
    public float turretRotationOffset = -90f; // offset cho Turret

    private PlayerSetup playerSetup; // Lưu tham chiếu PlayerSetup
    private Vector2 moveInput;
    private Rigidbody2D rb;

    private float targetBodyAngle;   // góc thân muốn xoay tới
    private float currentBodyAngle;  // góc thân hiện tại
    
    private void OnEnable()
    {
        moveAction.Enable();
        mousePositionAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        mousePositionAction.Disable();
    }

    private void Start()
    {   
        playerSetup = GetComponent<PlayerSetup>();
        if (playerSetup != null && playerSetup.IsOwner) 
        {
            mainCamera = playerSetup.LocalCamera;
        }
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Đảm bảo không có trọng lực
        rb.freezeRotation = false; // Cho phép quay
        // mainCamera = Camera.main; // Lấy camera chính
    }

    private void Update()
    {
        // Chỉ xử lý input và xoay turret cho người chơi local
        if (playerSetup != null && playerSetup.IsOwner)
        {
            // Đọc đầu vào di chuyển
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
        }
    }

    private void FixedUpdate()
    {
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

        // Update velocity along forward direction (no sideways sliding)
        rb.linearVelocity = forward * newSpeed;
    }
    // private Camera myCamera;

    // [Header("Movement Settings")]
    // public float moveSpeed = 5f;            // tốc độ tiến
    // public float bodyRotationSpeed = 180f;  // độ/giây, tốc độ xoay thân

    // [Header("Rotation Offsets")]
    // public float bodyRotationOffset = -90f;   // offset cho Player
    // public float turretRotationOffset = -90f; // offset cho Turret


    // private Vector2 moveInput;
    // private Vector2 lookDelta; // lấy delta từ Input System
    // private Vector2 virtualMousePos; // vị trí ảo của chuột trên màn hình
    // private Rigidbody2D rb;

    // [Header("References")]
    // public GameObject Turret;

    // private float targetBodyAngle;   // góc thân muốn xoay tới
    // private float currentBodyAngle;  // góc thân hiện tại

    // void Start()
    // {
    //     var setup = GetComponent<PlayerSetup>();
    // if (setup != null && setup.IsOwner) 
    // {
    //     myCamera = setup.LocalCamera;
    // }
    // }
    // private void Awake()
    // {
    //     rb = GetComponent<Rigidbody2D>();
    //     currentBodyAngle = rb.rotation;

    //     // Khởi tạo "chuột ảo" ở giữa màn hình
    //     virtualMousePos = Input.mousePosition;
    // }

    // // Gọi bởi PlayerInput khi action "Move"
    // private void OnMove(InputValue value)
    // {
    //     moveInput = value.Get<Vector2>();
    // }

    // // Gọi bởi PlayerInput khi action "Look" (delta)
    // private void OnLook(InputValue value)
    // {
    //     lookDelta = value.Get<Vector2>();
    // }

    // private void FixedUpdate()
    // {
    //     // Nếu có input thì xác định góc target
    //     if (moveInput.sqrMagnitude > 0.001f)
    //     {
    //         targetBodyAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + bodyRotationOffset;
    //     }

    //     // Quay mượt thân (dùng MoveTowardsAngle để tránh nhảy)
    //     currentBodyAngle = Mathf.MoveTowardsAngle(currentBodyAngle, targetBodyAngle, bodyRotationSpeed * Time.fixedDeltaTime);
    //     rb.MoveRotation(currentBodyAngle);

    //     // Tiến về phía trước theo hướng thân
    //     Vector2 forward = new Vector2(Mathf.Cos((currentBodyAngle - bodyRotationOffset) * Mathf.Deg2Rad),
    //                                   Mathf.Sin((currentBodyAngle - bodyRotationOffset) * Mathf.Deg2Rad));

    //     rb.MovePosition(rb.position + forward * moveInput.magnitude * moveSpeed * Time.fixedDeltaTime);
    // }

    // private void Update()
    // {
    //     if (Turret == null || myCamera == null) return;

    //     // dùng myCamera thay vì LocalCamera static
    //     Vector3 mouseWorldPosition = myCamera.ScreenToWorldPoint(
    //         new Vector3(lookDelta.x, lookDelta.y, myCamera.nearClipPlane)
    //     );
    //     mouseWorldPosition.z = transform.position.z;

    //     Vector3 direction = mouseWorldPosition - transform.position;
    //     float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    //     Turret.transform.rotation = Quaternion.Euler(0, 0, angle + turretRotationOffset);
    // }
}
