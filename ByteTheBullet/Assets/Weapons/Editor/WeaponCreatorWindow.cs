using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Weapons;

public class WeaponCreatorWindow : EditorWindow
{
    // Basic settings
    private string weaponName = "New Shotgun";
    
    // Weapon type selection
    private string[] weaponTypes = new string[] { "Shotgun" };
    private int selectedWeaponType = 0;
    
    // Rarity dropdown options
    private string[] rarityOptions = new string[] { 
        "Common", "Uncommon", "Rare", "Epic", "Legendary", "Unique" 
    };
    private int selectedRarity = 0;
    
    // Colors for different rarities
    private Color[] rarityColors = new Color[] {
        new Color(0.7f, 0.7f, 0.7f),     // Common - Gray
        new Color(0.3f, 0.8f, 0.3f),     // Uncommon - Green
        new Color(0.3f, 0.5f, 1.0f),     // Rare - Blue
        new Color(0.8f, 0.3f, 0.9f),     // Epic - Purple
        new Color(1.0f, 0.8f, 0.0f),     // Legendary - Gold
        new Color(1.0f, 0.4f, 0.0f)      // Unique - Orange
    };
    
    // Description
    private string weaponDescription = "A powerful shotgun with a wide spread.";
    
    // Visuals
    private Sprite weaponSprite;
    private bool addCollider = true;
    
    // Auto-reference settings
    private bool autoFindReferences = true;
    private GameObject playerGameObject;
    
    [MenuItem("Tools/Weapons/Weapon Creator")]
    public static void ShowWindow()
    {
        GetWindow<WeaponCreatorWindow>("Weapon Creator");
    }
    
    private void OnEnable()
    {
        // Try to find player
        if (playerGameObject == null && autoFindReferences)
        {
            playerGameObject = GameObject.FindGameObjectWithTag("Player");
            if (playerGameObject == null)
            {
                playerGameObject = GameObject.Find("Player");
            }
        }
    }
    
    private void OnGUI()
    {
        // Header
        EditorGUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 18;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Weapon Creator", headerStyle, GUILayout.Height(30));
        EditorGUILayout.Space(10);
        
        // Draw sections with EditorGUILayout.BeginFoldoutHeaderGroup for better visual
        EditorGUILayout.LabelField("Weapon Basics", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        // Weapon name field
        weaponName = EditorGUILayout.TextField("Weapon Name", weaponName);
        
        // Weapon type selection
        selectedWeaponType = EditorGUILayout.Popup("Weapon Type", selectedWeaponType, weaponTypes);
        
        // Rarity dropdown with color indication
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Rarity");
        GUI.backgroundColor = rarityColors[selectedRarity];
        selectedRarity = EditorGUILayout.Popup(selectedRarity, rarityOptions);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Description field
        EditorGUILayout.PrefixLabel("Description");
        weaponDescription = EditorGUILayout.TextArea(weaponDescription, GUILayout.Height(60));
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // Visual settings
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        // Sprite selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Weapon Sprite");
        weaponSprite = (Sprite)EditorGUILayout.ObjectField(
            weaponSprite, typeof(Sprite), false, GUILayout.Width(100), GUILayout.Height(100));
        EditorGUILayout.EndHorizontal();
        
        // Add collider option
        addCollider = EditorGUILayout.Toggle("Add Collider", addCollider);
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
        
        // Reference settings
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        autoFindReferences = EditorGUILayout.Toggle("Auto-Find References", autoFindReferences);
        
        if (!autoFindReferences)
        {
            playerGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Player GameObject", playerGameObject, typeof(GameObject), true);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Found Player");
            
            if (playerGameObject != null)
            {
                EditorGUILayout.LabelField(playerGameObject.name);
                
                if (GUILayout.Button("Highlight", GUILayout.Width(80)))
                {
                    Selection.activeGameObject = playerGameObject;
                    EditorGUIUtility.PingObject(playerGameObject);
                }
            }
            else
            {
                EditorGUILayout.LabelField("None - Will search at creation");
                
                if (GUILayout.Button("Find Now", GUILayout.Width(80)))
                {
                    playerGameObject = GameObject.FindGameObjectWithTag("Player");
                    if (playerGameObject == null)
                    {
                        playerGameObject = GameObject.Find("Player");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(20);
        
        // Create button
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("CREATE WEAPON", GUILayout.Height(40)))
        {
            CreateWeapon();
        }
        GUI.backgroundColor = Color.white;
    }
    
    private void CreateWeapon()
    {
        if (string.IsNullOrEmpty(weaponName))
        {
            EditorUtility.DisplayDialog("Error", "Weapon name cannot be empty", "OK");
            return;
        }
        
        if (weaponSprite == null)
        {
            if (!EditorUtility.DisplayDialog("Missing Sprite", 
                "You haven't assigned a sprite. Continue anyway?", "Yes", "No"))
            {
            return;
            }
        }
        
        // Try to find player if it's null and auto-find is enabled
        if (playerGameObject == null && autoFindReferences)
        {
            playerGameObject = GameObject.FindGameObjectWithTag("Player");
            if (playerGameObject == null)
            {
                playerGameObject = GameObject.Find("Player");
            }
        }
        
        switch (selectedWeaponType)
        {
            case 0: // Shotgun
                CreateShotgunWeapon();
                break;
            default:
                Debug.LogError("Unknown weapon type selected");
                break;
        }
    }
    
    private void CreateShotgunWeapon()
    {
        // Create the weapon GameObject
        GameObject weaponObject = new GameObject(weaponName);
        
        // Add a sprite renderer if we have a sprite
        if (weaponSprite != null)
        {
            SpriteRenderer spriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = weaponSprite;
            spriteRenderer.sortingLayerName = "InFrontOfPlayer";
            spriteRenderer.sortingOrder = 10;
            
            // Apply rarity color tint
            spriteRenderer.color = Color.Lerp(Color.white, rarityColors[selectedRarity], 0.3f);
            
            // Add a collider if required
            if (addCollider)
            {
                BoxCollider2D collider = weaponObject.AddComponent<BoxCollider2D>();
                // Size the collider to match the sprite
                collider.size = weaponSprite.bounds.size;
            }
        }
        
        // Add audio source (required by BaseWeapon)
        AudioSource audioSource = weaponObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Add the weapon aiming component
        WeaponAiming aiming = weaponObject.AddComponent<WeaponAiming>();
        
        // Set up weapon aiming references if possible
        if (playerGameObject != null)
        {
            // Set player reference
            aiming.player = playerGameObject.transform;
            
            // Find weapon points
            Transform leftPoint = playerGameObject.transform.Find("WeaponPoint_Left");
            Transform rightPoint = playerGameObject.transform.Find("WeaponPoint_Right");
            
            if (leftPoint != null)
            {
                aiming.weaponPointLeft = leftPoint;
                // Set left hand visual
                aiming.leftHandVisual = leftPoint.gameObject;
            }
            
            if (rightPoint != null)
            {
                aiming.weaponPointRight = rightPoint;
                // Set right hand visual
                aiming.rightHandVisual = rightPoint.gameObject;
            }
            
            // Set layering settings
            aiming.behindPlayerLayer = "BehindPlayer";
            aiming.inFrontOfPlayerLayer = "InFrontOfPlayer";
        }
        
        // Create a FirePoint child object
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(weaponObject.transform);
        firePoint.transform.localPosition = new Vector3(0.5f, 0, 0);
        
        // Add the metadata component
        WeaponMetadata metadata = weaponObject.AddComponent<WeaponMetadata>();
        metadata.weaponName = weaponName;
        metadata.weaponType = "Shotgun";
        metadata.rarity = rarityOptions[selectedRarity];
        metadata.fireType = "SemiAuto";
        metadata.description = weaponDescription;
        
        // Default values
        float defaultDamage = 10f;
        int defaultMagazineSize = 8;
        float defaultFireRate = 4f;
        
        metadata.damage = defaultDamage;
        metadata.fireRate = defaultFireRate;
        metadata.magazineSize = defaultMagazineSize;
        
        // Add the shotgun weapon component
        ShotgunWeapon shotgun = weaponObject.AddComponent<ShotgunWeapon>();
        shotgun.damage = defaultDamage;
        shotgun.magazineSize = defaultMagazineSize;
        shotgun.shotgunFireRate = defaultFireRate;
        shotgun.pelletCount = 8;
        shotgun.spreadAngle = 30f;
        shotgun.pelletColor = rarityColors[selectedRarity];
        shotgun.firePoint = firePoint.transform;
        
        // Select the created weapon
        Selection.activeGameObject = weaponObject;
        
        // Create message
        string message = $"{weaponName} has been created successfully!\n\n";
        
        if (playerGameObject == null)
        {
            message += "⚠️ Player reference not found. You need to manually set up the WeaponAiming component.\n\n";
        }
        else
        {
            bool missingReferences = false;
            if (aiming.weaponPointLeft == null || aiming.weaponPointRight == null)
            {
                message += "⚠️ Some weapon points could not be found. Check the WeaponAiming component.\n\n";
                missingReferences = true;
            }
            
            if (!missingReferences)
            {
                message += "✓ All player references were set up successfully!\n\n";
            }
        }
        
        message += "The weapon has been selected in the hierarchy.\nAdjust its settings in the Inspector!";
        
        // Notify the user
        EditorUtility.DisplayDialog("Weapon Created", message, "OK");
    }
} 