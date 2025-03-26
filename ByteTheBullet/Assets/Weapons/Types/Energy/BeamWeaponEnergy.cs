using UnityEngine;
using System.Collections;

namespace Weapons
{
    /// <summary>
    /// Manages energy for the beam weapon
    /// </summary>
    public class BeamWeaponEnergy : MonoBehaviour
    {
        private BeamWeapon beamWeapon;
        private BeamWeaponConfig config;
        
        [SerializeField] private float currentEnergy;
        private bool isRecharging = false;
        
        // Delegates for events
        public delegate void AmmoChangedDelegate(int current, int max);
        public delegate void ReloadDelegate();
        
        // Events
        public event AmmoChangedDelegate onAmmoChanged;
        public event ReloadDelegate onReloadStart;
        public event ReloadDelegate onReloadComplete;
        
        // Audio source for recharge sound
        private AudioSource audioSource;
        public AudioClip reloadSound;
        public float reloadTime = 1.5f;
        
        // Properties
        public float CurrentEnergy
        {
            get => currentEnergy;
            set => currentEnergy = Mathf.Clamp(value, 0, config.maxEnergy);
        }
        
        public bool IsRecharging => isRecharging;
        
        public void Initialize(BeamWeapon weapon, BeamWeaponConfig weaponConfig, AudioSource audio)
        {
            beamWeapon = weapon;
            config = weaponConfig;
            audioSource = audio;
            CurrentEnergy = config.maxEnergy;
        }
        
        public void RegenerateEnergy(bool isFiring)
        {
            // This method should be empty or simplified, as we're handling regeneration directly in BeamWeapon.Update
            // We'll keep this method for backward compatibility
        }
        
        public void ConsumeEnergy(float deltaTime)
        {
            CurrentEnergy -= config.energyDrainRate * deltaTime;
            
            // Update UI
            if (onAmmoChanged != null)
            {
                int currentEnergyInt = Mathf.FloorToInt(CurrentEnergy);
                onAmmoChanged.Invoke(currentEnergyInt, Mathf.FloorToInt(config.maxEnergy));
            }
        }
        
        public bool HasEnoughEnergy()
        {
            return CurrentEnergy > 0 && !isRecharging;
        }
        
        public IEnumerator RechargeBeam()
        {
            isRecharging = true;
            onReloadStart?.Invoke();
            
            // Play recharge sound
            if (audioSource != null && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
            
            // Calculate recharge time based on how depleted we are
            float rechargeTime = reloadTime * (1 - CurrentEnergy / config.maxEnergy);
            float startEnergy = CurrentEnergy;
            float elapsed = 0f;
            
            // Gradually recharge over time
            while (elapsed < rechargeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rechargeTime;
                CurrentEnergy = Mathf.Lerp(startEnergy, config.maxEnergy, t);
                
                // Update UI
                if (onAmmoChanged != null)
                {
                    int currentEnergyInt = Mathf.FloorToInt(CurrentEnergy);
                    onAmmoChanged.Invoke(currentEnergyInt, Mathf.FloorToInt(config.maxEnergy));
                }
                
                yield return null;
            }
            
            // Ensure we're fully charged
            CurrentEnergy = config.maxEnergy;
            isRecharging = false;
            onReloadComplete?.Invoke();
        }

        public void UpdateEnergyUI()
        {
            // Call the ammo changed event to update the UI
            onAmmoChanged?.Invoke(Mathf.RoundToInt(beamWeapon.CurrentEnergy), 
                                 Mathf.RoundToInt(beamWeapon.config.maxEnergy));
        }
    }
}