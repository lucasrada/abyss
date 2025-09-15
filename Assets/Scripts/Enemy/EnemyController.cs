using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed")]
    public float moveSpeed = 3f;
    [Tooltip("Detection range for player")]
    public float detectionRange = 8f;
    [Tooltip("Attack range")]
    public float attackRange = 1.2f;
    [Tooltip("Minimum distance to maintain from other enemies")]
    public float separationDistance = 1.5f;
    [Tooltip("Strength of separation force")]
    public float separationForce = 2f;
    [Tooltip("How smoothly to turn (lower = smoother)")]
    public float turnSmoothness = 5f;

    [Header("Combat Settings")]
    [Tooltip("Enemy's maximum health")]
    public int maxHealth = 50;
    [Tooltip("Damage dealt to player")]
    public int attackDamage = 15;
    [Tooltip("Time between attacks")]
    public float attackCooldown = 1.5f;

    [Header("AI Behavior")]
    [Tooltip("How often to update AI decisions")]
    public float aiUpdateInterval = 0.1f;
    [Tooltip("Distance threshold to consider 'reached' a target")]
    public float reachThreshold = 0.5f;
    [Tooltip("How long to search after losing player")]
    public float searchTime = 4f;
    [Tooltip("Speed when searching (multiplier)")]
    public float searchSpeedMultiplier = 0.7f;

    [Header("Pathfinding")]
    [Tooltip("Use simple pathfinding around obstacles")]
    public bool usePathfinding = true;
    [Tooltip("Ray distance for obstacle detection")]
    public float obstacleCheckDistance = 1.2f;
    [Tooltip("Number of directions to check for pathfinding")]
    public int pathfindingDirections = 8;

    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebug = false;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    
    // Health and combat
    private int currentHealth;
    private float nextAttackTime;
    private bool isDead = false;

    // AI State
    public enum EnemyState { Idle, Chasing, Searching, Attacking, Dead }
    private EnemyState currentState = EnemyState.Idle;
    
    // Movement and pathfinding
    private Vector2 targetPosition;
    private Vector2 lastKnownPlayerPosition;
    private float lastPlayerSeenTime;
    private float nextAIUpdate;
    private Vector2 currentVelocity;
    private Vector2 smoothedMovement;

    // Pathfinding
    private Queue<Vector2> pathQueue = new Queue<Vector2>();
    private Vector2 currentPathTarget;
    private bool isFollowingPath = false;

    // References to other enemies for separation
    private List<EnemyController> nearbyEnemies = new List<EnemyController>();
    private float lastSeparationUpdate;

    // Layer masks
    private LayerMask wallLayerMask;
    private LayerMask enemyLayerMask;

    void Start()
    {
        Initialize();
        StartCoroutine(AIUpdateRoutine());
    }

    void Initialize()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        
        currentHealth = maxHealth;
        targetPosition = transform.position;
        
        // Set up layer masks - be defensive about missing layers
        try
        {
            wallLayerMask = LayerMask.GetMask("Default", "Wall");
            enemyLayerMask = LayerMask.GetMask("Enemy");
        }
        catch
        {
            wallLayerMask = 1; // Default layer
            enemyLayerMask = 0;
            if (showDebug) Debug.LogWarning($"{name}: Could not find expected layers, using defaults");
        }

        if (showDebug) Debug.Log($"{name}: Enemy initialized at {transform.position}");
    }

    IEnumerator AIUpdateRoutine()
    {
        while (!isDead)
        {
            UpdateAI();
            UpdateNearbyEnemies();
            yield return new WaitForSeconds(aiUpdateInterval);
        }
    }

    void UpdateAI()
    {
        if (player == null || isDead) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer = CanSeePlayer();

        EnemyState oldState = currentState;

        switch (currentState)
        {
            case EnemyState.Idle:
                if (canSeePlayer && distanceToPlayer <= detectionRange)
                {
                    EnterChaseState();
                }
                break;

            case EnemyState.Chasing:
                if (canSeePlayer)
                {
                    UpdateChaseState(distanceToPlayer);
                }
                else
                {
                    EnterSearchState();
                }
                break;

            case EnemyState.Searching:
                UpdateSearchState(distanceToPlayer, canSeePlayer);
                break;

            case EnemyState.Attacking:
                UpdateAttackState(distanceToPlayer, canSeePlayer);
                break;
        }

        if (showDebug && oldState != currentState)
        {
            Debug.Log($"{name}: State changed from {oldState} to {currentState}");
        }
    }

    void EnterChaseState()
    {
        currentState = EnemyState.Chasing;
        lastKnownPlayerPosition = player.position;
        lastPlayerSeenTime = Time.time;
        ClearPath();
    }

    void UpdateChaseState(float distanceToPlayer)
    {
        lastKnownPlayerPosition = player.position;
        lastPlayerSeenTime = Time.time;
        
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            ClearPath();
        }
        else
        {
            SetTarget(player.position);
        }
    }

    void EnterSearchState()
    {
        currentState = EnemyState.Searching;
        SetTarget(lastKnownPlayerPosition);
        
        if (showDebug)
            Debug.Log($"{name}: Lost sight of player, searching at {lastKnownPlayerPosition}");
    }

    void UpdateSearchState(float distanceToPlayer, bool canSeePlayer)
    {
        if (canSeePlayer && distanceToPlayer <= detectionRange)
        {
            EnterChaseState();
            return;
        }

        if (Time.time - lastPlayerSeenTime > searchTime)
        {
            currentState = EnemyState.Idle;
            if (showDebug) Debug.Log($"{name}: Giving up search, returning to idle");
            return;
        }

        // Continue moving to last known position
        if (Vector2.Distance(transform.position, lastKnownPlayerPosition) < reachThreshold)
        {
            // Reached search location, look around
            Vector2 searchDirection = Random.insideUnitCircle.normalized * 2f;
            SetTarget(lastKnownPlayerPosition + searchDirection);
        }
    }

    void UpdateAttackState(float distanceToPlayer, bool canSeePlayer)
    {
        if (!canSeePlayer || distanceToPlayer > attackRange * 1.5f)
        {
            if (canSeePlayer)
                EnterChaseState();
            else
                EnterSearchState();
        }
        else if (Time.time >= nextAttackTime)
        {
            AttackPlayer();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        Vector2 moveDirection = CalculateMovement();
        
        // Smooth the movement for better visual quality
        smoothedMovement = Vector2.Lerp(smoothedMovement, moveDirection, Time.fixedDeltaTime * turnSmoothness);
        
        float currentSpeed = moveSpeed;
        if (currentState == EnemyState.Searching)
            currentSpeed *= searchSpeedMultiplier;

        rb.MovePosition(rb.position + smoothedMovement * currentSpeed * Time.fixedDeltaTime);
    }

    Vector2 CalculateMovement()
    {
        Vector2 desiredDirection = Vector2.zero;

        // Calculate direction based on state
        switch (currentState)
        {
            case EnemyState.Chasing:
            case EnemyState.Searching:
                desiredDirection = GetDirectionToTarget();
                break;
            case EnemyState.Attacking:
                // Face the player but don't move
                if (player != null)
                {
                    desiredDirection = (player.position - transform.position).normalized * 0.1f; // Slight movement
                }
                break;
        }

        // Apply separation from other enemies
        Vector2 separationForceVector = GetSeparationForce();
        desiredDirection += separationForceVector;

        // Use pathfinding if enabled and we detect obstacles
        if (usePathfinding && desiredDirection.magnitude > 0.1f)
        {
            desiredDirection = GetPathfindingDirection(desiredDirection);
        }

        return desiredDirection.magnitude > 1f ? desiredDirection.normalized : desiredDirection;
    }

    Vector2 GetDirectionToTarget()
    {
        Vector2 target = targetPosition;
        
        // Use path target if we have one
        if (isFollowingPath && pathQueue.Count > 0)
        {
            target = currentPathTarget;
            
            // Check if we reached the current path waypoint
            if (Vector2.Distance(transform.position, target) < reachThreshold)
            {
                if (pathQueue.Count > 0)
                {
                    currentPathTarget = pathQueue.Dequeue();
                }
                else
                {
                    isFollowingPath = false;
                }
            }
        }

        return (target - (Vector2)transform.position).normalized;
    }

    Vector2 GetPathfindingDirection(Vector2 desiredDirection)
    {
        // Check if the direct path is blocked
        RaycastHit2D hit = Physics2D.Raycast(transform.position, desiredDirection, obstacleCheckDistance, wallLayerMask);
        
        if (hit.collider == null)
        {
            return desiredDirection; // Direct path is clear
        }

        // Try alternative directions
        float[] angles = new float[pathfindingDirections];
        for (int i = 0; i < pathfindingDirections; i++)
        {
            angles[i] = (360f / pathfindingDirections) * i;
        }

        // Sort angles by how close they are to our desired direction
        float desiredAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
        System.Array.Sort(angles, (a, b) => 
        {
            float diffA = Mathf.DeltaAngle(desiredAngle, a);
            float diffB = Mathf.DeltaAngle(desiredAngle, b);
            return Mathf.Abs(diffA).CompareTo(Mathf.Abs(diffB));
        });

        // Try each direction until we find one that's not blocked
        foreach (float angle in angles)
        {
            Vector2 testDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            RaycastHit2D testHit = Physics2D.Raycast(transform.position, testDirection, obstacleCheckDistance, wallLayerMask);
            
            if (testHit.collider == null)
            {
                return testDirection;
            }
        }

        return Vector2.zero; // All directions blocked
    }

    Vector2 GetSeparationForce()
    {
        Vector2 separationVector = Vector2.zero;
        int count = 0;

        foreach (EnemyController enemy in nearbyEnemies)
        {
            if (enemy != null && enemy != this && !enemy.isDead)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < separationDistance && distance > 0.01f)
                {
                    Vector2 directionAway = ((Vector2)transform.position - (Vector2)enemy.transform.position).normalized;
                    float strength = (separationDistance - distance) / separationDistance; // Stronger when closer
                    separationVector += directionAway * strength;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            separationVector = (separationVector / count) * separationForce;
        }

        return separationVector;
    }

    void UpdateNearbyEnemies()
    {
        if (Time.time - lastSeparationUpdate < 0.2f) return; // Update every 0.2 seconds
        
        nearbyEnemies.Clear();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, separationDistance * 2f, enemyLayerMask);
        
        foreach (Collider2D col in colliders)
        {
            EnemyController enemy = col.GetComponent<EnemyController>();
            if (enemy != null && enemy != this)
            {
                nearbyEnemies.Add(enemy);
            }
        }
        
        lastSeparationUpdate = Time.time;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distanceToPlayer, wallLayerMask);
        return hit.collider == null;
    }

    void SetTarget(Vector2 newTarget)
    {
        targetPosition = newTarget;
        ClearPath(); // Clear any existing path when setting a new target
    }

    void ClearPath()
    {
        pathQueue.Clear();
        isFollowingPath = false;
    }

    void AttackPlayer()
    {
        nextAttackTime = Time.time + attackCooldown;
        
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null && !playerController.IsDead)
        {
            playerController.TakeDamage(attackDamage);
            if (showDebug) Debug.Log($"{name} attacks player for {attackDamage} damage!");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        if (showDebug) Debug.Log($"{name} takes {damage} damage! Health: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // React to being hit
            if (currentState == EnemyState.Idle && player != null)
            {
                EnterChaseState();
            }
        }
    }

    void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;
        
        StopAllCoroutines();
        
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        
        // Notify spawner
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.OnEnemyDestroyed(gameObject);
        }
        
        if (showDebug) Debug.Log($"{name} died!");
        
        Destroy(gameObject, 1f);
    }

    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Separation distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, separationDistance);
        
        // Current target
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // Path visualization
            if (isFollowingPath && pathQueue.Count > 0)
            {
                Gizmos.color = Color.cyan;
                Vector3 currentPos = transform.position;
                foreach (Vector2 pathPoint in pathQueue)
                {
                    Gizmos.DrawLine(currentPos, pathPoint);
                    Gizmos.DrawWireCube(pathPoint, Vector3.one * 0.2f);
                    currentPos = pathPoint;
                }
            }
            
            // Obstacle detection ray
            if (usePathfinding)
            {
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(transform.position, direction * obstacleCheckDistance);
            }
        }
    }

    // Public getters
    public bool IsDead => isDead;
    public EnemyState State => currentState;
    public int CurrentHealth => currentHealth;
    public Vector2 TargetPosition => targetPosition;
}