using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("Enemy prefab to spawn")]
    public GameObject enemyPrefab;
    [Tooltip("Number of enemies to spawn total")]
    public int totalEnemiestoSpawn = 10;
    [Tooltip("Minimum distance from player spawn")]
    public float minDistanceFromPlayer = 3f;
    [Tooltip("Minimum distance between enemies")]
    public float minDistanceBetweenEnemies = 2f;

    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Transform enemyParent;

    public void SpawnEnemiesInDungeon(Dungeon dungeon, Vector3 playerSpawnPosition)
    {
        ClearEnemies();
        
        if (enemyParent == null)
        {
            enemyParent = new GameObject("Enemies").transform;
        }

        // Get ALL floor positions by checking every coordinate in the dungeon
        List<Vector2Int> allFloorPositions = GetAllFloorPositions(dungeon);
        
        if (allFloorPositions.Count == 0)
        {
            Debug.LogError("No floor positions found in dungeon!");
            return;
        }

        Debug.Log($"Found {allFloorPositions.Count} floor positions in dungeon");

        // Convert player spawn to grid coordinates for comparison
        Vector2Int playerGridPos = new Vector2Int(
            Mathf.FloorToInt(playerSpawnPosition.x), 
            Mathf.FloorToInt(playerSpawnPosition.y)
        );

        // Filter out positions too close to player
        List<Vector2Int> validPositions = new List<Vector2Int>();
        foreach (Vector2Int pos in allFloorPositions)
        {
            float distance = Vector2.Distance(pos, playerGridPos);
            if (distance >= minDistanceFromPlayer)
            {
                validPositions.Add(pos);
            }
        }

        Debug.Log($"Found {validPositions.Count} valid positions after filtering player distance");

        // Spawn enemies with proper spacing
        int enemiesSpawned = 0;
        List<Vector2Int> usedPositions = new List<Vector2Int>();

        for (int attempt = 0; attempt < 1000 && enemiesSpawned < totalEnemiestoSpawn && validPositions.Count > 0; attempt++)
        {
            int randomIndex = Random.Range(0, validPositions.Count);
            Vector2Int candidatePos = validPositions[randomIndex];

            // Check if too close to other spawned enemies
            bool tooClose = false;
            foreach (Vector2Int usedPos in usedPositions)
            {
                if (Vector2.Distance(candidatePos, usedPos) < minDistanceBetweenEnemies)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                // Convert grid position to world position
                Vector3 worldPos = new Vector3(candidatePos.x + 0.5f, candidatePos.y + 0.5f, 0f);
                
                // Double-check this is still a floor position
                if (dungeon.IsFloor(candidatePos.x, candidatePos.y))
                {
                    SpawnEnemyAtPosition(worldPos);
                    usedPositions.Add(candidatePos);
                    enemiesSpawned++;
                    
                    Debug.Log($"Spawned enemy {enemiesSpawned} at grid ({candidatePos.x}, {candidatePos.y}) = world {worldPos}");
                }
            }

            // Remove this position from candidates to avoid infinite loops
            validPositions.RemoveAt(randomIndex);
        }

        Debug.Log($"Successfully spawned {enemiesSpawned} enemies in dungeon");
    }

    private List<Vector2Int> GetAllFloorPositions(Dungeon dungeon)
    {
        List<Vector2Int> floorPositions = new List<Vector2Int>();
        char[,] layout = dungeon.GetLayout();
        
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);

        Debug.Log($"Scanning dungeon layout: {width} x {height}");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Use the dungeon's own methods to check if it's a floor
                if (dungeon.IsFloor(x, y))
                {
                    floorPositions.Add(new Vector2Int(x, y));
                }
            }
        }

        return floorPositions;
    }

    private void SpawnEnemyAtPosition(Vector3 position)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab is not assigned!");
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.transform.parent = enemyParent;
        
        // Set layer if it exists
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            enemy.layer = enemyLayer;
        }
        
        spawnedEnemies.Add(enemy);
    }

    public void ClearEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                DestroyImmediate(enemy);
        }
        
        spawnedEnemies.Clear();
        
        if (enemyParent != null)
        {
            DestroyImmediate(enemyParent.gameObject);
            enemyParent = null;
        }
    }

    public List<GameObject> GetAliveEnemies()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return new List<GameObject>(spawnedEnemies);
    }

    public int GetAliveEnemyCount()
    {
        return GetAliveEnemies().Count;
    }

    public void OnEnemyDestroyed(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
    }

    void OnDestroy()
    {
        ClearEnemies();
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (Application.isPlaying && spawnedEnemies != null)
        {
            Gizmos.color = Color.red;
            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawWireSphere(enemy.transform.position, 0.3f);
                }
            }
        }
    }
}