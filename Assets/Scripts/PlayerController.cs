using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento del jugador")]
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Animator animator;
    Vector2 movement;
    Vector2 lastDirection = Vector2.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        if (movement.sqrMagnitude > 0.01f)
            lastDirection = movement.normalized;

        int dir;
        if (Mathf.Abs(lastDirection.x) > Mathf.Abs(lastDirection.y))
            dir = lastDirection.x > 0 ? 1 : 3;
        else
            dir = lastDirection.y > 0 ? 2 : 0;

        animator.SetFloat("Horizontal", lastDirection.x);
        animator.SetFloat("Vertical", lastDirection.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);
        animator.SetInteger("Dir", dir);

        if (Input.GetKeyDown(KeyCode.J))
            animator.SetTrigger("Attack1");
        if (Input.GetKeyDown(KeyCode.K))
            animator.SetTrigger("Attack2");
        if (Input.GetKeyDown(KeyCode.H))
            animator.SetTrigger("Hit");
        if (Input.GetKeyDown(KeyCode.M))
            animator.SetTrigger("Death");

        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.S))
            animator.SetTrigger("Mover");
    }

    void FixedUpdate()
    {
        if (movement.sqrMagnitude > 1f) 
            movement = movement.normalized;

        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
