using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class AITest : NetworkBehaviour
{
    [Header("NavMesh")]
    private NavMeshAgent agent;

    [Header("Targeting")]
    public float detectRange = 10f;
    public float attackRange = 5f;
    public float keepDistance = 3f;
    public float circleSpeed = 2f;

    [Header("Tank Movement")]
    public float rotationSpeed = 200f;
    public float acceleration = 5f;
    public float deceleration = 6f;
    public float maxSpeed = 3f;
    public float bodyRotationOffset = -90f;
    public float turretRotationOffset = -90f;

    private Rigidbody2D rb;
    private float currentSpeed;
    private Transform target;

    [Header("Turret")]
    public Transform turretTransform;
    public LayerMask obstacleMask;

    // --- Thêm ---
    private AIShooting aiShooting;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateUpAxis = false;
        agent.updateRotation = false;

        aiShooting = GetComponent<AIShooting>();
    }

    private void Update()
    {
        // Bot chỉ chạy logic trên Server (Netcode best practice)
        if (!IsServer) return;

        // --- Tìm mục tiêu ---
        if (target == null || Vector2.Distance(transform.position, target.position) > detectRange)
            target = FindClosestTarget();

        if (target == null)
        {
            StopMovement();
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);
        Vector2 movePos;

        if (dist > attackRange)
        {
            // Đi gần hơn
            movePos = target.position;
        }
        else
        {
            bool hasLOS = HasLineOfSight(target);

            if (!hasLOS)
            {
                // Nếu bị tường che, tìm chỗ có thể bắn
                movePos = FindCoverPosition(target);
            }
            else
            {
                // Nếu thấy target rõ → di chuyển vòng quanh và bắn
                float angle = Time.time * circleSpeed;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * keepDistance;
                movePos = (Vector2)target.position + offset;

                // --- Bắn ---
                if (aiShooting != null && turretTransform != null)
                {
                    Vector2 dir = (target.position - turretTransform.position).normalized;
                    aiShooting.TryShoot(dir);
                }
            }
        }

        MoveTowards(movePos);

        // --- Xoay turret ---
        if (target != null)
            RotateTurretTowards(target.position);
    }

    private void MoveTowards(Vector2 destination)
    {
        agent.SetDestination(destination);

        Vector2 desiredVelocity = agent.desiredVelocity;
        Vector2 moveDir = desiredVelocity.normalized;
        float targetSpeed = desiredVelocity.magnitude;

        // --- Xoay thân xe ---
        if (moveDir.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg + bodyRotationOffset;
            float currentAngle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
        }

        // --- Tăng giảm tốc ---
        float accelRate = (targetSpeed > 0.1f) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);

        // --- Di chuyển ---
        Vector2 forward = transform.right;
        rb.linearVelocity = forward * currentSpeed;
    }

    private void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
        currentSpeed = 0f;
    }

    private void RotateTurretTowards(Vector2 targetPos)
    {
        if (turretTransform == null) return;

        Vector2 direction = targetPos - (Vector2)turretTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + turretRotationOffset;
        turretTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Transform FindClosestTarget()
    {
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        GameObject[] tanks = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject t in tanks)
        {
            if (t == gameObject) continue;
            float dist = Vector2.Distance(transform.position, t.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestTarget = t.transform;
            }
        }
        return closestTarget;
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;
        float dist = Vector2.Distance(transform.position, target.position);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    private Vector2 FindCoverPosition(Transform target)
    {
        Vector2 dirToTarget = (target.position - transform.position).normalized;
        Vector2 backPos = (Vector2)transform.position - dirToTarget * keepDistance;

        Vector2 leftCover = backPos + Vector2.Perpendicular(dirToTarget) * 1.5f;
        Vector2 rightCover = backPos - Vector2.Perpendicular(dirToTarget) * 1.5f;

        bool leftHasWall = Physics2D.Raycast(leftCover, dirToTarget, attackRange, obstacleMask);
        bool rightHasWall = Physics2D.Raycast(rightCover, dirToTarget, attackRange, obstacleMask);

        if (leftHasWall && !rightHasWall) return leftCover;
        if (rightHasWall && !leftHasWall) return rightCover;
        return Random.value > 0.5f ? leftCover : rightCover;
    }
}
