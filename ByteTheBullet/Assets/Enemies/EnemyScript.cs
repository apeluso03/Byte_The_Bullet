using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;

    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure the player has the 'Player' tag.");
        }
    }

    void FixedUpdate()
    {
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(player.position, transform.position);

            if (distanceToPlayer <= detectionRange)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Current Health: " + currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Check if camera's currentRoomCenter is null (Final Boss Room)
        CameraSnap camSnap = Camera.main.GetComponent<CameraSnap>();
        if (camSnap != null && camSnap.player != null)
        {
            if (camSnap.player != null && camSnap.GetCurrentRoomCenter() == null)
            {
                // Find WallBlockRemover and trigger block removal
                WallBlockRemover blockRemover = FindObjectOfType<WallBlockRemover>();
                if (blockRemover != null)
                {
                    blockRemover.RemoveBlocks();
                    Debug.Log("Enemy: Final boss defeated, wall blocks removed.");
                }
                else
                {
                    Debug.LogWarning("Enemy: WallBlockRemover not found in the scene.");
                }
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Projectile"))
        {
            Destroy(collision.gameObject);
            TakeDamage(10);
        }
    }
}
