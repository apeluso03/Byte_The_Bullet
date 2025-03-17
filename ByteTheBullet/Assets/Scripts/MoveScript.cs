using UnityEngine;
using System.Collections;

public class TopDownMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Player Movement Speed
    public float dodgeSpeed = 10f; // Dodge Roll Speed
    public float dodgeDuration = 0.2f; // Duration of the dodge roll
    public float dodgeCooldown = 1f; // Cooldown between dodges
    // Base direction for idle dash (up and right)
    public float idleDashUpComponent = 0.7f;
    public float idleDashSideComponent = 0.3f;

    private Rigidbody2D rb; // Player's rb
    private Vector2 movement; // Movement Input
    private bool isFacingRight = true; // Tracks the sprite's facing direction (default to right)
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
    [SerializeField] private Animator animator;

    private bool isDodging = false; // Track if the player is currently dodging
    private float dodgeTimer = 0f; // Timer for dodge duration
    private float cooldownTimer = 0f; // Timer for dodge cooldown
    private Vector2 dodgeDirection; // Store the dodge direction
    private bool animationTriggered = false; // Flag to track if animation was triggered
    private bool isIdleDash = false; // Track if this is a dash from idle state

    void Start()
    {
        // Get Player's rb
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure the sprite is facing right by default
        spriteRenderer.flipX = !isFacingRight;
    }

    void Update()
    {
        // Update cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (isDodging)
        {
            // During dodge, ignore movement input
            return;
        }

        // Get input
        movement.x = Input.GetAxisRaw("Horizontal"); // Left/Right
        movement.y = Input.GetAxisRaw("Vertical"); // Up/Down

        // Normalize movement
        movement = movement.normalized;

        // Flip the sprite based on movement direction
        if (movement.x > 0 && !isFacingRight)
        {
            FlipSprite();
        }
        else if (movement.x < 0 && isFacingRight)
        {
            FlipSprite();
        }

        // Check for dodge roll input (e.g., Space key)
        if (Input.GetKeyDown(KeyCode.Space) && cooldownTimer <= 0 && !isDodging)
        {
            StartDodge();
        }
    }

    void FixedUpdate()
    {
        if (isDodging)
        {
            // Continue dodge movement for the specified duration
            dodgeTimer -= Time.fixedDeltaTime;
            
            // Apply dodge velocity throughout the duration
            rb.linearVelocity = dodgeDirection * dodgeSpeed;
            
            if (dodgeTimer <= 0)
            {
                EndDodge();
            }
            return;
        }

        // Move the player
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Update running animations
        if (!isDodging)
        {
            animator.SetBool("isRunningDown", movement.y < 0);
            animator.SetBool("isRunningRight", movement.x > 0); // Set isRunningRight when moving right
            animator.SetBool("isRunningLeft", movement.x < 0); // Set isRunningLeft when moving left
            animator.SetBool("isRunningUp", movement.y > 0);
        }
    }

    void StartDodge()
    {
        // Set dodge state
        isDodging = true;
        dodgeTimer = dodgeDuration;
        cooldownTimer = dodgeCooldown;

        // Determine dodge direction
        dodgeDirection = movement;
        
        // Check if this is an idle dash (no movement input)
        isIdleDash = dodgeDirection.magnitude == 0;
        
        if (isIdleDash)
        {
            // Create directional idle dash based on facing direction
            float horizontalComponent = isFacingRight ? idleDashSideComponent : -idleDashSideComponent;
            dodgeDirection = new Vector2(horizontalComponent, idleDashUpComponent);
            Debug.Log("Idle dash detected - using direction based on facing: " + 
                      (isFacingRight ? "right" : "left") + ", vector: " + dodgeDirection);
        }
        else if (dodgeDirection.magnitude == 0)
        {
            // Fallback to the direction the player is facing (should never happen now)
            dodgeDirection = isFacingRight ? Vector2.right : Vector2.left;
        }

        // Ensure the direction is normalized for consistent speed
        dodgeDirection.Normalize();

        // Reset all animation states first
        ResetAllAnimationStates();
        
        // Set dash animation immediately
        animator.SetBool("isDashing", true);
        
        // Set specific dash direction immediately
        SetDashDirectionAnimation(dodgeDirection);
        
        // Start the coroutine for timing purposes
        StartCoroutine(TriggerDashAnimation());
    }
    
    void SetDashDirectionAnimation(Vector2 direction)
    {
        // Special case for idle dash
        if (isIdleDash)
        {
            // Set the appropriate idle dash animation based on facing direction
            if (isFacingRight)
            {
                animator.SetBool("isDashingBW", true);
                Debug.Log("Setting isDashingBW to true (Idle Dash Right)");
            }
            else
            {
                animator.SetBool("isDashingBW", true);
                Debug.Log("Setting isDashingBW to true (Idle Dash Left)");
            }
            return;
        }
        
        // For diagonal dashes, set both horizontal and vertical animations
        if (direction.x != 0 && direction.y != 0)
        {
            // Set horizontal component
            if (direction.x > 0)
            {
                animator.SetBool("isDashingRight", true);
                Debug.Log("Setting isDashingRight to true (Diagonal)");
            }
            else
            {
                animator.SetBool("isDashingLeft", true);
                Debug.Log("Setting isDashingLeft to true (Diagonal)");
            }
            
            // Set vertical component
            if (direction.y > 0)
            {
                animator.SetBool("isDashingUp", true);
                Debug.Log("Setting isDashingUp to true (Diagonal)");
            }
            else
            {
                animator.SetBool("isDashingDown", true);
                Debug.Log("Setting isDashingDown to true (Diagonal)");
            }
            return;
        }
        
        // For pure horizontal movement
        if (direction.y == 0)
        {
            if (direction.x > 0)
            {
                animator.SetBool("isDashingRight", true);
                Debug.Log("Setting isDashingRight to true");
            }
            else
            {
                animator.SetBool("isDashingLeft", true);
                Debug.Log("Setting isDashingLeft to true");
            }
            return;
        }
        
        // For pure vertical movement
        if (direction.x == 0)
        {
            if (direction.y > 0)
            {
                animator.SetBool("isDashingUp", true);
                Debug.Log("Setting isDashingUp to true");
            }
            else
            {
                animator.SetBool("isDashingDown", true);
                Debug.Log("Setting isDashingDown to true");
            }
            return;
        }
    }
    
    IEnumerator TriggerDashAnimation()
    {
        // Wait for the full duration to ensure animation plays
        yield return new WaitForSeconds(dodgeDuration);
    }

    void EndDodge()
    {
        isDodging = false;
        rb.linearVelocity = Vector2.zero; // Stop dodge movement
        
        // Reset all animation states
        ResetAllAnimationStates();
        
        // Update running animations based on current input
        Vector2 currentMovement = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;
        
        if (currentMovement.magnitude > 0)
        {
            animator.SetBool("isRunningDown", currentMovement.y < 0);
            animator.SetBool("isRunningRight", currentMovement.x > 0);
            animator.SetBool("isRunningLeft", currentMovement.x < 0);
            animator.SetBool("isRunningUp", currentMovement.y > 0);
        }
    }
    
    IEnumerator ResetAnimationsWithDelay()
    {
        // Small delay to ensure animation completes
        yield return new WaitForSeconds(0.05f);
        
        // Reset all animation states
        ResetAllAnimationStates();
        
        // Update running animations based on current input
        if (movement.magnitude > 0)
        {
            animator.SetBool("isRunningDown", movement.y < 0);
            animator.SetBool("isRunningRight", movement.x > 0);
            animator.SetBool("isRunningLeft", movement.x < 0);
            animator.SetBool("isRunningUp", movement.y > 0);
        }
    }
    
    void ResetAllAnimationStates()
    {
        // Reset all animation variables to prevent conflicts
        animator.SetBool("isRunningDown", false);
        animator.SetBool("isRunningRight", false);
        animator.SetBool("isRunningLeft", false);
        animator.SetBool("isRunningUp", false);
        animator.SetBool("isDashing", false);
        animator.SetBool("isDashingRight", false);
        animator.SetBool("isDashingLeft", false);
        animator.SetBool("isDashingUp", false);
        animator.SetBool("isDashingDown", false);
        animator.SetBool("isDashingBW", false);
        
        Debug.Log("Reset all animation states");
    }

    void FlipSprite()
    {
        // Toggle the facing direction
        isFacingRight = !isFacingRight;

        // Flip the sprite horizontally
        spriteRenderer.flipX = !isFacingRight;
    }
}