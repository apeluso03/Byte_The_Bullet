using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Create this as a new file: ShotgunWeapon.cs
public class ShotgunWeapon : ModularSingleModeWeapon
{
    [Header("Shotgun Properties")]
    public int pelletsPerShot = 8;
    public float spreadAngle = 30f;
    public bool useScreenShake = true;
    public float screenShakeAmount = 0.1f;
    public float kickbackAmount = 0.2f;
    
    [Header("Burst Fire Settings")]
    [Range(2, 8)]
    public int burstCount = 3;
    public float burstDelay = 0.1f; // The rate between shots in a burst
    public float burstCooldown = 0.5f; // Cooldown between burst sequences
    public bool enableAutoBurst = true; // Whether holding trigger auto-fires bursts
    private int remainingBurstShots = 0;
    private bool isBursting = false;
    private float lastBurstShotTime = -1f;
    private float lastBurstSequenceTime = 0f; // Tracks when the last burst sequence completed
    private bool hasReleasedButtonSinceLastBurst = true;
    private bool isHoldingFireButton = false;
    private float holdingFireDuration = 0f;
    
    [Header("Charge Settings")]
    public float maxChargeTime = 1.5f;
    public float minChargeTime = 0.3f;
    public bool useChargeEffect = true;
    public float maxPelletMultiplier = 2.0f; // Double pellets at max charge
    public float maxDamageMultiplier = 1.5f; // 50% more damage at max charge
    public float maxSpreadReduction = 0.5f;  // 50% less spread at max charge
    private float currentChargeTime = 0f;
    private bool isCharging = false;
    private GameObject chargeEffectInstance;
    
    // For Semi-Auto mode
    private bool hasFiredThisPress = false;
    private float lastButtonPressTime = 0f;
    
    private Vector3 originalPosition;
    private bool hasStoredPosition = false;
    
    public override void Initialize(Transform player)
    {
        base.Initialize(player);
        
        // Store original position for recoil effects
        originalPosition = transform.localPosition;
        hasStoredPosition = true;
    }
    
    private new void Update()
    {
        if (!isActive || playerTransform == null)
            return;
            
        // Handle input based on fire mode
        switch (fireMode)
        {
            case FireMode.SemiAuto:
                HandleSemiAutoInput();
                break;
                
            case FireMode.FullAuto:
                HandleFullAutoInput();
                break;
                
            case FireMode.Burst:
                HandleBurstInput();
                break;
                
            case FireMode.Charge:
                HandleChargeInput();
                break;
        }
        
        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
        
        // Process ongoing burst
        if (isBursting && remainingBurstShots > 0 && Time.time >= lastBurstShotTime + burstDelay)
        {
            FireShotgun(1f, 1f, 1f);
            remainingBurstShots--;
            lastBurstShotTime = Time.time;
            
            if (remainingBurstShots <= 0)
            {
                isBursting = false;
                // Record when this burst sequence completed
                lastBurstSequenceTime = Time.time;
            }
        }
    }
    
    private void HandleSemiAutoInput()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            // Check for duplicate input prevention
            if (Time.unscaledTime - lastButtonPressTime > 0.1f)
            {
                lastButtonPressTime = Time.unscaledTime;
                hasFiredThisPress = false;
                Fire();
            }
        }
        
        if (Input.GetButtonUp("Fire1"))
        {
            hasFiredThisPress = false;
        }
    }
    
    private void HandleFullAutoInput()
    {
        if (Input.GetButton("Fire1"))
        {
            Fire();
        }
    }
    
    private void HandleBurstInput()
    {
        // Get current fire button state
        bool fireButtonDown = Input.GetButtonDown("Fire1");
        bool fireButtonHeld = Input.GetButton("Fire1");
        bool fireButtonUp = Input.GetButtonUp("Fire1");
        
        // Calculate cooldown status
        float timeSinceLastBurst = Time.time - lastBurstSequenceTime;
        bool cooldownComplete = timeSinceLastBurst >= burstCooldown;
        
        // Track how long the fire button has been held
        if (fireButtonHeld)
        {
            if (!isHoldingFireButton)
            {
                isHoldingFireButton = true;
                holdingFireDuration = 0f;
            }
            else
            {
                holdingFireDuration += Time.deltaTime;
            }
        }
        else
        {
            isHoldingFireButton = false;
            holdingFireDuration = 0f;
        }
        
        // Handle initial button press
        if (fireButtonDown)
        {
            // Check if we can start a new burst
            if (cooldownComplete && !isBursting)
            {
                hasReleasedButtonSinceLastBurst = false;
                TriggerBurst();
            }
        }
        
        // Handle auto-burst from holding the button
        if (enableAutoBurst && isHoldingFireButton && !isBursting && cooldownComplete && currentAmmo > 0 && !isReloading)
        {
            // If holding for longer than the burst cooldown, trigger a new burst
            if (holdingFireDuration >= burstCooldown * 0.8f) // Slight reduction to make it feel responsive
            {
                TriggerBurst();
                // Reset the holding timer so it doesn't immediately trigger again
                holdingFireDuration = 0f;
            }
        }

        // Mark button as released
        if (fireButtonUp)
        {
            hasReleasedButtonSinceLastBurst = true;
        }
    }
    
    private void TriggerBurst()
    {
        if (isReloading || isBursting)
            return;

        if (currentAmmo <= 0)
        {
            PlaySound(emptySound);
            Reload();
            return;
        }

        // Start burst sequence
        isBursting = true;
        remainingBurstShots = burstCount - 1; // -1 because we fire first shot immediately
        
        // Immediately fire the first shot
        FireShotgun(1f, 1f, 1f);
        lastBurstShotTime = Time.time;
    }
    
    private void HandleChargeInput()
    {
        // Start charging when button is pressed
        if (Input.GetButtonDown("Fire1") && !isCharging && !isReloading && currentAmmo > 0)
        {
            isCharging = true;
            currentChargeTime = 0f;
            
            // Create charge effect if enabled
            if (useChargeEffect)
            {
                CreateChargeEffect();
            }
        }
        
        // Continue charging while button is held
        if (Input.GetButton("Fire1") && isCharging)
        {
            currentChargeTime += Time.deltaTime;
            
            // Update charge effect
            if (useChargeEffect && chargeEffectInstance != null)
            {
                UpdateChargeEffect();
            }
        }
        
        // Fire when button is released
        if (Input.GetButtonUp("Fire1") && isCharging)
        {
            // Calculate charge percentage
            float chargePercentage = Mathf.Clamp01((currentChargeTime - minChargeTime) / (maxChargeTime - minChargeTime));
            
            // Only fire if minimum charge time is met
            if (currentChargeTime >= minChargeTime)
            {
                // Calculate modifiers based on charge level
                float pelletMultiplier = 1f + chargePercentage * (maxPelletMultiplier - 1f);
                float damageMultiplier = 1f + chargePercentage * (maxDamageMultiplier - 1f);
                float spreadMultiplier = 1f - chargePercentage * maxSpreadReduction;
                
                // Fire with modifiers
                FireShotgun(pelletMultiplier, damageMultiplier, spreadMultiplier);
            }
            
            // Clean up
            isCharging = false;
            currentChargeTime = 0f;
            
            if (chargeEffectInstance != null)
            {
                Destroy(chargeEffectInstance);
                chargeEffectInstance = null;
            }
        }
    }
    
    public override void Fire()
    {
        switch (fireMode)
        {
            case FireMode.SemiAuto:
                // Prevent firing multiple times per press
                if (hasFiredThisPress)
                    return;
                    
                hasFiredThisPress = true;
                break;
                
            case FireMode.Burst:
                // Calculate if cooldown is complete
                float timeSinceLastBurst = Time.time - lastBurstSequenceTime;
                bool cooldownComplete = timeSinceLastBurst >= burstCooldown;
                
                // Use the TriggerBurst method for burst mode
                if (cooldownComplete && !isBursting)
                {
                    TriggerBurst();
                }
                return; // Skip the rest of the method
                
            case FireMode.Charge:
                // Handled by charge input system
                return;
        }
        
        // Shared conditions check
        if (isReloading || Time.time < lastFireTime + fireRate)
            return;
            
        // Check ammo
        if (currentAmmo <= 0)
        {
            PlaySound(emptySound);
            
            // Auto reload
            if (!isReloading)
                Reload();
                
            return;
        }
        
        // Fire the shotgun with normal values
        FireShotgun(1f, 1f, 1f);
    }
    
    private void FireShotgun(float pelletMultiplier, float damageMultiplier, float spreadMultiplier)
    {
        // Update state
        lastFireTime = Time.time;
        currentAmmo--;
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Fire");
            
        // Play sound
        PlaySound(bulletFireSound);
        
        // Spawn pellets with modifiers
        SpawnPellets(pelletMultiplier, damageMultiplier, spreadMultiplier);
        
        // Create muzzle flash
        CreateMuzzleFlash();
        
        // Auto reload when empty
        if (currentAmmo <= 0 && !isBursting)
            Reload();
    }
    
    private void SpawnPellets(float pelletMultiplier, float damageMultiplier, float spreadMultiplier)
    {
        if (bulletPrefab != null && muzzlePoint != null)
        {
            // Calculate actual number of pellets based on modifier
            int actualPellets = Mathf.RoundToInt(pelletsPerShot * pelletMultiplier);
            float actualSpread = spreadAngle * spreadMultiplier;
            float actualDamage = bulletDamage * damageMultiplier;
            
            for (int i = 0; i < actualPellets; i++)
            {
                // Calculate random spread angle with modified spread
                float randomSpread = Random.Range(-actualSpread/2, actualSpread/2);
                
                // Create rotation with spread
                Quaternion spreadRotation = Quaternion.Euler(0, 0, randomSpread) * muzzlePoint.rotation;
                
                // Spawn pellet with spread
                GameObject pellet = Instantiate(bulletPrefab, muzzlePoint.position, spreadRotation);
                
                // Set velocity based on direction
                Rigidbody2D rb = pellet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = pellet.transform.right * bulletSpeed;
                }
                
                // Set damage
                Bullet bulletComponent = pellet.GetComponent<Bullet>();
                if (bulletComponent != null)
                {
                    bulletComponent.damage = actualDamage;
                }
                
                // Clean up after time
                Destroy(pellet, 1.0f);
            }
            
            // Apply shotgun-specific effects
            ApplyShotgunEffects();
        }
    }
    
    // For compatibility with the base class
    protected override void SpawnProjectile()
    {
        SpawnPellets(1f, 1f, 1f);
    }
    
    private void ApplyShotgunEffects()
    {
        // Apply recoil
        if (hasStoredPosition)
        {
            transform.localPosition = originalPosition - transform.right * kickbackAmount;
            
            // Return to original position gradually using coroutine
            StartCoroutine(SmoothReturnToPosition(0.2f));
        }
        
        // Apply screen shake if enabled
        if (useScreenShake)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                StartCoroutine(ShakeCamera(mainCam, 0.2f, screenShakeAmount));
            }
        }
    }
    
    // Charge effect methods
    private void CreateChargeEffect()
    {
        // Create a simple charge effect
        GameObject effectObj = new GameObject("ChargeEffect");
        effectObj.transform.position = muzzlePoint.position;
        effectObj.transform.SetParent(muzzlePoint);
        
        // Add a sprite renderer
        SpriteRenderer renderer = effectObj.AddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Effects/ChargeCircle"); // You'll need to create this sprite
        renderer.color = new Color(1f, 0.5f, 0f, 0.2f); // Orange-ish
        renderer.sortingOrder = 10;
        renderer.transform.localScale = Vector3.one * 0.1f;
        
        chargeEffectInstance = effectObj;
    }
    
    private void UpdateChargeEffect()
    {
        if (chargeEffectInstance == null) return;
        
        // Calculate charge percentage
        float chargePercentage = Mathf.Clamp01(currentChargeTime / maxChargeTime);
        
        // Scale based on charge
        float baseScale = 0.1f;
        float maxScale = 0.5f;
        float scale = baseScale + chargePercentage * (maxScale - baseScale);
        
        chargeEffectInstance.transform.localScale = Vector3.one * scale;
        
        // Change color based on charge
        SpriteRenderer renderer = chargeEffectInstance.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.Lerp(
                new Color(1f, 0.5f, 0f, 0.2f), // Starting orange
                new Color(1f, 0f, 0f, 0.5f),   // Ending red
                chargePercentage
            );
        }
    }
    
    // Coroutine to smoothly return to original position
    private IEnumerator SmoothReturnToPosition(float duration)
    {
        Vector3 startPos = transform.localPosition;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Smooth easing formula: t^2 * (3 - 2t)
            float smoothT = t * t * (3 - 2 * t);
            transform.localPosition = Vector3.Lerp(startPos, originalPosition, smoothT);
            yield return null;
        }
        
        transform.localPosition = originalPosition;
    }
    
    // Camera shake coroutine
    private IEnumerator ShakeCamera(Camera camera, float duration, float magnitude)
    {
        Vector3 originalPos = camera.transform.position;
        float elapsed = 0.0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            camera.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        camera.transform.position = originalPos;
    }
    
    // Override to prevent reloading during bursts
    public override void Reload()
    {
        // Don't reload during burst fire
        if (isBursting)
            return;
            
        base.Reload();
    }
}