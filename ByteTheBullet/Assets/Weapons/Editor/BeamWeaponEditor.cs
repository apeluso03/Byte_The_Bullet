using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Weapons.Editor
{
    [CustomEditor(typeof(BeamWeapon))]
    public class BeamWeaponEditor : UnityEditor.Editor
    {
        private bool showFreezeRaySettings = true;
        private bool showEnergySettings = true;
        
        // List of property names to exclude from inspector
        private readonly List<string> excludedProperties = new List<string>
        {
            "onFire", 
            "onAmmoChanged", 
            "onReloadStart", 
            "onReloadComplete", 
            "onOutOfAmmo",
            "weaponType",   // Exclude so we can display a fixed label instead
            "damageType",   // Exclude damage type completely
            "weaponName",   // We'll handle these properties manually
            "rarity",       // in our custom inspector section
            "description",
            "damage",       // Also handle damage property manually
            "m_Script",     // Hide the script reference at the top
            "currentEnergy", // We'll handle energy in our custom energy section
            "magazineSize",
            "currentAmmo",
            "reserveAmmo",
            "maxReserveAmmo"
        };
        
        // SerializedProperty for the weapon name
        private SerializedProperty weaponNameProperty;
        private SerializedProperty rarityProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty damageProperty;
        private SerializedProperty scriptProperty;
        private SerializedProperty currentEnergyProperty;
        private SerializedProperty configProperty;
        
        // Colors for different rarities
        private readonly Color[] rarityColors = new Color[] {
            new Color(0.7f, 0.7f, 0.7f),     // Common - Gray
            new Color(0.3f, 0.8f, 0.3f),     // Uncommon - Green
            new Color(0.3f, 0.5f, 1.0f),     // Rare - Blue
            new Color(0.8f, 0.3f, 0.9f),     // Epic - Purple
            new Color(1.0f, 0.8f, 0.0f),     // Legendary - Gold
            new Color(1.0f, 0.4f, 0.0f)      // Unique - Orange
        };
        
        private readonly string[] rarityOptions = new string[] { 
            "Common", "Uncommon", "Rare", "Epic", "Legendary", "Unique" 
        };
        
        private void OnEnable()
        {
            // Get properties
            scriptProperty = serializedObject.FindProperty("m_Script");
            weaponNameProperty = serializedObject.FindProperty("weaponName");
            rarityProperty = serializedObject.FindProperty("rarity");
            descriptionProperty = serializedObject.FindProperty("description");
            damageProperty = serializedObject.FindProperty("damage");
            currentEnergyProperty = serializedObject.FindProperty("currentEnergy");
            configProperty = serializedObject.FindProperty("config");
        }
        
        public override void OnInspectorGUI()
        {
            BeamWeapon beamWeapon = (BeamWeapon)target;
            
            // Update the serialized object
            serializedObject.Update();
            
            // Display script field at the top (but disabled)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(scriptProperty);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // Our custom Weapon Info section
            EditorGUILayout.LabelField("Weapon Info", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Draw the name property
            string oldName = beamWeapon.weaponName;
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(weaponNameProperty);
            
            if (EditorGUI.EndChangeCheck() && beamWeapon.weaponName != oldName)
            {
                // Also update the GameObject name to match
                Undo.RecordObject(beamWeapon.gameObject, "Change GameObject Name");
                beamWeapon.gameObject.name = beamWeapon.weaponName;
                EditorUtility.SetDirty(beamWeapon.gameObject);
                
                // Force scene save
                if (!Application.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(beamWeapon.gameObject.scene);
                }
            }
            
            // Display weapon type as a fixed label (not editable)
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Weapon Type", "Beam");
            }
            
            // Draw rarity field with visual indication of rarity color
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Rarity");
            
            // Find current rarity index
            int rarityIndex = 0;
            for (int i = 0; i < rarityOptions.Length; i++)
            {
                if (rarityOptions[i] == beamWeapon.rarity)
                {
                    rarityIndex = i;
                    break;
                }
            }
            
            // Apply the rarity color to the popup background
            Color originalBgColor = GUI.backgroundColor;
            GUI.backgroundColor = rarityColors[rarityIndex];
            
            // Create the dropdown
            int newRarityIndex = EditorGUILayout.Popup(rarityIndex, rarityOptions);
            if (newRarityIndex != rarityIndex)
            {
                Undo.RecordObject(beamWeapon, "Change Weapon Rarity");
                beamWeapon.rarity = rarityOptions[newRarityIndex];
                EditorUtility.SetDirty(beamWeapon);
            }
            
            // Reset background color
            GUI.backgroundColor = originalBgColor;
            EditorGUILayout.EndHorizontal();
            
            // Draw description with text area (now under rarity)
            EditorGUILayout.PropertyField(descriptionProperty);
            
            EditorGUI.indentLevel--;
            
            // Energy settings
            EditorGUILayout.Space(10);
            showEnergySettings = EditorGUILayout.Foldout(showEnergySettings, "Energy Settings", true, EditorStyles.foldoutHeader);
            
            if (showEnergySettings)
            {
                EditorGUI.indentLevel++;
                
                // Current energy display
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Current Energy");
                
                float maxEnergy = beamWeapon.config.maxEnergy;
                float currentEnergy = beamWeapon.CurrentEnergy;
                
                // Progress bar for energy
                Rect progressRect = EditorGUILayout.GetControlRect();
                EditorGUI.ProgressBar(progressRect, currentEnergy / maxEnergy, $"{currentEnergy:F0} / {maxEnergy:F0}");
                
                EditorGUILayout.EndHorizontal();
                
                // Energy config settings
                // Max energy setting
                SerializedProperty maxEnergyProp = configProperty.FindPropertyRelative("maxEnergy");
                EditorGUILayout.PropertyField(maxEnergyProp, new GUIContent("Max Energy"));
                
                // Energy regen rate
                SerializedProperty regenRateProp = configProperty.FindPropertyRelative("energyRegenRate");
                EditorGUILayout.PropertyField(regenRateProp, new GUIContent("Energy Regen Rate"));
                
                // Energy drain rate
                SerializedProperty drainRateProp = configProperty.FindPropertyRelative("energyDrainRate");
                EditorGUILayout.PropertyField(drainRateProp, new GUIContent("Energy Drain Rate"));
                
                // Add a fill energy button in play mode
                if (Application.isPlaying)
                {
                    if (GUILayout.Button("Fill Energy"))
                    {
                        beamWeapon.CurrentEnergy = beamWeapon.config.maxEnergy;
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Damage section below Weapon Info
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Damage Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Draw damage property
            EditorGUILayout.PropertyField(damageProperty);
            
            // Beam damage per second
            SerializedProperty beamDamagePerSecondProp = configProperty.FindPropertyRelative("beamDamagePerSecond");
            EditorGUILayout.PropertyField(beamDamagePerSecondProp, new GUIContent("Beam DPS"));
            
            EditorGUI.indentLevel--;
            
            // Now draw all remaining properties except excluded ones
            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());
            
            // Apply modified properties
            serializedObject.ApplyModifiedProperties();
            
            // Beam visibility toggle
            EditorGUILayout.Space(10);
            if (GUILayout.Button(beamWeapon.config.hideBeamColor ? "Show Beam Color" : "Hide Beam Color"))
            {
                Undo.RecordObject(beamWeapon, "Toggle Beam Visibility");
                beamWeapon.ToggleBeamVisibility();
                EditorUtility.SetDirty(beamWeapon);
            }
            
            // Freeze Ray Presets section
            EditorGUILayout.Space(10);
            showFreezeRaySettings = EditorGUILayout.Foldout(showFreezeRaySettings, "Freeze Ray Presets", true, EditorStyles.foldoutHeader);
            
            if (showFreezeRaySettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("Select a preset to quickly configure the freeze ray beam behavior:", MessageType.Info);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Enter the Gungeon Style"))
                {
                    Undo.RecordObject(beamWeapon, "Apply Beam Preset");
                    beamWeapon.config.beamSegments = 24;
                    beamWeapon.config.beamFollowSpeed = 8f;
                    beamWeapon.config.tipLagMultiplier = 1.5f;
                    EditorUtility.SetDirty(beamWeapon);
                }
                
                if (GUILayout.Button("Super Whippy"))
                {
                    Undo.RecordObject(beamWeapon, "Apply Beam Preset");
                    beamWeapon.config.beamSegments = 32;
                    beamWeapon.config.beamFollowSpeed = 5f;
                    beamWeapon.config.tipLagMultiplier = 2.5f;
                    EditorUtility.SetDirty(beamWeapon);
                }
                
                if (GUILayout.Button("Quick Response"))
                {
                    Undo.RecordObject(beamWeapon, "Apply Beam Preset");
                    beamWeapon.config.beamSegments = 16;
                    beamWeapon.config.beamFollowSpeed = 12f;
                    beamWeapon.config.tipLagMultiplier = 1.0f;
                    EditorUtility.SetDirty(beamWeapon);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Visual Quality:", EditorStyles.boldLabel);
                
                EditorGUILayout.PropertyField(configProperty.FindPropertyRelative("beamSegments"));
                EditorGUILayout.HelpBox("Higher values create a smoother curve but may impact performance.", MessageType.None);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Beam Behavior:", EditorStyles.boldLabel);
                
                EditorGUILayout.PropertyField(configProperty.FindPropertyRelative("beamFollowSpeed"));
                EditorGUILayout.HelpBox("Lower values create more lag. Higher values make the beam follow aim changes more quickly.", MessageType.None);
                
                EditorGUILayout.PropertyField(configProperty.FindPropertyRelative("tipLagMultiplier"));
                EditorGUILayout.HelpBox("Higher values create more dramatic whip effects at the beam tip.", MessageType.None);
                
                // Apply changes
                serializedObject.ApplyModifiedProperties();
                
                EditorGUI.indentLevel--;
            }
            
            // Add testing controls for play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Test Controls (Play Mode Only)", EditorStyles.boldLabel);
                
                // Add energy regeneration rate display
                EditorGUILayout.LabelField($"Energy Regeneration Rate: {beamWeapon.config.energyRegenRate}/sec");
                EditorGUILayout.LabelField($"Energy Drain Rate: {beamWeapon.config.energyDrainRate}/sec");
                
                if (GUILayout.Button(beamWeapon.IsFiring ? "Stop Beam" : "Start Beam"))
                {
                    if (beamWeapon.IsFiring)
                    {
                        beamWeapon.StopBeam();
                    }
                    else
                    {
                        beamWeapon.StartBeam();
                    }
                }
                
                // Add buttons for energy testing
                if (GUILayout.Button("Fill Energy"))
                {
                    beamWeapon.CurrentEnergy = beamWeapon.config.maxEnergy;
                }
                
                if (GUILayout.Button("Deplete Energy"))
                {
                    beamWeapon.CurrentEnergy = 0;
                }
                
                // Add a manual regeneration button
                if (GUILayout.Button("Regenerate Energy (+25%)"))
                {
                    beamWeapon.CurrentEnergy += beamWeapon.config.maxEnergy * 0.25f;
                }
                
                if (beamWeapon.IsFiring)
                {
                    if (GUILayout.Button("Simulate 360° Spin Test"))
                    {
                        // Start a spin test
                        EditorCoroutines.StartCoroutine(SimulateBeamSpin(beamWeapon), this);
                    }
                }
            }
        }
        
        // Simulate a 360° spin to test the beam curve
        private System.Collections.IEnumerator SimulateBeamSpin(BeamWeapon beamWeapon)
        {
            Transform firePoint = beamWeapon.firePoint;
            if (firePoint == null)
            {
                Debug.LogError("Beam weapon has no firePoint assigned");
                yield break;
            }
            
            // Store original rotation
            Quaternion originalRotation = firePoint.rotation;
            
            // Perform the rotation over time
            float duration = 1.0f; // 1 second for a full 360°
            float elapsed = 0f;
            float startAngle = firePoint.eulerAngles.z;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Apply a full 360° rotation
                float angle = startAngle + t * 360f;
                
                // Update the firePoint rotation
                firePoint.rotation = Quaternion.Euler(0, 0, angle);
                
                yield return null;
            }
            
            // Restore original rotation
            firePoint.rotation = originalRotation;
        }
    }
}