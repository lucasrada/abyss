using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento del jugador")]
    public float moveSpeed = 5f;

    [Header("Combat Settings")]
    [Tooltip("Damage dealt to enemies")]
    public int attackDamage = 25;
    [Tooltip("Attack range")]
    public float attackRange = 1.7f;
    [Tooltip("Forward offset for the melee hitbox")]
    public float attackForwardOffset = 0.45f;
    [Tooltip("Hitbox interior para enemigos pegados al jugador")]
    public float attackInnerRadius = 0.9f;
    [Tooltip("How wide the attack cone is (-1 = 360°, 1 = solo frente)")]
    [Range(-1f, 1f)] public float attackDirectionThreshold = -0.1f;
    [Tooltip("Knockback aplicado a los enemigos")]
    public float attackKnockbackForce = 5f;
    [Header("Attack Feedback")]
    [Tooltip("Duración del hitstop cuando conectás un golpe")]
    public float attackHitstopDuration = 0.05f;
    [Tooltip("Escala de tiempo durante el hitstop")]
    [Range(0.01f, 1f)] public float attackHitstopTimeScale = 0.2f;
    [Tooltip("Multiplicador de daño para el ataque ligero (J)")]
    public float lightAttackDamageMultiplier = 1f;
    [Tooltip("Multiplicador de daño para el ataque pesado (K)")]
    public float heavyAttackDamageMultiplier = 2.3f;
    [Tooltip("Player's maximum health")]
    public int maxHealth = 100;
    
    [Header("Dash Settings")]
    [Tooltip("Tecla para dachear")]
    public KeyCode dashKey = KeyCode.LeftShift;
    [Tooltip("Velocidad del dash")]
    public float dashSpeed = 16f;
    [Tooltip("Duración del dash (segundos)")]
    public float dashDuration = 0.25f;   // ágil
    [Tooltip("Enfriamiento del dash (segundos)")]
    public float dashCooldown = 5f;
    [Tooltip("Invulnerable durante el dash")]
    public bool invulnerableWhileDashing = true;
    [Tooltip("Ignorar colisiones con enemigos durante el dash (no con paredes)")]
    public bool ignoreEnemiesWhileDashing = false;
    [Tooltip("Nombre de la capa de Enemigos")]
    public string enemyLayerName = "Enemy";

    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down;

    // --- Estado de dash ---
    private bool isDashing = false;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 dashDir;
    private int playerLayer = -1, enemyLayer = -1;

    // --- Combat ---
    private int currentHealth;
    private bool isInvulnerable = false;
    private bool isDead = false;
    private Coroutine hitStopRoutine;
    private readonly HashSet<EnemyController> attackTargets = new HashSet<EnemyController>();

    // --- Events ---
    public System.Action<int> OnHealthChanged;
    public System.Action OnPlayerDeath;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth);
    }

    bool CanFireAttack()
    {
        if (isDead) return false;
        if (animator.IsInTransition(0)) return false;
        if (animator.GetBool("IsAttacking")) return false;
        return true;
    }

    void Update()
    {
        if (isDead) return;

        bool isAttacking = animator.GetBool("IsAttacking");

        if (!isDashing)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            movement = new Vector2(horizontal, vertical);
            if (movement.magnitude > 1f)
            {
                movement = movement.normalized;
            }
        }
        else
        {
            movement = Vector2.zero;
        }

        if (!isDashing && movement.sqrMagnitude > 0.01f)
            lastDirection = movement.normalized;

        int dir;
        if (Mathf.Abs(lastDirection.x) >= Mathf.Abs(lastDirection.y))
            dir = lastDirection.x >= 0 ? 1 : 3;
        else
            dir = lastDirection.y >= 0 ? 2 : 0;

        animator.SetFloat("Horizontal", lastDirection.x);
        animator.SetFloat("Vertical", lastDirection.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
        animator.SetInteger("Dir", dir);

        if (Input.GetKeyDown(KeyCode.J) && CanFireAttack())
        {
            animator.ResetTrigger("Attack1");
            animator.SetTrigger("Attack1");
            PerformAttack(false);
        }
        if (Input.GetKeyDown(KeyCode.K) && CanFireAttack())
        {
            animator.ResetTrigger("Attack2");
            animator.SetTrigger("Attack2");
            PerformAttack(true);
        }

        if (!isAttacking && Input.GetKeyDown(KeyCode.H))
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }
        if (!isAttacking && Input.GetKeyDown(KeyCode.M))
        {
            animator.ResetTrigger("Death");
            animator.SetTrigger("Death");
        }

        if (!isAttacking && (Input.GetKeyDown(KeyCode.A) ||
                             Input.GetKeyDown(KeyCode.W) ||
                             Input.GetKeyDown(KeyCode.D) ||
                             Input.GetKeyDown(KeyCode.S)))
        {
            animator.ResetTrigger("Mover");
            animator.SetTrigger("Mover");
        }

        if (!isDashing && !isAttacking && Time.time >= nextDashTime && Input.GetKeyDown(dashKey))
        {
            dashDir = GetDashDirectionSnap8();
            StartDash();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (isDashing)
        {
            rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);

            if (Time.time >= dashEndTime) EndDash();
        }
        else
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void PerformAttack(bool isHeavyAttack)
    {
        Vector2 attackDir = lastDirection.sqrMagnitude > 0.01f ? lastDirection.normalized : Vector2.down;
        Vector2 origin = rb ? rb.position : (Vector2)transform.position;
        Vector2 forwardOrigin = origin + attackDir * Mathf.Max(0f, attackForwardOffset);

        attackTargets.Clear();
        int mask = 1 << enemyLayer;

        AddTargets(Physics2D.OverlapCircleAll(origin, Mathf.Max(0.1f, attackInnerRadius), mask), attackDir, origin, false);
        AddTargets(Physics2D.OverlapCircleAll(forwardOrigin, Mathf.Max(attackInnerRadius, attackRange), mask), attackDir, forwardOrigin, true);

        if (attackTargets.Count == 0) return;

        int damage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * (isHeavyAttack ? heavyAttackDamageMultiplier : lightAttackDamageMultiplier)));

        foreach (var enemy in attackTargets)
        {
            if (!enemy) continue;
            enemy.TakeDamage(damage);
            Vector2 dirToEnemy = ((Vector2)enemy.transform.position - origin).normalized;
            enemy.ApplyHitFeedback(dirToEnemy, attackKnockbackForce);
        }

        TriggerHitStop(isHeavyAttack ? 1.4f : 1f);
    }

    void AddTargets(Collider2D[] hits, Vector2 attackDir, Vector2 origin, bool directionalCheck)
    {
        if (hits == null) return;
        foreach (var hit in hits)
        {
            if (!hit) continue;
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (!enemy) continue;
            if (directionalCheck)
            {
                Vector2 dirToEnemy = ((Vector2)enemy.transform.position - origin).normalized;
                if (Vector2.Dot(attackDir, dirToEnemy) < attackDirectionThreshold) continue;
            }
            attackTargets.Add(enemy);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable || isDead) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hit");
            StartCoroutine(InvulnerabilityFrames(1f));
        }
    }

    void Die()
    {
        isDead = true;
        currentHealth = 0;
        animator.SetTrigger("Death");
        OnPlayerDeath?.Invoke();

        DeathScreenManager dsm = FindFirstObjectByType<DeathScreenManager>();
        if (dsm != null)
        {
            dsm.ShowDeathScreen();
        }
    }

    IEnumerator InvulnerabilityFrames(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
    }

    // --- Utilidades de dash (8 direcciones) ---
    Vector2 GetDashDirectionSnap8()
    {
        bool w = Input.GetKey(KeyCode.W);
        bool a = Input.GetKey(KeyCode.A);
        bool s = Input.GetKey(KeyCode.S);
        bool d = Input.GetKey(KeyCode.D);

        if (w && d) return new Vector2(1f, 1f).normalized;
        if (w && a) return new Vector2(-1f, 1f).normalized;
        if (s && d) return new Vector2(1f, -1f).normalized;
        if (s && a) return new Vector2(-1f, -1f).normalized;

        if (w) return Vector2.up;
        if (s) return Vector2.down;
        if (a) return Vector2.left;
        if (d) return Vector2.right;

        if (lastDirection.sqrMagnitude < 0.0001f)
            return Vector2.down;

        float x = lastDirection.x;
        float y = lastDirection.y;

        int sx = Mathf.Abs(x) < 0.1f ? 0 : (x > 0 ? 1 : -1);
        int sy = Mathf.Abs(y) < 0.1f ? 0 : (y > 0 ? 1 : -1);

        if (sx != 0 && sy != 0) return new Vector2(sx, sy).normalized;
        if (sx != 0) return new Vector2(sx, 0f);
        if (sy != 0) return new Vector2(0f, sy);

        return Vector2.down;
    }

    void StartDash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        if (invulnerableWhileDashing)
            SetInvulnerable(true);

        if (ignoreEnemiesWhileDashing && enemyLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        if (HasParam(animator, "IsDashing")) animator.SetBool("IsDashing", true);
    }

    void EndDash()
    {
        isDashing = false;

        if (invulnerableWhileDashing)
            SetInvulnerable(false);

        if (ignoreEnemiesWhileDashing && enemyLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        if (HasParam(animator, "IsDashing")) animator.SetBool("IsDashing", false);
    }

    bool HasParam(Animator anim, string paramName)
    {
        foreach (var p in anim.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    void TriggerHitStop(float intensityMultiplier = 1f)
    {
        if (attackHitstopDuration <= 0f || Time.timeScale <= 0f) return;
        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        hitStopRoutine = StartCoroutine(HitStopRoutine(intensityMultiplier));
    }

    IEnumerator HitStopRoutine(float intensityMultiplier)
    {
        float previousScale = Time.timeScale;
        float targetScale = Mathf.Clamp(attackHitstopTimeScale, 0.01f, previousScale);
        Time.timeScale = targetScale;
        float duration = attackHitstopDuration * Mathf.Max(0.1f, intensityMultiplier);
        yield return new WaitForSecondsRealtime(duration);
        if (Mathf.Approximately(Time.timeScale, targetScale))
        {
            Time.timeScale = previousScale;
        }
        hitStopRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 attackDir = lastDirection.sqrMagnitude > 0.01f ? lastDirection.normalized : Vector2.down;
        Vector3 origin = Application.isPlaying && rb ? (Vector3)rb.position : transform.position;
        Vector3 forwardOrigin = origin + (Vector3)(attackDir * attackForwardOffset);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(forwardOrigin, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, attackInnerRadius);
        Gizmos.DrawLine(origin, forwardOrigin);
    }

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashTime - Time.time);
    public float DashCooldownNormalized
    {
        get
        {
            if (dashCooldown <= 0f) return 1f;
            float remaining = Mathf.Max(0f, nextDashTime - Time.time);
            return 1f - Mathf.Clamp01(remaining / dashCooldown);
        }
    }
    public bool IsDashReady => Time.time >= nextDashTime;
}
