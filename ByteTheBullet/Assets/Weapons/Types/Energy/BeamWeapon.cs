using UnityEngine;
using System.Collections;
using Weapons.BeamPhysics;

namespace Weapons
{
    public class BeamWeapon : BaseWeapon
    {
        [Tooltip("Configuration settings for the beam weapon")]
        public BeamWeaponConfig config = new BeamWeaponConfig();
        
        [Header("Component References")]
        [Tooltip("Reference to the BeamWeaponFX component")]
        public BeamWeaponFX beamFX;
        
        [Tooltip("Current energy level")]
        [SerializeField] private float currentEnergy;
        
        // Private state
        private bool isFiringBeam = false;
        private bool isCharging = false;
        private Coroutine chargeCoroutine;
        private float chargeStartTime;
        private bool isChargedShot = false;
        private float currentCharge = 0f;
        
        // Components
        private BeamWeaponEnergy energyManager;
        private IBeamPhysics beamPhysics;
        
        // Properties
        public bool IsFiring => isFiringBeam;
        
        public float CurrentEnergy
        {
            get => currentEnergy;
            set => currentEnergy = Mathf.Clamp(value, 0, config.maxEnergy);
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            // Initialize energy to max
            CurrentEnergy = config.maxEnergy;
            
            // If beamFX is not assigned, try to find or add it
            if (beamFX == null)
            {
                beamFX = GetComponent<BeamWeaponFX>();
                if (beamFX == null)
                {
                    beamFX = gameObject.AddComponent<BeamWeaponFX>();
                }
            }
            
            // Configure the BeamWeaponFX with our config values
            if (beamFX != null)
            {
                // Set prefabs from config
                beamFX.chargeFXPrefab = config.chargeFXPrefab;
                beamFX.flareFXPrefab = config.flareFXPrefab;
                beamFX.impactFXPrefab = config.impactFXPrefab;
                beamFX.beamMiddleAnimPrefab = config.beamMiddleAnimPrefab;
                
                // Set beam visual properties - using the single beamWidth property
                beamFX.beamWidth = config.beamWidth;
                beamFX.beamSectionDistance = config.beamSectionDistance;
                beamFX.sectionOverlap = config.sectionOverlap;
            }
            
            // Add energy manager
            energyManager = gameObject.AddComponent<BeamWeaponEnergy>();
            energyManager.Initialize(this, config, audioSource);
            energyManager.reloadSound = reloadSound;
            energyManager.reloadTime = reloadTime;
            
            // Hook up events
            energyManager.onAmmoChanged += (current, max) => { onAmmoChanged?.Invoke(current, max); };
            energyManager.onReloadStart += () => { onReloadStart?.Invoke(); };
            energyManager.onReloadComplete += () => { onReloadComplete?.Invoke(); };
            
            // Initialize beam physics
            InitializeBeamPhysics();
        }
        
        private void InitializeBeamPhysics()
        {
            GameObject physicsObj = new GameObject("BeamPhysics");
            physicsObj.transform.SetParent(transform);
            
            // Choose physics implementation based on config
            if (config.useRopePhysics)
            {
                beamPhysics = physicsObj.AddComponent<RopeBeamPhysics>();
            }
            else
            {
                beamPhysics = physicsObj.AddComponent<SmoothCurveBeamPhysics>();
            }
            
            beamPhysics.Initialize(config, firePoint);
        }
        
        protected override void Update()
        {
            HandleInput();
            
            // Update energy regeneration - only if using auto-recharge and not firing
            if (config.energySystemType == BeamWeaponConfig.EnergySystemType.AutoRecharge && 
                !isFiringBeam && !isReloading)
            {
                CurrentEnergy += config.energyRegenRate * Time.deltaTime;
                
                // Notify the UI about energy changes
                if (energyManager != null)
                {
                    energyManager.UpdateEnergyUI();
                }
            }
            
            if (isFiringBeam)
            {
                UpdateBeam();
            }
        }
        
        private void HandleInput()
        {
            // Get the weapon aiming component to check if we're equipped
            WeaponAiming aiming = GetComponent<WeaponAiming>();
            if (aiming != null && !aiming.isEquipped)
            {
                if (isFiringBeam)
                {
                    StopBeam();
                }
                return;
            }
            
            // Check for battery reload or energy recharge based on system type
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (config.energySystemType == BeamWeaponConfig.EnergySystemType.BatteryReload)
                {
                    // Start battery reload if we have batteries and aren't at max energy
                    if (config.CurrentBatteryCount > 0 && CurrentEnergy < config.maxEnergy)
                    {
                        StartCoroutine(ReloadBattery());
                    }
                }
                else
                {
                    // For auto-recharge mode, use the existing recharge mechanism
                    if (CurrentEnergy < config.maxEnergy)
                    {
                        StartCoroutine(energyManager.RechargeBeam());
                    }
                }
                return;
            }
            
            // Handle fire input based on fire mode
            if (config.fireMode == BeamWeaponConfig.BeamFireMode.Continuous)
            {
                // Hold to fire, release to stop
                if (Input.GetMouseButtonDown(0) && !isFiringBeam && CanFireBeam())
                {
                    StartBeam();
                }
                else if (Input.GetMouseButtonUp(0) && isFiringBeam)
                {
                    StopBeam();
                }
            }
            else if (config.fireMode == BeamWeaponConfig.BeamFireMode.ChargeBurst)
            {
                // Charge Burst mode - hold to charge, auto-fires when charge complete
                if (Input.GetMouseButtonDown(0) && !isChargedShot && !isFiringBeam && CanFireBeam())
                {
                    StartBeam();
                }
                else if (Input.GetMouseButtonUp(0) && isChargedShot)
                {
                    // Cancel charging if released before fully charged
                    CancelCharging();
                }
            }
        }
        
        private bool CanFireBeam()
        {
            return energyManager.HasEnoughEnergy() && !isReloading;
        }
        
        public void StartBeam()
        {
            if (!CanFireBeam())
                return;
            
            switch (config.fireMode)
            {
                case BeamWeaponConfig.BeamFireMode.Continuous:
                    // Check if there's a charge-up time set for continuous mode
                    if (config.continuousChargeTime > 0f)
                    {
                        if (!isCharging)
                        {
                            if (chargeCoroutine != null)
                                StopCoroutine(chargeCoroutine);
                            
                            // Use the continuous charge time
                            chargeCoroutine = StartCoroutine(ContinuousChargeBeam());
                        }
                    }
                    else
                    {
                        // No charge time, fire immediately
                        ActivateBeam();
                    }
                    break;
                    
                case BeamWeaponConfig.BeamFireMode.ChargeBurst:
                    // Start charging for burst
                    chargeStartTime = Time.time;
                    isChargedShot = true;
                    
                    // Play charge effect
                    if (beamFX != null)
                    {
                        beamFX.PlayChargeEffect(firePoint);
                    }
                    
                    // Play charge sound
                    if (audioSource != null && config.beamStartSound != null)
                    {
                        audioSource.PlayOneShot(config.beamStartSound, 0.5f);
                    }
                    
                    // Start the charge and auto-fire coroutine
                    StartCoroutine(ChargeAndAutoBurst());
                    break;
            }
        }
        
        private IEnumerator ContinuousChargeBeam()
        {
            isCharging = true;
            
            // Play charge sound
            if (audioSource != null && config.beamStartSound != null)
            {
                audioSource.PlayOneShot(config.beamStartSound, 0.5f);
            }
            
            // Play charge effect
            if (beamFX != null)
            {
                beamFX.PlayChargeEffect(firePoint);
            }
            
            // Wait for continuous charge duration
            yield return new WaitForSeconds(config.continuousChargeTime);
            
            // Fire the beam
            ActivateBeam();
            isCharging = false;
        }
        
        private IEnumerator ChargeAndAutoBurst()
        {
            // Wait for charge duration
            yield return new WaitForSeconds(config.maxChargeTime);
            
            // If we're still charging (player hasn't released button)
            if (isChargedShot)
            {
                isChargedShot = false;
                
                // Stop charge effect
                if (beamFX != null)
                {
                    beamFX.StopChargeEffect();
                }
                
                // Fire the burst at full charge
                StartCoroutine(FireChargedBeam(1.0f));
            }
        }
        
        private void ActivateBeam()
        {
            isFiringBeam = true;
            
            // Stop charge effect if needed
            if (beamFX != null && isCharging)
            {
                beamFX.StopChargeEffect();
            }
            
            // Play audio effects
            if (audioSource != null && !isCharging && config.beamStartSound != null)
            {
                audioSource.PlayOneShot(config.beamStartSound);
            }
            
            if (audioSource != null && config.beamLoopSound != null)
            {
                audioSource.clip = config.beamLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Invoke events
            onFire?.Invoke();
        }
        
        private void UpdateBeam()
        {
            // Add null checks at the beginning of the method
            if (firePoint == null)
            {
                Debug.LogError("Beam weapon is missing firePoint reference");
                StopBeam();
                return;
            }

            if (beamPhysics == null)
            {
                Debug.LogError("Beam weapon is missing beamPhysics reference");
                StopBeam();
                return;
            }

            // Force energy consumption - this line is crucial
            CurrentEnergy -= config.energyDrainRate * Time.deltaTime;
            
            // Check if energy is depleted
            if (CurrentEnergy <= 0)
            {
                // Stop the beam
                StopBeam();
                
                // If using battery reload and we have batteries, auto-reload
                if (config.energySystemType == BeamWeaponConfig.EnergySystemType.BatteryReload && 
                    config.CurrentBatteryCount > 0)
                {
                    StartCoroutine(AutoReloadBattery());
                }
                else
                {
                    // Trigger the out of ammo event
                    onOutOfAmmo?.Invoke();
                }
                return;
            }
            
            // Calculate beam start and target end positions
            Vector3 startPos = firePoint.position;
            Vector3 aimDirection = firePoint.right;
            
            // Raycast to find what the beam hits
            RaycastHit2D hit = Physics2D.Raycast(startPos, aimDirection, config.beamRange);
            
            Vector3 endPos;
            bool hasHit = false;
            Vector3 hitNormal = Vector3.zero;
            
            if (hit.collider != null)
            {
                endPos = hit.point;
                hasHit = true;
                hitNormal = hit.normal;
                
                // Apply damage to hit object
                float damageThisFrame = config.beamDamagePerSecond * Time.deltaTime;
                // Your damage code would go here
            }
            else
            {
                endPos = startPos + aimDirection * config.beamRange;
            }
            
            // Update the beam physics simulation with null check
            if (beamPhysics != null)
            {
                beamPhysics.UpdateBeam(startPos, endPos, aimDirection);
                
                // Update end rotation for effects
                Quaternion targetRotation = hasHit ? 
                    Quaternion.FromToRotation(Vector3.right, hitNormal) : firePoint.rotation;
                
                // Update visual effects with null check
                if (beamFX != null)
                {
                    Vector3 effectEndPos = beamPhysics.GetEndPosition();
                    
                    beamFX.PlayFlareEffects(startPos, effectEndPos, firePoint.rotation, targetRotation);
                    
                    if (hasHit)
                    {
                        beamFX.PlayImpactEffect(effectEndPos, hitNormal);
                    }
                    else
                    {
                        beamFX.StopImpactEffect();
                    }
                    
                    // Update beam animation with physics points
                    beamFX.UpdateCurvedBeamAnimation(beamPhysics.GetBeamPositions());
                }
            }
            else
            {
                Debug.LogError("Beam physics is null during beam update");
                StopBeam();
            }
        }
        
        public void StopBeam()
        {
            if (isCharging && chargeCoroutine != null)
            {
                StopCoroutine(chargeCoroutine);
                isCharging = false;
            }
            
            // Handle charged shot on release (only for manual release now)
            if (isChargedShot && config.fireMode == BeamWeaponConfig.BeamFireMode.ChargeBurst)
            {
                CancelCharging();
                return;
            }
            
            StopBeamInternal();
        }
        
        private void StopBeamInternal()
        {
            isFiringBeam = false;
            
            // Clean up physics
            if (beamPhysics != null)
            {
                beamPhysics.Cleanup();
            }
            
            // Clean up visual effects
            if (beamFX != null)
            {
                beamFX.CleanupAllEffects();
            }
            
            // Stop audio
            if (audioSource != null)
            {
                audioSource.loop = false;
                audioSource.Stop();
                
                // Play end sound
                if (config.beamEndSound != null)
                {
                    audioSource.PlayOneShot(config.beamEndSound);
                }
            }
        }
        
        private void CancelCharging()
        {
            if (isChargedShot)
            {
                isChargedShot = false;
                
                // Stop charge effect
                if (beamFX != null)
                {
                    beamFX.StopChargeEffect();
                }
                
                // Play cancel sound
                if (audioSource != null && config.beamEndSound != null)
                {
                    audioSource.PlayOneShot(config.beamEndSound, 0.5f);
                }
            }
        }
        
        private IEnumerator FireChargedBeam(float chargePercent)
        {
            // Calculate damage multiplier based on charge
            float damageMultiplier = 1f + chargePercent * (config.maxChargeDamageMultiplier - 1f);
            
            // Activate beam with increased damage
            ActivateBeam();
            
            // Store original damage
            float originalDamage = config.beamDamagePerSecond;
            
            // Apply multiplier
            config.beamDamagePerSecond *= damageMultiplier;
            
            // Play charged sound effect if available
            if (audioSource != null && config.beamStartSound != null)
            {
                audioSource.PlayOneShot(config.beamStartSound, 1.0f * chargePercent);
            }
            
            // Keep beam on for configured burst duration
            yield return new WaitForSeconds(config.burstDuration);
            
            // Restore original damage
            config.beamDamagePerSecond = originalDamage;
            
            // Turn beam off
            StopBeamInternal();
        }
        
        public override void Shoot()
        {
            // Toggle beam on/off
            if (!isFiringBeam && CanFireBeam())
            {
                StartBeam();
            }
            else if (isFiringBeam)
            {
                StopBeam();
            }
        }
        
        public void ToggleBeamVisibility()
        {
            config.hideBeamColor = !config.hideBeamColor;
            
            // Update beam physics visibility
            if (beamPhysics is RopeBeamPhysics ropePhysics)
            {
                ropePhysics.UpdateBeamVisibility();
            }
            else if (beamPhysics is SmoothCurveBeamPhysics smoothPhysics)
            {
                smoothPhysics.UpdateBeamVisibility();
            }
        }
        
        private void OnDisable()
        {
            // Make sure to stop the beam when weapon is disabled
            if (isFiringBeam)
            {
                StopBeam();
            }
        }
        
        // Validation for appropriate settings
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Apply recommended settings based on physics choice
            if (config.useRopePhysics)
            {
                config.useSmoothCurve = false;
            }
            
            // Ensure current energy is within valid range
            CurrentEnergy = Mathf.Clamp(currentEnergy, 0, config.maxEnergy);
        }
        
        // Method to reset the beam for debugging
        public void ResetBeam()
        {
            if (isFiringBeam)
            {
                StopBeam();
            }
            
            // Destroy and reinitialize beam physics
            if (beamPhysics != null)
            {
                Destroy((beamPhysics as MonoBehaviour).gameObject);
            }
            
            InitializeBeamPhysics();
        }
        
        public float GetCurrentChargePercent()
        {
            if (!isChargedShot)
                return currentCharge;
                
            float chargeDuration = Time.time - chargeStartTime;
            return Mathf.Clamp01(chargeDuration / config.maxChargeTime);
        }
        
        // Add this new method for auto-reloading
        private IEnumerator AutoReloadBattery()
        {
            // Only proceed if we have batteries and aren't already reloading
            if (config.CurrentBatteryCount <= 0 || isReloading)
                yield break;
            
            Debug.Log("Auto-reloading battery...");
            
            // Start the reload process
            isReloading = true;
            onReloadStart?.Invoke();
            
            // Play reload sound
            if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
            
            // Wait for reload time
            yield return new WaitForSeconds(config.batteryReloadTime);
            
            // Add energy from battery
            CurrentEnergy += config.energyPerBattery;
            
            // Use one battery
            config.CurrentBatteryCount--;
            
            // Notify of energy change
            if (energyManager != null)
            {
                energyManager.UpdateEnergyUI();
            }
            
            isReloading = false;
            onReloadComplete?.Invoke();
            
            Debug.Log($"Auto-reload complete! Energy: {CurrentEnergy}, Batteries remaining: {config.CurrentBatteryCount}");
        }
        
        // Update the ReloadBattery method to be public and use the auto-reload logic
        public IEnumerator ReloadBattery()
        {
            yield return StartCoroutine(AutoReloadBattery());
        }
        
        private bool ValidateFXPrefabs()
        {
            if (config.chargeFXPrefab == null)
            {
                Debug.LogWarning($"Beam weapon '{weaponName}' is missing Charge FX Prefab");
                return false;
            }
            
            if (config.flareFXPrefab == null)
            {
                Debug.LogWarning($"Beam weapon '{weaponName}' is missing Flare FX Prefab");
                return false;
            }
            
            if (config.impactFXPrefab == null)
            {
                Debug.LogWarning($"Beam weapon '{weaponName}' is missing Impact FX Prefab");
                return false;
            }
            
            if (config.beamMiddleAnimPrefab == null)
            {
                Debug.LogWarning($"Beam weapon '{weaponName}' is missing Beam Middle Prefab");
                return false;
            }
            
            return true;
        }
        
        private void Start()
        {
            // Validate that all required FX prefabs are assigned
            ValidateFXPrefabs();
        }
    }
}