using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Liike")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Maassaolon tarkistus")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;

    Rigidbody2D rb;
    bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    void Update()
    {
        // Sy�te
        float x = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

        // Hyppy pyynt�
        if (Input.GetButtonDown("Jump"))
            jumpQueued = true;
    }
    void FixedUpdate()
    {
        // Hyppy toteutus vain maassa
        if (jumpQueued && IsGrounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        jumpQueued = false;
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
