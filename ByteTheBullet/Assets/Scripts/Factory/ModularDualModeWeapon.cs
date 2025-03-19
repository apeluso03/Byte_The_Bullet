using UnityEngine;
using System.Collections.Generic;

public class ModularDualModeWeapon : WeaponBase
{
    [Header("Primary Fire Stats")]
    public float fireRate = 0.1f;
    public int magazineSize = 30;
    public int currentAmmo;
    public float bulletSpeed = 20f;
    public float bulletDamage = 10f;
    public float primaryReloadTime = 1.5f;
    public FireMode primaryFireMode = FireMode.FullAuto;
    
    [Header("Secondary Fire Stats")]
    public bool hasSecondaryFire = true;
    public float secondaryFireRate = 1.0f;
    public int secondaryMagazineSize = 3;
    public int secondaryAmmo;
    public float secondaryProjectileSpeed = 10f;
    public float secondaryDamage = 30f;
    public float secondaryReloadTime = 2.0f;
    public FireMode secondaryFireMode = FireMode.SemiAuto;
    
    [Header("Audio")]
    public AudioClip primaryFireSound;
    public AudioClip primaryReloadSound;
    public AudioClip secondaryFireSound;
    public AudioClip secondaryReloadSound;
    public AudioClip emptySound;
    public AudioClip switchModeSound;
    
    [Header("Effects")]
    public GameObject bulletPrefab;
    public GameObject secondaryProjectilePrefab;
    public List<Sprite> bulletSprites = new List<Sprite>();
    public GameObject primaryMuzzleFlashPrefab;
    public GameObject secondaryMuzzleFlashPrefab;
    
    // State tracking
    protected float lastPrimaryFireTime;
    protected float lastSecondaryFireTime;
    protected bool isReloading = false;
    protected bool isSecondaryMode = false;
    protected int currentBulletSprite = 0;
    protected bool triggerReleased = true; // For semi-auto mode
    
    // Audio sources
    protected AudioSource primaryAudioSource;
    protected AudioSource secondaryAudioSource;
    
    public enum FireMode
    {
        SemiAuto,
        FullAuto,
        Burst,
        Charge
    }
    
    public override void Initialize(Transform player)
    {
        base.Initialize(player);
        
        // Initialize ammo
        currentAmmo = magazineSize;
        secondaryAmmo = secondaryMagazineSize;
        
        // Set up muzzle point if needed
        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0.5f, 0.05f, 0);
            muzzlePoint = muzzleObj.transform;
        }
        
        // Set up audio sources
        primaryAudioSource = GetComponent<AudioSource>();
        if (primaryAudioSource == null)
        {
            primaryAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Set up secondary audio source
        GameObject audioObj = transform.Find("SecondaryAudioSource")?.gameObject;
        if (audioObj == null)
        {
            audioObj = new GameObject("SecondaryAudioSource");
            audioObj.transform.SetParent(transform);
            audioObj.transform.localPosition = Vector3.zero;
        }
        secondaryAudioSource = audioObj.GetComponent<AudioSource>();
        if (secondaryAudioSource == null)
        {
            secondaryAudioSource = audioObj.AddComponent<AudioSource>();
        }
        
        // Configure audio sources
        ConfigureAudioSources();
    }
    
    private void Update()
    {
        if (!isActive || playerTransform == null)
            return;
            
        // Handle firing based on current mode
        if (Input.GetButton("Fire1"))
        {
            if (isSecondaryMode && hasSecondaryFire)
            {
                FireSecondary();
            }
            else
            {
                FirePrimary();
            }
        }
        
        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
        
        // Toggle weapon mode
        if (Input.GetMouseButtonDown(1) && hasSecondaryFire)
        {
            ToggleWeaponMode();
        }
    }
    
    protected virtual void FirePrimary()
    {
        // Can't fire while hidden/dashing
        WeaponFollower follower = GetComponent<WeaponFollower>();
        if (follower != null && follower.IsWeaponHidden())
            return;
            
        // Check firing conditions
        if (isReloading || Time.time < lastPrimaryFireTime + fireRate)
            return;
            
        // Check ammo
        if (currentAmmo <= 0)
        {
            PlaySound(emptySound, false);
            
            // Auto reload when empty
            if (!isReloading)
                Reload();
                
            return;
        }
        
        // Update firing time and ammo
        lastPrimaryFireTime = Time.time;
        currentAmmo--;
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Fire");
            
        // Play fire sound
        PlaySound(primaryFireSound, false);
            
        // Spawn projectile
        SpawnPrimaryProjectile();
        
        // Create muzzle flash
        CreateMuzzleFlash(primaryMuzzleFlashPrefab);
        
        // Auto reload when empty
        if (currentAmmo <= 0)
            Reload();
    }
    
    protected virtual void FireSecondary()
    {
        // Can't fire while hidden/dashing
        WeaponFollower follower = GetComponent<WeaponFollower>();
        if (follower != null && follower.IsWeaponHidden())
            return;
            
        // Check firing conditions
        if (isReloading || Time.time < lastSecondaryFireTime + secondaryFireRate)
            return;
            
        // Check ammo
        if (secondaryAmmo <= 0)
        {
            PlaySound(emptySound, true);
            
            // Auto reload when empty
            if (!isReloading)
                Reload();
                
            return;
        }
        
        // Update firing time and ammo
        lastSecondaryFireTime = Time.time;
        secondaryAmmo--;
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("FireSecondary");
            
        // Play fire sound
        PlaySound(secondaryFireSound, true);
            
        // Spawn projectile
        SpawnSecondaryProjectile();
        
        // Create muzzle flash
        CreateMuzzleFlash(secondaryMuzzleFlashPrefab);
        
        // Auto reload when empty
        if (secondaryAmmo <= 0)
            Reload();
    }
    
    protected virtual void SpawnPrimaryProjectile()
    {
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
            Destroy(bullet, 5f);
        }
    }
    
    protected virtual void SpawnSecondaryProjectile()
    {
        if (secondaryProjectilePrefab != null && muzzlePoint != null)
        {
            GameObject projectile = Instantiate(secondaryProjectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
            
            // Launch projectile
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Add upward angle for arc
                Vector2 direction = muzzlePoint.right;
                direction += Vector2.up * 0.2f;
                rb.linearVelocity = direction.normalized * secondaryProjectileSpeed;
            }
            
            // Set damage if it's a grenade
            Grenade grenadeComponent = projectile.GetComponent<Grenade>();
            if (grenadeComponent != null)
                grenadeComponent.damage = secondaryDamage;
                
            // Clean up projectile after time if needed
            // Grenade has its own self-destruct, but add fallback
            Destroy(projectile, 10f);
        }
    }
    
    protected virtual void CreateMuzzleFlash(GameObject flashPrefab)
    {
        if (flashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(flashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.SetParent(muzzlePoint);
            Destroy(flash, 0.05f);
        }
    }
    
    protected virtual void PlaySound(AudioClip sound, bool isSecondary)
    {
        AudioSource sourceToUse = isSecondary ? secondaryAudioSource : primaryAudioSource;
        
        if (sourceToUse != null && sound != null)
        {
            if (isSecondary)
            {
                // For secondary sounds (like grenades), use direct play to prevent cutoffs
                if (!sourceToUse.isPlaying || sourceToUse.time > sourceToUse.clip?.length * 0.75f)
                {
                    sourceToUse.Stop();
                    sourceToUse.clip = sound;
                    sourceToUse.Play();
                }
            }
            else
            {
                // For primary weapons, use PlayOneShot for layered sounds
                sourceToUse.PlayOneShot(sound);
            }
        }
    }
    
    private void ConfigureAudioSources()
    {
        // Configure primary audio source
        if (primaryAudioSource != null)
        {
            primaryAudioSource.playOnAwake = false;
            primaryAudioSource.spatialBlend = 0f;
        }
        
        // Configure secondary audio source
        if (secondaryAudioSource != null)
        {
            secondaryAudioSource.playOnAwake = false;
            secondaryAudioSource.loop = false;
            secondaryAudioSource.priority = 0;
            secondaryAudioSource.volume = 1.0f;
            secondaryAudioSource.spatialBlend = 0f;
            secondaryAudioSource.dopplerLevel = 0f;
        }
    }
    
    public override void Fire()
    {
        if (isSecondaryMode && hasSecondaryFire)
        {
            FireSecondary();
        }
        else
        {
            FirePrimary();
        }
    }
    
    public override void Reload()
    {
        if (isReloading)
            return;
            
        // Handle reload based on current mode
        if (isSecondaryMode)
        {
            // Secondary reload
            if (secondaryAmmo >= secondaryMagazineSize)
                return;
                
            isReloading = true;
            
            // Play reload animation
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger("Reload");
                
            // Play reload sound
            PlaySound(secondaryReloadSound, true);
                
            // Complete reload after delay
            Invoke("CompleteSecondaryReload", secondaryReloadTime);
        }
        else
        {
            // Primary reload
            if (currentAmmo >= magazineSize)
                return;
                
            isReloading = true;
            
            // Play reload animation
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger("Reload");
                
            // Play reload sound
            PlaySound(primaryReloadSound, false);
                
            // Complete reload after delay
            Invoke("CompletePrimaryReload", primaryReloadTime);
        }
    }
    
// ... continuing ModularDualModeWeapon class
    protected virtual void ToggleWeaponMode()
    {
        // Don't allow mode toggle during reload
        if (isReloading)
            return;
            
        // Toggle mode
        isSecondaryMode = !isSecondaryMode;
        
        // Play switch sound
        PlaySound(switchModeSound, false);
        
        // Update animator boolean for mode
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool("SecondaryMode", isSecondaryMode);
        }
        
        // Check if we need to auto-reload
        if (isSecondaryMode && secondaryAmmo <= 0)
        {
            Reload();
        }
        else if (!isSecondaryMode && currentAmmo <= 0)
        {
            Reload();
        }
    }
    
    protected virtual void CompletePrimaryReload()
    {
        currentAmmo = magazineSize;
        isReloading = false;
    }
    
    protected virtual void CompleteSecondaryReload()
    {
        secondaryAmmo = secondaryMagazineSize;
        isReloading = false;
    }
    
    public override string GetAmmoText()
    {
        if (isSecondaryMode && hasSecondaryFire)
            return secondaryAmmo + " / " + secondaryMagazineSize + " ALT";
        else
            return currentAmmo + " / " + magazineSize;
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}