using UnityEngine;

public class SimpleWeaponPickup : MonoBehaviour
{
    // The weapon prefab to add to inventory when picked up
    public WeaponAiming weaponPrefab;
    
    // Optional visual effects
    public float bobHeight = 0.2f;
    public float bobSpeed = 2f;
    
    private Vector3 startPosition;
    private bool hasBeenPickedUp = false;
    private bool isProcessingPickup = false;
    
    void Start()
    {
        startPosition = transform.position;
        
        // Make sure we have a collider set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;
        
        // Set the tag to "WeaponPickup" instead of "Player"
        gameObject.tag = "WeaponPickup";
        
        // Use weapon prefab sprite if available
        if (weaponPrefab != null)
        {
            SpriteRenderer weaponRenderer = weaponPrefab.GetComponent<SpriteRenderer>();
            if (weaponRenderer != null && weaponRenderer.sprite != null)
            {
                SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = weaponRenderer.sprite;
                }
            }
        }
    }
    
    void Update()
    {
        if (hasBeenPickedUp) return;
        
        // Simple bobbing animation
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + new Vector3(0, yOffset, 0);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Multiple safeguards against duplicate pickups
        if (hasBeenPickedUp || isProcessingPickup) return;
        
        Debug.Log($"Pickup trigger entered by: {other.name}, tag: {other.tag}");
        
        // Check for player
        if (other.CompareTag("Player"))
        {
            isProcessingPickup = true;
            
            Debug.Log("Player tag detected");
            
            // Get the player's inventory
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = other.GetComponentInParent<PlayerInventory>();
            }
            
            if (inventory != null && weaponPrefab != null)
            {
                Debug.Log($"Found inventory on {other.name} and weapon prefab reference");
                
                // Create a new instance of the weapon
                WeaponAiming newWeapon = Instantiate(weaponPrefab);
                newWeapon.name = weaponPrefab.name; // Keep the original name
                
                // Ensure the weapon is initially inactive
                newWeapon.gameObject.SetActive(false);
                
                // Try to add to inventory
                if (inventory.AddWeapon(newWeapon))
                {
                    Debug.Log("Successfully added weapon to inventory!");
                    hasBeenPickedUp = true;
                    
                    // Immediately disable collider and renderer
                    Collider2D col = GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                    
                    SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                    if (renderer != null) renderer.enabled = false;
                    
                    // Destroy immediately
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("Failed to add weapon to inventory (inventory full?)");
                    Destroy(newWeapon.gameObject); // Clean up if we couldn't add it
                    isProcessingPickup = false; // Reset processing flag if failed
                }
            }
            else
            {
                Debug.Log($"Missing required components. Inventory: {inventory != null}, Weapon Prefab: {weaponPrefab != null}");
                isProcessingPickup = false; // Reset processing flag if failed
            }
        }
    }
}