using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    public float spawnOffset = 0.5f;
    public float minDistanceBetweenEnemies = 3f;
    public float minDistanceFromPlayer = 5f;
    public bool spreadAcrossRooms = true;

    private List<GameObject> aliveEnemies = new List<GameObject>();

    public void SpawnEnemiesInDungeon(Dungeon dungeon, Vector3 spawnPos, Transform playerT, int enemyCount)
    {
        if (enemyPrefab == null || dungeon == null)
        {
            Debug.LogWarning("[EnemySpawner] Cannot spawn: missing prefab or dungeon.");
            return;
        }

        aliveEnemies.Clear();

        List<Vector2> spawnPositions = spreadAcrossRooms
            ? GetDistributedSpawnPositions(dungeon, spawnPos, playerT, enemyCount)
            : GetNearbySpawnPositions(dungeon, spawnPos, playerT, enemyCount);

        int spawned = 0;
        foreach (Vector2 pos in spawnPositions)
        {
            if (spawned >= enemyCount) break;

            GameObject enemyGO = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
            EnemyController ec = enemyGO.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.InitializeWithDungeon(dungeon, playerT);
            }

            aliveEnemies.Add(enemyGO);
            spawned++;
        }

        if (spawned < enemyCount)
        {
            Debug.LogWarning($"[EnemySpawner] Only spawned {spawned}/{enemyCount} enemies.");
        }
    }

    private List<Vector2> GetDistributedSpawnPositions(Dungeon dungeon, Vector3 spawnPos, Transform playerT, int enemyCount)
    {
        List<Vector2> positions = new List<Vector2>();
        List<Room> rooms = dungeon.GetRooms();
        List<Vector2> candidatePositions = new List<Vector2>();

        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No rooms found in dungeon. Falling back to nearby spawning.");
            return GetNearbySpawnPositions(dungeon, spawnPos, playerT, enemyCount);
        }

        foreach (Room room in rooms)
        {
            List<Vector2> roomFloors = room.GetAllFloorPositions();
            foreach (Vector2 pos in roomFloors)
            {
                if (playerT != null && Vector2.Distance(pos, playerT.position) < minDistanceFromPlayer)
                    continue;

                candidatePositions.Add(pos);
            }
        }

        int maxAttempts = enemyCount * 20;
        int attempts = 0;

        while (positions.Count < enemyCount && attempts < maxAttempts && candidatePositions.Count > 0)
        {
            attempts++;
            int randomIndex = Random.Range(0, candidatePositions.Count);
            Vector2 candidate = candidatePositions[randomIndex];

            bool tooClose = false;
            foreach (Vector2 existing in positions)
            {
                if (Vector2.Distance(candidate, existing) < minDistanceBetweenEnemies)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                positions.Add(candidate + new Vector2(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset)));
            }

            candidatePositions.RemoveAt(randomIndex);
        }

        return positions;
    }

    private List<Vector2> GetNearbySpawnPositions(Dungeon dungeon, Vector3 spawnPos, Transform playerT, int enemyCount)
    {
        List<Vector2> positions = new List<Vector2>();
        int attempts = 0;
        int maxAttempts = enemyCount * 10;

        while (positions.Count < enemyCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2 randomPos = new Vector2(
                spawnPos.x + Random.Range(-5f, 5f),
                spawnPos.y + Random.Range(-5f, 5f)
            );

            if (playerT != null && Vector2.Distance(randomPos, playerT.position) < minDistanceFromPlayer)
                continue;

            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(randomPos.x), Mathf.RoundToInt(randomPos.y));

            if (!dungeon.IsInBounds(gridPos.x, gridPos.y) || !dungeon.IsFloor(gridPos.x, gridPos.y))
                continue;

            bool tooClose = false;
            foreach (Vector2 existing in positions)
            {
                if (Vector2.Distance(randomPos, existing) < minDistanceBetweenEnemies)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                positions.Add(randomPos);
            }
        }

        return positions;
    }

    /// <summary>
    /// Called by EnemyController when it dies
    /// </summary>
    public void OnEnemyDestroyed(GameObject enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    public int GetAliveEnemyCount()
    {
        // clean nulls
        aliveEnemies.RemoveAll(e => e == null);
        return aliveEnemies.Count;
    }
}
