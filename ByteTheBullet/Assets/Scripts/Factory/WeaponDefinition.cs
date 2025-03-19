using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName = "New Weapon";
    public Sprite weaponIcon;
    public int weaponID;
    public GameObject weaponPrefab;
    
    [Header("Primary Fire Mode")]
    public FireModeType primaryFireMode = FireModeType.SemiAuto;
    public ProjectileType primaryProjectileType = ProjectileType.Bullet;
    public GameObject primaryProjectilePrefab;
    public float primaryDamage = 10f;
    public float primaryFireRate = 0.1f;
    public float primaryProjectileSpeed = 20f;
    public int primaryMagazineSize = 30;
    public float primaryReloadTime = 1.5f;
    
    [Header("Secondary Fire Mode")]
    public bool hasSecondaryFire = false;
    public FireModeType secondaryFireMode = FireModeType.SemiAuto;
    public ProjectileType secondaryProjectileType = ProjectileType.Grenade;
    public GameObject secondaryProjectilePrefab;
    public float secondaryDamage = 30f;
    public float secondaryFireRate = 1.0f;
    public float secondaryProjectileSpeed = 10f;
    public int secondaryMagazineSize = 3;
    public float secondaryReloadTime = 2.0f;
    
    [Header("Burst Fire Settings")]
    [Range(2, 8)]
    public int burstSize = 3; // Number of bullets per burst
    [Range(0.05f, 0.3f)]
    public float burstFireRate = 0.1f; // Time between shots in a burst (spacing)
    [Range(0.2f, 2.0f)]
    public float burstCooldown = 0.5f; // Time between bursts
    public bool enableAutoBurst = true; // Whether holding trigger auto-fires bursts
    
    [Header("Audio")]
    public AudioClip primaryFireSound;
    public AudioClip primaryReloadSound;
    public AudioClip secondaryFireSound;
    public AudioClip secondaryReloadSound;
    public AudioClip emptySound;
    public AudioClip switchModeSound;
    
    [Header("Effects")]
    public GameObject muzzleFlashPrefab;
    public GameObject secondaryMuzzleFlashPrefab;
    public List<Sprite> bulletSprites = new List<Sprite>();

    public enum FireModeType
    {
        SemiAuto,   // One shot per click
        FullAuto,   // Continuous fire while holding
        Burst,      // Multiple shots in quick succession per click
        Charge      // Hold to charge, release to fire
    }
    
    public enum ProjectileType
    {
        Bullet,
        Grenade,
        Laser,
        Shotgun,
        Energy,
        Custom
    }
}
