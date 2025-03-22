using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class WeaponCreatorWindow : EditorWindow
{
    // Weapon Properties
    private string weaponName = "NewWeapon";
    private Sprite weaponSprite;
    private WeaponType weaponType = WeaponType.Pistol;
    private WeaponRarity weaponRarity = WeaponRarity.Common;
    private WeaponFireType fireType = WeaponFireType.SemiAuto;
    
    // Default values for grip points (used internally, not shown in UI)
    private Vector2 rightHandGripPoint = new Vector2(0.2f, 0);
    private Vector2 leftHandGripPoint = new Vector2(-0.2f, 0);
    
    // Weapon stats
    private float weaponDamage = 10f;
    private float weaponFireRate = 5f;
    private int weaponMagazineSize = 10;
    private float weaponReloadTime = 1.5f;
    private float weaponAccuracy = 0.8f;
    
    // Creation options
    private bool createPrefab = true;
    
    // Scroll position
    private Vector2 scrollPosition;
    
    // Cached references for auto-setup
    private Transform playerTransform;
    private Transform leftHandPoint;
    private Transform rightHandPoint;
    private GameObject leftHandVisual;
    private GameObject rightHandVisual;
    
    // Damage type selection
    private int damageTypeIndex = 0;
    private readonly string[] damageTypes = new string[] { 
        "Physical", "Fire", "Ice", "Electric", "Poison", "Explosive" 
    };
    
    // Enums for weapon properties
    public enum WeaponType { Pistol, SMG, Shotgun, AssaultRifle, SniperRifle, Melee, Special }
    public enum WeaponRarity { Common, Uncommon, Rare, Epic, Legendary }
    public enum WeaponFireType { SemiAuto, FullAuto, Burst, Charged, Beam, Projectile }
    
    // Enum color mappings for rarity
    private static readonly Dictionary<WeaponRarity, Color> rarityColors = new Dictionary<WeaponRarity, Color>
    {
        { WeaponRarity.Common, Color.gray },
        { WeaponRarity.Uncommon, Color.green },
        { WeaponRarity.Rare, Color.blue },
        { WeaponRarity.Epic, new Color(0.5f, 0, 0.5f) }, // Purple
        { WeaponRarity.Legendary, Color.yellow }
    };
    
    // Open the window
    [MenuItem("Window/Weapon Creator")]
    public static void ShowWindow()
    {
        WeaponCreatorWindow window = GetWindow<WeaponCreatorWindow>("Weapon Creator");
        window.minSize = new Vector2(350, 500);
        window.Show();
        
        // Find references when opening the window
        window.FindReferences();
    }
    
    private void FindReferences()
    {
        // Find player
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length > 0)
        {
            playerTransform = players[0].transform;
            Debug.Log($"Found player: {playerTransform.name}");
            
            // Look for the exact weapon point transforms
            leftHandPoint = playerTransform.Find("WeaponPoint_Left");
            rightHandPoint = playerTransform.Find("WeaponPoint_Right");
            
            if (leftHandPoint == null || rightHandPoint == null)
            {
                // Try a more generic search if exact names aren't found
                foreach (Transform child in playerTransform)
                {
                    if (child.name.Contains("Left") && (child.name.Contains("Weapon") || child.name.Contains("Hand")))
                    {
                        leftHandPoint = child;
                        Debug.Log($"Found left weapon point: {leftHandPoint.name}");
                    }
                    else if (child.name.Contains("Right") && (child.name.Contains("Weapon") || child.name.Contains("Hand")))
                    {
                        rightHandPoint = child;
                        Debug.Log($"Found right weapon point: {rightHandPoint.name}");
                    }
                }
            }
            else
            {
                Debug.Log($"Found weapon points: Left={leftHandPoint.name}, Right={rightHandPoint.name}");
            }
            
            // For hand visuals, use the same GameObjects as the weapon points
            if (leftHandPoint != null)
            {
                leftHandVisual = leftHandPoint.gameObject;
                Debug.Log($"Using left hand point as left hand visual: {leftHandVisual.name}");
            }
            
            if (rightHandPoint != null)
            {
                rightHandVisual = rightHandPoint.gameObject;
                Debug.Log($"Using right hand point as right hand visual: {rightHandVisual.name}");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Player' found in the scene.");
        }
    }
    
    private void OnGUI()
    {
        // Completely disconnect the GUI operations from the creation method
        // to avoid the possibility of layout group errors
        Event e = Event.current;
        
        // Use a simpler layout approach
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.LabelField("Weapon Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // ---- Basic Properties ----
        EditorGUILayout.LabelField("Weapon Properties", EditorStyles.boldLabel);
        weaponName = EditorGUILayout.TextField("Weapon Name", weaponName);
        weaponSprite = (Sprite)EditorGUILayout.ObjectField("Weapon Sprite", weaponSprite, typeof(Sprite), false);
        weaponType = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weaponType);
        
        // Rarity with color indicators
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Weapon Rarity");
        
        Color originalColor = GUI.backgroundColor;
        for (int i = 0; i < System.Enum.GetValues(typeof(WeaponRarity)).Length; i++)
        {
            WeaponRarity rarity = (WeaponRarity)i;
            GUI.backgroundColor = rarityColors[rarity];
            
            if (GUILayout.Toggle(weaponRarity == rarity, rarity.ToString(), EditorStyles.miniButton))
            {
                weaponRarity = rarity;
            }
        }
        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndHorizontal();
        
        fireType = (WeaponFireType)EditorGUILayout.EnumPopup("Fire Type", fireType);
        damageTypeIndex = EditorGUILayout.Popup("Damage Type", damageTypeIndex, damageTypes);
        
        // ---- Weapon Stats ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Stats", EditorStyles.boldLabel);
        
        weaponDamage = EditorGUILayout.Slider("Damage", weaponDamage, 1f, 50f);
        weaponFireRate = EditorGUILayout.Slider("Fire Rate", weaponFireRate, 0.5f, 20f);
        weaponMagazineSize = EditorGUILayout.IntSlider("Magazine Size", weaponMagazineSize, 1, 100);
        weaponReloadTime = EditorGUILayout.Slider("Reload Time", weaponReloadTime, 0.5f, 5f);
        weaponAccuracy = EditorGUILayout.Slider("Accuracy", weaponAccuracy, 0f, 1f);
        
        // ---- References ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-References", EditorStyles.boldLabel);
        
        playerTransform = (Transform)EditorGUILayout.ObjectField("Player", playerTransform, typeof(Transform), true);
        leftHandPoint = (Transform)EditorGUILayout.ObjectField("Left Hand Point", leftHandPoint, typeof(Transform), true);
        rightHandPoint = (Transform)EditorGUILayout.ObjectField("Right Hand Point", rightHandPoint, typeof(Transform), true);
        leftHandVisual = (GameObject)EditorGUILayout.ObjectField("Left Hand Visual", leftHandVisual, typeof(GameObject), true);
        rightHandVisual = (GameObject)EditorGUILayout.ObjectField("Right Hand Visual", rightHandVisual, typeof(GameObject), true);
        
        if (GUILayout.Button("Re-scan for References"))
        {
            FindReferences();
        }
        
        // ---- Creation Options ----
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Creation Options", EditorStyles.boldLabel);
        createPrefab = EditorGUILayout.Toggle("Create Prefab", createPrefab);
        
        // ---- Preview ----
        if (weaponSprite != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            
            Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f, 1));
            
            // Draw sprite with proper aspect ratio
            float spriteAspect = weaponSprite.rect.width / weaponSprite.rect.height;
            float previewAspect = previewRect.width / previewRect.height;
            
            Rect spriteRect;
            if (spriteAspect > previewAspect)
            {
                float height = previewRect.width / spriteAspect;
                spriteRect = new Rect(
                    previewRect.x,
                    previewRect.y + (previewRect.height - height) / 2,
                    previewRect.width,
                    height
                );
            }
            else
            {
                float width = previewRect.height * spriteAspect;
                spriteRect = new Rect(
                    previewRect.x + (previewRect.width - width) / 2,
                    previewRect.y,
                    width,
                    previewRect.height
                );
            }
            
            GUI.DrawTextureWithTexCoords(
                spriteRect,
                weaponSprite.texture,
                new Rect(
                    weaponSprite.rect.x / weaponSprite.texture.width,
                    weaponSprite.rect.y / weaponSprite.texture.height,
                    weaponSprite.rect.width / weaponSprite.texture.width,
                    weaponSprite.rect.height / weaponSprite.texture.height
                )
            );
        }
        
        // End scroll view
        EditorGUILayout.EndScrollView();
        
        // Create button, outside of scroll view
        GUI.backgroundColor = Color.green;
        bool createPressed = GUILayout.Button("Create Weapon GameObject", GUILayout.Height(40));
        GUI.backgroundColor = originalColor;
        
        // End vertical layout
        EditorGUILayout.EndVertical();
        
        // Handle create button click after all GUI layout is done
        if (createPressed)
        {
            // Delay the creation to avoid interfering with GUI layout
            EditorApplication.delayCall += () => {
                CreateWeaponObject();
            };
        }
    }
    
    private void CreateWeaponObject()
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(weaponName))
        {
            EditorUtility.DisplayDialog("Error", "Weapon name cannot be empty.", "OK");
            return;
        }
        
        if (weaponSprite == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a weapon sprite.", "OK");
            return;
        }
        
        // Create the weapon GameObject
        GameObject weaponObject = new GameObject(weaponName);
        
        // Add necessary components
        SpriteRenderer spriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = weaponSprite;
        spriteRenderer.sortingLayerName = "InFrontOfPlayer";
        
        // Add BoxCollider2D (trigger)
        BoxCollider2D boxCollider = weaponObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = spriteRenderer.sprite.bounds.size;
        
        // Add WeaponAiming component with auto-references
        WeaponAiming weaponAiming = weaponObject.AddComponent<WeaponAiming>();
        weaponAiming.rightHandGripPoint = rightHandGripPoint;
        weaponAiming.leftHandGripPoint = leftHandGripPoint;
        
        // Set up references automatically
        if (playerTransform != null)
            weaponAiming.player = playerTransform;
            
        if (leftHandPoint != null)
        {
            weaponAiming.weaponPointLeft = leftHandPoint;
            weaponAiming.leftHandVisual = leftHandVisual;
        }
            
        if (rightHandPoint != null)
        {
            weaponAiming.weaponPointRight = rightHandPoint;
            weaponAiming.rightHandVisual = rightHandVisual;
        }
        
        // Add weapon metadata component with our values
        WeaponMetadata metadata = weaponObject.AddComponent<WeaponMetadata>();
        metadata.weaponName = weaponName;
        metadata.weaponType = weaponType.ToString();
        metadata.rarity = weaponRarity.ToString();
        metadata.fireType = fireType.ToString();
        metadata.damageType = damageTypes[damageTypeIndex];
        
        // Set stats
        metadata.damage = weaponDamage;
        metadata.fireRate = weaponFireRate;
        metadata.magazineSize = weaponMagazineSize;
        metadata.reloadTime = weaponReloadTime;
        metadata.accuracy = weaponAccuracy;
        
        // Add appropriate shooting component based on weapon type
        switch (weaponType)
        {
            case WeaponType.Shotgun:
                // Add only the ShotgunShooting component (not both)
                ShotgunShooting shotgun = weaponObject.AddComponent<ShotgunShooting>();
                ConfigureShotgun(shotgun, metadata);
                break;
            
            case WeaponType.Pistol:
            case WeaponType.SMG:
            case WeaponType.AssaultRifle:
            case WeaponType.SniperRifle:
            default:
                // Use base weapon shooting for other types
                WeaponShooting shooting = weaponObject.AddComponent<WeaponShooting>();
                ConfigureBaseWeapon(shooting, metadata);
                break;
        }
        
        // Generate a simple description based on properties
        metadata.description = $"A {weaponRarity.ToString().ToLower()} {weaponType.ToString().ToLower()} that deals {damageTypes[damageTypeIndex].ToLower()} damage.";
        
        // Add weapon pickup component
        try
        {
            // Check if SimpleWeaponPickup exists in the project
            System.Type pickupType = System.Type.GetType("SimpleWeaponPickup");
            
            if (pickupType == null)
            {
                // Try looking in all loaded assemblies
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    pickupType = assembly.GetType("SimpleWeaponPickup");
                    if (pickupType != null) break;
                }
            }
            
            if (pickupType != null)
            {
                // Add the pickup component if found
                Component pickup = weaponObject.AddComponent(pickupType);
                Debug.Log($"Added pickup component of type: {pickupType.Name}");
                
                // Try to set the weaponPrefab field using reflection
                var field = pickupType.GetField("weaponPrefab");
                if (field != null)
                {
                    // Will be set to the prefab later
                    field.SetValue(pickup, null);
                }
            }
            else
            {
                // Create a simple weapon pickup component if you want
                // or just skip this if you don't need it yet
                Debug.Log("No weapon pickup component found. Skipping pickup setup.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to add weapon pickup component: {e.Message}");
        }
        
        // Position the weapon in the scene view
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            weaponObject.transform.position = sceneView.camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10));
            weaponObject.transform.position = new Vector3(
                weaponObject.transform.position.x,
                weaponObject.transform.position.y,
                0
            );
        }
        
        // Select the created GameObject
        Selection.activeGameObject = weaponObject;
        
        // Create prefab if requested
        if (createPrefab)
        {
            // Determine the save path
            string path = "Assets/Weapons/Prefabs";
            
            // Create directories if they don't exist
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Weapons")))
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Weapons"));
                
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Weapons/Prefabs")))
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Weapons/Prefabs"));
            
            // Create prefab
            string prefabPath = Path.Combine(path, $"{weaponName}.prefab");
            
            // Create the prefab
            #if UNITY_2018_3_OR_NEWER
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(weaponObject, prefabPath);
            #else
            GameObject prefab = PrefabUtility.CreatePrefab(prefabPath, weaponObject);
            #endif
            
            // Try to link the pickup component to the prefab
            try
            {
                // Check for either pickup component type
                Component pickup = null;
                Component[] components = weaponObject.GetComponents<Component>();
                foreach (Component comp in components)
                {
                    if (comp.GetType().Name == "WeaponPickupItem" || comp.GetType().Name == "WeaponPickup")
                    {
                        pickup = comp;
                        break;
                    }
                }
                
                if (pickup != null)
                {
                    // Try to set the weaponPrefab field using reflection
                    var field = pickup.GetType().GetField("weaponPrefab");
                    if (field != null)
                    {
                        field.SetValue(pickup, weaponAiming);
                        
                        // Update the prefab with the new reference
                        #if UNITY_2018_3_OR_NEWER
                        PrefabUtility.SaveAsPrefabAsset(weaponObject, prefabPath);
                        #else
                        PrefabUtility.ReplacePrefab(weaponObject, prefab);
                        #endif
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to set weaponPrefab field: {e.Message}");
            }
            
            EditorUtility.DisplayDialog("Success", $"Weapon '{weaponName}' created and saved as a prefab!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Success", $"Weapon '{weaponName}' created!", "OK");
        }
        
        // Close the window
        Close();
    }
    
    // Helper method to configure shotgun-specific settings
    private void ConfigureShotgun(ShotgunShooting shotgun, WeaponMetadata metadata)
    {
        // Create a fire point
        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(shotgun.transform);
        firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
        shotgun.firePoint = firePointObj.transform;
        
        // Set up shotgun specific settings based on metadata
        shotgun.projectileSpeed = 15f; // Slightly slower for shotgun pellets
        
        // Adjust pellet count based on weapon type variant
        if (metadata.weaponType.Contains("Sawed"))
        {
            shotgun.pelletCount = 12;      // More pellets for sawed-off
            shotgun.spreadAngle = 30f;     // Wider spread
        }
        else
        {
            shotgun.pelletCount = 8;       // Default pellet count
            shotgun.spreadAngle = Mathf.Lerp(45f, 15f, metadata.accuracy); // Adjust spread based on accuracy
        }
        
        // Set firing properties based on the fire type
        switch (fireType)
        {
            case WeaponFireType.SemiAuto:
                shotgun.fireMode = ShotgunShooting.FireMode.SemiAuto;
                shotgun.semiAutoFireDelay = 0.8f;
                break;
            
            case WeaponFireType.FullAuto:
                shotgun.fireMode = ShotgunShooting.FireMode.FullAuto;
                shotgun.fullAutoFireRate = 5.0f; // Increased to 5 shots per second for better full-auto feel
                break;
            
            case WeaponFireType.Burst:
                shotgun.fireMode = ShotgunShooting.FireMode.Burst;
                shotgun.burstCount = 2;
                shotgun.burstDelay = 0.2f;
                shotgun.burstCooldown = 0.8f;
                break;
            
            case WeaponFireType.Charged:
                shotgun.fireMode = ShotgunShooting.FireMode.Charged;
                shotgun.chargeTime = 1.5f;
                shotgun.maxChargedPelletCount = 16;
                shotgun.chargedShotCooldown = 1.2f;
                break;
            
            default:
                shotgun.fireMode = ShotgunShooting.FireMode.SemiAuto;
                shotgun.semiAutoFireDelay = 0.8f;
                break;
        }
        
        // Try to locate common projectile prefabs in the project
        if (shotgun.projectilePrefab == null)
        {
            // Look for projectile assets in common folders
            string[] pelletGuids = AssetDatabase.FindAssets("t:Prefab Pellet");
            if (pelletGuids.Length > 0)
            {
                string pelletPath = AssetDatabase.GUIDToAssetPath(pelletGuids[0]);
                shotgun.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pelletPath);
            }
            else
            {
                string[] bulletGuids = AssetDatabase.FindAssets("t:Prefab Bullet");
                if (bulletGuids.Length > 0)
                {
                    string bulletPath = AssetDatabase.GUIDToAssetPath(bulletGuids[0]);
                    shotgun.projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
                }
            }
        }
        
        // Try to locate shell casing prefab
        string[] shellGuids = AssetDatabase.FindAssets("t:Prefab Shell");
        if (shellGuids.Length > 0)
        {
            string shellPath = AssetDatabase.GUIDToAssetPath(shellGuids[0]);
            shotgun.shellCasingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shellPath);
        }
        
        // Try to locate sound effects
        string[] pumpGuids = AssetDatabase.FindAssets("t:AudioClip Pump");
        if (pumpGuids.Length > 0)
        {
            string pumpPath = AssetDatabase.GUIDToAssetPath(pumpGuids[0]);
            shotgun.pumpSound = AssetDatabase.LoadAssetAtPath<AudioClip>(pumpPath);
        }
        
        // Try to locate charge sounds if using charged mode
        if (shotgun.fireMode == ShotgunShooting.FireMode.Charged)
        {
            string[] chargeGuids = AssetDatabase.FindAssets("t:AudioClip Charge");
            if (chargeGuids.Length > 0)
            {
                string chargePath = AssetDatabase.GUIDToAssetPath(chargeGuids[0]);
                shotgun.chargeSound = AssetDatabase.LoadAssetAtPath<AudioClip>(chargePath);
            }
            
            string[] releaseGuids = AssetDatabase.FindAssets("t:AudioClip Release");
            if (releaseGuids.Length > 0)
            {
                string releasePath = AssetDatabase.GUIDToAssetPath(releaseGuids[0]);
                shotgun.chargeReleaseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(releasePath);
            }
        }
    }
    
    // Helper method to configure base weapon shooting component
    private void ConfigureBaseWeapon(WeaponShooting shooting, WeaponMetadata metadata)
    {
        // Create a fire point
        GameObject firePointObj = new GameObject("FirePoint");
        firePointObj.transform.SetParent(shooting.transform);
        firePointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
        shooting.firePoint = firePointObj.transform;
        
        // Configure based on weapon type
        switch (metadata.weaponType)
        {
            case "Pistol":
                shooting.projectileSpeed = 20f;
                break;
            case "SMG":
                shooting.projectileSpeed = 25f;
                break;
            case "AssaultRifle":
                shooting.projectileSpeed = 30f;
                break;
            case "SniperRifle":
                shooting.projectileSpeed = 40f;
                break;
            default:
                shooting.projectileSpeed = 20f;
                break;
        }
        
        // Set fire rate from metadata
        shooting.fireRate = metadata.fireRate;
    }
    
    // Make sure WeaponMetadata exists
    [InitializeOnLoadMethod]
    static void EnsureWeaponMetadataExists()
    {
        // Check if WeaponMetadata script exists
        var type = System.Type.GetType("WeaponMetadata");
        if (type == null)
        {
            // Create the script
            string path = "Assets/Weapons/Scripts";
            if (!Directory.Exists(Path.Combine(Application.dataPath, "Weapons/Scripts")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Weapons/Scripts"));
            }
            
            string scriptPath = Path.Combine(path, "WeaponMetadata.cs");
            if (!File.Exists(Path.Combine(Application.dataPath.Replace("Assets", ""), scriptPath)))
            {
                string script = @"using UnityEngine;

[DisallowMultipleComponent]
public class WeaponMetadata : MonoBehaviour
{
    public string weaponName = ""Unnamed Weapon"";
    public string weaponType = ""Pistol"";
    public string rarity = ""Common"";
    public string fireType = ""SemiAuto"";
    public string damageType = ""Physical"";
    
    [Tooltip(""Weapon damage amount"")]
    public float damage = 10f;
    
    [Tooltip(""Fire rate in rounds per second"")]
    public float fireRate = 5f;
    
    [Tooltip(""Magazine size (bullets before reload)"")]
    public int magazineSize = 10;
    
    [Tooltip(""Reload time in seconds"")]
    public float reloadTime = 1.5f;
    
    [Tooltip(""Weapon accuracy (0-1 where 1 is perfect accuracy)"")]
    [Range(0, 1)]
    public float accuracy = 0.8f;
    
    [Tooltip(""Description text"")]
    [TextArea(3, 5)]
    public string description = """";
}";
                File.WriteAllText(Path.Combine(Application.dataPath.Replace("Assets", ""), scriptPath), script);
                AssetDatabase.Refresh();
            }
        }
    }
} 