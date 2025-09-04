using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento del jugador")]
    public float moveSpeed = 5f;

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
    private Vector2 lastDirection = Vector2.down; // dirección inicial

    // --- Estado de dash ---
    private bool isDashing = false;
    private float dashEndTime;
    private float nextDashTime;
    private Vector2 dashDir;
    private int playerLayer = -1, enemyLayer = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer(enemyLayerName);
    }

    // No dispares ataques si está en transición o ya atacando
    bool CanFireAttack()
    {
        if (animator.IsInTransition(0)) return false;
        if (animator.GetBool("IsAttacking")) return false;
        return true;
    }

    void Update()
    {
        bool isAttacking = animator.GetBool("IsAttacking");

        // 1) Input de movimiento solo si NO está atacando ni dacheando
        if (!isAttacking && !isDashing)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            movement = Vector2.zero; // inmóvil mientras dura el ataque o dash
        }

        // 2) Guardar última dirección no nula
        if (!isDashing && movement.sqrMagnitude > 0.01f)
            lastDirection = movement.normalized;

        // 3) Calcular Dir: 0=Down,1=Right,2=Up,3=Left
        //    Bias horizontal en empates (diagonales) para que W+D use "derecha".
        int dir;
        if (Mathf.Abs(lastDirection.x) >= Mathf.Abs(lastDirection.y))
            dir = lastDirection.x >= 0 ? 1 : 3;
        else
            dir = lastDirection.y >= 0 ? 2 : 0;

        // 4) Parámetros al Animator (no rompemos tu flujo)
        animator.SetFloat("Horizontal", lastDirection.x);
        animator.SetFloat("Vertical", lastDirection.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
        animator.SetInteger("Dir", dir);

        // 5) Ataques (ejemplo con J/K)
        if (Input.GetKeyDown(KeyCode.J) && CanFireAttack())
        {
            animator.ResetTrigger("Attack1");
            animator.SetTrigger("Attack1");
        }
        if (Input.GetKeyDown(KeyCode.K) && CanFireAttack())
        {
            animator.ResetTrigger("Attack2");
            animator.SetTrigger("Attack2");
        }

        // 6) Otros triggers de ejemplo
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

        // (Opcional) Si usás "Mover" como trigger de arranque de locomoción
        if (!isAttacking && (Input.GetKeyDown(KeyCode.A) ||
                             Input.GetKeyDown(KeyCode.W) ||
                             Input.GetKeyDown(KeyCode.D) ||
                             Input.GetKeyDown(KeyCode.S)))
        {
            animator.ResetTrigger("Mover");
            animator.SetTrigger("Mover");
        }

        // 7) Iniciar dash (funciona aunque estés corriendo)
        if (!isDashing && !isAttacking && Time.time >= nextDashTime && Input.GetKeyDown(dashKey))
        {
            dashDir = GetDashDirectionSnap8(); // 8 direcciones
            StartDash();
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            // MovePosition respeta colisiones con paredes
            rb.MovePosition(rb.position + dashDir * dashSpeed * Time.fixedDeltaTime);

            if (Time.time >= dashEndTime) EndDash();
        }
        else
        {
            rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // --- Utilidades de dash (8 direcciones) ---
    Vector2 GetDashDirectionSnap8()
    {
        // Prioridad: combinaciones diagonales si se aprieta L junto a WASD
        bool w = Input.GetKey(KeyCode.W);
        bool a = Input.GetKey(KeyCode.A);
        bool s = Input.GetKey(KeyCode.S);
        bool d = Input.GetKey(KeyCode.D);

        if (w && d) return new Vector2(1f, 1f).normalized;   // up-right
        if (w && a) return new Vector2(-1f, 1f).normalized;  // up-left
        if (s && d) return new Vector2(1f, -1f).normalized;  // down-right
        if (s && a) return new Vector2(-1f, -1f).normalized; // down-left

        if (w) return Vector2.up;
        if (s) return Vector2.down;
        if (a) return Vector2.left;
        if (d) return Vector2.right;

        // Si no acompañás con WASD, snap de la última dirección a 8 direcciones
        if (lastDirection.sqrMagnitude < 0.0001f)
            return Vector2.down; // fallback

        float x = lastDirection.x;
        float y = lastDirection.y;

        int sx = Mathf.Abs(x) < 0.1f ? 0 : (x > 0 ? 1 : -1);
        int sy = Mathf.Abs(y) < 0.1f ? 0 : (y > 0 ? 1 : -1);

        if (sx != 0 && sy != 0) return new Vector2(sx, sy).normalized; // diagonal
        if (sx != 0) return new Vector2(sx, 0f);
        if (sy != 0) return new Vector2(0f, sy);

        return Vector2.down; // último fallback
    }

    void StartDash()
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        if (invulnerableWhileDashing)
            SendMessage("SetInvulnerable", true, SendMessageOptions.DontRequireReceiver);

        if (ignoreEnemiesWhileDashing && enemyLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        // Si tenés un bool IsDashing en el Animator y querés usarlo:
        if (HasParam(animator, "IsDashing")) animator.SetBool("IsDashing", true);
    }

    void EndDash()
    {
        isDashing = false;

        if (invulnerableWhileDashing)
            SendMessage("SetInvulnerable", false, SendMessageOptions.DontRequireReceiver);

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
}
