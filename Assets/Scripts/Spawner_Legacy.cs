/*
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject entityToSpawn;

    public SpawnManagerScriptableObject spawnManagerValues;

    int instanceNumber = 1;

    void Start()
    {
        SpawnEntities();
    }

    void SpawnEntities()
    {
        int currentSpawnPointIndex = 0;

        for (int i = 0; i < spawnManagerValues.numberOfPrefabsToCreate; i++)
        {
            GameObject currentEntity = Instantiate(entityToSpawn, spawnManagerValues.spawnPoints[currentSpawnPointIndex], Quaternion.identity);

            currentEntity.name = spawnManagerValues.prefabName + instanceNumber;

            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnManagerValues.spawnPoints.Length;

            instanceNumber++;
        }
    }
}
*/