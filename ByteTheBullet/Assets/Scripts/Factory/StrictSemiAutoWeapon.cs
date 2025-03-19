using UnityEngine;
using System.Collections.Generic;

// Create this as a new file: StrictSemiAutoWeapon.cs
public class StrictSemiAutoWeapon : WeaponBase
{
    [Header("Weapon Stats")]
    public float fireRate = 0.2f;
    public int magazineSize = 6;
    public int currentAmmo;
    public float bulletSpeed = 15f;
    public float bulletDamage = 15f;
    public float reloadTime = 1.5f;
    
    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    [Header("Effects")]
    public GameObject bulletPrefab;
    public List<Sprite> bulletSprites;
    public GameObject muzzleFlashPrefab;
    
    // State tracking
    private float lastFireTime;
    private bool isReloading = false;
    private bool hasFiredThisPress = false; // KEY VARIABLE: tracks if we've already fired during this button press
    
    private AudioSource weaponAudioSource;
    
    // Add a timestamp to track precisely when we last fired
    private float lastButtonPressTime = 0f;
    
    // Add at the class level
    private int debugFireCount = 0;
    private float lastDebugTime = 0f;
    
    public override void Initialize(Transform player)
    {
        base.Initialize(player);
        
        // Initialize ammo
        currentAmmo = magazineSize;
        
        // Set up audio
        weaponAudioSource = GetComponent<AudioSource>();
        if (weaponAudioSource == null)
            weaponAudioSource = gameObject.AddComponent<AudioSource>();
        
        // Store reference to base audio 
        audioSource = weaponAudioSource;
        
        // Set up muzzle point if needed
        if (muzzlePoint == null)
        {
            GameObject muzzleObj = new GameObject("MuzzlePoint");
            muzzleObj.transform.SetParent(transform);
            muzzleObj.transform.localPosition = new Vector3(0.5f, 0.05f, 0);
            muzzlePoint = muzzleObj.transform;
        }
        
        Debug.Log("StrictSemiAutoWeapon initialized");
    }
    
    private new void Update()
    {
        if (!isActive || playerTransform == null)
            return;
        
        // Add extra protection against double-firing
        if (Input.GetButtonDown("Fire1"))
        {
            // Check if enough time has passed since last button press (in case of duplicate input events)
            if (Time.unscaledTime - lastButtonPressTime > 0.1f)
            {
                lastButtonPressTime = Time.unscaledTime;
                hasFiredThisPress = false;
                
                // Call Fire directly only once per button press
                Fire();
                
                // Debug
                Debug.Log("==== BUTTON PRESSED AT " + Time.unscaledTime);
            }
            else
            {
                Debug.Log("Ignored duplicate button press at " + Time.unscaledTime);
            }
        }
        
        // If button held down, do nothing
        
        // Button released - reset so we can fire again on next press
        if (Input.GetButtonUp("Fire1"))
        {
            hasFiredThisPress = false;
            // Debug
            Debug.Log("==== BUTTON RELEASED AT " + Time.unscaledTime);
        }
        
        // Handle reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
    }
    
    public override void Fire()
    {
        // Debug info to help identify the issue
        debugFireCount++;
        float timeSinceLastDebug = Time.unscaledTime - lastDebugTime;
        Debug.Log($"[SEMI-AUTO] Fire() called for {debugFireCount} time. Time since last call: {timeSinceLastDebug}s");
        Debug.Log($"[SEMI-AUTO] hasFiredThisPress: {hasFiredThisPress}, Stack: {new System.Diagnostics.StackTrace()}");
        lastDebugTime = Time.unscaledTime;
        
        // CRITICAL CHECK: Only allow one shot per button press
        if (hasFiredThisPress)
        {
            Debug.Log("[SEMI-AUTO] Already fired this press - blocking");
            return;
        }
        
        // Can't fire while hidden
        WeaponFollower follower = GetComponent<WeaponFollower>();
        if (follower != null && follower.IsWeaponHidden())
            return;
        
        // Check conditions
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
        
        // We're actually firing - set the flag
        hasFiredThisPress = true;
        
        // Update state
        lastFireTime = Time.time;
        currentAmmo--;
        
        Debug.Log("SEMI-AUTO WEAPON FIRED - SHOT " + (magazineSize - currentAmmo) + " OF " + magazineSize);
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Fire");
        
        // Play sound
        PlaySound(fireSound);
        
        // Spawn bullet
        SpawnProjectile();
        
        // Create muzzle flash
        CreateMuzzleFlash();
        
        // Auto reload when empty
        if (currentAmmo <= 0)
            Reload();
    }
    
    private void SpawnProjectile()
    {
        if (bulletPrefab != null && muzzlePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
            
            // Set velocity
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = muzzlePoint.right * bulletSpeed;
            
            // Set bullet sprite if available
            if (bulletSprites != null && bulletSprites.Count > 0)
            {
                SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
                if (bulletRenderer != null)
                {
                    int spriteIndex = Random.Range(0, bulletSprites.Count);
                    bulletRenderer.sprite = bulletSprites[spriteIndex];
                }
            }
            
            // Set damage
            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            if (bulletComponent != null)
                bulletComponent.damage = bulletDamage;
            
            // Clean up
            Destroy(bullet, 5f);
        }
    }
    
    private void CreateMuzzleFlash()
    {
        if (muzzleFlashPrefab != null && muzzlePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
            flash.transform.SetParent(muzzlePoint);
            Destroy(flash, 0.05f);
        }
    }
    
    private void PlaySound(AudioClip sound)
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
        
        // Play animation
        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Reload");
        
        // Play sound
        PlaySound(reloadSound);
        
        // Complete reload after delay
        Invoke("CompleteReload", reloadTime);
    }
    
    private void CompleteReload()
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