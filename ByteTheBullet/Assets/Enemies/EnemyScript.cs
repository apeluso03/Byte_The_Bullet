using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f; // 👈 Enemy will only chase the player within this range

    private Transform player;
    private Rigidbody2D rb;

    public AudioClip takeDamageClip;

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
        else
        {
            Debug.LogError("Player transform is null!");
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        AudioSource.PlayClipAtPoint(takeDamageClip, transform.position, 1f);
        Debug.Log("Current Health: " + currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
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
