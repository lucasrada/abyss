using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public LevelExit levelExitPrefab;
    public Transform player;
    public EnemySpawner enemySpawner;

    [Header("Level Settings")]
    [Tooltip("Spawn offset for positioning")]
    public Vector3 spawnOffset = new Vector3(0.5f, 0.5f);
    
    [Header("Level Progression")]
    [Tooltip("Increase enemy count per level")]
    public int enemyIncreasePerLevel = 1;
    [Tooltip("Current level number")]
    public int currentLevel = 1;

    private Dungeon currentDungeon;
    private Transform levelParent;

    // Events for other systems
    public System.Action<int> OnLevelStart;
    public System.Action<int> OnLevelComplete;

    private void Start()
    {
        // Validate references
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<EnemySpawner>();
            if (enemySpawner == null)
            {
                Debug.LogError("EnemySpawner not found! Please assign it in the inspector or add it to the scene.");
            }
        }

        Vector3 startPosition = NextLevel();
        if (player != null)
        {
            player.transform.position = startPosition;
        }
    }

    public Vector3 NextLevel()
    {
        // Clean up previous level
        if (levelParent != null)
        {
            Destroy(levelParent.gameObject);
        }

        // Create new level parent
        levelParent = new GameObject($"Level {currentLevel}").transform;

        // Generate new dungeon
        currentDungeon = dungeonGenerator.CreateDungeon();

        // Get spawn position
        Vector3 playerSpawnPosition = currentDungeon.GetStartLocation() + spawnOffset;

        // Create exit
        LevelExit exit = Instantiate(
            levelExitPrefab, 
            currentDungeon.GetExitLocation() + spawnOffset, 
            Quaternion.identity
        );

        exit.dungeonManager = this;
        exit.transform.parent = levelParent;

        // Spawn enemies if spawner is available
        if (enemySpawner != null)
        {
            // Increase enemy count based on level
            int baseEnemyCount = enemySpawner.totalEnemiestoSpawn;
            enemySpawner.totalEnemiestoSpawn = Mathf.Max(5, baseEnemyCount + (currentLevel - 1) * enemyIncreasePerLevel);
            
            enemySpawner.SpawnEnemiesInDungeon(currentDungeon, playerSpawnPosition);
            
            // Reset to original value
            enemySpawner.totalEnemiestoSpawn = baseEnemyCount;
            
            Debug.Log($"Attempted to spawn enemies. Total spawned: {enemySpawner.GetAliveEnemyCount()}");
        }
        else
        {
            Debug.LogWarning("EnemySpawner is null! Enemies will not spawn properly.");
        }

        // Notify other systems about level start
        OnLevelStart?.Invoke(currentLevel);

        Debug.Log($"Generated Level {currentLevel} with {enemySpawner?.GetAliveEnemyCount() ?? 0} enemies");

        return playerSpawnPosition;
    }

    public void CompleteLevel()
    {
        // Notify other systems about level completion
        OnLevelComplete?.Invoke(currentLevel);
        
        Debug.Log($"Level {currentLevel} completed!");
        
        currentLevel++;
        
        // Move to next level
        Vector3 nextSpawnPosition = NextLevel();
        if (player != null)
        {
            player.transform.position = nextSpawnPosition;
        }
    }

    // Public getters for other scripts
    public Dungeon GetCurrentDungeon() => currentDungeon;
    public int GetCurrentLevel() => currentLevel;
    public int GetRemainingEnemies() => enemySpawner?.GetAliveEnemyCount() ?? 0;

    // Method to check if level should be completed (optional - can be used for different win conditions)
    public bool ShouldCompleteLevel()
    {
        // For now, level is complete when player reaches exit
        // But you could add other conditions like "kill all enemies first"
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (currentDungeon != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentDungeon.GetStartLocation() + spawnOffset, 0.5f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentDungeon.GetExitLocation() + spawnOffset, 0.5f);
        }
    }
}