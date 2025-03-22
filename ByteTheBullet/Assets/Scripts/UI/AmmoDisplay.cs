using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Weapons; // Add this for BaseWeapon

public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image ammoBar;
    
    // Change WeaponStats to BaseWeapon
    private BaseWeapon currentWeapon;
    
    private void Start()
    {
        if (ammoText == null)
            ammoText = GetComponent<TextMeshProUGUI>();
    }
    
    public void SetWeapon(BaseWeapon weapon) // Change from WeaponStats to BaseWeapon
    {
        currentWeapon = weapon;
        
        // Subscribe to the ammo changed event
        if (currentWeapon != null)
        {
            currentWeapon.onAmmoChanged.AddListener(UpdateAmmoDisplay);
            
            // Initialize display with current values
            UpdateAmmoDisplay(currentWeapon.CurrentAmmo, currentWeapon.magazineSize);
        }
    }
    
    public void ClearWeapon()
    {
        if (currentWeapon != null)
        {
            currentWeapon.onAmmoChanged.RemoveListener(UpdateAmmoDisplay);
            currentWeapon = null;
        }
        
        // Clear display
        if (ammoText != null)
            ammoText.text = "-- / --";
            
        if (ammoBar != null)
            ammoBar.fillAmount = 0;
    }
    
    private void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = $"{current} / {max}";
            
        if (ammoBar != null)
            ammoBar.fillAmount = (float)current / max;
    }
    
    private void OnDestroy()
    {
        ClearWeapon();
    }
} 