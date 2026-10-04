using UnityEngine;
using UnityEngine.InputSystem;
public class OldLadyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Grappling")]
    public float swingForce = 12f;
    public PlayerGrapple grapple;

    [Header("Air Movement")]
    public float airAcceleration = 20f;
    public float maxAirSpeed = 10f;


    [Header("Landing Rotation")]
    public float uprightDelay = 0.5f;
    public float uprightSpeed = 360f;

    private float groundedTimer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.7f;
    private bool wasGrounded;

    [Header("Air Rotation")]
    public InputActionReference rotateAction;
    public float rotationSpeed = 360f;

    private float rotationInput;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        rotateAction.action.Enable();

        jumpAction.action.performed += Jump;
    }

    private void OnDisable()
    {
        jumpAction.action.performed -= Jump;

        moveAction.action.Disable();
        jumpAction.action.Disable();
        rotateAction.action.Disable();
    }

    private void Update()
    {
        horizontalInput = moveAction.action.ReadValue<Vector2>().x;
        rotationInput = rotateAction.action.ReadValue<float>();
        isGrounded = Physics2D.Raycast(rb.position, Vector2.down, groundCheckDistance, groundLayer);

        // Detect the exact frame Granny lands.
        bool justLanded = isGrounded && !wasGrounded;

        if (justLanded && grapple != null && grapple.IsGrappling)
        {
            grapple.DisconnectGrapple();
        }

        wasGrounded = isGrounded;
    }

    private void FixedUpdate()
    {
        HandleAirRotation();
        HandleUprightRotation();
        // Rope is attached and taut:
        // A/D pumps Granny's swing.
        if (grapple != null && grapple.IsRopeTaut)
        {
            rb.AddForce(
                Vector2.right * horizontalInput * swingForce,
                ForceMode2D.Force
            );

            return;
        }

        // We're still grappling, but the rope is temporarily slack
        // during the winch boost.
        //
        // Don't apply normal air movement here.
        // Let the winch impulse and gravity control Granny.
        if (grapple != null && grapple.IsGrappling)
        {
            return;
        }

        // Normal airborne movement when NOT grappling.
        if (!isGrounded)
        {
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                // If Granny is already travelling faster than normal air speed
                // because of grapple momentum, preserve that speed.
                if (Mathf.Abs(rb.linearVelocity.x) > maxAirSpeed)
                {
                    // Only allow input that continues in the current direction.
                    // Don't accelerate her even faster.
                    if (Mathf.Sign(horizontalInput) == Mathf.Sign(rb.linearVelocity.x))
                    {
                        // Preserve momentum - don't add anything.
                    }
                    else
                    {
                        // Allow opposite input to gradually steer/brake.
                        rb.AddForce(
                            Vector2.right * horizontalInput * airAcceleration,
                            ForceMode2D.Force
                        );
                    }
                }
                else
                {
                    // Ordinary jump air control.
                    rb.AddForce(
                        Vector2.right * horizontalInput * airAcceleration,
                        ForceMode2D.Force
                    );

                    // Normal jumping can't exceed maxAirSpeed.
                    rb.linearVelocity = new Vector2(
                        Mathf.Clamp(
                            rb.linearVelocity.x,
                            -maxAirSpeed,
                            maxAirSpeed
                        ),
                        rb.linearVelocity.y
                    );
                }
            }

            return;
        }

        // Normal grounded movement.
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y
        );
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (!isGrounded)
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce
        );
    }

    private void HandleUprightRotation()
    {
        if (isGrounded && (grapple == null || !grapple.IsGrappling))
        {
            groundedTimer += Time.fixedDeltaTime;

            if (groundedTimer >= uprightDelay)
            {
                float newRotation = Mathf.MoveTowardsAngle(
                    rb.rotation,
                    0f,
                    uprightSpeed * Time.fixedDeltaTime
                );

                rb.MoveRotation(newRotation);

                // Stop leftover spinning once we're basically upright.
                if (Mathf.Abs(Mathf.DeltaAngle(rb.rotation, 0f)) < 1f)
                {
                    rb.rotation = 0f;
                    rb.angularVelocity = 0f;
                }
            }
        }
        else
        {
            groundedTimer = 0f;
        }
    }

    private void HandleAirRotation()
    {
        if (isGrounded)
            return;

        rb.MoveRotation(
            rb.rotation - rotationInput * rotationSpeed * Time.fixedDeltaTime
        );
    }

}