using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
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

    private PlayerSetup playerSetup;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private Collider2D col;
    private float targetBodyAngle;
    private float currentBodyAngle;
    private bool isInputEnabled = false;

    private bool useMobileInput = false;
    private Vector2 mobileInput = Vector2.zero;

    public void SetMobileInput(Vector2 input)
    {
        useMobileInput = true;
        mobileInput = input;
    }
    private void OnEnable()
    {
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
        playerSetup = GetComponent<PlayerSetup>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = false;
        col = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // Chỉ xử lý input nếu là owner
        if (playerSetup == null || !playerSetup.IsOwner) return;
        
        // Lấy camera nếu chưa có (camera có thể spawn sau)
        if (mainCamera == null && playerSetup.LocalCamera != null)
        {
            mainCamera = playerSetup.LocalCamera;
        }
        
        // Kiểm tra các component cần thiết
        if (turretTransform == null || mainCamera == null || moveAction == null || mousePositionAction == null) return;

        // Đọc đầu vào di chuyển
        if (useMobileInput)
            moveInput = mobileInput;
        else
            moveInput = moveAction.ReadValue<Vector2>();

        // Xoay turret theo chuột
        Vector2 mousePos = mousePositionAction.ReadValue<Vector2>();
        Vector3 worldMousePos = mainCamera.ScreenToWorldPoint(mousePos);
        Vector2 direction = (Vector2)(worldMousePos - turretTransform.transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + turretRotationOffset;
        turretTransform.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void FixedUpdate()
    {
        if (!isInputEnabled || playerSetup == null || !playerSetup.IsOwner) return;

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

        Vector2 forward = transform.up;
        float currentSpeed = Vector2.Dot(rb.linearVelocity, forward);
        float targetSpeed = moveInput.magnitude * maxSpeed;
        float accelRate = (moveInput.magnitude > 0.01f) ? acceleration : deceleration;
        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

        if (moveInput.magnitude > 0.01f)
        {
            float moveDistance = newSpeed * Time.fixedDeltaTime;
            Vector2 boxSize = col.bounds.size * 0.9f;

            RaycastHit2D hit = Physics2D.BoxCast(
                rb.position,
                boxSize,
                0f,
                forward,
                moveDistance,
                wallLayer
            );

            Color rayColor = (hit.collider != null) ? Color.red : Color.green;
            Debug.DrawRay(rb.position, forward * moveDistance, rayColor, 0.1f);

            if (hit.collider == null)
            {
                rb.linearVelocity = forward * newSpeed;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void EnableInput()
    {
        if (!isInputEnabled)
        {
            moveAction.Enable();
            mousePositionAction.Enable();
            isInputEnabled = true;

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
}