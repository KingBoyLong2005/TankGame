using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("SpawnPointManager: No spawn points defined. Returning default position.");
            return Vector3.zero;
        }

        if (index < 0 || index >= spawnPoints.Count)
        {
            Debug.LogWarning($"SpawnPointManager: Index {index} out of range. Using first spawn point.");
            return spawnPoints[0].position;
        }

        return spawnPoints[index].position;
    }

    public int GetSpawnPointCount()
    {
        return spawnPoints.Count;
    }
}