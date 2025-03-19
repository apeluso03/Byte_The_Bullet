using UnityEngine;
using System.Collections.Generic;

public class M16Weapon : WeaponBase
{
    [Header("M16 Stats")]
    public float fireRate = 0.1f;
    public int magazineSize = 30;
    public int currentAmmo;
    public float bulletSpeed = 20f;
    public float bulletDamage = 10f;
    
    [Header("Weapon Audio")]
    public AudioClip bulletFireSound;    // Renamed for clarity
    public AudioClip bulletReloadSound;  // Renamed for clarity
    public AudioClip emptySound;
    public AudioClip switchModeSound;
    public AudioClip grenadeLauncherFireSound;  // Dedicated grenade sound
    public AudioClip grenadeLauncherReloadSound; // Dedicated grenade reload sound
    
    [Header("Prefabs & Effects")]
    public GameObject bulletPrefab;
    public List<Sprite> bulletSprites = new List<Sprite>();
    public GameObject muzzleFlashPrefab;
    
    [Header("Grenade Launcher")]
    public bool hasGrenadeLauncher = false;
    public GameObject grenadePrefab;
    public GameObject backupGrenadePrefab;
    public float grenadeSpeed = 10f;
    public int grenadeAmmo = 3;
    public int maxGrenadeAmmo = 3;
    
    // State tracking
    private float lastBulletFireTime;
    private float lastGrenadeFireTime;
    private bool isReloading = false;
    private bool grenadeMode = false;
    private int currentBulletSprite = 0;
    
    // Reference to the launcher component
    [HideInInspector]
    public GrenadeLauncher grenadeLauncherComponent;
    
    [Header("Audio System")]
    private AudioSource mainAudioSource; // Main sound source
    private AudioSource grenadeAudioSource; // Dedicated to grenade sounds
    
    // Override Initialize to set up weapon specifics
    public override void Initialize(Transform player)
    {
        base.Initialize(player);
        
        // Add weapon follower if missing
        if (GetComponent<WeaponFollower>() == null)
        {
            WeaponFollower follower = gameObject.AddComponent<WeaponFollower>();
            follower.SetPlayer(player);
        }
        else
        {
            GetComponent<WeaponFollower>().SetPlayer(player);
        }
        
        // Find grenade launcher component
        grenadeLauncherComponent = GetComponentInChildren<GrenadeLauncher>();
        
        // Initialize ammo
        currentAmmo = magazineSize;
        
        // Set up muzzle point if needed
        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0.5f, 0.05f, 0);
            muzzlePoint = muzzleObj.transform;
        }
        
        // Store a backup of the grenade prefab
        if (grenadePrefab != null && backupGrenadePrefab == null)
        {
            backupGrenadePrefab = grenadePrefab;
        }
        
        // Ensure we have an audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Share audio clips with the grenade launcher component if found
        if (grenadeLauncherComponent != null)
        {
            // Tell the component NOT to play its own sounds
            grenadeLauncherComponent.useSeparateAudio = false;
            Debug.Log("Set grenade launcher to use M16Weapon's audio system");
        }
        
        // Setup audio sources
        mainAudioSource = GetComponent<AudioSource>();
        if (mainAudioSource == null)
            mainAudioSource = gameObject.AddComponent<AudioSource>();
            
        // Create a separate audio source for grenade sounds
        GameObject audioObj = new GameObject("GrenadeAudioSource");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
        grenadeAudioSource = audioObj.AddComponent<AudioSource>();
        
        // Configure audio sources with optimal settings
        ConfigureAudioSources();
        
        // Store reference
        audioSource = mainAudioSource;
    }
    
    private void Update()
    {
        if (!isActive || playerTransform == null)
            return;
            
        // Handle firing based on current mode
        if (Input.GetButton("Fire1"))
        {
            // Completely separate firing paths based on mode
            if (grenadeMode && hasGrenadeLauncher)
            {
                // ONLY try to fire grenades in grenade mode
                FireGrenade();
            }
            else if (!grenadeMode)
            {
                // ONLY try to fire bullets in regular mode
                FireBullet();
            }
        }
        
        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
        
        // Toggle grenade mode
        if (Input.GetMouseButtonDown(1) && hasGrenadeLauncher)
        {
            ToggleWeaponMode();
        }
    }
    
    private void FireBullet()
    {
        // Can't fire while hidden/dashing
        WeaponFollower follower = GetComponent<WeaponFollower>();
        if (follower != null && follower.IsWeaponHidden())
            return;
        
        // Check bullet-specific conditions
        if (isReloading || Time.time < lastBulletFireTime + fireRate)
            return;
            
        // Check ammo
        if (currentAmmo <= 0)
        {
            // Play empty sound
            PlaySound(emptySound);
            return;
        }
        
        // Update bullet firing time
        lastBulletFireTime = Time.time;
        currentAmmo--;
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Fire");
            
        // Play bullet fire sound
        PlaySound(bulletFireSound);
            
        // Spawn bullet
        if (bulletPrefab != null && muzzlePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
            
            // Set bullet properties
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = muzzlePoint.right * bulletSpeed;
                
            // Set bullet sprite if available
            if (bulletSprites.Count > 0)
            {
                SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
                if (bulletRenderer != null)
                {
                    bulletRenderer.sprite = bulletSprites[currentBulletSprite];
                    currentBulletSprite = (currentBulletSprite + 1) % bulletSprites.Count;
                }
            }
            
            // Handle any special bullet behavior
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
                bulletComponent.damage = bulletDamage;
                
            // Clean up bullet after time
            Destroy(bullet, 2f);
        }
        
        // Create muzzle flash if available
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.SetParent(muzzlePoint);
            Destroy(flash, 0.05f);
        }
        
        // Auto reload when empty
        if (currentAmmo <= 0)
            Reload();
    }
    
    private void FireGrenade()
    {
        // Changed cooldown to better match a typical 1.31s grenade sound
        float grenadeFireCooldown = 0.85f; // Shorter than the sound duration
        
        // Check if weapon is hidden (can't fire during dash)
        WeaponFollower follower = GetComponent<WeaponFollower>();
        if (follower != null && follower.IsWeaponHidden())
            return;
        
        // If we have a component, let it handle the actual firing logic
        if (grenadeLauncherComponent != null && grenadeLauncherComponent.isActive)
        {
            // Check if we can actually fire
            if (grenadeAmmo <= 0 || isReloading)
            {
                if (grenadeAmmo <= 0)
                {
                    PlaySound(emptySound);
                    
                    if (!isReloading)
                        Reload();
                }
                return;
            }
            
            // Make sure we respect cooldown for both sound and fire
            if (Time.time < lastGrenadeFireTime + grenadeFireCooldown)
            {
                return;  // Exit early if trying to fire too fast
            }
            
            // Update last fire time BEFORE playing sound
            lastGrenadeFireTime = Time.time;
            
            // Play grenade fire sound FIRST
            PlaySound(grenadeLauncherFireSound, true);
            
            // Let component handle actual firing logic
            grenadeLauncherComponent.FireGrenade();
            return;
        }
        
        // Direct firing path (no component)
        
        // Grenade-specific cooldown check - stricter enforcement
        if (isReloading || Time.time < lastGrenadeFireTime + grenadeFireCooldown)
            return;
            
        // Check grenade ammo
        if (grenadeAmmo <= 0 || grenadePrefab == null)
        {
            // Play empty sound
            PlaySound(emptySound);
            
            // Auto-reload when trying to fire with no ammo
            if (grenadeAmmo <= 0 && !isReloading)
            {
                Reload();
            }
                
            return;
        }
            
        // Update grenade firing time and ammo
        lastGrenadeFireTime = Time.time;
        grenadeAmmo--;
        
        Debug.Log("Fired grenade. Remaining: " + grenadeAmmo);
        
        // Play the grenade fire animation
        if (weaponAnimator != null)
        {
            // Set the trigger to play the animation
            weaponAnimator.SetTrigger("FireGrenade");
        }
        
        // Play grenade launcher fire sound
        PlaySound(grenadeLauncherFireSound, true);
            
        // Spawn grenade
        if (muzzlePoint != null && grenadePrefab != null)
        {
            GameObject grenade = Instantiate(grenadePrefab, muzzlePoint.position, muzzlePoint.rotation);
            
            // Launch the grenade
            Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Add upward angle for arc
                Vector2 direction = muzzlePoint.right;
                direction += Vector2.up * 0.2f;
                rb.linearVelocity = direction.normalized * grenadeSpeed;
            }
        }
        
        // Auto reload when empty
        if (grenadeAmmo <= 0 && !isReloading)
        {
            Reload();
        }
    }
    
    // Updated PlaySound method to better handle both tapping and holding fire
    private void PlaySound(AudioClip sound, bool isGrenadeSound = false)
    {
        // Choose the appropriate audio source
        AudioSource sourceToUse = isGrenadeSound ? grenadeAudioSource : mainAudioSource;
        
        if (sourceToUse != null && sound != null)
        {
            // Special handling for grenade sounds
            if (isGrenadeSound)
            {
                // If it's a tap-fire (not currently playing), play the whole sound
                if (!sourceToUse.isPlaying)
                {
                    sourceToUse.clip = sound;
                    sourceToUse.time = 0; // Start from beginning
                    sourceToUse.Play();
                    Debug.Log("Starting new grenade fire sound (tap)");
                }
                // If holding fire and the sound is nearing completion, restart it
                // but skip the intro part of the sound
                else if (sourceToUse.time > sourceToUse.clip.length * 0.75f)
                {
                    float startTime = 0.0f; // Start from beginning for first shot
                    
                    // For continuous fire, we'll start at a specific point in the audio
                    // to create a more natural-sounding loop
                    if (Time.time - lastGrenadeFireTime < 1.5f)
                    {
                        // Get 20% into the sound to skip initial attack if firing rapidly
                        startTime = 0.2f; 
                    }
                    
                    sourceToUse.Stop();
                    sourceToUse.clip = sound;
                    sourceToUse.time = startTime;
                    sourceToUse.Play();
                    Debug.Log("Restarting grenade fire sound (hold) at time: " + startTime);
                }
                // Otherwise we're still playing a recent sound, so don't interrupt
                else
                {
                    Debug.Log("Skipping sound - previous grenade sound still playing: " + sourceToUse.time);
                }
            }
            else
            {
                // Regular sounds can use PlayOneShot
                sourceToUse.PlayOneShot(sound);
            }
        }
        else
        {
            Debug.LogWarning("Cannot play sound: " + (sound == null ? "Null sound clip" : "Null audio source"));
        }
    }
    
    public override void Fire()
    {
        // This is the base method that gets called from outside
        // We'll just redirect to the appropriate firing method
        if (grenadeMode && hasGrenadeLauncher)
        {
            FireGrenade();
        }
        else
        {
            FireBullet();
        }
    }
    
    public override void Reload()
    {
        if (isReloading)
            return;
            
        // Handle reload based on current mode
        if (grenadeMode)
        {
            // Grenade reload
            if (grenadeAmmo >= maxGrenadeAmmo)
                return;
                
            isReloading = true;
            
            Debug.Log("Starting grenade reload...");
            
            // If we have component, let it handle the logic (but we'll still play sound)
            if (grenadeLauncherComponent != null && grenadeLauncherComponent.isActive)
            {
                // Play animation and sound first
                if (weaponAnimator != null)
                    weaponAnimator.SetTrigger("Reload");
                    
                PlaySound(grenadeLauncherReloadSound);
                
                // Let component handle actual reload logic
                grenadeLauncherComponent.StartReload();
                return;
            }
            
            // Otherwise handle it directly
            
            // Play reload animation
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger("Reload");
                
            // Play grenade reload sound
            PlaySound(grenadeLauncherReloadSound);
                
            // Complete reload after delay
            Invoke("CompleteGrenadeReload", 2.0f);
        }
        else
        {
            // Bullet reload
            if (currentAmmo >= magazineSize)
                return;
                
            isReloading = true;
            
            // Play reload animation
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger("Reload");
                
            // Play reload sound
            PlaySound(bulletReloadSound);
                
            // Complete reload after delay
            Invoke("CompleteBulletReload", 1.5f);
        }
    }
    
    private void ToggleWeaponMode()
    {
        // Don't allow mode toggle during reload
        if (isReloading)
            return;
            
        // Toggle mode
        grenadeMode = !grenadeMode;
        
        // Play switch sound
        PlaySound(switchModeSound);
        
        // Log for debugging
        Debug.Log($"WEAPON MODE SWITCHED TO: {(grenadeMode ? "GRENADE LAUNCHER" : "REGULAR")}");
        
        // IMPORTANT: Update animator boolean for grenade mode
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool("GrenadeMode", grenadeMode);
        }
        
        // Update grenade launcher component if available
        if (grenadeLauncherComponent != null)
        {
            grenadeLauncherComponent.isActive = grenadeMode;
            
            // Sync ammo counts
            if (grenadeMode)
            {
                grenadeAmmo = grenadeLauncherComponent.currentAmmo;
                maxGrenadeAmmo = grenadeLauncherComponent.magazineSize;
            }
            else
            {
                grenadeLauncherComponent.currentAmmo = grenadeAmmo;
            }
        }
        
        // Check if we need to auto-reload
        if (grenadeMode && grenadeAmmo <= 0)
        {
            Debug.Log("Auto-reloading grenades because ammo is 0");
            Reload();
        }
        else if (!grenadeMode && currentAmmo <= 0)
        {
            Debug.Log("Auto-reloading bullets because ammo is 0");
            Reload();
        }
    }
    
    private void CompleteBulletReload()
    {
        currentAmmo = magazineSize;
        isReloading = false;
    }
    
    private void CompleteGrenadeReload()
    {
        grenadeAmmo = maxGrenadeAmmo;
        isReloading = false;
    }
    
    public override string GetAmmoText()
    {
        if (grenadeMode && hasGrenadeLauncher)
            return grenadeAmmo + " / " + maxGrenadeAmmo + " GRENADES";
        else
            return currentAmmo + " / " + magazineSize;
    }
    
    // Public methods for the GrenadeLauncher to access
    public bool IsReloading()
    {
        return isReloading;
    }
    
    public void SetReloadingState(bool state)
    {
        isReloading = state;
    }
    
    public void PlayGrenadeFireAnimation()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("FireGrenade");
        }
    }
    
    public void PlayReloadAnimation()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Reload");
        }
    }
    
    // Add this method to avoid cutting off sounds when destroyed
    private void OnDestroy()
    {
        // Cancel any pending invokes that might try to access destroyed objects
        CancelInvoke();
    }
    
    // Configure audio sources with better settings
    private void ConfigureAudioSources()
    {
        // Configure grenade audio source with optimal settings
        if (grenadeAudioSource != null)
        {
            grenadeAudioSource.playOnAwake = false;
            grenadeAudioSource.loop = false;
            grenadeAudioSource.priority = 0; // Highest priority
            grenadeAudioSource.volume = 1.0f;
            grenadeAudioSource.spatialBlend = 0f; // 2D sound
            
            // Important for handling longer sounds
            grenadeAudioSource.dopplerLevel = 0f;
            grenadeAudioSource.bypassEffects = true; // Skip effects processing
            grenadeAudioSource.bypassListenerEffects = true;
            grenadeAudioSource.ignoreListenerPause = true; // Keep playing when game paused
            
            // Set output mixer group to default if needed
            grenadeAudioSource.outputAudioMixerGroup = null;
        }
        
        // Configure main audio source
        if (mainAudioSource != null)
        {
            mainAudioSource.playOnAwake = false;
            mainAudioSource.spatialBlend = 0f; // 2D sound
        }
    }
}