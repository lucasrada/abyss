using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float detectionRange = 8f;
    public float attackRange = 1.2f;
    public float separationDistance = 1.5f;
    public float separationForce = 2f;
    public float turnSmoothness = 8f;

    [Header("Combat Settings")]
    public int maxHealth = 50;
    public int attackDamage = 15;
    public float attackCooldown = 1.5f;

    [Header("AI")]
    public float aiUpdateInterval = 0.12f;
    public float reachThreshold = 0.35f;
    public float searchTime = 4f;
    public float searchSpeedMultiplier = 0.7f;

    [Header("Pathfinding")]
    public bool usePathfinding = true;
    public float pathRecalcInterval = 0.5f; // how often to recompute A*
    public int maxAStarIterations = 20000; // safety cap

    [Header("Debug")]
    public bool showDebug = false;

    // runtime
    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    private int currentHealth;
    private bool isDead = false;

    private enum EnemyState { Idle, Chasing, Searching, Attacking, Dead }
    private EnemyState currentState = EnemyState.Idle;

    // path following
    private Queue<Vector2> pathQueue = new Queue<Vector2>();
    private Vector2 currentPathTarget;
    private bool isFollowingPath = false;
    private float lastPathTime = -99f;

    // movement smoothing
    private Vector2 currentVelocity = Vector2.zero;
    private Vector2 desiredVelocity = Vector2.zero;

    // separation
    private List<EnemyController> nearbyEnemies = new List<EnemyController>();
    private float lastSeparationUpdate = 0f;

    // dungeon reference (injected by spawner)
    private Dungeon dungeon = null;

    // layers
    private LayerMask wallLayerMask;
    private LayerMask enemyLayerMask;

    // attack
    private float nextAttackTime = 0f;

    void Start()
    {
        Initialize();
        ValidateConfiguration();
        StartCoroutine(AIUpdateRoutine());
    }

    public void InitializeWithDungeon(Dungeon dungeonRef, Transform playerTransform = null)
    {
        this.dungeon = dungeonRef;
        if (playerTransform != null) player = playerTransform;
    }

    void Initialize()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();

        currentHealth = maxHealth;

        wallLayerMask = LayerMask.GetMask("Default", "Wall");
        enemyLayerMask = LayerMask.GetMask("Enemy");

        if (showDebug) Debug.Log($"{name}: Initialized. Player={player?.name}");
    }

    void ValidateConfiguration()
    {
        bool hasErrors = false;

        if (player == null)
        {
            Debug.LogWarning($"[{name}] Player not found! Make sure Player has 'Player' tag assigned.");
            hasErrors = true;
        }

        if (rb == null)
        {
            Debug.LogError($"[{name}] Rigidbody2D component is missing!");
            hasErrors = true;
        }

        if (LayerMask.NameToLayer("Enemy") == -1)
        {
            Debug.LogWarning($"[{name}] 'Enemy' layer not found. Create it in Tags & Layers.");
            hasErrors = true;
        }

        if (LayerMask.NameToLayer("Wall") == -1)
        {
            Debug.LogWarning($"[{name}] 'Wall' layer not found. Create it in Tags & Layers for better collision detection.");
        }

        if (dungeon == null && usePathfinding)
        {
            Debug.LogWarning($"[{name}] Dungeon reference not set. Pathfinding will fall back to direct steering. Call InitializeWithDungeon() from spawner.");
        }

        if (hasErrors)
        {
            Debug.LogError($"[{name}] Enemy has configuration errors. Check warnings above.");
        }
    }

    void ClearPath()
    {
        pathQueue.Clear();
        isFollowingPath = false;
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

        float dist = Vector2.Distance(transform.position, player.position);
        bool canSee = CanSeePlayer();

        switch (currentState)
        {
            case EnemyState.Idle:
                if (canSee && dist <= detectionRange)
                {
                    currentState = EnemyState.Chasing;
                }
                break;

            case EnemyState.Chasing:
                if (canSee)
                {
                    // direct chase if no obstacle, else pathfind
                    if (!usePathfinding || DirectPathTo(player.position))
                    {
                        // direct pursuit
                        isFollowingPath = false;
                        SetDesiredVelocityTowards(player.position, 1f);
                    }
                    else // need pathfinding around obstacles
                    {
                        TryEnsurePathTo(player.position);
                        FollowPathBehavior();
                    }
                }
                else
                {
                    // lost sight -> search
                    currentState = EnemyState.Searching;
                    // record last known position
                    lastKnownPlayerPosition = player.position;
                    lastPlayerSeenTime = Time.time;
                    TryEnsurePathTo(lastKnownPlayerPosition);
                }
                break;

            case EnemyState.Searching:
                // follow last known position or wander around a bit
                if (canSee)
                {
                    currentState = EnemyState.Chasing;
                    break;
                }

                // if following a path, continue; otherwise slow roam toward last known pos
                if (isFollowingPath)
                {
                    FollowPathBehavior();
                }
                else
                {
                    // small slow move towards last known
                    if (Vector2.Distance(transform.position, lastKnownPlayerPosition) > reachThreshold)
                        SetDesiredVelocityTowards(lastKnownPlayerPosition, searchSpeedMultiplier);
                    else
                        SetDesiredVelocity(Vector2.zero);
                }

                // exit search after timeout
                if (Time.time - lastPlayerSeenTime > searchTime)
                {
                    currentState = EnemyState.Idle;
                    ClearPath();
                }
                break;

            case EnemyState.Attacking:
                // Attack logic
                if (dist <= attackRange && Time.time >= nextAttackTime)
                {
                    AttackPlayer();
                }
                else if (dist > attackRange)
                {
                    currentState = EnemyState.Chasing;
                }
                break;
        }

        // if extremely close, trigger attack state
        if (dist <= attackRange && Time.time >= nextAttackTime && !isDead)
        {
            currentState = EnemyState.Attacking;
            SetDesiredVelocity(Vector2.zero);
        }
    }

    // ---------- Path & steering helpers ----------

    private Vector2 lastKnownPlayerPosition;
    private float lastPlayerSeenTime = 0f;

    void TryEnsurePathTo(Vector2 targetWorld)
    {
        if (dungeon == null)
        {
            // no dungeon grid available, we will use raycasts / direct steering
            return;
        }

        if (Time.time - lastPathTime < pathRecalcInterval && isFollowingPath) return; // rate-limit

        // compute A* path (grid -> world)
        List<Vector2> path = ComputePathAStar(transform.position, targetWorld, maxAStarIterations);

        lastPathTime = Time.time;

        if (path != null && path.Count > 0)
        {
            pathQueue.Clear();
            foreach (var p in path) pathQueue.Enqueue(p);
            currentPathTarget = pathQueue.Dequeue();
            isFollowingPath = true;
        }
        else
        {
            // no path found; fallback to direct steering (will try again later)
            isFollowingPath = false;
        }
    }

    void FollowPathBehavior()
    {
        if (!isFollowingPath)
        {
            SetDesiredVelocity(Vector2.zero);
            return;
        }

        // if reached currentPathTarget, advance
        if (Vector2.Distance(transform.position, currentPathTarget) <= reachThreshold)
        {
            if (pathQueue.Count > 0)
            {
                currentPathTarget = pathQueue.Dequeue();
            }
            else
            {
                isFollowingPath = false;
                SetDesiredVelocity(Vector2.zero);
                return;
            }
        }

        // desired move direction towards currentPathTarget
        SetDesiredVelocityTowards(currentPathTarget, 1f);
    }

    bool DirectPathTo(Vector2 worldTarget)
    {
        // if there's a clear raycast to target with no wall, treat as direct
        Vector2 dir = (worldTarget - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, worldTarget);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, wallLayerMask);
        return hit.collider == null;
    }

    void SetDesiredVelocityTowards(Vector2 worldTarget, float speedMultiplier)
    {
        Vector2 dir = (worldTarget - (Vector2)transform.position).normalized;
        Vector2 sep = GetSeparationForce();
        Vector2 combined = (dir * moveSpeed * speedMultiplier) + sep;
        SetDesiredVelocity(combined);
    }

    void SetDesiredVelocity(Vector2 v)
    {
        desiredVelocity = v;
    }

    Vector2 GetSeparationForce()
    {
        Vector2 sep = Vector2.zero;
        int count = 0;

        foreach (var e in nearbyEnemies)
        {
            if (e == null || e == this || e.isDead) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < separationDistance && d > 0.001f)
            {
                Vector2 away = ((Vector2)transform.position - (Vector2)e.transform.position).normalized;
                float strength = (separationDistance - d) / separationDistance;
                sep += away * strength;
                count++;
            }
        }

        if (count > 0) sep = (sep / count) * separationForce;
        return sep;
    }

    void UpdateNearbyEnemies()
    {
        if (Time.time - lastSeparationUpdate < 0.2f) return;
        nearbyEnemies.Clear();
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, separationDistance * 2f, enemyLayerMask);
        foreach (var c in cols)
        {
            var e = c.GetComponent<EnemyController>();
            if (e != null && e != this) nearbyEnemies.Add(e);
        }
        lastSeparationUpdate = Time.time;
    }

    // ---------- Movement application (physics) ----------
    void FixedUpdate()
    {
        if (isDead || rb == null) return;

        // smooth the velocity
        currentVelocity = Vector2.Lerp(currentVelocity, desiredVelocity, Time.fixedDeltaTime * turnSmoothness);

        // finally move
        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }

    // ---------- Pathfinding: A* on dungeon grid ----------
    List<Vector2> ComputePathAStar(Vector2 startWorld, Vector2 goalWorld, int maxIterations = 20000)
    {
        if (dungeon == null) return null;

        char[,] layout = dungeon.GetLayout();
        int width = layout.GetLength(0);
        int height = layout.GetLength(1);

        Vector2Int start = WorldToGrid(startWorld, width, height);
        Vector2Int goal = WorldToGrid(goalWorld, width, height);

        // if start or goal is not on a floor, find nearest floor tile
        if (!IsWalkable(start, width, height)) start = FindNearestFloor(start, width, height);
        if (!IsWalkable(goal, width, height)) goal = FindNearestFloor(goal, width, height);

        if (start == goal)
            return new List<Vector2>(); // already there

        // basic A*
        var open = new List<Node>();
        var closed = new bool[width, height];
        var gScore = new float[width, height];

        for (int x=0;x<width;x++) for (int y=0;y<height;y++) gScore[x,y] = float.PositiveInfinity;

        Node startNode = new Node(start, 0f, Heuristic(start, goal), null);
        open.Add(startNode);
        gScore[start.x, start.y] = 0f;

        int iterations = 0;
        while (open.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            // get node with lowest f
            open.Sort((a,b)=> a.f.CompareTo(b.f));
            Node current = open[0];
            open.RemoveAt(0);

            if (current.pos == goal)
            {
                // build path
                List<Vector2> path = new List<Vector2>();
                Node n = current;
                // we want path from next step after start to goal
                while (n != null && n.parent != null)
                {
                    path.Add(GridToWorld(n.pos));
                    n = n.parent;
                }
                path.Reverse();
                return path;
            }

            closed[current.pos.x, current.pos.y] = true;

            foreach (var offset in neighborOffsets)
            {
                Vector2Int nb = current.pos + offset;
                if (nb.x < 0 || nb.x >= width || nb.y < 0 || nb.y >= height) continue;
                if (closed[nb.x, nb.y]) continue;
                if (!IsWalkable(nb, width, height)) continue;

                // diagonal corner cutting prevention
                bool isDiag = Mathf.Abs(offset.x) == 1 && Mathf.Abs(offset.y) == 1;
                if (isDiag)
                {
                    Vector2Int a = new Vector2Int(current.pos.x + offset.x, current.pos.y);
                    Vector2Int b = new Vector2Int(current.pos.x, current.pos.y + offset.y);
                    if (!IsWalkable(a, width, height) || !IsWalkable(b, width, height)) continue;
                }

                float tentativeG = gScore[current.pos.x, current.pos.y] + (isDiag ? 1.41421356f : 1f);
                if (tentativeG < gScore[nb.x, nb.y])
                {
                    gScore[nb.x, nb.y] = tentativeG;
                    float h = Heuristic(nb, goal);
                    Node existing = open.Find(node => node.pos == nb);
                    if (existing == null)
                    {
                        open.Add(new Node(nb, tentativeG, h, current));
                    }
                    else
                    {
                        existing.g = tentativeG;
                        existing.f = tentativeG + h;
                        existing.parent = current;
                    }
                }
            }
        }

        // failed to find path
        return null;
    }

    static readonly Vector2Int[] neighborOffsets = new Vector2Int[]
    {
        new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(-1,0),
        new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
    };

    Vector2Int WorldToGrid(Vector2 w, int width, int height)
    {
        int gx = Mathf.Clamp(Mathf.FloorToInt(w.x), 0, width - 1);
        int gy = Mathf.Clamp(Mathf.FloorToInt(w.y), 0, height - 1);
        return new Vector2Int(gx, gy);
    }

    Vector2 GridToWorld(Vector2Int g)
    {
        return new Vector2(g.x + 0.5f, g.y + 0.5f);
    }

    bool IsWalkable(Vector2Int g, int width, int height)
    {
        if (g.x < 0 || g.x >= width || g.y < 0 || g.y >= height) return false;
        return dungeon.IsFloor(g.x, g.y);
    }

    Vector2Int FindNearestFloor(Vector2Int start, int width, int height)
    {
        // BFS outward until we find a floor cell
        var q = new Queue<Vector2Int>();
        var seen = new bool[width, height];
        q.Enqueue(start);
        seen[start.x, start.y] = true;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (IsWalkable(p, width, height)) return p;
            foreach (var off in neighborOffsets)
            {
                var nb = p + off;
                if (nb.x < 0 || nb.x >= width || nb.y < 0 || nb.y >= height) continue;
                if (seen[nb.x, nb.y]) continue;
                seen[nb.x, nb.y] = true;
                q.Enqueue(nb);
            }
        }
        return start; // fallback
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2.Distance(a, b);
    }

    class Node
    {
        public Vector2Int pos;
        public float g;
        public float f;
        public Node parent;
        public Node(Vector2Int pos, float g, float h, Node parent)
        {
            this.pos = pos; this.g = g; this.f = g + h; this.parent = parent;
        }
    }

    // ---------- Combat & misc ----------
    void AttackPlayer()
    {
        nextAttackTime = Time.time + attackCooldown;
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && !pc.IsDead)
        {
            pc.TakeDamage(attackDamage);
            if (showDebug) Debug.Log($"{name} attacked player for {attackDamage}");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (showDebug) Debug.Log($"{name} took {damage}. HP={currentHealth}");
        if (currentHealth <= 0) Die();
        else
        {
            // react by chasing
            currentState = EnemyState.Chasing;
            TryEnsurePathTo(player != null ? (Vector2)player.position : (Vector2)transform.position);
        }
    }

    void Die()
    {
        isDead = true;
        currentState = EnemyState.Dead;
        StopAllCoroutines();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;

        // notify spawner (safely try to find spawner)
        var spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.OnEnemyDestroyed(gameObject);

        Destroy(gameObject, 1f);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;
        Vector2 dir = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, wallLayerMask);
        return hit.collider == null;
    }

    // small public getters
    public bool IsDead => isDead;
    public Vector2 TargetPosition => isFollowingPath ? currentPathTarget : (player ? (Vector2)player.position : (Vector2)transform.position);

    void OnDrawGizmos()
    {
        if (!showDebug) return;

        if (isDead) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (player != null)
        {
            bool canSee = CanSeePlayer();
            Gizmos.color = canSee ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, player.position);
        }

        if (isFollowingPath && pathQueue.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Vector3 prev = transform.position;
            Gizmos.DrawSphere(currentPathTarget, 0.2f);
            Gizmos.DrawLine(prev, currentPathTarget);
            prev = currentPathTarget;

            foreach (Vector2 waypoint in pathQueue)
            {
                Gizmos.DrawSphere(waypoint, 0.15f);
                Gizmos.DrawLine(prev, waypoint);
                prev = waypoint;
            }
        }

#if UNITY_EDITOR
        Handles.Label(transform.position + Vector3.up * 1.5f, $"State: {currentState}\nHP: {currentHealth}/{maxHealth}");
#endif
    }
}
