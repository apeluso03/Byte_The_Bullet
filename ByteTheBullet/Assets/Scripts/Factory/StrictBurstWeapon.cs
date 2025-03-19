using System.Collections;
using UnityEngine;

/// <summary>
/// Specialized weapon class that implements true burst-fire functionality.
/// Controls burst size, timing, and prevents full-auto behavior.
/// </summary>
public class StrictBurstWeapon : WeaponBase
{
    [Header("Weapon Stats")]
    public float fireRate = 0.25f;
    public int magazineSize = 30;
    public int currentAmmo = 30;
    public float bulletSpeed = 20f;
    public float bulletDamage = 10f;
    public float reloadTime = 1.5f;

    [Header("Burst Settings")]
    [Range(2, 8)]
    public int burstSize = 3;
    [Tooltip("Time between bullets in a single burst")]
    public float burstFireRate = 0.1f;
    [Tooltip("Time between bursts")]
    public float burstCooldown = 0.5f;
    
    [Header("Auto-Burst Settings")]
    [Tooltip("When enabled, holding the trigger fires bursts automatically")]
    public bool enableAutoBurst = true;
    
    [Header("Audio")]
    public AudioClip bulletFireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    [Header("Effects")]
    public GameObject bulletPrefab;
    public GameObject muzzleFlashPrefab;

    // State tracking
    private bool isReloading = false;
    private float lastFireTime = -1f;
    private bool isBursting = false;
    private int remainingBurstShots = 0;
    private float lastBurstShotTime = -1f;
    private float lastBurstSequenceTime = 0f;
    private bool isHoldingFireButton = false;
    private float holdingFireDuration = 0f;

    // Debug tools
    [SerializeField] private bool showDebugUI = false;
    private string debugText = "";

    protected new void Update()
    {
        if (!isActive || playerTransform == null)
            return;

        UpdateInputState();
        ProcessBurstSequence();
        HandleReloadInput();
        UpdateDebugText();
    }

    private void UpdateInputState()
    {
        // Get button states
        bool fireButtonDown = Input.GetButtonDown("Fire1");
        bool fireButtonHeld = Input.GetButton("Fire1");
        bool fireButtonUp = Input.GetButtonUp("Fire1");
        bool cooldownComplete = (Time.time - lastBurstSequenceTime) >= burstCooldown;

        // Track how long fire button has been held
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
        if (fireButtonDown && cooldownComplete && !isBursting)
        {
            TriggerBurst();
        }
        
        // Handle auto-burst when holding button
        if (enableAutoBurst && isHoldingFireButton && !isBursting && cooldownComplete 
            && currentAmmo > 0 && !isReloading)
        {
            if (holdingFireDuration >= burstCooldown * 0.8f)
            {
                TriggerBurst();
                holdingFireDuration = 0f;
            }
        }
    }

    private void ProcessBurstSequence()
    {
        // Process ongoing burst shots
        if (isBursting && remainingBurstShots > 0 && Time.time >= lastBurstShotTime + burstFireRate)
        {
            FireBurst();
            remainingBurstShots--;
            lastBurstShotTime = Time.time;
            
            if (remainingBurstShots <= 0)
            {
                isBursting = false;
                lastBurstSequenceTime = Time.time;
            }
        }
    }

    private void HandleReloadInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
            StartReload();
    }

    private void UpdateDebugText()
    {
        if (!showDebugUI)
            return;
            
        float timeSinceLastBurst = Time.time - lastBurstSequenceTime;
        bool cooldownComplete = timeSinceLastBurst >= burstCooldown;
        
        debugText = $"Burst: {(isBursting ? "ON" : "OFF")}\n" +
                    $"Remaining: {remainingBurstShots}\n" +
                    $"Cooldown: {(cooldownComplete ? "READY" : $"{burstCooldown - timeSinceLastBurst:0.0}s")}\n" +
                    $"Holding: {(isHoldingFireButton ? $"{holdingFireDuration:0.0}s" : "NO")}";
    }

    private void TriggerBurst()
    {
        if (isReloading || isBursting)
            return;

        if (currentAmmo <= 0)
        {
            PlaySound(emptySound);
            StartReload();
            return;
        }

        // Start burst sequence
        isBursting = true;
        remainingBurstShots = burstSize - 1; // -1 because we fire first shot immediately
        
        // Immediately fire the first shot
        FireBurst();
        lastBurstShotTime = Time.time;
    }

    public override void Fire()
    {
        float timeSinceLastBurst = Time.time - lastBurstSequenceTime;
        bool cooldownComplete = timeSinceLastBurst >= burstCooldown;
        
        if (cooldownComplete && !isBursting)
            TriggerBurst();
    }

    private void FireBurst()
    {
        if (isReloading || currentAmmo <= 0)
        {
            if (currentAmmo <= 0)
            {
                PlaySound(emptySound);
                StartReload();
            }
            return;
        }

        // Update state
        lastFireTime = Time.time;
        currentAmmo--;

        // Play animation if available
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Fire");

        // Play sound and effects
        PlaySound(bulletFireSound);
        SpawnProjectile();
        CreateMuzzleFlash();

        // Auto reload when empty
        if (currentAmmo <= 0 && !isBursting)
            StartReload();
    }

    private void SpawnProjectile()
    {
        if (bulletPrefab != null && muzzlePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
            
            // Set velocity based on direction
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = bullet.transform.right * bulletSpeed;
            
            // Set damage
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
                bulletComponent.damage = bulletDamage;
            
            // Clean up after time
            Destroy(bullet, 2.0f);
        }
    }

    private void CreateMuzzleFlash()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            muzzleFlash.transform.SetParent(muzzlePoint);
            Destroy(muzzleFlash, 0.1f);
        }
    }

    private void StartReload()
    {
        if (isReloading || currentAmmo == magazineSize)
            return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // Play animation if available
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Reload");

        // Play sound
        PlaySound(reloadSound);

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public override void Reload()
    {
        if (!isReloading)
            StartReload();
    }

    public override string GetAmmoText()
    {
        return $"{currentAmmo} / {magazineSize}";
    }

    public override void Initialize(Transform player)
    {
        base.Initialize(player);
        currentAmmo = magazineSize;
    }

    public override void Deactivate()
    {
        base.Deactivate();
        
        // Cancel any ongoing bursting
        isBursting = false;
        remainingBurstShots = 0;
        isHoldingFireButton = false;
        holdingFireDuration = 0f;
    }

    // Draw debug info in game view
    void OnGUI()
    {
        if (showDebugUI && isActive)
            GUI.Label(new Rect(10, 10, 200, 100), debugText);
    }
}