using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f;
    public GameObject hitEffectPrefab;
    public LayerMask collisionLayers;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Remove the CompareTag("Enemy") check that's causing the error
        // Instead, you can check for enemy components or layers
        
        // Option 1: Check if the object has an enemy component
        /*if (other.GetComponent<EnemyHealth>() != null)*/
        {
            // It's an enemy, do damage here
        }
        
        // Option 2: Check for a specific layer
        // Make sure to set your enemies to this layer in Unity
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemies"))
        {
            // It's an enemy, do damage here
        }
        
        // Option 3: Use a more general approach
        // Exclude player, bullets and other non-damageable objects
        if (!other.CompareTag("Player") && !other.CompareTag("Bullet"))
        {
            // It's probably something we can damage
        }
        
        // Check if we hit something on the collision layers
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            // Apply damage if it's an enemy
            // Uncomment when you have an enemy health component
            // EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            // if (enemyHealth != null)
            // {
            //     enemyHealth.TakeDamage(damage);
            // }
            
            // Spawn hit effect
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Destroy the bullet
            Destroy(gameObject);
        }
    }
} 