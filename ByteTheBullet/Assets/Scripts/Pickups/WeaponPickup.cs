using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public GameObject weaponPrefab;
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    public AudioClip pickupSound;
    
    private Vector3 startPos;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        startPos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set the sprite to the weapon's icon if available
        if (weaponPrefab != null)
        {
            WeaponBase weapon = weaponPrefab.GetComponent<WeaponBase>();
            if (weapon != null && weapon.weaponIcon != null)
                spriteRenderer.sprite = weapon.weaponIcon;
        }
    }
    
    void Update()
    {
        // Make the pickup bob up and down
        transform.position = startPos + new Vector3(
            0, 
            Mathf.Sin(Time.time * bobSpeed) * bobHeight, 
            0
        );
        
        // Optional: rotate the pickup
        transform.Rotate(0, 0, 30 * Time.deltaTime);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Find the weapon manager
            WeaponManager weaponManager = other.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                // Check if the player already has this weapon
                if (weaponPrefab != null)
                {
                    WeaponBase weapon = weaponPrefab.GetComponent<WeaponBase>();
                    if (weapon != null && !weaponManager.HasWeapon(weapon.weaponID))
                    {
                        // Add the weapon to inventory
                        weaponManager.AddWeaponToInventory(weaponPrefab);
                        
                        // Play pickup sound
                        if (pickupSound != null)
                            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                            
                        // Destroy the pickup
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
} 