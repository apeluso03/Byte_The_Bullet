using UnityEngine;

namespace Weapons
{
    /// <summary>
    /// Holds metadata about a weapon for inventory and UI display
    /// </summary>
    public class WeaponMetadata : MonoBehaviour
    {
        [Header("Weapon Identity")]
        public string weaponName = "Unnamed Weapon";
        public string description = "No description available.";
        public string rarity = "Common";
        
        [Header("Stats")]
        public float damage = 10f;
        public int magazineSize = 10;
        public float fireRate = 5f;
        
        [Header("Display")]
        public Sprite weaponIcon;
        public Color rarityColor = Color.white;
    }
} 