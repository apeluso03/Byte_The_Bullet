using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class WeaponPrefabCreator : EditorWindow
{
    private WeaponDefinition weaponDefinition;
    private string savePath = "Assets/Prefabs/Weapons/";
    private bool createShotgun = false;
    
    [MenuItem("Weapons/Create Weapon Prefab")]
    public static void ShowWindow()
    {
        GetWindow<WeaponPrefabCreator>("Weapon Prefab Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Create Weapon Prefab From Definition", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        weaponDefinition = (WeaponDefinition)EditorGUILayout.ObjectField(
            "Weapon Definition", 
            weaponDefinition, 
            typeof(WeaponDefinition), 
            false);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Specialized Weapon Types", EditorStyles.boldLabel);
        createShotgun = EditorGUILayout.Toggle("Create as Shotgun", createShotgun);
            
        EditorGUILayout.Space();
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Create Prefab") && weaponDefinition != null)
        {
            CreateWeaponPrefab();
        }
    }
    
    void CreateWeaponPrefab()
    {
        // Create the base GameObject
        GameObject weaponObject = new GameObject(weaponDefinition.weaponName);
        
        // Add required components
        SpriteRenderer spriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
        if (weaponDefinition.weaponIcon != null)
        {
            spriteRenderer.sprite = weaponDefinition.weaponIcon;
        }
        
        // Add appropriate weapon component based on type
        if (createShotgun)
        {
            ShotgunWeapon shotgun = weaponObject.AddComponent<ShotgunWeapon>();
            ConfigureShotgunWeapon(shotgun, weaponDefinition);
        }
        else if (weaponDefinition.hasSecondaryFire)
        {
            ModularDualModeWeapon dualWeapon = weaponObject.AddComponent<ModularDualModeWeapon>();
            ConfigureDualModeWeapon(dualWeapon, weaponDefinition);
        }
        else if (weaponDefinition.primaryFireMode == WeaponDefinition.FireModeType.SemiAuto)
        {
            // For semi-auto, use our strict class that guarantees single-shot behavior
            StrictSemiAutoWeapon semiWeapon = weaponObject.AddComponent<StrictSemiAutoWeapon>();
            ConfigureStrictSemiAutoWeapon(semiWeapon, weaponDefinition);
        }
        else if (weaponDefinition.primaryFireMode == WeaponDefinition.FireModeType.Burst)
        {
            // For burst, use our strict class that guarantees proper burst behavior
            StrictBurstWeapon burstWeapon = weaponObject.AddComponent<StrictBurstWeapon>();
            ConfigureStrictBurstWeapon(burstWeapon, weaponDefinition);
        }
        else
        {
            ModularSingleModeWeapon singleWeapon = weaponObject.AddComponent<ModularSingleModeWeapon>();
            ConfigureSingleModeWeapon(singleWeapon, weaponDefinition);
        }
        
        // Add weapon follower
        WeaponFollower follower = weaponObject.AddComponent<WeaponFollower>();
        follower.positionOffset = new Vector3(0.4f, 0f, 0f);
        follower.leftPositionOffset = new Vector3(-0.4f, 0f, 0f);
        
        // Add audio source
        AudioSource audioSource = weaponObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        // Create muzzle point
        GameObject muzzleObj = new GameObject("MuzzlePoint");
        muzzleObj.transform.SetParent(weaponObject.transform);
        muzzleObj.transform.localPosition = new Vector3(0.5f, 0.05f, 0);
        
        // Add a collider
        BoxCollider2D collider = weaponObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        
        // Save prefab
        if (!System.IO.Directory.Exists(savePath))
        {
            System.IO.Directory.CreateDirectory(savePath);
        }
        
        string prefabPath = savePath + weaponDefinition.weaponName + ".prefab";
        
        // Create the prefab
        #if UNITY_2018_3_OR_NEWER
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(weaponObject, prefabPath);
        #else
        GameObject prefab = PrefabUtility.CreatePrefab(prefabPath, weaponObject);
        #endif
        
        // Cleanup temporary object
        DestroyImmediate(weaponObject);
        
        // Select the new prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log("Created prefab at: " + prefabPath);
    }
    
    private void ConfigureSingleModeWeapon(ModularSingleModeWeapon weapon, WeaponDefinition definition)
    {
        // Set basic info
        weapon.weaponName = definition.weaponName;
        weapon.weaponIcon = definition.weaponIcon;
        weapon.weaponID = definition.weaponID;
        
        // Set primary fire properties
        weapon.fireRate = definition.primaryFireRate;
        weapon.magazineSize = definition.primaryMagazineSize;
        weapon.currentAmmo = definition.primaryMagazineSize;
        weapon.bulletSpeed = definition.primaryProjectileSpeed;
        weapon.bulletDamage = definition.primaryDamage;
        weapon.reloadTime = definition.primaryReloadTime;
        
        // Set projectile prefab
        weapon.bulletPrefab = definition.primaryProjectilePrefab;
        
        // Set audio
        weapon.bulletFireSound = definition.primaryFireSound;
        weapon.bulletReloadSound = definition.primaryReloadSound;
        weapon.emptySound = definition.emptySound;
        
        // Set effects
        weapon.muzzleFlashPrefab = definition.muzzleFlashPrefab;
        weapon.bulletSprites = definition.bulletSprites;
        
        // Set fire mode
        weapon.fireMode = (ModularSingleModeWeapon.FireMode)definition.primaryFireMode;
    }
    
    private void ConfigureDualModeWeapon(ModularDualModeWeapon weapon, WeaponDefinition definition)
    {
        // Set basic info
        weapon.weaponName = definition.weaponName;
        weapon.weaponIcon = definition.weaponIcon;
        weapon.weaponID = definition.weaponID;
        
        // Set primary fire properties
        weapon.fireRate = definition.primaryFireRate;
        weapon.magazineSize = definition.primaryMagazineSize;
        weapon.currentAmmo = definition.primaryMagazineSize;
        weapon.bulletSpeed = definition.primaryProjectileSpeed;
        weapon.bulletDamage = definition.primaryDamage;
        weapon.primaryReloadTime = definition.primaryReloadTime;
        
        // Set secondary fire properties
        weapon.hasSecondaryFire = definition.hasSecondaryFire;
        weapon.secondaryFireRate = definition.secondaryFireRate;
        weapon.secondaryMagazineSize = definition.secondaryMagazineSize;
        weapon.secondaryAmmo = definition.secondaryMagazineSize;
        weapon.secondaryProjectileSpeed = definition.secondaryProjectileSpeed;
        weapon.secondaryDamage = definition.secondaryDamage;
        weapon.secondaryReloadTime = definition.secondaryReloadTime;
        
        // Set projectile prefabs
        weapon.bulletPrefab = definition.primaryProjectilePrefab;
        weapon.secondaryProjectilePrefab = definition.secondaryProjectilePrefab;
        
        // Set audio
        weapon.primaryFireSound = definition.primaryFireSound;
        weapon.primaryReloadSound = definition.primaryReloadSound;
        weapon.secondaryFireSound = definition.secondaryFireSound;
        weapon.secondaryReloadSound = definition.secondaryReloadSound;
        weapon.emptySound = definition.emptySound;
        weapon.switchModeSound = definition.switchModeSound;
        
        // Set effects
        weapon.primaryMuzzleFlashPrefab = definition.muzzleFlashPrefab;
        weapon.secondaryMuzzleFlashPrefab = definition.secondaryMuzzleFlashPrefab;
        weapon.bulletSprites = definition.bulletSprites;
        
        // Set fire modes
        weapon.primaryFireMode = (ModularDualModeWeapon.FireMode)definition.primaryFireMode;
        weapon.secondaryFireMode = (ModularDualModeWeapon.FireMode)definition.secondaryFireMode;
    }
    
    private void ConfigureShotgunWeapon(ShotgunWeapon weapon, WeaponDefinition definition)
    {
        // Configure base properties first
        ConfigureSingleModeWeapon(weapon, definition);
        
        // Add shotgun-specific properties
        weapon.pelletsPerShot = 8;
        weapon.spreadAngle = 30f;
        
        // Set fire mode - respect the definition's fire mode
        weapon.fireMode = (ModularSingleModeWeapon.FireMode)definition.primaryFireMode;
        
        // If it's a double barrel, adjust magazine size
        if (definition.weaponName.ToLower().Contains("double") || 
            definition.weaponName.ToLower().Contains("barrel"))
        {
            weapon.magazineSize = 2;
            weapon.currentAmmo = 2;
        }
        
        // Configure special fire modes
        if (definition.primaryFireMode == WeaponDefinition.FireModeType.Burst)
        {
            weapon.burstCount = definition.burstSize;
            weapon.burstDelay = definition.burstFireRate;
            // Also apply cooldown if the shotgun implements it
            if (weapon.GetType().GetField("burstCooldown") != null)
            {
                weapon.GetType().GetField("burstCooldown").SetValue(weapon, definition.burstCooldown);
            }
        }
        else if (definition.primaryFireMode == WeaponDefinition.FireModeType.Charge)
        {
            weapon.maxChargeTime = 1.5f;
            weapon.minChargeTime = 0.3f;
            weapon.maxPelletMultiplier = 2.0f;
            weapon.maxDamageMultiplier = 1.5f;
        }
    }

    private void ConfigureStrictSemiAutoWeapon(StrictSemiAutoWeapon weapon, WeaponDefinition definition)
    {
        // Set basic info
        weapon.weaponName = definition.weaponName;
        weapon.weaponIcon = definition.weaponIcon;
        weapon.weaponID = definition.weaponID;
        
        // Set fire properties
        weapon.fireRate = definition.primaryFireRate;
        weapon.magazineSize = definition.primaryMagazineSize;
        weapon.currentAmmo = definition.primaryMagazineSize;
        weapon.bulletSpeed = definition.primaryProjectileSpeed;
        weapon.bulletDamage = definition.primaryDamage;
        weapon.reloadTime = definition.primaryReloadTime;
        
        // Set projectile
        weapon.bulletPrefab = definition.primaryProjectilePrefab;
        
        // Set audio
        weapon.fireSound = definition.primaryFireSound;
        weapon.reloadSound = definition.primaryReloadSound;
        weapon.emptySound = definition.emptySound;
        
        // Set effects
        weapon.muzzleFlashPrefab = definition.muzzleFlashPrefab;
        weapon.bulletSprites = definition.bulletSprites;
    }

    private void ConfigureStrictBurstWeapon(StrictBurstWeapon weapon, WeaponDefinition definition)
    {
        // Add debug logging
        Debug.Log("Configuring StrictBurstWeapon: " + definition.weaponName);
        
        // Set basic info
        weapon.weaponName = definition.weaponName;
        weapon.weaponIcon = definition.weaponIcon;
        weapon.weaponID = definition.weaponID;
        
        // Set fire properties
        weapon.fireRate = definition.primaryFireRate;
        weapon.magazineSize = definition.primaryMagazineSize;
        weapon.currentAmmo = definition.primaryMagazineSize;
        weapon.bulletSpeed = definition.primaryProjectileSpeed;
        weapon.bulletDamage = definition.primaryDamage;
        weapon.reloadTime = definition.primaryReloadTime;
        
        // Set burst properties with safeguards
        weapon.burstSize = Mathf.Clamp(definition.burstSize, 2, 8);
        weapon.burstFireRate = definition.burstFireRate;
        weapon.burstCooldown = definition.burstCooldown;
        weapon.enableAutoBurst = definition.enableAutoBurst;
        
        Debug.Log("Setting burstSize to: " + weapon.burstSize);
        Debug.Log("Setting burstFireRate to: " + weapon.burstFireRate);
        Debug.Log("Setting burstCooldown to: " + weapon.burstCooldown);
        Debug.Log("Auto-Burst enabled: " + weapon.enableAutoBurst);
        
        // Set projectile
        weapon.bulletPrefab = definition.primaryProjectilePrefab;
        
        // Set audio
        weapon.bulletFireSound = definition.primaryFireSound;
        weapon.reloadSound = definition.primaryReloadSound;
        weapon.emptySound = definition.emptySound;
        
        // Set effects
        weapon.muzzleFlashPrefab = definition.muzzleFlashPrefab;
        
        // Set muzzle point if needed
        if (weapon.muzzlePoint == null)
        {
            // Find or create muzzle point
            Transform muzzlePoint = weapon.transform.Find("MuzzlePoint");
            if (muzzlePoint == null)
            {
                GameObject muzzleObj = new GameObject("MuzzlePoint");
                muzzleObj.transform.SetParent(weapon.transform);
                muzzleObj.transform.localPosition = new Vector3(0.5f, 0.05f, 0);
                weapon.muzzlePoint = muzzleObj.transform;
            }
            else
            {
                weapon.muzzlePoint = muzzlePoint;
            }
        }
    }

    public void SelectWeaponDefinition(WeaponDefinition definition)
    {
        weaponDefinition = definition;
    }
}
#endif