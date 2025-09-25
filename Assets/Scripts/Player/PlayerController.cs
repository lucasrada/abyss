using UnityEngine;
using System.Collections;

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
    public float attackRange = 1.5f;
    [Tooltip("Player's maximum health")]
    public int maxHealth = 100;
    
    [Header("Dash Settings")]
    [Tooltip("Tecla para dachear")]
    public KeyCode dashKey = KeyCode.L;
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

        if (!isAttacking && !isDashing)
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
            PerformAttack();
        }
        if (Input.GetKeyDown(KeyCode.K) && CanFireAttack())
        {
            animator.ResetTrigger("Attack2");
            animator.SetTrigger("Attack2");
            PerformAttack();
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

    void PerformAttack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, 1 << enemyLayer);
        
        Vector2 attackDirection = lastDirection;
        
        foreach (Collider2D enemy in hitEnemies)
        {
            Vector2 dirToEnemy = (enemy.transform.position - transform.position).normalized;
            float dot = Vector2.Dot(attackDirection, dirToEnemy);
            
            if (dot > 0.5f)
            {
                EnemyController enemyController = enemy.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    enemyController.TakeDamage(attackDamage);
                }
            }
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
}