using UnityEngine;
using System.Collections;
using Weapons;

namespace Weapons
{
    /// <summary>
    /// Simplified shotgun weapon that fires multiple projectiles with configurable spread
    /// </summary>
    public class ShotgunWeapon : BaseWeapon
    {
        [Header("Pellet Settings")]
        public int pelletCount = 12;
        public float pelletSpeed = 15f;
        public float spreadAngle = 45f;
        public Color pelletColor = Color.yellow;
        
        public float pelletSize = 0.2f;
        [Tooltip("When enabled, pellets follow random paths within the spread angle. When disabled, pellets are evenly distributed.")]
        [SerializeField] public bool randomPelletPaths = true;
        
        [Tooltip("Optional custom prefab for pellets. If assigned, uses this instead of generating basic circular pellets")]
        public GameObject pelletPrefab;
        [Tooltip("Whether to apply color/size settings to prefab pellets")]
        public bool applySettingsToPrefab = true;
        
        [Header("Firing Settings")]
        public FiringType firingType = FiringType.SemiAuto;
        [Tooltip("Time between shots in semi-auto and auto modes")]
        public float shotgunFireRate = 4f; // Renamed to avoid conflict with BaseWeapon.fireRate
        
        [Header("Burst Settings")]
        [Tooltip("Number of shots fired in each burst")]
        public int burstCount = 3;
        
        [Tooltip("How tightly packed shots are within a burst (lower = tighter)")]
        public float burstDensity = 0.1f; // Renamed from burstDelay for clarity
        
        [Tooltip("Time to wait between consecutive bursts")]
        public float timeBetweenBursts = 0.5f; // New parameter replacing fireRate for burst mode
        
        [Tooltip("When enabled, holding the fire button will continuously fire bursts")]
        [SerializeField] public bool continuousBurst = true;
        
        [Header("Pump Action Settings")]
        [Tooltip("Time required to pump the shotgun between shots")]
        public float pumpDelay = 0.5f;
        
        // Screen Shake settings
        [SerializeField] public bool enableScreenShake = true;
        [SerializeField] public float screenShakeIntensity = 0.5f;
        [SerializeField] public float screenShakeDuration = 0.2f;
        [SerializeField] public float screenShakeFrequency = 25f;
        
        // Visual Feedback
        [SerializeField] public bool enableMuzzleFlash = true;
        [SerializeField] public GameObject muzzleFlashPrefab;
        [SerializeField] public float muzzleFlashDuration = 0.1f;
        [SerializeField] public float muzzleFlashScale = 1f;
        
        // Physical Feedback
        [SerializeField] public float recoilForce = 2f;
        [SerializeField] public float recoilDuration = 0.2f;
        
        // Internal state
        private float shotgunNextFireTime = 0f;
        private bool isPumping = false;
        private Coroutine firingCoroutine;
        private bool isBurstFiring = false;
        private int currentPelletIndex = 0;
        
        // Cache the metadata component
        private WeaponMetadata metadata;
        
        // Public property to expose the protected maxReserveAmmo
        public int MaxReserveAmmo
        {
            get { return maxReserveAmmo; }
            set { maxReserveAmmo = value; }
        }

        // Public property to expose the protected reserveAmmo
        public int ReserveAmmo
        {
            get { return reserveAmmo; }
            set { reserveAmmo = value; }
        }

        // Public method to fill ammo that won't require reflection
        public void FillAmmo(bool fillMagazine = true, bool fillReserve = false)
        {
            if (fillMagazine)
                CurrentAmmo = magazineSize;
            
            if (fillReserve)
                reserveAmmo = maxReserveAmmo;
        }
        
        public enum FiringType
        {
            SemiAuto,   // One shot per click
            Auto,       // Hold to continue firing
            Burst,      // Fire multiple shots in sequence
            PumpAction  // Must "pump" between shots
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            // Cache metadata component
            metadata = GetComponent<WeaponMetadata>();
            
            // Sync values with metadata if present
            if (metadata != null)
            {
                SyncWithMetadata();
            }
            
            // Additional shotgun-specific initialization
            // If no fire point assigned, look for one
            if (firePoint == null)
            {
                // Check if there's a child named "FirePoint"
                Transform foundFirePoint = transform.Find("FirePoint");
                
                if (foundFirePoint != null)
                {
                    firePoint = foundFirePoint;
                    Debug.Log("Found existing FirePoint");
                }
                else
                {
                    // Create a new FirePoint child
                    GameObject newFirePoint = new GameObject("FirePoint");
                    newFirePoint.transform.SetParent(transform);
                    newFirePoint.transform.localPosition = new Vector3(0.5f, 0, 0);
                    newFirePoint.transform.localRotation = Quaternion.identity;
                    firePoint = newFirePoint.transform;
                    Debug.Log("Created new FirePoint");
                }
            }
        }
        
        protected override void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            // Get the weapon aiming component to check if we're equipped
            WeaponAiming aiming = GetComponent<WeaponAiming>();
            if (aiming != null && !aiming.isEquipped)
            {
                // Don't process input if weapon isn't equipped
                return;
            }

            // Check for reload input first
            if ((Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Backspace)) && !isReloading && !IsMagazineFull && reserveAmmo > 0)
            {
                StartReload();
                return;
            }
            
            // Skip firing if reloading
            if (isReloading)
                return;
                
            // Check if we can fire (uses BaseWeapon's CanFire property)
            bool canFire = CanFire && Time.time >= shotgunNextFireTime && !isPumping;
            
            switch (firingType)
            {
                case FiringType.SemiAuto:
                    if (canFire && Input.GetMouseButtonDown(0))
                    {
                        FireShotgun();
                        shotgunNextFireTime = Time.time + (1f / shotgunFireRate);
                    }
                    break;
                
                case FiringType.Auto:
                    if (canFire && Input.GetMouseButton(0))
                    {
                        FireShotgun();
                        shotgunNextFireTime = Time.time + (1f / shotgunFireRate);
                    }
                    break;
                
                case FiringType.Burst:
                    // Only allow starting a burst if we're not already in one
                    if (!isBurstFiring && canFire && Input.GetMouseButtonDown(0))
                    {
                        if (firingCoroutine != null)
                            StopCoroutine(firingCoroutine);
                        firingCoroutine = StartCoroutine(FireBurst());
                    }
                    // Continuous burst firing when button is held (also respects the "not already firing" rule)
                    else if (continuousBurst && !isBurstFiring && canFire && Input.GetMouseButton(0))
                    {
                        if (firingCoroutine != null)
                            StopCoroutine(firingCoroutine);
                        firingCoroutine = StartCoroutine(FireBurst());
                    }
                    break;
                
                case FiringType.PumpAction:
                    if (canFire && Input.GetMouseButtonDown(0))
                    {
                        FireShotgun();
                        isPumping = true;
                        StartCoroutine(PumpAction());
                    }
                    
                    // Manual pump action with R key
                    if (isPumping && Input.GetKeyDown(KeyCode.R))
                    {
                        StopAllCoroutines();
                        Debug.Log("Manual pump action");
                        StartCoroutine(QuickPumpAction());
                    }
                    break;
            }
        }
        
        // Modify FireShotgun to check ammo and use BaseWeapon's methods
        public void FireShotgun()
        {
            // Check if we can fire (has ammo and not reloading)
            if (!CanFire)
            {
                // Play empty click sound if out of ammo
                if (CurrentAmmo <= 0 && audioSource != null && shotgunEmptySound != null)
                {
                    audioSource.PlayOneShot(shotgunEmptySound, soundVolume);
                    onOutOfAmmo?.Invoke();
                }
                return;
            }

            // Reset the pellet index when starting a new shot
            currentPelletIndex = 0;

            for (int i = 0; i < pelletCount; i++)
            {
                FirePellet(i); // Pass the pellet index
            }

            // Play SFX
            if (audioSource != null && shotgunFireSound != null)
            {
                if (randomizePitch)
                    audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
                else
                    audioSource.pitch = 1.0f;

                audioSource.clip = shotgunFireSound;
                audioSource.Play();
            }
            
            // Apply feedback effects
            ApplyFeedbackEffects();

            // Use BaseWeapon's ammo system
            CurrentAmmo--;
            shotgunNextFireTime = Time.time + (1f / shotgunFireRate);
            
            // Trigger base weapon events
            onFire?.Invoke();
            onAmmoChanged?.Invoke(CurrentAmmo, magazineSize);
            
            // Auto-reload check
            if (autoReloadWhenEmpty && CurrentAmmo <= 0 && ReserveAmmo > 0 && !isReloading)
            {
                StartReload();
            }
        }
        
        // Update FirePellet to take the pellet index
        private void FirePellet(int pelletIndex)
        {
            // Get the spawn position
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

            // Calculate direction based on random or fixed paths
            float angle;
            
            if (randomPelletPaths)
            {
                // Truly random angle within the spread
                angle = Random.Range(-spreadAngle/2, spreadAngle/2);
            }
            else
            {
                // For fixed paths, distribute pellets evenly across the spread angle
                if (pelletCount <= 1)
                {
                    angle = 0; // Single pellet fires straight
                }
                else
                {
                    // Distribute evenly across the spread - THIS IS THE KEY PART
                    float step = spreadAngle / (pelletCount - 1);
                    angle = -spreadAngle/2 + (pelletIndex * step);
                }
            }
            
            Vector3 direction;
            
            // Use firePoint direction if available, otherwise use transform.right
            if (firePoint != null)
                direction = Quaternion.Euler(0, 0, angle) * firePoint.right;
            else
                direction = Quaternion.Euler(0, 0, angle) * transform.right;
            
            GameObject pellet;
            
            // Use prefab if available, otherwise create basic pellet
            if (pelletPrefab != null)
            {
                // Instantiate from prefab
                pellet = Instantiate(pelletPrefab, spawnPos, Quaternion.identity);
                
                // Apply settings if enabled
                if (applySettingsToPrefab)
                {
                    // Apply color if there's a sprite renderer
                    SpriteRenderer renderer = pellet.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = pelletColor;
                        renderer.sortingOrder = 100; // Ensure visibility
                    }
                    
                    // Apply scale
                    pellet.transform.localScale = Vector3.one * pelletSize;
                }
            }
            else
            {
                // Create basic pellet GameObject (existing code)
                pellet = new GameObject("Pellet");
                pellet.transform.position = spawnPos;
                
                // Add a visible sprite
                SpriteRenderer renderer = pellet.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateCircleSprite();
                renderer.color = pelletColor;
                renderer.sortingOrder = 100; // Make sure it's visible on top
                pellet.transform.localScale = Vector3.one * pelletSize;
            }
            
            // Check for and add required components
            
            // Add collider if needed
            if (pellet.GetComponent<Collider2D>() == null)
            {
                CircleCollider2D collider = pellet.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
            }
            
            // Add rigidbody if needed
            Rigidbody2D rb = pellet.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = pellet.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
            }
            
            // Set velocity
            rb.linearVelocity = direction * pelletSpeed;
            
            // Add destruction script with range limit and shorter lifetime
            PelletBehavior behavior = pellet.GetComponent<PelletBehavior>() ?? pellet.AddComponent<PelletBehavior>();
            behavior.lifetime = 0.5f; // Reduced from 5f to 0.5f for quicker cleanup
            behavior.maxRange = shotgunMaxRange;
            
            // Remove the long Destroy call since PelletBehavior handles destruction
            // Destroy(pellet, 10f); // Remove this line
        }
        
        // Simple circle sprite creation
        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            Texture2D texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];
            
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 16;
                    float dy = y - 16;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 16)
                        colors[y * 32 + x] = Color.white;
                    else
                        colors[y * 32 + x] = Color.clear;
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        // Burst fire coroutine
        private IEnumerator FireBurst()
        {
            isBurstFiring = true;
            int shotsFired = 0;
            
            while (shotsFired < burstCount)
            {
                FireShotgun();
                shotsFired++;
                
                if (shotsFired < burstCount)
                    yield return new WaitForSeconds(burstDensity); // Using burstDensity instead of burstDelay
            }
            
            // Additional cooldown after burst using timeBetweenBursts instead of fireRate
            shotgunNextFireTime = Time.time + timeBetweenBursts;
            isBurstFiring = false;
        }
        
        // Pump action coroutine
        private IEnumerator PumpAction()
        {
            Debug.Log("Pump action started");
            
            // Wait for the pump time (using the configured pumpDelay)
            yield return new WaitForSeconds(pumpDelay);
            
            // Play pump SFX
            if (audioSource != null && pumpSound != null)
            {
                if (randomizePitch)
                    audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
                else
                    audioSource.pitch = 1.0f;
                
                audioSource.PlayOneShot(pumpSound, soundVolume * 0.8f); // Slightly lower volume for pump sound
            }
            
            Debug.Log("Pump complete");
            isPumping = false;
        }
        
        // Quick pump action (for manual pumping)
        private IEnumerator QuickPumpAction()
        {
            yield return new WaitForSeconds(0.2f);
            
            // Play pump SFX
            if (audioSource != null && pumpSound != null)
            {
                if (randomizePitch)
                    audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
                else
                    audioSource.pitch = 1.0f;
                
                audioSource.PlayOneShot(pumpSound, soundVolume * 0.8f); // Slightly lower volume for pump sound
            }
            
            isPumping = false;
        }
        
        // Draw the firepoint in the editor
        private void OnDrawGizmos()
        {
            if (firePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(firePoint.position, 0.05f);
                Gizmos.DrawRay(firePoint.position, firePoint.right * 0.5f);
            }
        }
        
        // Helper method to create a FirePoint in the editor
        public void CreateFirePoint()
        {
            GameObject newFirePoint = new GameObject("FirePoint");
            newFirePoint.transform.SetParent(transform);
            newFirePoint.transform.localPosition = new Vector3(0.5f, 0, 0);
            newFirePoint.transform.localRotation = Quaternion.identity;
            firePoint = newFirePoint.transform;
        }
        
        // Add this to your fire method where you want the feedback to happen
        private void ApplyFeedbackEffects()
        {
            // Apply screen shake
            if (enableScreenShake && Camera.main != null)
            {
                StartCoroutine(BasicScreenShake());
            }
            
            // Spawn muzzle flash
            if (enableMuzzleFlash && muzzleFlashPrefab != null && firePoint != null)
            {
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                flash.transform.localScale *= muzzleFlashScale;
                Destroy(flash, muzzleFlashDuration);
            }
            
            // Apply recoil
            if (recoilForce > 0)
            {
                StartCoroutine(ApplyRecoil());
            }
        }
        
        // Basic screen shake implementation
       /* private IEnumerator BasicScreenShake()
        {
            Vector3 originalPos = Camera.main.transform.localPosition;
            float elapsed = 0f;
            
            while (elapsed < screenShakeDuration)
            {
                float x = Random.Range(-1f, 1f) * screenShakeIntensity;
                float y = Random.Range(-1f, 1f) * screenShakeIntensity;
                
                Camera.main.transform.localPosition = new Vector3(x, y, originalPos.z);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Camera.main.transform.localPosition = originalPos;
        } */

            private IEnumerator BasicScreenShake()
        {
            Vector3 originalPos = Camera.main.transform.position;
            float elapsed = 0f;

            while (elapsed < screenShakeDuration)
            {
                float x = Random.Range(-1f, 1f) * screenShakeIntensity;
                float y = Random.Range(-1f, 1f) * screenShakeIntensity;

                Camera.main.transform.position = new Vector3(
                    originalPos.x + x,
                    originalPos.y + y,
                    originalPos.z
                );

                elapsed += Time.deltaTime;
                yield return null;
            }

            Camera.main.transform.position = originalPos;
        }

        
        // Recoil effect
        private IEnumerator ApplyRecoil()
        {
            Vector3 originalPos = transform.localPosition;
            Vector3 recoilPos = originalPos - new Vector3(recoilForce, 0, 0);
            
            // Quick kick back
            float elapsed = 0f;
            float kickDuration = recoilDuration * 0.3f;
            
            while (elapsed < kickDuration)
            {
                transform.localPosition = Vector3.Lerp(originalPos, recoilPos, elapsed / kickDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Slower return
            elapsed = 0f;
            float returnDuration = recoilDuration * 0.7f;
            
            while (elapsed < returnDuration)
            {
                transform.localPosition = Vector3.Lerp(recoilPos, originalPos, elapsed / returnDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localPosition = originalPos;
        }
        
        // Override Shoot method from BaseWeapon to use our shotgun firing instead
        public override void Shoot()
        {
            FireShotgun(); // Just call our shotgun firing method
        }

        // Public method to empty the magazine
        public void EmptyMagazine()
        {
            // We can access the protected setter from within the derived class
            CurrentAmmo = 0;
        }

        // Add this field with the other weapon properties
        [Header("Ammunition Options")]
        [Tooltip("When enabled, weapon will automatically reload when magazine is empty")]
        [SerializeField] public bool autoReloadWhenEmpty = true;

        // Add this with your other properties
        public bool IsReloading
        {
            get { return isReloading; }
        }

        // Add these fields to your ShotgunWeapon class
        [Header("Sound Settings")]
        [Tooltip("Sound played when firing the shotgun")]
        public AudioClip shotgunFireSound;
        [Tooltip("Sound played when reloading")]
        public AudioClip shotgunReloadSound;
        [Tooltip("Sound played when trying to fire with no ammo")]
        public AudioClip shotgunEmptySound;
        [Tooltip("Sound played when pumping the shotgun (pump action only)")]
        public AudioClip pumpSound;
        [Tooltip("Master volume multiplier for all weapon sounds")]
        [Range(0f, 1f)]
        public float soundVolume = 1.0f;
        [Tooltip("Randomize pitch slightly for more natural sound")]
        public bool randomizePitch = true;
        [Tooltip("Range for random pitch variation")]
        [Range(0f, 0.3f)]
        public float pitchVariation = 0.1f;

        // Property to get/set weapon name 
        public string WeaponName 
        {
            get 
            { 
                if (metadata != null)
                    return metadata.weaponName;
                return weaponName; // Fallback to BaseWeapon's property
            }
            set
            {
                // Update the weapon component name
                weaponName = value;
                
                // Update the metadata if available
                if (metadata != null)
                    metadata.weaponName = value;
                
                // Update the GameObject name
                gameObject.name = value;
            }
        }

        // Property to get/set weapon description
        public string WeaponDescription
        {
            get 
            { 
                if (metadata != null)
                    return metadata.description;
                return description; // Fallback to BaseWeapon's property
            }
            set
            {
                // Update the weapon component description
                description = value;
                
                // Update the metadata if available
                if (metadata != null)
                    metadata.description = value;
            }
        }

        // Property to get/set weapon rarity
        public string WeaponRarity
        {
            get 
            { 
                if (metadata != null)
                    return metadata.rarity;
                return rarity; // Fallback to BaseWeapon's property
            }
            set
            {
                // Update the weapon component rarity
                rarity = value;
                
                // Update the metadata if available
                if (metadata != null)
                    metadata.rarity = value;
            }
        }

        // Method to sync data with the metadata component
        public void SyncWithMetadata()
        {
            if (metadata == null)
                return;
                
            // Sync from metadata to weapon properties
            weaponName = metadata.weaponName;
            description = metadata.description;
            rarity = metadata.rarity;
            damage = metadata.damage;
            magazineSize = metadata.magazineSize;
            
            // Update the GameObject name
            gameObject.name = weaponName;
        }

        // Method to sync data from weapon to metadata
        public void UpdateMetadata()
        {
            if (metadata == null)
                return;
            
            // Sync from weapon properties to metadata
            metadata.weaponName = weaponName;
            metadata.description = description;
            metadata.rarity = rarity;
            metadata.damage = damage;
            metadata.magazineSize = magazineSize;
            metadata.fireRate = shotgunFireRate;
        }

        // Add this to reset weapon state when disabled
        private void OnDisable()
        {
            // Stop any active coroutines
            if (firingCoroutine != null)
            {
                StopCoroutine(firingCoroutine);
                firingCoroutine = null;
            }
            
            // Reset state variables
            isBurstFiring = false;
            isPumping = false;
            shotgunNextFireTime = 0f;
        }

        // Add this to ensure the weapon is ready when re-equipped
        private void OnEnable()
        {
            // Reset firing times to prevent cooldown carrying over from previous equipped state
            shotgunNextFireTime = 0f;
            nextFireTime = 0f;
            
            // Reset state 
            isBurstFiring = false;
            isPumping = false;
            
            // Ensure weapon knows it's equipped (should be set by the inventory system)
            WeaponAiming aiming = GetComponent<WeaponAiming>();
            if (aiming != null)
            {
                aiming.isEquipped = true;
            }
        }

        [Header("Range Settings")]
        [SerializeField] private float shotgunMaxRange = 5f;
        [SerializeField] private float shotgunDamageDropoffStart = 2f;
        [SerializeField] private float shotgunMinDamageMultiplier = 0.1f;

        // Properties that sync with base class values
        private float ShotgunMaxRange
        {
            get { return shotgunMaxRange; }
            set 
            { 
                shotgunMaxRange = Mathf.Clamp(value, 3f, 10f);
                maxRange = shotgunMaxRange;
            }
        }

        private float ShotgunDamageDropoffStart
        {
            get { return shotgunDamageDropoffStart; }
            set 
            { 
                shotgunDamageDropoffStart = Mathf.Clamp(value, 1f, maxRange * 0.5f);
                damageDropoffStart = shotgunDamageDropoffStart;
            }
        }

        private float ShotgunMinDamageMultiplier
        {
            get { return shotgunMinDamageMultiplier; }
            set 
            { 
                shotgunMinDamageMultiplier = Mathf.Clamp(value, 0.05f, 0.3f);
                minDamageMultiplier = shotgunMinDamageMultiplier;
            }
        }

        protected virtual void Start()
        {
            // Set very short range values
            ShotgunMaxRange = 5f;            // Very short maximum range
            ShotgunDamageDropoffStart = 2f;  // Start damage falloff very close
            ShotgunMinDamageMultiplier = 0.1f; // Sharp damage falloff
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Update our properties which will in turn update the base class fields
            ShotgunMaxRange = shotgunMaxRange;
            ShotgunDamageDropoffStart = shotgunDamageDropoffStart;
            ShotgunMinDamageMultiplier = shotgunMinDamageMultiplier;
        }
    } // Close ShotgunWeapon class
    
    // Update the PelletBehavior class
    public class PelletBehavior : MonoBehaviour
    {
        public float lifetime = 0.5f;
        public float maxRange = 5f;
        private Vector3 startPosition;
        private bool isDestroying = false; // Add flag to prevent multiple destroy calls
        
        void Start()
        {
            startPosition = transform.position;
            Destroy(gameObject, lifetime);
        }

        void Update()
        {
            if (isDestroying) return; // Skip if already being destroyed
            
            float distanceTraveled = Vector3.Distance(transform.position, startPosition);
            if (distanceTraveled >= maxRange)
            {
                DestroyPellet();
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (isDestroying) return; // Skip if already being destroyed
            
            // Ignore collisions with other pellets
            if (other.gameObject.name.Contains("Pellet"))
                return;
            
            DestroyPellet();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDestroying) return; // Skip if already being destroyed
            
            // Ignore collisions with other pellets
            if (collision.gameObject.name.Contains("Pellet"))
                return;
            
            DestroyPellet();
        }

        void DestroyPellet()
        {
            if (isDestroying) return; // Prevent multiple destroy calls
            
            isDestroying = true;
            
            // Immediately disable physics to prevent further collisions
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }
            
            // Disable collider
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            // Quick fade out
            StartCoroutine(QuickFadeOut());
        }

        IEnumerator QuickFadeOut()
        {
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // Quick fade over 0.1 seconds
                float elapsed = 0f;
                float duration = 0.1f;
                Color startColor = renderer.color;
                Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    renderer.color = Color.Lerp(startColor, endColor, elapsed / duration);
                    yield return null;
                }
            }
            
            Destroy(gameObject);
        }
    }

    // Example UI script that could subscribe to the ammo events
    public class AmmoDisplay : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI ammoText;
        
        void Start()
        {
            // Update to use FindFirstObjectByType instead of FindObjectOfType
            BaseWeapon weapon = Object.FindFirstObjectByType<BaseWeapon>();
            if (weapon != null)
            {
                weapon.onAmmoChanged.AddListener(UpdateAmmoDisplay);
            }
        }
        
        public void UpdateAmmoDisplay(int current, int max)
        {
            if (ammoText != null)
                ammoText.text = $"{current} / {max}";
        }
    }
}