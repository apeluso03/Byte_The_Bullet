using UnityEngine;
using System.Collections;

public class ShotgunShooting : WeaponShooting
{
    [Header("Shotgun Settings")]
    [Tooltip("Number of pellets per shot")]
    public int pelletCount = 8;
    
    [Tooltip("Spread angle in degrees")]
    [Range(0, 90)]
    public float spreadAngle = 15f;
    
    [Header("Fire Type")]
    [Tooltip("How the weapon fires when trigger is pulled")]
    public FireMode fireMode = FireMode.SemiAuto;
    
    // Semi-Auto Settings
    [Header("Semi-Auto Settings")]
    [Tooltip("Time between shots (seconds) for semi-auto")]
    public float semiAutoFireDelay = 0.8f;
    
    // Full-Auto Settings
    [Header("Full-Auto Settings")]
    [Tooltip("Shots per second for full-auto")]
    public float fullAutoFireRate = 5.0f;
    
    // Burst Fire Settings
    [Header("Burst Fire Settings")]
    [Tooltip("How many shells to fire in a burst")]
    [Range(2, 5)]
    public int burstCount = 2;
    
    [Tooltip("Delay between shots in a burst (seconds)")]
    [Range(0.05f, 0.5f)]
    public float burstDelay = 0.2f;
    
    [Tooltip("Time to wait after a complete burst before allowing another (seconds)")]
    public float burstCooldown = 0.8f;
    
    // Charged Shot Settings
    [Header("Charged Shot Settings")]
    [Tooltip("Charging time for charged shots (seconds)")]
    [Range(0.5f, 3f)]
    public float chargeTime = 1.5f;
    
    [Tooltip("Maximum pellet count when fully charged")]
    [Range(8, 20)]
    public int maxChargedPelletCount = 16;
    
    [Tooltip("Cooldown after firing a charged shot (seconds)")]
    public float chargedShotCooldown = 1.2f;
    
    [Header("Effects")]
    [Tooltip("Optional shell ejection effect")]
    public ParticleSystem shellEjection;
    
    [Tooltip("Optional sound for pumping the shotgun")]
    public AudioClip pumpSound;
    
    [Tooltip("Optional sound for charging the weapon")]
    public AudioClip chargeSound;
    
    [Tooltip("Optional sound for releasing a charged shot")]
    public AudioClip chargeReleaseSound;
    
    [Tooltip("Optional shell casing prefab to spawn when firing")]
    public GameObject shellCasingPrefab;
    
    [Tooltip("Force to apply to ejected shell casings")]
    public float shellEjectionForce = 2f;
    
    [Header("Visual Feedback")]
    [Tooltip("Material to flash when charging")]
    public Material chargingMaterial;
    
    [Tooltip("Color to use when fully charged")]
    public Color fullyChargedColor = Color.red;

    [Header("Camera Shake")]
    [Tooltip("Whether to enable camera shake when firing")]
    public bool enableCameraShake = true;
    
    [Tooltip("Intensity of camera shake when firing")]
    [Range(0.1f, 2.0f)]
    public float shakeIntensity = 0.5f;
    
    [Tooltip("Duration of camera shake in seconds")]
    [Range(0.1f, 1.0f)]
    public float shakeDuration = 0.2f;
    
    // Private variables for firing state
    private int remainingBurstShots = 0;
    private float nextBurstTime = 0f;
    private float chargeStartTime = 0f;
    private bool isCharging = false;
    private Material originalMaterial;
    private SpriteRenderer spriteRenderer;
    
    // Debug variables
    [Header("Debug Info")]
    [SerializeField] private string lastActionInfo = "";
    [SerializeField] private float debugNextFireTime = 0f;
    
    // Firebase types
    public enum FireMode
    {
        SemiAuto,   // One shot per trigger pull
        FullAuto,   // Continuous fire while trigger held
        Burst,      // Multiple shots per trigger pull
        Charged     // Hold to charge, release for stronger shot
    }
    
    protected override void Awake()
    {
        // Call the base class Awake implementation
        base.Awake();
        
        // Get renderer reference
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && chargingMaterial != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // Make sure nextFireTime is initialized properly
        nextFireTime = 0f;
        
        // Get weapon metadata for fire type
        WeaponMetadata metadata = GetComponent<WeaponMetadata>();
        if (metadata != null)
        {
            // Parse the fire type from metadata
            Debug.Log($"Initializing shotgun with fire type: {metadata.fireType}");
            
            if (metadata.fireType == "SemiAuto")
                fireMode = FireMode.SemiAuto;
            else if (metadata.fireType == "FullAuto")
                fireMode = FireMode.FullAuto;
            else if (metadata.fireType == "Burst")
                fireMode = FireMode.Burst;
            else if (metadata.fireType == "Charged")
                fireMode = FireMode.Charged;
                
            Debug.Log($"Shotgun fire mode set to: {fireMode}");
        }
    }
    
    protected override void Update()
    {
        // Override the base update completely - don't call base.Update()
        if (!weaponAiming.isEquipped) return;
        
        debugNextFireTime = nextFireTime;
        
        // Handle different fire modes
        switch (fireMode)
        {
            case FireMode.SemiAuto:
                HandleSemiAutoFiring();
                break;
                
            case FireMode.FullAuto:
                HandleFullAutoFiring();
                break;
                
            case FireMode.Burst:
                HandleBurstFiring();
                break;
                
            case FireMode.Charged:
                HandleChargedFiring();
                break;
        }
    }
    
    // Completely revised full-auto handler that's more direct and straightforward
    private void HandleFullAutoFiring()
    {
        // IMPORTANT: Print firing conditions every frame for troubleshooting
        Debug.Log($"FullAuto Check: Time={Time.time:F2}, NextFire={nextFireTime:F2}, " +
                  $"CanFire={(Time.time >= nextFireTime)}, " +
                  $"Fire1={Input.GetButton("Fire1")}, Space={Input.GetKey(KeyCode.Space)}, " +
                  $"Mouse0={Input.GetKey(KeyCode.Mouse0)}");
                
        // Try multiple input methods to see which ones work
        bool usingFire1 = Input.GetButton("Fire1");
        bool usingSpace = Input.GetKey(KeyCode.Space);
        bool usingMouse0 = Input.GetKey(KeyCode.Mouse0);
        bool anyFireInput = usingFire1 || usingSpace || usingMouse0;
        
        // IMPORTANT DEBUG INFO
        if (anyFireInput)
        {
            string inputUsed = usingFire1 ? "Fire1" : (usingSpace ? "Space" : "Mouse0");
            Debug.Log($"Fire input detected: {inputUsed}");
        }
        
        // If ANY fire input is pressed and we can fire
        if (anyFireInput && Time.time >= nextFireTime)
        {
            // Log which input was successful
            string inputUsed = usingFire1 ? "Fire1" : (usingSpace ? "Space" : "Mouse0");
            Debug.Log($"FIRING USING: {inputUsed}");
            
            // Fire the weapon
            FireShotgun(pelletCount);
            
            // Use a fixed fire rate for testing - try making it very fast
            float secondsBetweenShots = 0.1f; // 10 shots per second for testing
            nextFireTime = Time.time + secondsBetweenShots;
            
            // Debug info
            lastActionInfo = $"Full-auto shot fired using {inputUsed}. Next shot in {secondsBetweenShots:F2}s";
            Debug.Log(lastActionInfo);
        }
    }
    
    private void HandleSemiAutoFiring()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            FireShotgun(pelletCount);
            nextFireTime = Time.time + semiAutoFireDelay;
            lastActionInfo = $"Semi-auto shot fired at {Time.time:F2}, next shot at {nextFireTime:F2}";
        }
    }
    
    private void HandleBurstFiring()
    {
        // Start a new burst when Fire1 is pressed and we're not in an active burst
        if (Input.GetButtonDown("Fire1") && remainingBurstShots <= 0 && Time.time >= nextFireTime)
        {
            remainingBurstShots = burstCount;
            nextBurstTime = Time.time;
            lastActionInfo = $"Burst started at {Time.time:F2}";
        }
        
        // Process active burst
        if (remainingBurstShots > 0 && Time.time >= nextBurstTime)
        {
            FireShotgun(pelletCount);
            remainingBurstShots--;
            nextBurstTime = Time.time + burstDelay;
            
            lastActionInfo = $"Burst shot fired, {remainingBurstShots} remaining";
            
            // If burst is complete, set the next time we can start a new burst
            if (remainingBurstShots <= 0)
            {
                nextFireTime = Time.time + burstCooldown;
                lastActionInfo += $", next burst at {nextFireTime:F2}";
            }
        }
    }
    
    private void HandleChargedFiring()
    {
        // First, decide if we're allowed to start charging
        bool canStartCharging = Time.time >= nextFireTime && !isCharging;
        
        // Track key state changes
        bool buttonPressed = Input.GetButtonDown("Fire1");
        bool buttonReleased = Input.GetButtonUp("Fire1");
        bool buttonHeld = Input.GetButton("Fire1");
        
        // Only track state changes if we're allowed to fire
        if (buttonPressed && canStartCharging)
        {
            // Start charging, no matter what
            isCharging = true;
            chargeStartTime = Time.time;
            Debug.Log("CHARGE STARTED - button pressed");
            
            // Play charge sound
            if (audioSource != null && chargeSound != null)
            {
                audioSource.PlayOneShot(chargeSound);
            }
            
            // Switch to charging material if available
            if (spriteRenderer != null && chargingMaterial != null)
            {
                spriteRenderer.material = chargingMaterial;
            }
        }
        
        // If we're charging, update the charge indicators
        if (isCharging)
        {
            float chargePercent = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
            lastActionInfo = $"Charging: {chargePercent:P0}";
            
            // Update material color based on charge
            if (spriteRenderer != null && chargingMaterial != null)
            {
                Color chargeColor = Color.Lerp(Color.white, fullyChargedColor, chargePercent);
                spriteRenderer.material.SetColor("_Color", chargeColor);
            }
            
            // Check if the button is STILL held
            if (!buttonHeld)
            {
                // Button was released or lost focus - complete the charge and fire
                Debug.Log("FIRING CHARGED SHOT - button released");
                
                // Calculate final charge level when button is released
                chargePercent = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
                
                // Scale pellet count based on charge level
                int currentPelletCount = Mathf.RoundToInt(Mathf.Lerp(pelletCount, maxChargedPelletCount, chargePercent));
                
                // Only fire if we've been charging for a minimum time (to prevent rapid clicks)
                // This is the key protection against click spam
                float currentChargeTime = Time.time - chargeStartTime;
                if (currentChargeTime >= 0.2f) // Minimum 0.2 seconds of charging required
                {
                    // Fire with appropriate pellet count
                    FireShotgun(currentPelletCount);
                    lastActionInfo = $"Charged shot! ({chargePercent:P0}) - {currentPelletCount} pellets";
                    
                    // Play appropriate sound effect
                    if (audioSource != null && chargeReleaseSound != null && chargePercent > 0.5f)
                    {
                        audioSource.PlayOneShot(chargeReleaseSound);
                    }
                    
                    // Apply a longer cooldown after a successful charged shot
                    nextFireTime = Time.time + chargedShotCooldown;
                }
                else
                {
                    // Not charged long enough - cancel without firing
                    Debug.Log("CHARGE CANCELED - not held long enough");
                    lastActionInfo = "Charge canceled - hold longer!";
                    nextFireTime = Time.time + 0.5f; // Penalty for quick-clicking
                }
                
                // Reset charging state
                isCharging = false;
                
                // Restore original material
                if (spriteRenderer != null && originalMaterial != null)
                {
                    spriteRenderer.material = originalMaterial;
                }
            }
        }
    }
    
    // Fire a shotgun blast with the specified number of pellets
    public void FireShotgun(int pellets)
    {
        if (projectilePrefab == null || firePoint == null) return;
        
        // Fire multiple pellets with spread
        for (int i = 0; i < pellets; i++)
        {
            // Calculate random spread angle
            float randomSpread = Random.Range(-spreadAngle / 2, spreadAngle / 2);
            Quaternion spreadRotation = Quaternion.Euler(0, 0, randomSpread);
            
            // Combine the fire point's rotation with the spread
            Quaternion finalRotation = firePoint.rotation * spreadRotation;
            
            // Create the projectile with spread rotation
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, finalRotation);
            
            // Handle 2D or 3D rigidbody
            Rigidbody2D rb2d = projectile.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                // Apply velocity in the direction of the rotated vector
                rb2d.linearVelocity = finalRotation * Vector2.right * projectileSpeed;
            }
            else
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = finalRotation * Vector3.right * projectileSpeed;
                }
            }
            
            // Make sure the projectile has the ProjectileBehavior component
            if (projectile.GetComponent<ProjectileBehavior>() == null)
            {
                ProjectileBehavior behavior = projectile.AddComponent<ProjectileBehavior>();
                
                // Set damage from weapon metadata if available
                if (weaponMetadata != null)
                {
                    // Shotguns typically do less damage per pellet
                    behavior.damage = weaponMetadata.damage / pellets;
                    behavior.damageType = weaponMetadata.damageType;
                }
            }
        }
        
        // Spawn a shell casing if needed
        if (shellCasingPrefab != null)
        {
            SpawnShellCasing();
        }
        
        // Play effects
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        
        if (shellEjection != null)
        {
            shellEjection.Play();
        }
        
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
            
            // Play pump sound after a delay if we're using semi-auto
            if (fireMode == FireMode.SemiAuto && pumpSound != null)
            {
                StartCoroutine(PlayPumpSoundDelayed(0.2f));
            }
        }
        
        // Apply camera shake
        if (enableCameraShake && Camera.main != null)
        {
            // Try to get CameraShake component from main camera
            CameraShake cameraShake = Camera.main.GetComponent<CameraShake>();
            if (cameraShake != null)
            {
                // Scale intensity based on pellet count
                float multiplier = (float)pellets / pelletCount;
                cameraShake.ShakeFromWeapon(shakeIntensity, multiplier, shakeDuration);
            }
        }
    }
    
    // Override the base Shoot method to use our FireShotgun method
    public override void Shoot()
    {
        FireShotgun(pelletCount);
    }
    
    // Method to spawn shell casings
    private void SpawnShellCasing()
    {
        if (shellCasingPrefab == null) return;
        
        // Calculate spawn position slightly to the side and back of the weapon
        Vector3 spawnPos = firePoint.position - (firePoint.right * 0.1f);
        spawnPos += firePoint.up * 0.05f; // Slightly above
        
        // Instantiate the casing
        GameObject casing = Instantiate(shellCasingPrefab, spawnPos, Quaternion.identity);
        
        // Add random rotation
        if (casing.GetComponent<Rigidbody2D>())
        {
            Rigidbody2D rb = casing.GetComponent<Rigidbody2D>();
            
            // Apply force in a somewhat random direction
            Vector2 ejectDir = -firePoint.right + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 1f), 0);
            rb.linearVelocity = ejectDir.normalized * shellEjectionForce;
            
            // Add random rotation
            rb.angularVelocity = Random.Range(-720f, 720f);
            
            // Add script to destroy after delay if not already present
            if (!casing.GetComponent<DestroyAfterTime>())
            {
                DestroyAfterTime destroyScript = casing.AddComponent<DestroyAfterTime>();
                destroyScript.destroyAfter = 3f; // Destroy after 3 seconds
            }
        }
    }
    
    // Delayed sound effect for pump action
    private IEnumerator PlayPumpSoundDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (audioSource != null && pumpSound != null)
        {
            audioSource.PlayOneShot(pumpSound);
        }
    }
    
    // Cancel charging if weapon is deactivated
    private void OnDisable()
    {
        if (isCharging)
        {
            isCharging = false;
            
            // Restore original material
            if (spriteRenderer != null && originalMaterial != null)
            {
                spriteRenderer.material = originalMaterial;
            }
        }
    }
    
    // Draw the spread visualization in the editor
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            
            // Draw the spread cone
            Vector3 leftBoundary = Quaternion.Euler(0, 0, -spreadAngle / 2) * firePoint.right;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, spreadAngle / 2) * firePoint.right;
            
            Gizmos.DrawRay(firePoint.position, leftBoundary * 1.5f);
            Gizmos.DrawRay(firePoint.position, rightBoundary * 1.5f);
            
            // Draw an arc representing the spread range
            int segments = 10;
            Vector3 prevPos = firePoint.position + firePoint.rotation * leftBoundary * 1.5f;
            
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-spreadAngle / 2, spreadAngle / 2, t);
                Vector3 direction = Quaternion.Euler(0, 0, angle) * firePoint.right;
                Vector3 newPos = firePoint.position + direction * 1.5f;
                
                Gizmos.DrawLine(prevPos, newPos);
                prevPos = newPos;
            }
        }
    }
}

// Helper class for destroying shell casings after a delay
public class DestroyAfterTime : MonoBehaviour
{
    public float destroyAfter = 3f;
    
    private void Start()
    {
        Destroy(gameObject, destroyAfter);
    }
} 