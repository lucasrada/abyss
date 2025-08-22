using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementBehaviour : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 moveVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        moveInput = new Vector3(moveHorizontal, moveVertical, 0f);
        moveVelocity = moveInput.normalized * moveSpeed;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(moveVelocity.x, moveVelocity.y, rb.linearVelocity.z);
    }
}
