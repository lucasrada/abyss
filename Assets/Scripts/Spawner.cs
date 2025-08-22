using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Spawner : MonoBehaviour
{
    private Transform target;
    public GameObject entityToSpawn;
    public SpawnManagerScriptableObject spawnManagerValues;
    int instanceNumber = 1;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        SpawnEntities();
    }

    void SpawnEntities()
    {
        int currentSpawnPointIndex = 0;

        for (int i = 0; i < spawnManagerValues.numberOfPrefabsToCreate; i++)
        {
            GameObject currentEntity = Instantiate(entityToSpawn, new Vector2(target.position.x, target.position.y) + Random.insideUnitCircle * 5, Quaternion.identity);
            currentEntity.name = spawnManagerValues.prefabName + instanceNumber;
            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnManagerValues.spawnPoints.Length;
            instanceNumber++;
        }
    }
}
