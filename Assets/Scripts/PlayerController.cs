using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento del jugador")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;

    private Vector2 movement;
    private Vector2 lastDirection = Vector2.down; // dirección inicial

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
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

        // 1) Leer input de movimiento solo si NO está atacando
        if (!isAttacking)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }
        else
        {
            movement = Vector2.zero; // inmóvil mientras dura el ataque
        }

        // 2) Guardar última dirección no nula
        if (movement.sqrMagnitude > 0.01f)
            lastDirection = movement.normalized;

        // 3) Calcular Dir: 0=Down,1=Right,2=Up,3=Left
        int dir;
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            dir = lastDirection.x > 0 ? 1 : 3;
        else
            dir = lastDirection.y > 0 ? 2 : 0;

        // 4) Mandar parámetros al Animator (Blend Tree y Dir)
        animator.SetFloat("Horizontal", lastDirection.x);
        animator.SetFloat("Vertical", lastDirection.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
        animator.SetInteger("Dir", dir);

        // 5) Ataques (no se disparan si está atacando o en transición)
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

        // 6) Otros triggers: bloqueados mientras ataca
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
    }

    void FixedUpdate()
    {
        // 7) Movimiento físico
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
