using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(WeaponDefinition))]
public class WeaponDefinitionEditor : Editor
{
    private bool showPrimarySettings = true;
    private bool showSecondarySettings = true;
    private bool showBurstSettings = true;
    private bool showAudioSettings = true;
    private bool showEffectSettings = true;
    
    public override void OnInspectorGUI()
    {
        WeaponDefinition definition = (WeaponDefinition)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Weapon Definition", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Basic info section
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        definition.weaponName = EditorGUILayout.TextField("Weapon Name", definition.weaponName);
        definition.weaponIcon = (Sprite)EditorGUILayout.ObjectField("Weapon Icon", definition.weaponIcon, typeof(Sprite), false);
        definition.weaponID = EditorGUILayout.IntField("Weapon ID", definition.weaponID);
        definition.weaponPrefab = (GameObject)EditorGUILayout.ObjectField("Weapon Prefab", definition.weaponPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        // Primary fire settings
        showPrimarySettings = EditorGUILayout.Foldout(showPrimarySettings, "Primary Fire Settings");
        if (showPrimarySettings)
        {
            EditorGUI.indentLevel++;
            
            definition.primaryFireMode = (WeaponDefinition.FireModeType)EditorGUILayout.EnumPopup("Fire Mode", definition.primaryFireMode);
            definition.primaryProjectileType = (WeaponDefinition.ProjectileType)EditorGUILayout.EnumPopup("Projectile Type", definition.primaryProjectileType);
            definition.primaryProjectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", definition.primaryProjectilePrefab, typeof(GameObject), false);
            definition.primaryDamage = EditorGUILayout.FloatField("Damage", definition.primaryDamage);
            definition.primaryFireRate = EditorGUILayout.FloatField("Fire Rate", definition.primaryFireRate);
            definition.primaryProjectileSpeed = EditorGUILayout.FloatField("Projectile Speed", definition.primaryProjectileSpeed);
            definition.primaryMagazineSize = EditorGUILayout.IntField("Magazine Size", definition.primaryMagazineSize);
            definition.primaryReloadTime = EditorGUILayout.FloatField("Reload Time", definition.primaryReloadTime);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Burst Fire Settings (only shown when primary or secondary mode is Burst)
        bool showBurstSection = definition.primaryFireMode == WeaponDefinition.FireModeType.Burst || 
                                (definition.hasSecondaryFire && definition.secondaryFireMode == WeaponDefinition.FireModeType.Burst);
        
        if (showBurstSection)
        {
            showBurstSettings = EditorGUILayout.Foldout(showBurstSettings, "Burst Fire Settings");
            if (showBurstSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Burst Configuration", EditorStyles.boldLabel);
                
                // Display helpful slider for burst size
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Burst Size", GUILayout.Width(140));
                definition.burstSize = EditorGUILayout.IntSlider(definition.burstSize, 2, 8);
                EditorGUILayout.EndHorizontal();
                
                // Display helpful slider for spacing between shots in a burst
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Shot Spacing", GUILayout.Width(140));
                definition.burstFireRate = EditorGUILayout.Slider(definition.burstFireRate, 0.05f, 0.3f);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("(Lower values = faster burst)", EditorStyles.miniLabel);
                
                // Display helpful slider for cooldown between bursts
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Burst Cooldown", GUILayout.Width(140));
                definition.burstCooldown = EditorGUILayout.Slider(definition.burstCooldown, 0.2f, 2.0f);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("(Time between bursts)", EditorStyles.miniLabel);
                
                // Auto burst toggle
                definition.enableAutoBurst = EditorGUILayout.Toggle("Hold To Auto-Burst", definition.enableAutoBurst);
                if (definition.enableAutoBurst)
                {
                    EditorGUILayout.HelpBox("When enabled, holding the fire button will automatically fire bursts at regular intervals.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.Space();
        
        // Secondary fire settings
        definition.hasSecondaryFire = EditorGUILayout.Toggle("Has Secondary Fire", definition.hasSecondaryFire);
        
        if (definition.hasSecondaryFire)
        {
            showSecondarySettings = EditorGUILayout.Foldout(showSecondarySettings, "Secondary Fire Settings");
            if (showSecondarySettings)
            {
                EditorGUI.indentLevel++;
                
                definition.secondaryFireMode = (WeaponDefinition.FireModeType)EditorGUILayout.EnumPopup("Fire Mode", definition.secondaryFireMode);
                definition.secondaryProjectileType = (WeaponDefinition.ProjectileType)EditorGUILayout.EnumPopup("Projectile Type", definition.secondaryProjectileType);
                definition.secondaryProjectilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", definition.secondaryProjectilePrefab, typeof(GameObject), false);
                definition.secondaryDamage = EditorGUILayout.FloatField("Damage", definition.secondaryDamage);
                definition.secondaryFireRate = EditorGUILayout.FloatField("Fire Rate", definition.secondaryFireRate);
                definition.secondaryProjectileSpeed = EditorGUILayout.FloatField("Projectile Speed", definition.secondaryProjectileSpeed);
                definition.secondaryMagazineSize = EditorGUILayout.IntField("Magazine Size", definition.secondaryMagazineSize);
                definition.secondaryReloadTime = EditorGUILayout.FloatField("Reload Time", definition.secondaryReloadTime);
                
                EditorGUI.indentLevel--;
            }
        }
        
        EditorGUILayout.Space();
        
        // Audio settings
        showAudioSettings = EditorGUILayout.Foldout(showAudioSettings, "Audio Settings");
        if (showAudioSettings)
        {
            EditorGUI.indentLevel++;
            
            definition.primaryFireSound = (AudioClip)EditorGUILayout.ObjectField("Primary Fire Sound", definition.primaryFireSound, typeof(AudioClip), false);
            definition.primaryReloadSound = (AudioClip)EditorGUILayout.ObjectField("Primary Reload Sound", definition.primaryReloadSound, typeof(AudioClip), false);
            
            if (definition.hasSecondaryFire)
            {
                definition.secondaryFireSound = (AudioClip)EditorGUILayout.ObjectField("Secondary Fire Sound", definition.secondaryFireSound, typeof(AudioClip), false);
                definition.secondaryReloadSound = (AudioClip)EditorGUILayout.ObjectField("Secondary Reload Sound", definition.secondaryReloadSound, typeof(AudioClip), false);
                definition.switchModeSound = (AudioClip)EditorGUILayout.ObjectField("Switch Mode Sound", definition.switchModeSound, typeof(AudioClip), false);
            }
            
            definition.emptySound = (AudioClip)EditorGUILayout.ObjectField("Empty Sound", definition.emptySound, typeof(AudioClip), false);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Effects settings
        showEffectSettings = EditorGUILayout.Foldout(showEffectSettings, "Effects Settings");
        if (showEffectSettings)
        {
            EditorGUI.indentLevel++;
            
            definition.muzzleFlashPrefab = (GameObject)EditorGUILayout.ObjectField("Primary Muzzle Flash", definition.muzzleFlashPrefab, typeof(GameObject), false);
            
            if (definition.hasSecondaryFire)
            {
                definition.secondaryMuzzleFlashPrefab = (GameObject)EditorGUILayout.ObjectField("Secondary Muzzle Flash", definition.secondaryMuzzleFlashPrefab, typeof(GameObject), false);
            }
            
            // Display bullet sprite list
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletSprites"), true);
            
            EditorGUI.indentLevel--;
        }
        
        // Display testing buttons
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create Prefab", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Weapon Prefab"))
        {
            // Open the Weapon Prefab Creator with this definition selected
            WeaponPrefabCreator window = EditorWindow.GetWindow<WeaponPrefabCreator>("Weapon Prefab Creator");
            window.SelectWeaponDefinition(definition);
        }
        
        // Save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif