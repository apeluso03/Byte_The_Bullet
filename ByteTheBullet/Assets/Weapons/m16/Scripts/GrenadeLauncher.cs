using UnityEngine;

public class GrenadeLauncher : MonoBehaviour
{
    [Header("Grenade Stats")]
    public float fireRate = 1.0f;
    public float grenadeSpeed = 10f;
    public float launchAngle = 15f;
    
    [Header("Ammunition")]
    public int magazineSize = 6;
    [HideInInspector]
    public int currentAmmo;
    
    [Header("Prefabs & Visuals")]
    public GameObject grenadePrefab;
    public Transform muzzlePoint;
    public Sprite launcherSprite;
    
    [Header("Audio Control")]
    public bool useSeparateAudio = false;
    
    // State
    [HideInInspector]
    public bool isActive = false;
    [HideInInspector]
    public bool isReloading = false;
    
    // Private variables
    private float lastFireTime;
    private AudioSource audioSource;
    private M16Weapon parentWeapon;
    
    void Awake()
    {
        // Cache references
        audioSource = GetComponentInParent<AudioSource>();
        parentWeapon = GetComponentInParent<M16Weapon>();
        
        // Initialize ammo
        currentAmmo = magazineSize;
        
        // Log grenade prefab reference
        if (grenadePrefab != null)
        {
            Debug.Log("GRENADE LAUNCHER AWAKE: Found grenade prefab: " + grenadePrefab.name);
        }
        else
        {
            Debug.LogError("GRENADE LAUNCHER AWAKE: No grenade prefab assigned!");
        }
        
        // Make sure parent weapon knows about this component
        if (parentWeapon != null)
        {
            parentWeapon.grenadeLauncherComponent = this;
        }
    }
    
    void Start()
    {
        // Setup muzzle point if needed
        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("GrenadeMuzzle");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0.4f, -0.1f, 0);
            muzzlePoint = muzzleObj.transform;
        }
        
        // Check grenade prefab again
        if (grenadePrefab == null)
        {
            Debug.LogError("GRENADE LAUNCHER START: No grenade prefab assigned!");
            
            // Try to get from parent as fallback
            if (parentWeapon != null && parentWeapon.backupGrenadePrefab != null)
            {
                grenadePrefab = parentWeapon.backupGrenadePrefab;
                Debug.Log("Restored grenade prefab from parent weapon");
            }
        }
        
        // Make EXTRA sure the reference is saved
        if (parentWeapon != null && grenadePrefab != null)
        {
            parentWeapon.backupGrenadePrefab = grenadePrefab;
        }
    }
    
    void OnEnable()
    {
        if (grenadePrefab == null && parentWeapon != null && parentWeapon.backupGrenadePrefab != null)
        {
            grenadePrefab = parentWeapon.backupGrenadePrefab;
            Debug.Log("Restored grenade prefab on enable");
        }
    }
    
    public void UpdateMuzzlePosition(bool isFlipped)
    {
        if (muzzlePoint == null) return;
        
        Vector3 pos = muzzlePoint.localPosition;
        
        // Keep X the same, but flip Y when the weapon is flipped
        if (isFlipped)
            pos.y = -Mathf.Abs(pos.y);
        else
            pos.y = Mathf.Abs(pos.y);
            
        muzzlePoint.localPosition = pos;
    }
    
    public void FireGrenade()
    {
        // Check all conditions
        if (!CanFire()) return;
        
        Debug.Log("Firing grenade successfully!");
        
        // Update firing data
        lastFireTime = Time.time;
        currentAmmo--;
        
        // Make sure parent is in sync
        if (parentWeapon != null)
        {
            parentWeapon.grenadeAmmo = currentAmmo;
            parentWeapon.PlayGrenadeFireAnimation();
            
            // Here we can double-check sound is triggered if needed
            // This is commented out because we should rely on the sound already triggered in M16Weapon.FireGrenade
            // if (!useSeparateAudio) {
            //    Debug.Log("GrenadeLauncher telling parent to play fire sound");
            // }
        }
        
        // Get fire position
        Vector3 firePos = muzzlePoint.position;
        
        // Create grenade
        GameObject grenade = Instantiate(grenadePrefab, firePos, transform.rotation);
        
        // Calculate launch direction (slightly upward for arc)
        float angleInRadians = launchAngle * Mathf.Deg2Rad;
        Vector2 launchDir = new Vector2(
            Mathf.Cos(angleInRadians) * transform.right.x,
            Mathf.Sin(angleInRadians)
        ).normalized;
        
        // Set velocity via the Grenade script
        Grenade grenadeComponent = grenade.GetComponent<Grenade>();
        if (grenadeComponent != null)
        {
            grenadeComponent.Launch(launchDir, grenadeSpeed);
        }
        else
        {
            // Fallback: Add velocity directly to rigidbody
            Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = launchDir * grenadeSpeed;
            }
        }
        
        // Auto reload when empty
        if (currentAmmo <= 0)
            ReloadGrenades();
    }
    
    bool CanFire()
    {
        if (Time.time < lastFireTime + fireRate)
        {
            return false;
        }
        
        if (currentAmmo <= 0)
        {
            Debug.Log("Can't fire: out of ammo");
            
            // Auto-reload when trying to fire with no ammo
            if (!isReloading)
            {
                ReloadGrenades();
            }
            
            return false;
        }
        
        if (isReloading)
        {
            Debug.Log("Can't fire: currently reloading");
            return false;
        }
        
        if (grenadePrefab == null)
        {
            // Try one more time to recover
            if (parentWeapon != null && parentWeapon.backupGrenadePrefab != null)
            {
                grenadePrefab = parentWeapon.backupGrenadePrefab;
                Debug.Log("Recovered grenade prefab in CanFire");
                return true;
            }
            
            Debug.LogError("Cannot fire: no grenade prefab assigned!");
            return false;
        }
        
        return true;
    }
    
    public void ReloadGrenades()
    {
        if (isReloading || currentAmmo == magazineSize) return;
        
        isReloading = true;
        
        // IMPORTANT: Trigger reload animation via parent weapon
        if (parentWeapon != null)
        {
            parentWeapon.PlayReloadAnimation();
            parentWeapon.SetReloadingState(true);
        }
        
        Debug.Log("Grenade launcher started reloading");
        
        // Reload after a delay
        CancelInvoke("FinishReload"); 
        Invoke("FinishReload", 2.0f);
    }
    
    void FinishReload()
    {
        currentAmmo = magazineSize;
        isReloading = false;
        
        Debug.Log("GrenadeLauncher finished reloading. New ammo: " + currentAmmo);
        
        // Sync with parent weapon if available
        if (parentWeapon != null)
        {
            parentWeapon.grenadeAmmo = currentAmmo;
            parentWeapon.SetReloadingState(false); // Clear the reloading state
        }
    }
    
    // For UI display
    public string GetAmmoStatus()
    {
        return currentAmmo + " / " + magazineSize + " GRENADES";
    }
    
    // Add this method to synchronize state with parent weapon
    public void SyncWithParentWeapon()
    {
        if (parentWeapon != null)
        {
            // Update ammo in parent weapon
            parentWeapon.grenadeAmmo = currentAmmo;
            parentWeapon.maxGrenadeAmmo = magazineSize;
        }
    }
    
    // Make sure this method also plays the animation
    public void StartReload()
    {
        if (isReloading || currentAmmo == magazineSize) return;
        
        Debug.Log("GrenadeLauncher StartReload called");
        isReloading = true;
        
        // Reload after a delay
        CancelInvoke("FinishReload");
        Invoke("FinishReload", 2.0f);
    }
    
    void Update()
    {
        // Check if reference was lost
        if (grenadePrefab == null && isActive)
        {
            Debug.LogWarning("REFERENCE LOST: Grenade prefab is null in Update!");
            
            // Try to recover from parent
            if (GetComponentInParent<M16Weapon>() != null && 
                GetComponentInParent<M16Weapon>().backupGrenadePrefab != null)
            {
                grenadePrefab = GetComponentInParent<M16Weapon>().backupGrenadePrefab;
                Debug.Log("Recovered grenade prefab reference in Update");
            }
        }
        
        // Added for debug - press G to check reference status
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("LAUNCHER STATUS: Prefab=" + 
                     (grenadePrefab != null ? grenadePrefab.name : "NULL") + 
                     ", isActive=" + isActive);
        }
        
        // Keep ammo synchronized
        if (parentWeapon != null)
        {
            // Two-way sync
            if (currentAmmo != parentWeapon.grenadeAmmo)
            {
                if (isActive)  // If active, we're the authority
                    parentWeapon.grenadeAmmo = currentAmmo;
                else  // If not active, parent is the authority
                    currentAmmo = parentWeapon.grenadeAmmo;
            }
        }
    }
}

// Keep this here to avoid the naming conflict
public class AutoDestroyEffect : MonoBehaviour
{
    public float lifetime = 1f;
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
} 