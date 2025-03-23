using UnityEngine;
using System.Collections;
using Weapons;

namespace Weapons 
{
    public class EnergyWeapon : BaseWeapon 
    {
        [Header("Energy Settings")]
        [Tooltip("Maximum energy capacity")]
        public float maxEnergy = 100f;
        
        [Tooltip("Current energy level")]
        [SerializeField] private float currentEnergy;
        
        [Tooltip("Energy regeneration rate per second")]
        public float energyRegenRate = 10f;
        
        [Tooltip("Energy cost per shot")]
        public float energyCostPerShot = 10f;
        
        [Header("Firing Settings")]
        [Tooltip("Type of energy weapon")]
        public EnergyType energyType = EnergyType.Blaster;
        
        [Tooltip("Color of the energy projectile/beam")]
        public Color energyColor = Color.cyan;
        
        [Header("Blaster Settings")]
        [Tooltip("Projectile speed for blaster shots")]
        public float energyProjectileSpeed = 30f;
        
        [Tooltip("Size of blaster projectiles")]
        public float projectileSize = 0.3f;
        
        [Header("Beam Settings")]
        [Tooltip("Maximum distance the beam can reach")]
        public float beamRange = 20f;
        
        [Tooltip("Beam width")]
        public float beamWidth = 0.2f;
        
        [Tooltip("Energy drain per second while firing beam")]
        public float beamEnergyCostPerSecond = 20f;
        
        [Header("Charge Settings")]
        [Tooltip("How long to fully charge")]
        public float maxEnergyChargeTime = 2f;
        
        [Tooltip("Damage multiplier at full charge")]
        public float fullChargeDamageMultiplier = 3f;
        
        [Tooltip("Energy cost for a fully charged shot")]
        public float fullChargeEnergyCost = 40f;
        
        [Header("Visual Effects")]
        public GameObject muzzleFlashPrefab;
        public GameObject chargeEffectPrefab;
        
        [Header("Beam Visual Options")]
        [Tooltip("Should the weapon play a charge animation before firing?")]
        public bool useChargeEffect = true;
        
        [Tooltip("How long to charge before firing")]
        public float chargeTime = 0.5f;
        
        [Tooltip("Hide the beam color but keep effects visible")]
        public bool hideBeamColor = false;
        
        [Tooltip("Number of segments for the beam (higher = smoother)")]
        [Range(10, 40)]
        public int beamSegments = 24;
        
        [Tooltip("How quickly the beam catches up to your aim direction (lower = more lag)")]
        [Range(1f, 15f)]
        public float beamFollowSpeed = 10f;
        
        [Tooltip("How much the further segments lag behind (higher = more whip effect)")]
        [Range(0.1f, 3f)]
        public float tipLagMultiplier = 1.2f;
        
        [Header("Beam Physics Options")]
        [Tooltip("How stiff the rope is (lower = more floppy)")]
        [Range(0.1f, 1.0f)]
        public float ropeStiffness = 0.7f;
        
        [Tooltip("Number of simulation iterations (higher = more stable)")]
        [Range(5, 50)]
        public int simulationIterations = 30;
        
        [Tooltip("Distance between points in the beam")]
        [Range(0.01f, 0.3f)]
        public float segmentDistance = 0.1f;
        
        [Header("Beam Advanced Options")]
        [Tooltip("Length of straight section coming out of the gun (as percentage of total beam)")]
        [Range(0.05f, 0.5f)]
        public float straightSectionLength = 0.12f;
        
        [Tooltip("How many points to use for blending between straight and curved sections")]
        [Range(2, 8)]
        public int transitionPointCount = 4;
        
        [Tooltip("Amplitude of the wave motion")]
        [Range(0.2f, 3f)]
        public float waveAmplitude = 0.8f;
        
        [Tooltip("Speed of the wave motion")]
        [Range(0.1f, 5f)]
        public float waveFrequency = 1.2f;
        
        [Tooltip("Number of smoothing passes (higher = smoother curves)")]
        [Range(1, 5)]
        public int smoothingPasses = 2;
        
        [Tooltip("Smoothing factor (higher = more smoothing)")]
        [Range(0.05f, 0.5f)]
        public float smoothingStrength = 0.25f;
        
        // Internal state
        private bool isFiringBeam = false;
        private bool isCharging = false;
        private float chargeStartTime;
        private float chargeLevel = 0f;
        private LineRenderer beamLine;
        private GameObject chargeEffect;
        
        // Properties
        public float CurrentEnergy 
        {
            get => currentEnergy;
            set => currentEnergy = Mathf.Clamp(value, 0, maxEnergy);
        }
        
        public bool IsFiring => isFiringBeam;
        
        public enum EnergyType 
        {
            Blaster,    // Fires energy projectiles
            Beam,       // Continuous energy beam
            Charge      // Charged energy blasts
        }
        
        protected override void Awake() 
        {
            base.Awake();
            CurrentEnergy = maxEnergy;
            
            // If this is a beam weapon, add a line renderer
            if (energyType == EnergyType.Beam) 
            {
                SetupBeamRenderer();
            }
        }
        
        private void SetupBeamRenderer() 
        {
            if (beamLine == null) 
            {
                beamLine = gameObject.AddComponent<LineRenderer>();
                beamLine.positionCount = 2;
                beamLine.startWidth = beamWidth;
                beamLine.endWidth = beamWidth / 2f;
                beamLine.material = new Material(Shader.Find("Sprites/Default"));
                
                // Set colors based on hideBeamColor setting
                if (hideBeamColor)
                {
                    beamLine.startColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0f);
                    beamLine.endColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0f);
                }
                else
                {
                    beamLine.startColor = energyColor;
                    beamLine.endColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0.5f);
                }
                
                beamLine.enabled = false;
            }
        }
        
        protected override void Update() 
        {
            HandleInput();
            RegenerateEnergy();
        }
        
        private void HandleInput() 
        {
            WeaponAiming aiming = GetComponent<WeaponAiming>();
            if (aiming != null && !aiming.isEquipped) 
            {
                return;
            }
            
            // Check for reload input (full energy recharge in this case)
            if (Input.GetKeyDown(KeyCode.R) && CurrentEnergy < maxEnergy) 
            {
                StartCoroutine(RechargeWeapon());
                return;
            }
            
            switch (energyType) 
            {
                case EnergyType.Blaster:
                    if (Input.GetMouseButtonDown(0) && CanFireEnergy()) 
                    {
                        FireBlaster();
                    }
                    break;
                    
                case EnergyType.Beam:
                    if (Input.GetMouseButtonDown(0) && CanFireEnergy()) 
                    {
                        StartBeam();
                    }
                    else if (Input.GetMouseButtonUp(0)) 
                    {
                        StopBeam();
                    }
                    
                    if (isFiringBeam) 
                    {
                        UpdateBeam();
                    }
                    break;
                    
                case EnergyType.Charge:
                    if (Input.GetMouseButtonDown(0) && CanFireEnergy()) 
                    {
                        StartCharging();
                    }
                    else if (Input.GetMouseButtonUp(0) && isCharging) 
                    {
                        FireChargedShot();
                    }
                    
                    if (isCharging) 
                    {
                        UpdateCharging();
                    }
                    break;
            }
        }
        
        private bool CanFireEnergy() 
        {
            return CurrentEnergy >= energyCostPerShot && !isReloading && Time.time >= nextFireTime;
        }
        
        private void RegenerateEnergy() 
        {
            if (!isReloading && CurrentEnergy < maxEnergy && !isFiringBeam) 
            {
                CurrentEnergy += energyRegenRate * Time.deltaTime;
                // No need to clamp as the property already does this
            }
        }
        
        private IEnumerator RechargeWeapon() 
        {
            isReloading = true;
            onReloadStart?.Invoke();
            
            // Play recharge sound
            if (audioSource != null && reloadSound != null) 
            {
                audioSource.PlayOneShot(reloadSound);
            }
            
            float rechargeTime = reloadTime * (1 - CurrentEnergy / maxEnergy);
            float startEnergy = CurrentEnergy;
            float elapsed = 0f;
            
            while (elapsed < rechargeTime) 
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rechargeTime;
                CurrentEnergy = Mathf.Lerp(startEnergy, maxEnergy, t);
                
                yield return null;
            }
            
            CurrentEnergy = maxEnergy;
            isReloading = false;
            onReloadComplete?.Invoke();
        }
        
        #region Blaster Methods
        private void FireBlaster() 
        {
            if (CurrentEnergy < energyCostPerShot) 
                return;
                
            // Consume energy
            CurrentEnergy -= energyCostPerShot;
            
            // Create projectile
            if (firePoint != null) 
            {
                GameObject projectile = CreateEnergyProjectile();
                Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                if (rb != null) 
                {
                    rb.linearVelocity = firePoint.right * energyProjectileSpeed;
                }
                
                // Set projectile properties
                EnergyProjectile energyProj = projectile.GetComponent<EnergyProjectile>();
                if (energyProj != null) 
                {
                    energyProj.damage = damage;
                    energyProj.damageType = "Energy";
                    energyProj.range = maxRange;
                    energyProj.owner = this;
                }
            }
            
            // Play effects
            PlayMuzzleEffect();
            
            // Set cooldown
            nextFireTime = Time.time + (1f / fireRate);
            
            // Invoke event
            onFire?.Invoke();
        }
        
        private GameObject CreateEnergyProjectile() 
        {
            // Create a basic energy projectile
            GameObject projectile = new GameObject("EnergyProjectile");
            projectile.transform.position = firePoint.position;
            projectile.transform.rotation = firePoint.rotation;
            
            // Add visual
            SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateEnergySprite();
            renderer.color = energyColor;
            renderer.sortingOrder = 10;
            
            // Add physics
            CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = projectileSize / 2f;
            
            Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            
            // Add behavior
            EnergyProjectile behavior = projectile.AddComponent<EnergyProjectile>();
            
            return projectile;
        }
        
        private void PlayMuzzleEffect() 
        {
            // Play sound
            if (audioSource != null && shootSound != null) 
            {
                audioSource.PlayOneShot(shootSound);
            }
            
            // Spawn muzzle effect
            if (muzzleFlashPrefab != null && firePoint != null) 
            {
                GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                muzzleFlash.transform.SetParent(firePoint);
                Destroy(muzzleFlash, 0.1f);
            }
        }
        #endregion
        
        #region Beam Methods
        public void StartBeam() 
        {
            if (CurrentEnergy < energyCostPerShot) 
                return;
                
            isFiringBeam = true;
            
            // Setup beam renderer if not already done
            if (beamLine == null) 
            {
                SetupBeamRenderer();
            }
            
            beamLine.enabled = true;
            
            // Play sound
            if (audioSource != null && shootSound != null) 
            {
                audioSource.PlayOneShot(shootSound);
            }
            
            // Invoke event
            onFire?.Invoke();
        }
        
        private void UpdateBeam() 
        {
            // Check if we have enough energy
            if (CurrentEnergy <= 0) 
            {
                StopBeam();
                return;
            }
            
            // Consume energy
            CurrentEnergy -= beamEnergyCostPerSecond * Time.deltaTime;
            
            // Calculate beam positions
            if (firePoint != null) 
            {
                Vector3 start = firePoint.position;
                Vector3 direction = firePoint.right;
                
                // Use raycast to find endpoint
                RaycastHit2D hit = Physics2D.Raycast(start, direction, beamRange);
                Vector3 end;
                
                if (hit.collider != null) 
                {
                    end = hit.point;
                    
                    // Handle damage to hit object
                    float damageAmount = damage * Time.deltaTime;
                    // Here you would apply damage to the hit object
                    Debug.Log($"Beam hit: {hit.collider.name}, damage: {damageAmount}");
                }
                else 
                {
                    end = start + direction * beamRange;
                }
                
                // Update beam positions
                beamLine.SetPosition(0, start);
                beamLine.SetPosition(1, end);
            }
        }
        
        public void StopBeam() 
        {
            isFiringBeam = false;
            
            if (beamLine != null) 
            {
                beamLine.enabled = false;
            }
        }
        #endregion
        
        #region Charge Methods
        private void StartCharging() 
        {
            if (CurrentEnergy < energyCostPerShot) 
                return;
                
            isCharging = true;
            chargeStartTime = Time.time;
            chargeLevel = 0f;
            
            // Play charge start sound
            if (audioSource != null && shootSound != null) 
            {
                audioSource.PlayOneShot(shootSound, 0.5f);
            }
            
            // Create charge effect
            if (chargeEffectPrefab != null && firePoint != null) 
            {
                chargeEffect = Instantiate(chargeEffectPrefab, firePoint.position, firePoint.rotation);
                chargeEffect.transform.SetParent(firePoint);
            }
        }
        
        private void UpdateCharging() 
        {
            float elapsedTime = Time.time - chargeStartTime;
            chargeLevel = Mathf.Clamp01(elapsedTime / maxEnergyChargeTime);
            
            // Update charge effect size/intensity
            if (chargeEffect != null) 
            {
                chargeEffect.transform.localScale = Vector3.one * (1f + chargeLevel);
            }
            
            // If fully charged, you might want to play a "ready" sound or effect
            if (chargeLevel >= 0.99f && !audioSource.isPlaying) 
            {
                audioSource.PlayOneShot(shootSound, 0.3f);
            }
        }
        
        private void FireChargedShot() 
        {
            if (!isCharging) 
                return;
                
            isCharging = false;
            
            // Calculate energy cost based on charge level
            float cost = Mathf.Lerp(energyCostPerShot, fullChargeEnergyCost, chargeLevel);
            
            // Check if we have enough energy
            if (CurrentEnergy < cost) 
            {
                if (chargeEffect != null) 
                {
                    Destroy(chargeEffect);
                }
                return;
            }
            
            // Consume energy
            CurrentEnergy -= cost;
            
            // Calculate damage multiplier
            float damageMultiplier = Mathf.Lerp(1f, fullChargeDamageMultiplier, chargeLevel);
            
            // Create projectile with scaled size
            if (firePoint != null) 
            {
                GameObject projectile = CreateEnergyProjectile();
                
                // Scale based on charge
                projectile.transform.localScale = Vector3.one * (1f + chargeLevel);
                
                Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
                if (rb != null) 
                {
                    rb.linearVelocity = firePoint.right * energyProjectileSpeed;
                }
                
                // Set projectile properties
                EnergyProjectile energyProj = projectile.GetComponent<EnergyProjectile>();
                if (energyProj != null) 
                {
                    energyProj.damage = damage * damageMultiplier;
                    energyProj.damageType = "Energy";
                    energyProj.range = maxRange;
                    energyProj.owner = this;
                }
            }
            
            // Clean up charge effect
            if (chargeEffect != null) 
            {
                Destroy(chargeEffect);
            }
            
            // Play effects
            PlayMuzzleEffect();
            
            // Set cooldown
            nextFireTime = Time.time + (1f / fireRate);
            
            // Apply camera shake based on charge level
            ApplyCameraShake(shakeIntensity * (1f + chargeLevel), shakeDuration);
            
            // Invoke event
            onFire?.Invoke();
        }
        #endregion
        
        public override void Shoot() 
        {
            switch (energyType) 
            {
                case EnergyType.Blaster:
                    FireBlaster();
                    break;
                case EnergyType.Beam:
                    // Toggle beam
                    if (!isFiringBeam)
                        StartBeam();
                    else
                        StopBeam();
                    break;
                case EnergyType.Charge:
                    // Can't really handle charged shots this way
                    FireBlaster(); // Fallback to basic shot
                    break;
            }
        }
        
        private Sprite CreateEnergySprite() 
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
                    {
                        // Create a glow effect
                        float alpha = 1f - (dist / 16f);
                        colors[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else 
                    {
                        colors[y * 32 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        private void OnDisable() 
        {
            // Clean up any ongoing effects
            if (isFiringBeam) 
            {
                StopBeam();
            }
            
            if (isCharging && chargeEffect != null) 
            {
                Destroy(chargeEffect);
                isCharging = false;
            }
        }
        
        public void ToggleBeamVisibility()
        {
            hideBeamColor = !hideBeamColor;
            
            // Update the beam renderer colors
            if (beamLine != null)
            {
                if (hideBeamColor)
                {
                    beamLine.startColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0f);
                    beamLine.endColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0f);
                }
                else
                {
                    beamLine.startColor = energyColor;
                    beamLine.endColor = new Color(energyColor.r, energyColor.g, energyColor.b, 0.5f);
                }
            }
        }
    }
    
    // Projectile behavior class for energy weapons
    public class EnergyProjectile : MonoBehaviour 
    {
        public float damage = 10f;
        public string damageType = "Energy";
        public float range = 20f;
        public EnergyWeapon owner;
        
        private Vector3 startPosition;
        
        void Start() 
        {
            startPosition = transform.position;
            
            // Auto-destroy after traveling max range
            float lifetime = range / GetComponent<Rigidbody2D>().linearVelocity.magnitude;
            Destroy(gameObject, lifetime);
        }
        
        void Update() 
        {
            // Check distance traveled
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            if (distanceTraveled > range) 
            {
                Destroy(gameObject);
            }
        }
        
        void OnTriggerEnter2D(Collider2D other) 
        {
            // Skip collisions with own owner
            if (owner != null && other.gameObject == owner.gameObject) 
            {
                return;
            }
            
            Debug.Log($"Energy hit: {other.name}, damage: {damage}");
            
            // Apply damage to target (would connect to your damage system)
            
            // Spawn hit effect here
            
            // Destroy projectile
            Destroy(gameObject);
        }
    }
} 