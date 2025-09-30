using System.Collections;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private GameObject levelExitPrefab;   // Prefab with PortalController
    [SerializeField] private Transform player;
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Level Settings")]
    [Tooltip("Spawn offset for positioning")]
    public Vector3 spawnOffset = new Vector3(0.5f, 0.5f, 0f);

    [Header("Level Progression")]
    [Tooltip("Increase enemy count per level")]
    public int enemyIncreasePerLevel = 1;
    [Tooltip("Current level number")]
    public int currentLevel = 1;

    // Events
    public System.Action<int> OnLevelStart;
    public System.Action<int> OnLevelComplete;

    // Runtime
    private Dungeon currentDungeon;
    private Transform levelParent;
    private PortalController exitController;
    private Coroutine watcherRoutine;

    private IEnumerator Start()
    {
        // Auto-find DungeonGenerator if missing
        if (!dungeonGenerator)
        {
#if UNITY_2022_1_OR_NEWER
            dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();
#else
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
#endif
            if (!dungeonGenerator)
            {
                Debug.LogError("[DungeonManager] DungeonGenerator not assigned or found.");
                yield break;
            }
        }

        // Auto-find EnemySpawner if missing
        if (enemySpawner)
        {
            int baseCount = enemySpawner.totalEnemiestoSpawn;
            enemySpawner.totalEnemiestoSpawn = Mathf.Max(5, baseCount + (currentLevel - 1) * enemyIncreasePerLevel);

            enemySpawner.SpawnEnemiesInDungeon(currentDungeon, playerSpawn, player);

            enemySpawner.totalEnemiestoSpawn = baseCount;

            Debug.Log($"[DungeonManager] Spawned enemies, alive: {GetRemainingEnemies()}");
        }


        if (!levelExitPrefab)
        {
            Debug.LogError("[DungeonManager] Level Exit Prefab not assigned (drag it from Project).");
            yield break;
        }

        yield return null; // wait a frame so other Start() methods run

        Vector3 startPos = NextLevel();
        if (player) player.position = startPos;
    }

    public Vector3 NextLevel()
    {
        // Stop watcher + clear previous level
        if (watcherRoutine != null)
        {
            StopCoroutine(watcherRoutine);
            watcherRoutine = null;
        }
        if (levelParent)
        {
            Destroy(levelParent.gameObject);
            levelParent = null;
        }

        // Create level root
        levelParent = new GameObject($"Level {currentLevel}").transform;

        // Generate dungeon
        currentDungeon = dungeonGenerator.CreateDungeon();
        if (currentDungeon == null)
        {
            Debug.LogError("[DungeonManager] CreateDungeon() returned null.");
            return player ? player.position : Vector3.zero;
        }

        Vector3 playerSpawn = currentDungeon.GetStartLocation() + spawnOffset;
        Vector3 exitPos = currentDungeon.GetExitLocation() + spawnOffset;

        // Instantiate exit portal
        GameObject exitGO = Instantiate(levelExitPrefab, exitPos, Quaternion.identity, levelParent);
        exitController = exitGO.GetComponent<PortalController>();
        if (exitController)
        {
            exitController.dungeonManager = this;
            exitController.SetActive(false); // start locked
        }

        // Spawn enemies
        if (enemySpawner)
        {
            int baseCount = enemySpawner.totalEnemiestoSpawn;
            enemySpawner.totalEnemiestoSpawn = Mathf.Max(5, baseCount + (currentLevel - 1) * enemyIncreasePerLevel);

            // Pass player Transform here
            enemySpawner.SpawnEnemiesInDungeon(currentDungeon, playerSpawn, player);

            enemySpawner.totalEnemiestoSpawn = baseCount;
            Debug.Log($"[DungeonManager] Spawned enemies, alive: {GetRemainingEnemies()}");
        }

        // Events + watcher
        OnLevelStart?.Invoke(currentLevel);
        watcherRoutine = StartCoroutine(WatchEnemiesAndActivatePortal());

        return playerSpawn;
    }

    private IEnumerator WatchEnemiesAndActivatePortal()
    {
        if (!exitController) yield break;

        if (!enemySpawner)
        {
            exitController.SetActive(true);
            yield break;
        }

        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            if (GetRemainingEnemies() <= 0)
            {
                exitController.SetActive(true);
                yield break;
            }
            yield return wait;
        }
    }

    public void CompleteLevel()
    {
        OnLevelComplete?.Invoke(currentLevel);
        Debug.Log($"[DungeonManager] Level {currentLevel} complete.");
        currentLevel++;

        Vector3 nextSpawn = NextLevel();
        if (player) player.position = nextSpawn;
    }

    // Getters
    public Dungeon GetCurrentDungeon() => currentDungeon;
    public int GetCurrentLevel() => currentLevel;
    public int GetRemainingEnemies() => enemySpawner ? enemySpawner.GetAliveEnemyCount() : 0;
    public bool ShouldCompleteLevel() => true;

    private void OnDrawGizmosSelected()
    {
        if (currentDungeon == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentDungeon.GetStartLocation() + spawnOffset, 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentDungeon.GetExitLocation() + spawnOffset, 0.5f);
    }
}
