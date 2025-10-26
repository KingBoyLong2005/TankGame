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
        Vector3 pos = spawnPoints[nextSpawnIndex].position;
        nextSpawnIndex = (nextSpawnIndex + 1) % spawnPoints.Count;
        return pos;
    }

    public void ResetSpawnCycle()
    {
        nextSpawnIndex = 0;
    }
}