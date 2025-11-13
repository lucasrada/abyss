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

    [Header("Difficulty Scaling")]
    [Tooltip("Salud relativa de los enemigos en el nivel 1.")]
    public float baseHealthMultiplier = 0.6f;
    [Tooltip("Incremento lineal de salud por nivel.")]
    public float healthPerLevel = 0.12f;
    [Tooltip("Variación aleatoria de la salud (+/-).")]
    public float healthRandomVariance = 0.15f;
    [Tooltip("Daño relativo de los enemigos en el nivel 1.")]
    public float baseDamageMultiplier = 0.9f;
    [Tooltip("Incremento lineal del daño por nivel.")]
    public float damagePerLevel = 0.08f;

    public void SpawnEnemiesInDungeon(Dungeon dungeon, Vector3 spawnPos, Transform playerT, int enemyCount, int level = 1)
    {
        if (enemyPrefab == null || dungeon == null)
        {
            Debug.LogWarning("[EnemySpawner] Cannot spawn: missing prefab or dungeon.");
            return;
        }

        aliveEnemies.Clear();

        Vector2 spawnCenterOffset = (Vector2)(spawnPos - (Vector3)dungeon.GetStartLocation());

        List<Vector2> spawnPositions = spreadAcrossRooms
            ? GetDistributedSpawnPositions(dungeon, playerT, enemyCount, spawnCenterOffset)
            : GetNearbySpawnPositions(dungeon, playerT, enemyCount, spawnCenterOffset);

        int spawned = 0;
        foreach (Vector2 pos in spawnPositions)
        {
            if (spawned >= enemyCount) break;

            GameObject enemyGO = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
            EnemyController ec = enemyGO.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.InitializeWithDungeon(dungeon, playerT);
                ApplyDifficulty(ec, level);
            }

            aliveEnemies.Add(enemyGO);
            spawned++;
        }

        if (spawned < enemyCount)
        {
            Debug.LogWarning($"[EnemySpawner] Only spawned {spawned}/{enemyCount} enemies.");
        }
    }

    private List<Vector2> GetDistributedSpawnPositions(Dungeon dungeon, Transform playerT, int enemyCount, Vector2 gridToWorldOffset)
    {
        List<Vector2> positions = new List<Vector2>();
        List<Room> rooms = dungeon.GetRooms();
        List<Vector2> candidatePositions = new List<Vector2>();
        Vector2 playerPos = playerT ? (Vector2)playerT.position : Vector2.zero;

        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No rooms found in dungeon. Falling back to nearby spawning.");
            return GetNearbySpawnPositions(dungeon, playerT, enemyCount, gridToWorldOffset);
        }

        foreach (Room room in rooms)
        {
            List<Vector2> roomFloors = room.GetAllFloorPositions();
            foreach (Vector2 pos in roomFloors)
            {
                Vector2 worldPos = GridToWorld(pos, gridToWorldOffset);
                if (playerT != null && Vector2.Distance(worldPos, playerPos) < minDistanceFromPlayer)
                    continue;

                candidatePositions.Add(worldPos);
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
                Vector2 jitter = new Vector2(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset));
                positions.Add(candidate + jitter);
            }

            candidatePositions.RemoveAt(randomIndex);
        }

        return positions;
    }

    private void ApplyDifficulty(EnemyController enemy, int level)
    {
        if (enemy == null) return;
        level = Mathf.Max(1, level);

        float baseHealth = baseHealthMultiplier + (level - 1) * healthPerLevel;
        baseHealth = Mathf.Max(0.1f, baseHealth);
        float variance = 1f;
        if (healthRandomVariance > 0f)
        {
            variance = Random.Range(1f - healthRandomVariance, 1f + healthRandomVariance);
        }
        float healthMultiplier = Mathf.Max(0.1f, baseHealth * variance);
        float damageMultiplier = Mathf.Max(0.1f, baseDamageMultiplier + (level - 1) * damagePerLevel);

        enemy.ApplyDifficultyScaling(healthMultiplier, damageMultiplier);
    }

    private List<Vector2> GetNearbySpawnPositions(Dungeon dungeon, Transform playerT, int enemyCount, Vector2 gridToWorldOffset)
    {
        List<Vector2> positions = new List<Vector2>();
        int attempts = 0;
        int maxAttempts = enemyCount * 10;
        Vector2 playerPosWorld = playerT ? (Vector2)playerT.position : Vector2.zero;
        Vector2Int playerGrid = playerT
            ? Vector2Int.RoundToInt((Vector2)playerT.position - gridToWorldOffset)
            : Vector2Int.zero;

        while (positions.Count < enemyCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2 randomOffset = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            Vector2Int gridPos = playerGrid + Vector2Int.RoundToInt(randomOffset);

            if (!dungeon.IsInBounds(gridPos.x, gridPos.y) || !dungeon.IsFloor(gridPos.x, gridPos.y))
                continue;

            Vector2 worldPos = GridToWorld(gridPos, gridToWorldOffset);

            if (playerT != null && Vector2.Distance(worldPos, playerPosWorld) < minDistanceFromPlayer)
                continue;

            bool tooClose = false;
            foreach (Vector2 existing in positions)
            {
                if (Vector2.Distance(worldPos, existing) < minDistanceBetweenEnemies)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                Vector2 jitter = new Vector2(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset));
                positions.Add(worldPos + jitter);
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

    private static Vector2 GridToWorld(Vector2 gridPosition, Vector2 offset)
    {
        return gridPosition + offset;
    }
}
