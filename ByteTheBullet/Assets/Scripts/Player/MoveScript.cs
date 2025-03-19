// Assets/Scripts/MoveScript.cs
using UnityEngine;
using System.Collections;

public class MoveScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dodgeSpeed = 10f;
    public float dodgeDuration = 0.2f;
    public float dodgeCooldown = 1f;
    
    [Header("Idle Dash Settings")]
    public float idleDashUpComponent = 0.7f;
    public float idleDashSideComponent = 0.3f;

    // Private components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    // Movement state
    private Vector2 movement;
    private bool isFacingRight = true;
    
    // Dodge state
    private bool isDodging = false;
    private float dodgeTimer = 0f;
    private float cooldownTimer = 0f;
    private Vector2 dodgeDirection;
    private bool isIdleDash = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.flipX = !isFacingRight;
    }

    void Update()
    {
        // Update cooldown timer
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        if (isDodging)
            return;

        // Get movement input
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement.Normalize();

        // Handle sprite flipping based on movement
        if (movement.x > 0 && !isFacingRight)
            FlipSpriteInternal();
        else if (movement.x < 0 && isFacingRight)
            FlipSpriteInternal();

        // Check for dodge input
        if (Input.GetKeyDown(KeyCode.Space) && cooldownTimer <= 0)
            StartDodge();
    }

    void FixedUpdate()
    {
        if (isDodging)
        {
            // Handle dodge movement
            dodgeTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = dodgeDirection * dodgeSpeed;
            
            if (dodgeTimer <= 0)
                EndDodge();
                
            return;
        }

        // Handle normal movement
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Update animations
        UpdateMovementAnimations();
    }
    
    void UpdateMovementAnimations()
    {
        if (isDodging) return;
        
        animator.SetBool("isRunningDown", movement.y < 0);
        animator.SetBool("isRunningRight", movement.x > 0);
        animator.SetBool("isRunningLeft", movement.x < 0);
        animator.SetBool("isRunningUp", movement.y > 0);
    }

    void StartDodge()
    {
        isDodging = true;
        dodgeTimer = dodgeDuration;
        cooldownTimer = dodgeCooldown;

        // Determine dodge direction
        dodgeDirection = movement;
        isIdleDash = dodgeDirection.magnitude == 0;
        
        if (isIdleDash)
        {
            // Create directional idle dash
            float horizontalComponent = isFacingRight ? idleDashSideComponent : -idleDashSideComponent;
            dodgeDirection = new Vector2(horizontalComponent, idleDashUpComponent).normalized;
        }
        else if (dodgeDirection.magnitude == 0)
        {
            // Fallback direction (should never happen now)
            dodgeDirection = isFacingRight ? Vector2.right : Vector2.left;
        }

        // Set animations
        ResetAllAnimationStates();
        animator.SetBool("isDashing", true);
        SetDashDirectionAnimation(dodgeDirection);
        
        StartCoroutine(TriggerDashAnimation());
    }
    
    void SetDashDirectionAnimation(Vector2 direction)
    {
        // Idle dash animation
        if (isIdleDash)
        {
            animator.SetBool("isDashingBW", true);
            return;
        }
        
        // Diagonal dash
        if (direction.x != 0 && direction.y != 0)
        {
            // Set horizontal component
            animator.SetBool(direction.x > 0 ? "isDashingRight" : "isDashingLeft", true);
            
            // Set vertical component
            animator.SetBool(direction.y > 0 ? "isDashingUp" : "isDashingDown", true);
            return;
        }
        
        // Pure horizontal dash
        if (direction.y == 0)
        {
            animator.SetBool(direction.x > 0 ? "isDashingRight" : "isDashingLeft", true);
            return;
        }
        
        // Pure vertical dash
        if (direction.x == 0)
        {
            animator.SetBool(direction.y > 0 ? "isDashingUp" : "isDashingDown", true);
        }
    }
    
    IEnumerator TriggerDashAnimation()
    {
        yield return new WaitForSeconds(dodgeDuration);
    }

    void EndDodge()
    {
        isDodging = false;
        rb.linearVelocity = Vector2.zero;
        
        ResetAllAnimationStates();
        
        // Update animations based on current input
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
    
    void ResetAllAnimationStates()
    {
        // Reset all animation booleans
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
    }

    // Public methods
    public bool IsFacingRight()
    {
        return isFacingRight;
    }
    
    public void FlipSprite()
    {
        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = !isFacingRight;
    }
    
    public Vector2 GetMovementDirection()
    {
        return movement;
    }
    
    public bool IsDashing()
    {
        return isDodging;
    }
    
    private void FlipSpriteInternal()
    {
        FlipSprite();
    }
}