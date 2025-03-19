using UnityEngine;
using System.Collections.Generic;

public class ModularSingleModeWeapon : WeaponBase
{
    [Header("Primary Fire Stats")]
    public float fireRate = 0.1f;
    public int magazineSize = 30;
    public int currentAmmo;
    public float bulletSpeed = 20f;
    public float bulletDamage = 10f;
    public float reloadTime = 1.5f;
    public FireMode fireMode = FireMode.FullAuto;
    
    [Header("Audio")]
    public AudioClip bulletFireSound;
    public AudioClip bulletReloadSound;
    public AudioClip emptySound;
    
    [Header("Effects")]
    public GameObject bulletPrefab;
    public List<Sprite> bulletSprites = new List<Sprite>();
    public GameObject muzzleFlashPrefab;
    
    // State tracking
    protected float lastFireTime;
    protected bool isReloading = false;
    protected int currentBulletSprite = 0;
    protected bool triggerReleased = true; // For semi-auto mode
    
    // MODIFIED: Fix the serialization conflict by using a different field name
    // and keeping a reference to the base class audioSource
    [System.NonSerialized] // Don't serialize this field
    protected AudioSource weaponAudioSource; // New name to avoid conflict
    
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
        
        // Set up muzzle point if needed
        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0.5f, 0.05f, 0);
            muzzlePoint = muzzleObj.transform;
        }
        
        // Get audio source and store in our renamed field
        weaponAudioSource = GetComponent<AudioSource>();
        if (weaponAudioSource == null)
        {
            weaponAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Keep the base class audioSource in sync
        audioSource = weaponAudioSource;
    }
    
    // MODIFIED: Use 'new' keyword to explicitly hide the base class Update
    private new void Update()
    {
        if (!isActive || playerTransform == null)
            return;
            
        // Handle input based on fire mode
        switch (fireMode)
        {
            case FireMode.SemiAuto:
                if (Input.GetButtonDown("Fire1"))
                {
                    Fire();
                }
                break;
                
            case FireMode.FullAuto:
                if (Input.GetButton("Fire1"))
                {
                    Fire();
                }
                break;
                
            case FireMode.Burst:
                // Implemented in derived classes
                break;
                
            case FireMode.Charge:
                // Implemented in derived classes
                break;
        }
        
        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
    }
    
    public override void Fire()
    {
        // Can't fire while hidden/dashing
        WeaponFollower follower = GetComponent<WeaponFollower>();
        if (follower != null && follower.IsWeaponHidden())
            return;
            
        // Check firing conditions
        if (isReloading || Time.time < lastFireTime + fireRate)
            return;
            
        // Check ammo
        if (currentAmmo <= 0)
        {
            PlaySound(emptySound);
            
            // Auto reload when empty
            if (!isReloading)
                Reload();
                
            return;
        }
        
        // Update firing time
        lastFireTime = Time.time;
        currentAmmo--;
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Fire");
            
        // Play fire sound
        PlaySound(bulletFireSound);
            
        // Spawn projectile
        SpawnProjectile();
        
        // Create muzzle flash
        CreateMuzzleFlash();
        
        // Auto reload when empty
        if (currentAmmo <= 0)
            Reload();
    }
    
    protected virtual void SpawnProjectile()
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
    
    protected virtual void CreateMuzzleFlash()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.SetParent(muzzlePoint);
            Destroy(flash, 0.05f);
        }
    }
    
    protected virtual void PlaySound(AudioClip sound)
    {
        if (weaponAudioSource != null && sound != null)
        {
            weaponAudioSource.PlayOneShot(sound);
        }
    }
    
    public override void Reload()
    {
        if (isReloading || currentAmmo >= magazineSize)
            return;
            
        isReloading = true;
        
        // Play reload animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Reload");
            
        // Play reload sound
        PlaySound(bulletReloadSound);
            
        // Complete reload after delay
        Invoke("CompleteReload", reloadTime);
    }
    
    protected virtual void CompleteReload()
    {
        currentAmmo = magazineSize;
        isReloading = false;
    }
    
    public override string GetAmmoText()
    {
        return currentAmmo + " / " + magazineSize;
    }
    
    private void OnDestroy()
    {
        CancelInvoke();
    }
}

public class SemiAutoWeapon : ModularSingleModeWeapon
{
    private bool hasReleasedButtonSinceFiring = true;
    
    // Override the Update method to enforce true semi-auto behavior
    private new void Update()
    {
        if (!isActive || playerTransform == null)
            return;
            
        // Handle firing with proper semi-auto behavior
        if (Input.GetButtonDown("Fire1") && hasReleasedButtonSinceFiring)
        {
            Fire();
            hasReleasedButtonSinceFiring = false;
        }
        
        // Must release button between shots
        if (Input.GetButtonUp("Fire1"))
        {
            hasReleasedButtonSinceFiring = true;
        }
        
        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
    }
}