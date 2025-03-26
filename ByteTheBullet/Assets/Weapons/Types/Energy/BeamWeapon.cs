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
            energyManager.RegenerateEnergy(isFiringBeam);
            
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
            
            // Check for energy recharge (reload equivalent)
            if (Input.GetKeyDown(KeyCode.R) && CurrentEnergy < config.maxEnergy)
            {
                StartCoroutine(energyManager.RechargeBeam());
                return;
            }
            
            // Handle firing input
            if (Input.GetMouseButtonDown(0) && !isFiringBeam && CanFireBeam())
            {
                StartBeam();
            }
            else if (Input.GetMouseButtonUp(0) && isFiringBeam)
            {
                StopBeam();
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
            
            // If using charge effect, start the charging sequence
            if (config.useChargeEffect && config.chargeTime > 0)
            {
                if (!isCharging)
                {
                    if (chargeCoroutine != null)
                        StopCoroutine(chargeCoroutine);
                        
                    chargeCoroutine = StartCoroutine(ChargeAndFireBeam());
                }
            }
            else
            {
                // Fire immediately if not using charge effect
                ActivateBeam();
            }
        }
        
        private IEnumerator ChargeAndFireBeam()
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
            
            // Wait for charge duration
            yield return new WaitForSeconds(config.chargeTime);
            
            // Fire the beam
            ActivateBeam();
            isCharging = false;
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
            // Energy consumption
            energyManager.ConsumeEnergy(Time.deltaTime);
            
            // If energy is depleted, stop the beam
            if (CurrentEnergy <= 0)
            {
                StopBeam();
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
            
            // Update the beam physics simulation
            beamPhysics.UpdateBeam(startPos, endPos, aimDirection);
            
            // Update end rotation for effects
            Quaternion targetRotation = hasHit ? 
                Quaternion.FromToRotation(Vector3.right, hitNormal) : firePoint.rotation;
            
            // Update visual effects
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
        
        public void StopBeam()
        {
            if (isCharging && chargeCoroutine != null)
            {
                StopCoroutine(chargeCoroutine);
                isCharging = false;
            }
            
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
    }
}