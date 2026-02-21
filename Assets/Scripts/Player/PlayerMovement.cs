using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Networked] public float MoveSpeed { get; set; } = 5f;

    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float deceleration = 45f;
    [SerializeField] private float deadzone = 0.2f;

    private Animator animator;
    private Rigidbody2D rb;

    public override void Spawned()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Dynamic physics body
        rb.bodyType = RigidbodyType2D.Dynamic;

        // No interpolation (as requested)
        rb.interpolation = RigidbodyInterpolation2D.None;

        // Stop physics from rotating the player (important for 2D chars)
        rb.freezeRotation = true;

        // Optional: keep gravity from affecting top-down characters
        rb.gravityScale = 0f;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        if (!GetInput<PlayerNetworkInput>(out var input))
            return;

        Vector2 move = input.movementInput;

        // Animator locally for responsiveness
        if (animator != null)
            animator.SetBool("moving", move.magnitude > deadzone);

        // Apply physics only on StateAuthority
        if (!HasStateAuthority) return;

        // Deadzone + normalize
        if (move.magnitude > deadzone)
            move.Normalize();
        else
            move = Vector2.zero;

        // Current velocity (XY)
        Vector2 currentVel = rb.linearVelocity;

        // Desired velocity
        Vector2 desiredVel = move * MoveSpeed;

        // Accelerate/decelerate toward desired velocity
        Vector2 velDelta = desiredVel - currentVel;
        float accel = (move == Vector2.zero) ? deceleration : acceleration;

        // Apply as acceleration (mass-independent)
        rb.AddForce(velDelta * accel, ForceMode2D.Force);

        // Optional: clamp to MoveSpeed (prevents overshoot)
        Vector2 newVel = rb.linearVelocity;
        if (newVel.magnitude > MoveSpeed)
            rb.linearVelocity = newVel.normalized * MoveSpeed;

        // Flip sprite based on move direction
        if (move.x > 0.1f)
            transform.rotation = Quaternion.Euler(0, 180, 0);
        else if (move.x < -0.1f)
            transform.rotation = Quaternion.identity;
    }
}