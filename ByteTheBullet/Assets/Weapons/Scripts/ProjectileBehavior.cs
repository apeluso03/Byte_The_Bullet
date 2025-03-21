using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float damage = 10f;
    public string damageType = "Physical";
    public float lifetime = 5f;
    
    [Header("On Hit Effects")]
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
    
    private void Start()
    {
        // Destroy the projectile after its lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Skip collision with the shooter or other projectiles
        if (collision.CompareTag("Player") || collision.CompareTag("Projectile"))
            return;
            
        HandleCollision(collision.gameObject);
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Skip collision with the shooter or other projectiles
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Projectile"))
            return;
            
        HandleCollision(collision.gameObject);
    }
    
    private void HandleCollision(GameObject hitObject)
    {
        // Just print a debug message for now
        Debug.Log($"Hit object: {hitObject.name}, Damage: {damage}, Type: {damageType}");
        
        // Spawn hit effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Play hit sound
        if (hitSound != null)
        {
            // Create a temporary audio source to play the sound
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        
        // Destroy the projectile
        Destroy(gameObject);
    }
} 