using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Player Movement Speed
    private Rigidbody2D rb; // Player's rb

    private Vector2 movement; // Movement Input

    private bool isFacingRight = false; // Tracks the sprite's facing direction
    private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component

    void Start()
    {
        // Get Player's rb
        rb = GetComponent<Rigidbody2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure the sprite is facing left by default
        spriteRenderer.flipX = isFacingRight;
    }

    void Update()
    {
        // Get input
        movement.x = Input.GetAxisRaw("Horizontal"); // Left/Right
        movement.y = Input.GetAxisRaw("Vertical"); // Up/Down

        // Normalize movement
        movement = movement.normalized;

        // Flip the sprite
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
    }

    void FlipSprite()
    {
        isFacingRight = !isFacingRight;
        spriteRenderer.flipX = isFacingRight;
    }
}