using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 50f;
    public float deceleration = 50f;

    [Header("References")]
    public Animator animator; // Reference to the character's animator
    public SpriteRenderer characterSprite; // Reference to the character's sprite renderer

    private Rigidbody2D rb;
    [HideInInspector]
    public Vector2 moveDirection; // Make this public so CharacterAnimator can access it
    private Vector2 currentVelocity;
    [SerializeField] private bool isFacingRight = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // If animator isn't assigned, try to get it from this game object
        if (animator == null)
            animator = GetComponent<Animator>();
            
        // If characterSprite isn't assigned, try to get it from this game object
        if (characterSprite == null)
            characterSprite = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // Get input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        
        // Create move direction vector (normalized to prevent diagonal movement being faster)
        moveDirection = new Vector2(moveX, moveY).normalized;
    }
    
    void FixedUpdate()
    {
        // Handle movement physics in FixedUpdate
        Move();
    }
    
    void Move()
    {
        // Calculate target velocity
        Vector2 targetVelocity = moveDirection * moveSpeed;
        
        // Smoothly interpolate between current velocity and target velocity
        if (moveDirection != Vector2.zero)
        {
            // Accelerate
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            // Decelerate
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.deltaTime);
        }
        
        // Apply the velocity to the rigidbody
        rb.linearVelocity = currentVelocity;
    }

    // Add a method that can be called from PlayerDodge
    public void SetVelocity(Vector2 velocity)
    {
        currentVelocity = velocity;
        rb.linearVelocity = velocity;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GetComponent<PlayerHealth>().TakeDamage(1);
        }
    }

}
