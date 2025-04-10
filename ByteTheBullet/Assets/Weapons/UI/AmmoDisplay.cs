using UnityEngine;
using TMPro;
using Weapons;

namespace Weapons.UI
{
    // Example UI script that could subscribe to the ammo events
    public class AmmoDisplay : MonoBehaviour
    {
        public TextMeshProUGUI ammoText;
        
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