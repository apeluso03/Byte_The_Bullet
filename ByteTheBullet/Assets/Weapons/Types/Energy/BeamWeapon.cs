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
        
        // Add this field to BeamWeapon class
        private Rect heightSliderRect = new Rect(20, Screen.height - 80, 250, 60);
        
        // Add this public property to the BeamWeapon class
        public IBeamPhysics BeamPhysics => beamPhysics;
        
        // Add this property to track if we've modified the beam system
        private bool isUsingCustomRangeSystem = false;
        
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
            
            // Make sure beam physics is initialized
            InitializeBeamPhysics();
        }
        
        private void InitializeBeamPhysics()
        {
            // Clean up ALL existing beam physics to prevent duplicates
            CleanupAllBeamComponents();
            
            // If we already have physics, don't re-create it
            if (beamPhysics != null) return;
            
            // Create the appropriate beam physics implementation based on config
            if (config.useRopePhysics)
            {
                RopeBeamPhysics ropePhysics = gameObject.AddComponent<RopeBeamPhysics>();
                ropePhysics.Initialize(config, firePoint);
                beamPhysics = ropePhysics;
            }
            else if (config.useSmoothCurve)
            {
                SmoothCurveBeamPhysics curvePhysics = gameObject.AddComponent<SmoothCurveBeamPhysics>();
                curvePhysics.Initialize(config, firePoint);
                beamPhysics = curvePhysics;
            }
            else
            {
                // Fallback to simple beam physics
                SimpleBeamPhysics simplePhysics = gameObject.AddComponent<SimpleBeamPhysics>();
                simplePhysics.Initialize(config, firePoint);
                beamPhysics = simplePhysics;
            }
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
            // If we have a custom range set, be more thorough with cleanup
            if (isUsingCustomRangeSystem)
            {
                CleanupAllBeamComponents();
            }
            
            // Make sure we have physics initialized
            if (beamPhysics == null)
            {
                InitializeBeamPhysics();
            }
            
            // Make sure we don't have duplicate renderers before starting
            CleanupBeamPhysics();
            
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
            if (!isFiringBeam || beamPhysics == null) return;
            
            // Get the fire point position and direction
            Vector3 firePosition = firePoint.position;
            Vector3 fireDirection = firePoint.right;
            
            // Perform raycast to find where the beam hits
            RaycastHit2D hit = Physics2D.Raycast(firePosition, fireDirection, config.beamRange);
            
            // Calculate the end position - either hit point or max range
            Vector3 endPosition;
            Vector3 hitNormal = Vector3.up;
            bool didHit = false;
            
            if (hit.collider != null)
            {
                // Beam hit something within range
                endPosition = hit.point;
                hitNormal = hit.normal;
                didHit = true;
            }
            else
            {
                // Beam reaches maximum range
                endPosition = firePosition + fireDirection * config.beamRange;
                didHit = false;
            }
            
            // Update the beam physics
            beamPhysics.UpdateBeam(firePosition, endPosition, fireDirection);
            
            // Update visual effects
            if (beamFX != null)
            {
                // Apply visual updates
                beamFX.beamWidth = config.beamWidth;
                beamFX.beamSectionDistance = config.beamSectionDistance;
                beamFX.sectionOverlap = config.sectionOverlap;
                
                // Get beam positions from physics
                Vector3[] beamPositions = beamPhysics.GetBeamPositions();
                
                // If we have at least start and end positions
                if (beamPositions != null && beamPositions.Length >= 2)
                {
                    if (beamPositions.Length > 10)
                    {
                        // For complex curved beams, use the full curve
                        beamFX.UpdateCurvedBeamAnimation(beamPositions);
                    }
                    else
                    {
                        // For simple beams, just use start and end
                        beamFX.UpdateBeamMiddleAnimation(beamPositions[0], beamPositions[beamPositions.Length - 1]);
                    }
                    
                    // Update flare effects
                    Vector3 startPos = beamPositions[0];
                    Vector3 endPos = beamPositions[beamPositions.Length - 1];
                    
                    // Calculate rotations for flare effects
                    Quaternion startRot = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    Quaternion endRot = didHit ? 
                        Quaternion.FromToRotation(Vector3.up, hitNormal) : 
                        Quaternion.LookRotation(Vector3.forward, Vector3.up);
                    
                    beamFX.PlayFlareEffects(startPos, endPos, startRot, endRot);
                    
                    // Update impact effect
                    if (didHit)
                    {
                        beamFX.PlayImpactEffect(endPos, hitNormal);
                    }
                    else
                    {
                        beamFX.StopImpactEffect();
                    }
                }
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
            
            // After stopping, clean up more thoroughly if using custom range
            if (isUsingCustomRangeSystem)
            {
                // Don't call CleanupAllBeamComponents() directly to avoid removing physics component
                // Just ensure visual elements are hidden/cleaned up
                if (beamFX != null)
                {
                    beamFX.CleanupAllEffects();
                }
            }
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
        
        // Add this method to BeamWeapon class to update FX settings
        private void UpdateBeamFXSettings()
        {
            if (beamFX != null)
            {
                // Sync FX settings from config
                beamFX.beamWidth = config.beamWidth;
                beamFX.beamSectionDistance = config.beamSectionDistance;
                beamFX.sectionOverlap = config.sectionOverlap;
            }
        }
        
        // Add this method to the BeamWeapon class
        private void SyncBeamFXWithConfig()
        {
            if (beamFX != null)
            {
                // Update FX settings from config
                beamFX.beamWidth = config.beamWidth;
                beamFX.beamSectionDistance = config.beamSectionDistance;
                beamFX.sectionOverlap = config.sectionOverlap;
            }
        }
        
        // Add this method to directly update beam width
        public void UpdateBeamWidth(float width)
        {
            // Update the config
            config.beamWidth = width;
            
            // Update the beam FX
            if (beamFX != null)
            {
                beamFX.UpdateBeamWidth(width);
            }
            
            // Update beam physics if it exists
            if (beamPhysics != null)
            {
                // Since beamPhysics is an interface, we need to get the MonoBehaviour component
                MonoBehaviour physicsComponent = beamPhysics as MonoBehaviour;
                if (physicsComponent != null)
                {
                    // Now we can get the LineRenderer components
                    LineRenderer[] lineRenderers = physicsComponent.GetComponentsInChildren<LineRenderer>();
                    foreach (LineRenderer lr in lineRenderers)
                    {
                        lr.startWidth = width;
                        lr.endWidth = width * 0.7f;
                    }
                }
            }
        }
        
        // Update the OnGUI method in BeamWeapon class
        private void OnGUI()
        {
            // Only show the GUI if enabled and in play mode
            if (Application.isPlaying && config.showPositionAdjustmentGUI && isFiringBeam)
            {
                // Make the GUI box taller to fit three sliders
                Rect positionRect = new Rect(20, Screen.height - 160, 250, 140);
                GUI.Box(positionRect, "Beam Adjustment");
                
                // Height slider
                Rect heightSliderRect = new Rect(
                    positionRect.x + 10, 
                    positionRect.y + 30, 
                    positionRect.width - 20, 
                    20
                );
                
                GUI.Label(new Rect(heightSliderRect.x, heightSliderRect.y - 15, 100, 20), "Height:");
                
                float newHeightOffset = GUI.HorizontalSlider(
                    heightSliderRect, 
                    config.beamHeightOffset, 
                    -1f, 
                    1f
                );
                
                if (newHeightOffset != config.beamHeightOffset)
                {
                    config.beamHeightOffset = newHeightOffset;
                }
                
                GUI.Label(new Rect(heightSliderRect.x, heightSliderRect.y + 5, 50, 20), "Lower");
                GUI.Label(new Rect(heightSliderRect.x + heightSliderRect.width - 40, heightSliderRect.y + 5, 50, 20), "Higher");
                
                // Forward offset slider
                Rect forwardSliderRect = new Rect(
                    positionRect.x + 10, 
                    positionRect.y + 70, 
                    positionRect.width - 20, 
                    20
                );
                
                GUI.Label(new Rect(forwardSliderRect.x, forwardSliderRect.y - 15, 100, 20), "Forward:");
                
                float newForwardOffset = GUI.HorizontalSlider(
                    forwardSliderRect, 
                    config.beamForwardOffset, 
                    -1f, 
                    1f
                );
                
                if (newForwardOffset != config.beamForwardOffset)
                {
                    config.beamForwardOffset = newForwardOffset;
                }
                
                GUI.Label(new Rect(forwardSliderRect.x, forwardSliderRect.y + 5, 50, 20), "Back");
                GUI.Label(new Rect(forwardSliderRect.x + forwardSliderRect.width - 40, forwardSliderRect.y + 5, 50, 20), "Forward");
                
                // Range slider
                Rect rangeSliderRect = new Rect(
                    positionRect.x + 10, 
                    positionRect.y + 110, 
                    positionRect.width - 20, 
                    20
                );
                
                GUI.Label(new Rect(rangeSliderRect.x, rangeSliderRect.y - 15, 100, 20), "Range:");
                
                // Allow range adjustment between 5 and 30
                float newRange = GUI.HorizontalSlider(
                    rangeSliderRect, 
                    config.beamRange, 
                    5f, 
                    30f
                );
                
                if (Mathf.Abs(newRange - config.beamRange) > 0.1f)
                {
                    config.beamRange = newRange;
                }
                
                GUI.Label(new Rect(rangeSliderRect.x, rangeSliderRect.y + 5, 50, 20), "Short");
                GUI.Label(new Rect(rangeSliderRect.x + rangeSliderRect.width - 40, rangeSliderRect.y + 5, 50, 20), "Long");
                
                // Show the current range value
                GUI.Label(new Rect(positionRect.x + positionRect.width / 2 - 30, rangeSliderRect.y + 5, 60, 20), 
                         config.beamRange.ToString("F1") + " units");
            }
        }
        
        // Add this method to BeamWeapon class
        public void RefreshBeamPhysics()
        {
            if (beamPhysics != null)
            {
                MonoBehaviour physicsComponent = beamPhysics as MonoBehaviour;
                if (physicsComponent != null)
                {
                    physicsComponent.enabled = false;
                    physicsComponent.enabled = true;
                }
            }
        }
        
        // Modify the UpdateBeamRange method to handle low range values
        public void UpdateBeamRange(float range)
        {
            // Update the config
            config.beamRange = Mathf.Max(0.1f, range); // Ensure minimum range to prevent issues
            
            // Mark that we're using the custom range system
            isUsingCustomRangeSystem = true;
            
            // Force cleanup and reinitialize when range changes significantly
            if (beamPhysics != null)
            {
                // Get the current physics component
                MonoBehaviour currentPhysics = beamPhysics as MonoBehaviour;
                
                // Destroy it to ensure clean state
                if (currentPhysics != null)
                {
                    Destroy(currentPhysics);
                    beamPhysics = null;
                }
            }
            
            // Re-initialize the beam physics with new range
            InitializeBeamPhysics();
            
            // If beam is active, refresh it
            if (isFiringBeam)
            {
                RefreshBeamPhysics();
            }
        }
        
        // Add this method to the BeamWeapon class
        private void OnEnable()
        {
            // Cleanup any existing physics components to prevent duplicates
            CleanupBeamPhysics();
        }
        
        // Add this method to cleanup existing physics components
        private void CleanupBeamPhysics()
        {
            // Remove any existing beam physics components to prevent duplicates
            RopeBeamPhysics[] ropePhysics = GetComponents<RopeBeamPhysics>();
            foreach (var physics in ropePhysics)
            {
                if (physics != beamPhysics as RopeBeamPhysics)
                {
                    Destroy(physics);
                }
            }
            
            SmoothCurveBeamPhysics[] curvePhysics = GetComponents<SmoothCurveBeamPhysics>();
            foreach (var physics in curvePhysics)
            {
                if (physics != beamPhysics as SmoothCurveBeamPhysics)
                {
                    Destroy(physics);
                }
            }
            
            // Also check for duplicate LineRenderers that might be creating a second beam
            LineRenderer[] lineRenderers = GetComponents<LineRenderer>();
            if (lineRenderers.Length > 1)
            {
                // Keep only the one being used by our beam physics
                foreach (var lr in lineRenderers)
                {
                    // If this isn't attached to active beam physics, destroy it
                    bool usedByPhysics = false;
                    if (beamPhysics != null)
                    {
                        MonoBehaviour physicsComponent = beamPhysics as MonoBehaviour;
                        if (physicsComponent != null && physicsComponent.GetComponent<LineRenderer>() == lr)
                        {
                            usedByPhysics = true;
                        }
                    }
                    
                    if (!usedByPhysics)
                    {
                        Destroy(lr);
                    }
                }
            }
        }
        
        // Add this method to very thoroughly clean up ALL beam components
        private void CleanupAllBeamComponents()
        {
            // Destroy ALL physics components
            foreach (RopeBeamPhysics physics in GetComponents<RopeBeamPhysics>())
            {
                Destroy(physics);
            }
            
            foreach (SmoothCurveBeamPhysics physics in GetComponents<SmoothCurveBeamPhysics>())
            {
                Destroy(physics);
            }
            
            foreach (SimpleBeamPhysics physics in GetComponents<SimpleBeamPhysics>())
            {
                Destroy(physics);
            }
            
            // Destroy ALL LineRenderers (they'll be recreated by the physics)
            foreach (LineRenderer lr in GetComponents<LineRenderer>())
            {
                Destroy(lr);
            }
            
            // Reset beam FX to clear all sections
            if (beamFX != null)
            {
                beamFX.ResetBeamSections();
            }
            
            // Set to null to ensure we re-initialize
            beamPhysics = null;
        }
    }
}