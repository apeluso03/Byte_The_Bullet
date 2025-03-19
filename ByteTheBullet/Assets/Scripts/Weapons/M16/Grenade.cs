using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Grenade Properties")]
    public float damage = 30f;
    public float explosionRadius = 3f;
    public float fuseTime = 3f;
    public LayerMask targetLayers;
    
    [Header("Effects")]
    public GameObject explosionEffectPrefab;
    public AudioClip explosionSound;
    
    private bool hasExploded = false;
    private Rigidbody2D rb;
    private float spawnTime;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
            
        spawnTime = Time.time;
        
        // Debug log to confirm grenade is created
        Debug.Log("Grenade created at " + transform.position);
        
        // Set a timer to explode after fuse time
        Invoke("Explode", fuseTime);
    }
    
    public void Launch(Vector2 direction, float speed)
    {
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
            // Debug to verify launch
            Debug.Log("Grenade launched with velocity: " + rb.linearVelocity);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Debug collision
        Debug.Log("Grenade collision with: " + collision.gameObject.name);
        
        // Avoid exploding on collision right after spawn (to prevent exploding on player)
        if (Time.time - spawnTime < 0.1f) return;
        
        // Explode on collision with walls or enemies
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Grenade"))
        {
            Explode();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug trigger
        Debug.Log("Grenade trigger with: " + other.gameObject.name);
        
        // Alternative collision detection if using triggers
        if (Time.time - spawnTime < 0.1f) return;
        
        if (!other.CompareTag("Player") && !other.CompareTag("Grenade"))
        {
            Explode();
        }
    }
    
    void Explode()
    {
        if (hasExploded) return;
        
        // Debug explosion
        Debug.Log("Grenade exploding at " + transform.position);
        
        hasExploded = true;
        
        // Create explosion effect with animation
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Debug.Log("Explosion effect instantiated");
            
            // Make sure explosion destroys itself
            if (!explosion.GetComponent<ExplosionAutoDestroy>())
            {
                explosion.AddComponent<ExplosionAutoDestroy>();
            }
        }
        else
        {
            Debug.LogError("No explosion effect prefab assigned to grenade!");
        }
        
        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
        
        // Apply damage to objects in radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayers);
        foreach (Collider2D nearbyObject in colliders)
        {
            // Skip self
            if (nearbyObject.gameObject == gameObject) continue;
            
            // Calculate damage based on distance
            float distance = Vector2.Distance(transform.position, nearbyObject.transform.position);
            float damagePercent = 1f - Mathf.Clamp01(distance / explosionRadius);
            float damageAmount = damage * damagePercent;
            
            Debug.Log($"Explosion hit {nearbyObject.name} for {damageAmount} damage");
        }
        
        // Destroy the grenade
        Destroy(gameObject);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
} 