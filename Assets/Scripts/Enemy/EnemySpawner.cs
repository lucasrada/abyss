using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    public int totalEnemiestoSpawn = 5;
    public float spawnOffset = 0.5f; // small random offset to avoid stacking

    // Runtime
    private List<GameObject> aliveEnemies = new List<GameObject>();

    /// <summary>
    /// Spawns enemies in the dungeon at random walkable positions near spawnPos.
    /// Pass playerT so enemies can target the player.
    /// </summary>
    public void SpawnEnemiesInDungeon(Dungeon dungeon, Vector3 spawnPos, Transform playerT)
    {
        if (enemyPrefab == null || dungeon == null)
        {
            Debug.LogWarning("[EnemySpawner] Cannot spawn: missing prefab or dungeon.");
            return;
        }

        aliveEnemies.Clear();

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = totalEnemiestoSpawn * 10; // prevent infinite loops

        while (spawned < totalEnemiestoSpawn && attempts < maxAttempts)
        {
            attempts++;

            // pick random position near spawnPos
            Vector2 randomPos = new Vector2(
                spawnPos.x + Random.Range(-2f, 2f),
                spawnPos.y + Random.Range(-2f, 2f)
            );

            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(randomPos.x), Mathf.RoundToInt(randomPos.y));

            // check if inside dungeon and walkable
            if (!dungeon.IsInBounds(gridPos.x, gridPos.y) || !dungeon.IsFloor(gridPos.x, gridPos.y))
                continue;

            // instantiate enemy
            GameObject enemyGO = Instantiate(enemyPrefab, randomPos, Quaternion.identity, transform);
            EnemyController ec = enemyGO.GetComponent<EnemyController>();
            if (ec != null)
            {
                ec.InitializeWithDungeon(dungeon, playerT);
            }

            aliveEnemies.Add(enemyGO);
            spawned++;
        }

        if (spawned < totalEnemiestoSpawn)
        {
            Debug.LogWarning($"[EnemySpawner] Only spawned {spawned}/{totalEnemiestoSpawn} enemies.");
        }
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
