using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }
    
    [SerializeField] private List<Transform> spawnPoints;
    private int nextSpawnIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("❌ Không có spawn point nào!");
            return Vector3.zero;
        }

        Vector3 pos = spawnPoints[nextSpawnIndex].position;
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;
        return pos;
    }

    // ✅ Lấy vị trí spawn theo index cụ thể
    public Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("❌ Không có spawn point nào!");
            return Vector3.zero;
        }

        if (index < 0 || index >= spawnPoints.Count)
        {
            Debug.LogWarning($"⚠️ Index {index} ngoài phạm vi, dùng index 0");
            index = 0;
        }

        return spawnPoints[index].position;
    }

    // ✅ Lấy index hiện tại mà không tăng
    public int GetCurrentSpawnIndex()
    {
        return nextSpawnIndex;
    }

    // ✅ Tăng index thủ công
    public void AdvanceSpawnIndex()
    {
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;
    }

    // ✅ Lấy tổng số spawn point
    public int GetTotalSpawnPoints()
    {
        return spawnPoints != null ? spawnPoints.Count : 0;
    }

    public void ResetSpawnCycle()
    {
        nextSpawnIndex = 0;
    }
}