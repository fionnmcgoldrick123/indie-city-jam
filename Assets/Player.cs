using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothTime = 0.1f;

    private Vector2 currentVelocity;
    private Vector2 smoothVelocity;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Get input
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        // Normalize to prevent faster diagonal movement
        if (input.magnitude > 1f)
            input.Normalize();

        // Smooth the velocity for Vampire Survivors-like feel
        Vector2 targetVelocity = input * moveSpeed;
        currentVelocity = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref smoothVelocity, smoothTime);

        // Apply movement
        transform.position += (Vector3)currentVelocity * Time.deltaTime;

        // Flip sprite based on movement direction
        if (input.x > 0.1f)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (input.x < -0.1f)
            transform.localScale = new Vector3(1, 1, 1);

        // Trigger animation
        bool isMoving = input.magnitude > 0.1f;
        if (animator != null)
        {
            animator.SetBool("moving", isMoving);
            Debug.Log("Player is " + (isMoving ? "moving" : "idle"));
        }
    }
}
