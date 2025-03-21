using UnityEngine;

[RequireComponent(typeof(WeaponAiming))]
public class WeaponShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    [Tooltip("Prefab of the projectile to instantiate")]
    public GameObject projectilePrefab;
    
    [Tooltip("Position where projectiles will spawn")]
    public Transform firePoint;
    
    [Tooltip("Speed of the projectile")]
    public float projectileSpeed = 20f;
    
    [Tooltip("How frequently the weapon can fire (shots per second)")]
    public float fireRate = 5f;
    
    [Header("Effects")]
    [Tooltip("Optional particle effect for muzzle flash")]
    public ParticleSystem muzzleFlash;
    
    [Tooltip("Optional sound effect for shooting")]
    public AudioClip shootSound;
    
    // Private references
    private WeaponAiming weaponAiming;
    private WeaponMetadata weaponMetadata;
    private AudioSource audioSource;
    private float nextFireTime = 0f;
    
    void Awake()
    {
        // Get component references
        weaponAiming = GetComponent<WeaponAiming>();
        weaponMetadata = GetComponent<WeaponMetadata>();
        audioSource = GetComponent<AudioSource>();
        
        // If we don't have an audio source but we have a sound, add one
        if (audioSource == null && shootSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
        
        // Create fire point if none is assigned
        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = new Vector3(0.5f, 0, 0); // Default position at the end of weapon
        }
        
        // Use weapon metadata if available
        if (weaponMetadata != null)
        {
            fireRate = weaponMetadata.fireRate;
        }
    }
    
    void Update()
    {
        // Only shoot if weapon is equipped by player
        if (!weaponAiming.isEquipped) return;
        
        // Check for fire input (left mouse button)
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
        }
    }
    
    public void Shoot()
    {
        // Spawn projectile at fire point
        if (projectilePrefab != null && firePoint != null)
        {
            // Create the projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            
            // Get rigidbody (2D or 3D) and apply force in the forward direction of the fire point
            Rigidbody2D rb2d = projectile.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity = firePoint.right * projectileSpeed;
            }
            else
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = firePoint.right * projectileSpeed;
                }
            }
            
            // Add a ProjectileBehavior component if it doesn't have one
            if (projectile.GetComponent<ProjectileBehavior>() == null)
            {
                ProjectileBehavior behavior = projectile.AddComponent<ProjectileBehavior>();
                
                // Set damage from weapon metadata if available
                if (weaponMetadata != null)
                {
                    behavior.damage = weaponMetadata.damage;
                    behavior.damageType = weaponMetadata.damageType;
                }
            }
            
            // Play visual effects
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Play sound effect
            if (audioSource != null && shootSound != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }
        else
        {
            Debug.LogWarning("Missing projectile prefab or fire point. Cannot shoot.");
        }
    }
    
    // Draw the fire point in the editor
    void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.1f);
            Gizmos.DrawRay(firePoint.position, firePoint.right * 0.5f);
        }
    }
} 