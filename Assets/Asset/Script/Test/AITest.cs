using UnityEngine;
using UnityEngine.AI;

public class AITest : MonoBehaviour
{
    [SerializeField] Transform Player;

    NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateUpAxis = false;     // dùng cho 2D (không đẩy theo trục Y)
            agent.updateRotation = false;   // tắt xoay theo trục 3D
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Nav Mesh Agent");
        }
    }

    private void Update()
    {
        agent.SetDestination(Player.position);
    }
}
