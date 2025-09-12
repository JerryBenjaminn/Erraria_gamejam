using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Liike")]
    public float moveSpeed = 8f;
    public float acceleration = 80f;
    public float deceleration = 90f;

    [Header("Hyppy")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Painovoima")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.0f;

    [Header("Maakontakti")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundMask;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.30f;

    [Header("Dash (lisähiuslakkaa)")]
    public PhysicsMaterial2D noFrictionMat; // materiaali kitka=0 (tee Projectissa)
    float storedLinearDrag;
    float dashDir; // -1 tai +1
    Collider2D col;


    [Header("Sprint")]
    public float sprintSpeed = 12f;         // perus 8f → sprint 12f
    public float sprintAccel = 100f;        // saa tuntua terävämmältä kuin perus acceleration
    public float sprintExitIdleThreshold = 0.1f; // jos ei liike-inputtia, poistu sprintistä
    public float postDashSprintGrace = 0.25f;    // ikkuna, jonka aikana Shift down aloittaa sprintin

    public bool isSprinting;
    public float sprintGraceTimer;
    
    Rigidbody2D rb;
    float xInput;
    bool jumpPressed;
    bool jumpHeld;

    float coyoteCounter;
    float jumpBufferCounter;

    bool isDashing;
    float dashTimer;
    float dashCooldownTimer;
    float storedGravityScale;

    // Ilmadash kerran per ilmalento
    bool airDashAvailable;

    bool IsGrounded => Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        jumpPressed = Input.GetButtonDown("Jump");
        jumpHeld = Input.GetButton("Jump");
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        if (IsGrounded)
        {
            coyoteCounter = coyoteTime;
            airDashAvailable = true;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        if (!isDashing && jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            Jump();
            jumpBufferCounter = 0;
        }

        dashCooldownTimer -= Time.deltaTime;
        if (dashPressed && !isDashing && dashCooldownTimer <= 0f)
        {
            if (IsGrounded || airDashAvailable)
            {
                StartDash();
                if (!IsGrounded) airDashAvailable = false;
            }
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
        }

        // Suunnan flip
        if (Mathf.Abs(xInput) > 0.01f)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Sign(xInput) * Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        // Sprint-grace dashin jälkeen
        sprintGraceTimer -= Time.deltaTime;

        // Sprintin kytkentä: jos Shift pohjassa ja (juuri dashed tai vain jatketaan sprinttiä)
        if (!isDashing && IsGrounded)
        {
            // Käynnistä sprintti, jos Shift on pohjassa ja ollaan grace-ikkunassa TAI sprintti on jo päällä
            if (shiftHeld && (sprintGraceTimer > 0f || isSprinting))
            {
                // Onko liike-inputtia? Muuten sprintti ei ole järkevä
                if (Mathf.Abs(xInput) > sprintExitIdleThreshold)
                    isSprinting = true;
            }
            else if (!shiftHeld)
            {
                isSprinting = false;
            }

            // Jos ei liike-inputtia, tiputa sprint pois
            if (Mathf.Abs(xInput) <= sprintExitIdleThreshold)
                isSprinting = false;
        }

    }

    void FixedUpdate()
    {
        // Valitse nopeus ja kiihtyvyys sprintin mukaan
        float targetMoveSpeed = isSprinting ? sprintSpeed : moveSpeed;
        float useAccel = isSprinting
            ? Mathf.Max(acceleration, sprintAccel)   // sprintissä voi käyttää omaa kiihtyvyyttä
            : acceleration;

        float targetSpeed = xInput * targetMoveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accel = Mathf.Abs(targetSpeed) > 0.01f ? useAccel : deceleration;
        float change = Mathf.Clamp(speedDiff * accel * Time.fixedDeltaTime,
                                   -Mathf.Abs(accel) * Time.fixedDeltaTime,
                                   Mathf.Abs(accel) * Time.fixedDeltaTime);
        if (isDashing)
        {
            // Pidä vaakanopeus lukittuna, ettei kitka tai kontaktit syö sitä
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) EndDash();

            // Jos tökkäät seinään, lopeta dashi
            if (Mathf.Abs(rb.linearVelocity.x) < 0.1f) EndDash();
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + change, rb.linearVelocity.y);

        // Painovoimat kuten ennen...
        if (rb.linearVelocity.y < -0.01f)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
        }
        else if (rb.linearVelocity.y > 0.01f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime);
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;

        dashDir = Mathf.Abs(xInput) > 0.01f ? Mathf.Sign(xInput) : Mathf.Sign(transform.localScale.x);
        if (dashDir == 0) dashDir = 1f;

        // Seinä edessä? Älä aloita turhaan
        var hit = Physics2D.Raycast(transform.position, Vector2.right * dashDir, 0.3f, groundMask);
        if (hit.collider != null) { EndDash(); return; }

        storedGravityScale = rb.gravityScale;
        storedLinearDrag = rb.linearDamping; // Unity 6: linearDamping = drag
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;

        if (col && noFrictionMat) col.sharedMaterial = noFrictionMat;

        // Aloitusnopeus
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        sprintGraceTimer = 0f; // ei sprinttiä vielä
    }


    void EndDash()
    {
        if (!isDashing) return;
        isDashing = false;
        rb.gravityScale = storedGravityScale;
        rb.linearDamping = storedLinearDrag;
        // Palauta materiaali
        if (col) col.sharedMaterial = null;

        sprintGraceTimer = postDashSprintGrace;
    }


    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
