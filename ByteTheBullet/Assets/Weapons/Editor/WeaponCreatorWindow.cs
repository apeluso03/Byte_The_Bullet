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
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.LabelField("Weapon Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Basic Properties Section
        EditorGUILayout.LabelField("Weapon Properties", EditorStyles.boldLabel);
        
        weaponName = EditorGUILayout.TextField("Weapon Name", weaponName);
        weaponSprite = (Sprite)EditorGUILayout.ObjectField("Weapon Sprite", weaponSprite, typeof(Sprite), false);
        weaponType = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", weaponType);
        
        // Draw rarity with color indicators
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
        
        // Add dropdown for damage type
        damageTypeIndex = EditorGUILayout.Popup("Damage Type", damageTypeIndex, damageTypes);
        
        // Stat sliders for metadata values
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Stats", EditorStyles.boldLabel);
        
        weaponDamage = EditorGUILayout.Slider("Damage", weaponDamage, 1f, 50f);
        weaponFireRate = EditorGUILayout.Slider("Fire Rate", weaponFireRate, 0.5f, 20f);
        weaponMagazineSize = EditorGUILayout.IntSlider("Magazine Size", weaponMagazineSize, 1, 100);
        weaponReloadTime = EditorGUILayout.Slider("Reload Time", weaponReloadTime, 0.5f, 5f);
        weaponAccuracy = EditorGUILayout.Slider("Accuracy", weaponAccuracy, 0f, 1f);
        
        EditorGUILayout.Space();
        
        // Automatic reference section
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
        
        EditorGUILayout.Space();
        
        // Creation Options
        EditorGUILayout.LabelField("Creation Options", EditorStyles.boldLabel);
        createPrefab = EditorGUILayout.Toggle("Create Prefab", createPrefab);
        
        EditorGUILayout.Space();
        
        // Preview section
        if (weaponSprite != null)
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            
            // Draw a box with the sprite preview
            Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));
            
            // Color the background based on rarity
            EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f, 1));
            
            // Calculate sprite dimensions preserving aspect ratio
            float spriteAspect = weaponSprite.rect.width / weaponSprite.rect.height;
            float previewAspect = previewRect.width / previewRect.height;
            
            Rect spriteRect;
            if (spriteAspect > previewAspect)
            {
                // Wider than tall, constrain width
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
                // Taller than wide, constrain height
                float width = previewRect.height * spriteAspect;
                spriteRect = new Rect(
                    previewRect.x + (previewRect.width - width) / 2,
                    previewRect.y,
                    width,
                    previewRect.height
                );
            }
            
            // Draw the sprite
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
        
        EditorGUILayout.Space();
        
        // Create button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create Weapon GameObject", GUILayout.Height(40)))
        {
            CreateWeaponObject();
        }
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
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
        
        // Add weapon pickup component
        try
        {
            // Try to find the weapon pickup component type
            System.Type pickupType = null;
            
            // First try with assembly-qualified type names
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                pickupType = assembly.GetType("WeaponPickupItem");
                if (pickupType != null) break;
                
                pickupType = assembly.GetType("WeaponPickup");
                if (pickupType != null) break;
            }
            
            // If still not found, try getting from a component in the scene
            if (pickupType == null)
            {
                var existingPickups = FindObjectsOfType<MonoBehaviour>();
                foreach (var component in existingPickups)
                {
                    if (component.GetType().Name == "WeaponPickupItem" || 
                        component.GetType().Name == "WeaponPickup")
                    {
                        pickupType = component.GetType();
                        break;
                    }
                }
            }
            
            if (pickupType != null)
            {
                // Add the pickup component
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
                Debug.LogWarning("Couldn't find WeaponPickupItem or WeaponPickup types in the project");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to add weapon pickup component: {e.Message}");
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
        
        // Generate a simple description based on properties
        metadata.description = $"A {weaponRarity.ToString().ToLower()} {weaponType.ToString().ToLower()} that deals {damageTypes[damageTypeIndex].ToLower()} damage.";
        
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