using UnityEngine;
using Weapons;

namespace Weapons.Utility
{
    /// <summary>
    /// Utility class to handle compatibility between different weapon types
    /// </summary>
    public static class WeaponCompat
    {
        /// <summary>
        /// Check if a weapon is currently firing
        /// </summary>
        public static bool IsWeaponFiring(BaseWeapon weapon)
        {
            // Check different weapon types
            if (weapon is BeamWeapon beamWeapon)
            {
                return beamWeapon.IsFiring;
            }
            else if (weapon is EnergyWeapon energyWeapon)
            {
                return energyWeapon.IsFiring;
            }
            
            // Default for other weapon types - assume not firing
            return false;
        }
        
        /// <summary>
        /// Start firing a weapon if it supports continuous fire
        /// </summary>
        public static void StartFiring(BaseWeapon weapon)
        {
            if (weapon is BeamWeapon beamWeapon)
            {
                beamWeapon.StartBeam();
            }
            else if (weapon is EnergyWeapon energyWeapon)
            {
                // Only call StartBeam if it's a beam type energy weapon
                if (energyWeapon.energyType == EnergyWeapon.EnergyType.Beam)
                {
                    energyWeapon.StartBeam();
                }
                else
                {
                    // For non-beam weapons, just use the regular Shoot method
                    energyWeapon.Shoot();
                }
            }
            else
            {
                // Default behavior for other weapons
                weapon.Shoot();
            }
        }
        
        /// <summary>
        /// Stop firing a weapon if it supports continuous fire
        /// </summary>
        public static void StopFiring(BaseWeapon weapon)
        {
            if (weapon is BeamWeapon beamWeapon && beamWeapon.IsFiring)
            {
                beamWeapon.StopBeam();
            }
            else if (weapon is EnergyWeapon energyWeapon && energyWeapon.IsFiring)
            {
                energyWeapon.StopBeam();
            }
            // Other weapon types don't need to explicitly stop firing
        }
    }
} 