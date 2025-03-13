using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Player Movement Speed
    private Rigidbody2D rb; // Player's rb

    private Vector2 movement; // Movement Input

    private bool isFacingRight = true; // Tracks the sprite's facing direction (default to right)
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
    [SerializeField] private Animator animator;

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
    }

    void FixedUpdate()
    {
        // Move the player
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Plays Run_Down animation clip when Y is moving down
        if (movement.y < 0)
        {
            animator.SetBool("isRunningDown", true);
        }
        else
        { // Plays Idle Animation otherwise
            animator.SetBool("isRunningDown", false);
        }

        // Plays Run_Left or Run_Right animation clip based on X movement
        if (movement.x != 0)
        {
            animator.SetBool("isRunningRight", true); // Always use the "running right" animation
        }
        else
        { // Plays Idle Animation otherwise
            animator.SetBool("isRunningRight", false);
        }

        // Plays Run_Up animation clip when Y is moving up
        if (movement.y > 0)
        {
            animator.SetBool("isRunningUp", true);
        }
        else
        { // Plays Idle Animation otherwise
            animator.SetBool("isRunningUp", false);
        }
    }

    void FlipSprite()
    {
        // Toggle the facing direction
        isFacingRight = !isFacingRight;

        // Flip the sprite horizontally
        spriteRenderer.flipX = !isFacingRight;
    }
}