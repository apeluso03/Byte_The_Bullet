using UnityEngine;

[DisallowMultipleComponent]
public class WeaponMetadata : MonoBehaviour
{
    public string weaponName = "Unnamed Weapon";
    public string weaponType = "Pistol";
    public string rarity = "Common";
    public string fireType = "SemiAuto";
    public string damageType = "Physical";
    
    [Tooltip("Weapon damage amount")]
    public float damage = 10f;
    
    [Tooltip("Fire rate in rounds per second")]
    public float fireRate = 5f;
    
    [Tooltip("Magazine size (bullets before reload)")]
    public int magazineSize = 10;
    
    [Tooltip("Reload time in seconds")]
    public float reloadTime = 1.5f;
    
    [Tooltip("Weapon accuracy (0-1 where 1 is perfect accuracy)")]
    [Range(0, 1)]
    public float accuracy = 0.8f;
    
    [Tooltip("Description text")]
    [TextArea(3, 5)]
    public string description = "";
    
    // You can add more weapon stats and properties here
} 