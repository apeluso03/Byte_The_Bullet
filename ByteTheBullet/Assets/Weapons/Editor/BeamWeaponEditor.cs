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
            "maxReserveAmmo",
            
            // Additional properties to hide from BaseWeapon
            "fireRate",           // Hide fire rate from base weapon
            "fireMode",           // Hide fire mode from base weapon (we have our own)
            "nextFireTime",       // Hide next fire time 
            "maxChargeTime",      // Hide the base weapon charge time
            "maxChargeDamageMultiplier", // Hide base weapon charge multiplier
            "isReloading",        // Hide reload state
            
            // Additional properties to hide
            "reloadTime",         // Hide reload time (we handle this in battery settings)
            "projectilePrefab",   // Hide projectile prefab (not needed for beam)
            "projectileSpeed",    // Hide projectile speed (not needed for beam)
            "accuracy",           // Hide accuracy slider (not needed for beam)
            "muzzleFlash",        // Hide muzzle flash (not relevant for beams)
            "shootSound",         // We use beam-specific sounds
            "emptySound",         // Not relevant for beam
            "reloadSound",        // We handle this in battery settings
            "impactEffectPrefab", // We handle this in the Visual FX section
            "shakeIntensity",     // Hide camera shake settings
            "shakeDuration",
            
            // Add this line to fix the duplicate fire point
            "firePoint"           // Hide default fire point (we show it in Firing Settings now)
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
            string oldName = weaponNameProperty.stringValue;
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(weaponNameProperty);
            
            if (EditorGUI.EndChangeCheck())
            {
                // Apply this specific property change immediately
                serializedObject.ApplyModifiedProperties();
                
                // Now get the new value and update GameObject name if it changed
                string newName = weaponNameProperty.stringValue;
                if (newName != oldName)
                {
                    // Also update the GameObject name to match
                    Undo.RecordObject(beamWeapon.gameObject, "Change GameObject Name");
                    beamWeapon.gameObject.name = newName;
                    EditorUtility.SetDirty(beamWeapon.gameObject);
                    
                    // Force scene save
                    if (!Application.isPlaying)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(beamWeapon.gameObject.scene);
                    }
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
            
            // MOVED UP: Firing Settings section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Firing Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // MOVED: Fire point reference from elsewhere
            SerializedProperty firePointProp = serializedObject.FindProperty("firePoint");
            EditorGUILayout.PropertyField(firePointProp, new GUIContent("Fire Point"));
            
            // Fire mode dropdown
            SerializedProperty fireModeProp = configProperty.FindPropertyRelative("fireMode");
            EditorGUILayout.PropertyField(fireModeProp, new GUIContent("Beam Fire Mode"));
            
            // Show relevant settings based on selected fire mode
            BeamWeaponConfig.BeamFireMode selectedMode = (BeamWeaponConfig.BeamFireMode)fireModeProp.enumValueIndex;
            
            if (selectedMode == BeamWeaponConfig.BeamFireMode.Continuous)
            {
                SerializedProperty continuousChargeTimeProp = configProperty.FindPropertyRelative("continuousChargeTime");
                EditorGUILayout.PropertyField(continuousChargeTimeProp, new GUIContent("Charge-up Time"));
                
                if (continuousChargeTimeProp.floatValue > 0)
                {
                    EditorGUILayout.HelpBox("Beam requires a brief charge-up before firing continuously.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Beam fires immediately with no charge-up time.", MessageType.Info);
                }
            }
            else if (selectedMode == BeamWeaponConfig.BeamFireMode.ChargeBurst)
            {
                SerializedProperty maxChargeTimeProp = configProperty.FindPropertyRelative("maxChargeTime");
                SerializedProperty maxChargeDmgMultProp = configProperty.FindPropertyRelative("maxChargeDamageMultiplier");
                SerializedProperty burstDurationProp = configProperty.FindPropertyRelative("burstDuration");
                
                EditorGUILayout.PropertyField(maxChargeTimeProp, new GUIContent("Charge Time"));
                EditorGUILayout.PropertyField(burstDurationProp, new GUIContent("Burst Duration"));
                EditorGUILayout.PropertyField(maxChargeDmgMultProp, new GUIContent("Damage Multiplier"));
                
                EditorGUILayout.HelpBox("Hold fire button to charge. Beam will automatically fire when fully charged.", MessageType.Info);
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Range Settings", EditorStyles.boldLabel);

            SerializedProperty beamRangeProp = configProperty.FindPropertyRelative("beamRange");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(beamRangeProp, new GUIContent("Beam Range"));
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                // Apply immediately in play mode
                serializedObject.ApplyModifiedProperties();
                
                // Update beam range
                beamWeapon.UpdateBeamRange(beamRangeProp.floatValue);
            }

            // Add a helpful visualization of the range
            if (beamRangeProp.floatValue > 0)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                // Draw a background
                EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
                
                // Calculate width based on a reasonable max range (e.g., 30 units)
                float maxDisplayRange = 30f;
                float normalizedRange = Mathf.Clamp01(beamRangeProp.floatValue / maxDisplayRange);
                
                // Draw the range bar
                Rect rangeRect = new Rect(progressRect.x, progressRect.y, progressRect.width * normalizedRange, progressRect.height);
                EditorGUI.DrawRect(rangeRect, new Color(0.3f, 0.7f, 0.9f));
                
                // Draw a label showing the exact range
                string rangeText = beamRangeProp.floatValue.ToString("F1") + " units";
                GUIStyle centeredStyle = new GUIStyle(EditorStyles.boldLabel);
                centeredStyle.alignment = TextAnchor.MiddleCenter;
                EditorGUI.LabelField(progressRect, rangeText, centeredStyle);
                
                // Show a warning if range is set extremely high
                if (beamRangeProp.floatValue > maxDisplayRange)
                {
                    EditorGUILayout.HelpBox("Very high beam range may impact performance.", MessageType.Info);
                }
            }

            // Add range testing buttons in play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Increase Range"))
                {
                    beamWeapon.config.beamRange += 2f;
                    beamWeapon.UpdateBeamRange(beamWeapon.config.beamRange);
                }
                
                if (GUILayout.Button("Decrease Range"))
                {
                    beamWeapon.config.beamRange = Mathf.Max(1f, beamWeapon.config.beamRange - 2f);
                    beamWeapon.UpdateBeamRange(beamWeapon.config.beamRange);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            
            // Damage section below Firing Settings
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Damage Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Draw damage property
            EditorGUILayout.PropertyField(damageProperty);
            
            // Beam damage per second
            SerializedProperty beamDamagePerSecondProp = configProperty.FindPropertyRelative("beamDamagePerSecond");
            EditorGUILayout.PropertyField(beamDamagePerSecondProp, new GUIContent("Beam DPS"));
            
            EditorGUI.indentLevel--;
            
            // Energy settings - now AFTER firing settings
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
                
                // Energy system type dropdown
                SerializedProperty energySystemTypeProp = configProperty.FindPropertyRelative("energySystemType");
                EditorGUILayout.PropertyField(energySystemTypeProp, new GUIContent("Energy System"));
                
                // Max energy setting (common to both systems)
                SerializedProperty maxEnergyProp = configProperty.FindPropertyRelative("maxEnergy");
                EditorGUILayout.PropertyField(maxEnergyProp, new GUIContent("Max Energy"));
                
                // Energy drain rate (common to both systems)
                SerializedProperty drainRateProp = configProperty.FindPropertyRelative("energyDrainRate");
                EditorGUILayout.PropertyField(drainRateProp, new GUIContent("Energy Drain Rate"));
                
                // Show relevant settings based on energy system type
                BeamWeaponConfig.EnergySystemType selectedEnergySystem = 
                    (BeamWeaponConfig.EnergySystemType)energySystemTypeProp.enumValueIndex;
                
                if (selectedEnergySystem == BeamWeaponConfig.EnergySystemType.AutoRecharge)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Auto-Recharge Settings", EditorStyles.boldLabel);
                    
                    // Energy regen rate
                    SerializedProperty regenRateProp = configProperty.FindPropertyRelative("energyRegenRate");
                    EditorGUILayout.PropertyField(regenRateProp, new GUIContent("Energy Regen Rate"));
                    
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.LabelField($"Energy will regenerate at {beamWeapon.config.energyRegenRate} units per second");
                    }
                }
                else if (selectedEnergySystem == BeamWeaponConfig.EnergySystemType.BatteryReload)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Battery Reload Settings", EditorStyles.boldLabel);
                    
                    // Battery settings
                    SerializedProperty maxBatteryProp = configProperty.FindPropertyRelative("maxBatteryCount");
                    SerializedProperty currentBatteryProp = configProperty.FindPropertyRelative("currentBatteryCount");
                    SerializedProperty energyPerBatteryProp = configProperty.FindPropertyRelative("energyPerBattery");
                    SerializedProperty reloadTimeProp = configProperty.FindPropertyRelative("batteryReloadTime");
                    
                    EditorGUILayout.PropertyField(maxBatteryProp, new GUIContent("Max Batteries"));
                    EditorGUILayout.PropertyField(energyPerBatteryProp, new GUIContent("Energy Per Battery"));
                    EditorGUILayout.PropertyField(reloadTimeProp, new GUIContent("Reload Time"));
                    
                    // Current battery count with progress bar
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Batteries");
                    
                    // Progress bar for batteries
                    int maxBatteries = beamWeapon.config.maxBatteryCount;
                    int currentBatteries = beamWeapon.config.CurrentBatteryCount;
                    
                    Rect batteryProgressRect = EditorGUILayout.GetControlRect();
                    EditorGUI.ProgressBar(batteryProgressRect, (float)currentBatteries / maxBatteries, $"{currentBatteries} / {maxBatteries}");
                    
                    EditorGUILayout.EndHorizontal();
                    
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.LabelField($"Press R to reload a battery (+{beamWeapon.config.energyPerBattery} energy)");
                        
                        // Add reload battery button in play mode
                        if (GUILayout.Button("Reload Battery"))
                        {
                            if (beamWeapon.config.CurrentBatteryCount > 0)
                            {
                                beamWeapon.StartCoroutine(beamWeapon.ReloadBattery());
                            }
                            else
                            {
                                Debug.Log("No batteries remaining!");
                            }
                        }
                        
                        // Add get more batteries button in play mode
                        if (GUILayout.Button("Add Battery"))
                        {
                            if (beamWeapon.config.CurrentBatteryCount < beamWeapon.config.maxBatteryCount)
                            {
                                beamWeapon.config.CurrentBatteryCount++;
                                EditorUtility.SetDirty(beamWeapon);
                            }
                            else
                            {
                                Debug.Log("Battery count already at maximum!");
                            }
                        }
                    }
                }
                
                // Add a fill energy button in play mode (common to both systems)
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space(5);
                    if (GUILayout.Button("Fill Energy"))
                    {
                        beamWeapon.CurrentEnergy = beamWeapon.config.maxEnergy;
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Add a new Visual FX section after Firing Settings or before Freeze Ray Presets
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Visual FX Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            // Beam FX prefabs
            SerializedProperty chargeFXProp = configProperty.FindPropertyRelative("chargeFXPrefab");
            SerializedProperty flareFXProp = configProperty.FindPropertyRelative("flareFXPrefab");
            SerializedProperty impactFXProp = configProperty.FindPropertyRelative("impactFXPrefab");
            SerializedProperty beamMiddleProp = configProperty.FindPropertyRelative("beamMiddleAnimPrefab");
            
            EditorGUILayout.PropertyField(chargeFXProp, new GUIContent("Charge Effect Prefab"));
            EditorGUILayout.PropertyField(flareFXProp, new GUIContent("Flare Effect Prefab"));
            EditorGUILayout.PropertyField(impactFXProp, new GUIContent("Impact Effect Prefab"));
            EditorGUILayout.PropertyField(beamMiddleProp, new GUIContent("Beam Middle Prefab"));
            
            // Beam visual properties
            SerializedProperty beamWidthProp = configProperty.FindPropertyRelative("beamWidth");
            SerializedProperty beamSectionDistanceProp = configProperty.FindPropertyRelative("beamSectionDistance");
            SerializedProperty sectionOverlapProp = configProperty.FindPropertyRelative("sectionOverlap");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(beamWidthProp, new GUIContent("Beam Width"));
            EditorGUILayout.PropertyField(beamSectionDistanceProp, new GUIContent("Section Distance"));
            EditorGUILayout.PropertyField(sectionOverlapProp, new GUIContent("Section Overlap"));
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
                
                // If the beam is currently active, force an update
                if (beamWeapon.IsFiring)
                {
                    beamWeapon.RefreshBeamPhysics();
                }
            }
            
            EditorGUILayout.HelpBox("These prefabs will be used by the beam's visual effects system. The beam middle prefab is particularly important as it forms the visible beam.", MessageType.Info);
            
            // Add this after the Visual FX Settings section
            if (chargeFXProp.objectReferenceValue == null || 
                flareFXProp.objectReferenceValue == null || 
                impactFXProp.objectReferenceValue == null || 
                beamMiddleProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Some FX prefabs are not assigned. The beam may not display correctly without these.", MessageType.Warning);
                
                if (GUILayout.Button("Use Default FX Prefabs"))
                {
                    // Try to find default prefabs in the project
                    if (chargeFXProp.objectReferenceValue == null)
                        chargeFXProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapons/Prefabs/FX/ChargeEffect.prefab");
                        
                    if (flareFXProp.objectReferenceValue == null)
                        flareFXProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapons/Prefabs/FX/FlareEffect.prefab");
                        
                    if (impactFXProp.objectReferenceValue == null)
                        impactFXProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapons/Prefabs/FX/ImpactEffect.prefab");
                        
                    if (beamMiddleProp.objectReferenceValue == null)
                        beamMiddleProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapons/Prefabs/FX/BeamMiddle.prefab");
                        
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(beamWeapon);
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Beam Position Adjustment", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            SerializedProperty beamHeightOffsetProp = configProperty.FindPropertyRelative("beamHeightOffset");
            SerializedProperty beamForwardOffsetProp = configProperty.FindPropertyRelative("beamForwardOffset");
            SerializedProperty showPositionGUIProp = configProperty.FindPropertyRelative("showPositionAdjustmentGUI");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(beamHeightOffsetProp, new GUIContent("Beam Height Offset"));
            EditorGUILayout.PropertyField(beamForwardOffsetProp, new GUIContent("Beam Forward Offset"));
            EditorGUILayout.PropertyField(showPositionGUIProp, new GUIContent("Show Position Adjustment GUI"));

            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
                
                // If the beam is currently active, force an update
                if (beamWeapon.IsFiring)
                {
                    beamWeapon.RefreshBeamPhysics();
                }
            }

            EditorGUI.indentLevel--;

            // Add test buttons for adjusting position in the editor
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Height Adjustment", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Raise Beam"))
                {
                    beamWeapon.config.beamHeightOffset += 0.1f;
                    beamWeapon.config.beamHeightOffset = Mathf.Clamp(beamWeapon.config.beamHeightOffset, -1f, 1f);
                    beamWeapon.RefreshBeamPhysics();
                }
                
                if (GUILayout.Button("Lower Beam"))
                {
                    beamWeapon.config.beamHeightOffset -= 0.1f;
                    beamWeapon.config.beamHeightOffset = Mathf.Clamp(beamWeapon.config.beamHeightOffset, -1f, 1f);
                    beamWeapon.RefreshBeamPhysics();
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Add forward/back adjustment buttons
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Forward Adjustment", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Move Forward"))
                {
                    beamWeapon.config.beamForwardOffset += 0.1f;
                    beamWeapon.config.beamForwardOffset = Mathf.Clamp(beamWeapon.config.beamForwardOffset, -1f, 1f);
                    beamWeapon.RefreshBeamPhysics();
                }
                
                if (GUILayout.Button("Move Back"))
                {
                    beamWeapon.config.beamForwardOffset -= 0.1f;
                    beamWeapon.config.beamForwardOffset = Mathf.Clamp(beamWeapon.config.beamForwardOffset, -1f, 1f);
                    beamWeapon.RefreshBeamPhysics();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
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
                
                // Display appropriate energy system info
                if (beamWeapon.config.energySystemType == BeamWeaponConfig.EnergySystemType.AutoRecharge)
                {
                    EditorGUILayout.LabelField($"Energy Regeneration Rate: {beamWeapon.config.energyRegenRate}/sec");
                }
                else
                {
                    EditorGUILayout.LabelField($"Batteries: {beamWeapon.config.CurrentBatteryCount}/{beamWeapon.config.maxBatteryCount}");
                    EditorGUILayout.LabelField($"Energy Per Battery: {beamWeapon.config.energyPerBattery}");
                }
                
                EditorGUILayout.LabelField($"Energy Drain Rate: {beamWeapon.config.energyDrainRate}/sec");
                
                // Firing control button
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
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Fill Energy"))
                {
                    beamWeapon.CurrentEnergy = beamWeapon.config.maxEnergy;
                }
                
                if (GUILayout.Button("Deplete Energy"))
                {
                    beamWeapon.CurrentEnergy = 0;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Add battery-specific buttons if using battery reload system
                if (beamWeapon.config.energySystemType == BeamWeaponConfig.EnergySystemType.BatteryReload)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Add Battery"))
                    {
                        if (beamWeapon.config.CurrentBatteryCount < beamWeapon.config.maxBatteryCount)
                        {
                            beamWeapon.config.CurrentBatteryCount++;
                        }
                    }
                    
                    if (GUILayout.Button("Use Battery"))
                    {
                        if (beamWeapon.config.CurrentBatteryCount > 0 && beamWeapon.CurrentEnergy < beamWeapon.config.maxEnergy)
                        {
                            beamWeapon.StartCoroutine(beamWeapon.ReloadBattery());
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Add a manual regeneration button for auto-recharge mode
                    if (GUILayout.Button("Regenerate Energy (+25%)"))
                    {
                        beamWeapon.CurrentEnergy += beamWeapon.config.maxEnergy * 0.25f;
                    }
                }
                
                if (beamWeapon.IsFiring)
                {
                    if (GUILayout.Button("Simulate 360° Spin Test"))
                    {
                        // Start a spin test
                        EditorCoroutines.StartCoroutine(SimulateBeamSpin(beamWeapon), this);
                    }
                }
                
                if (beamWeapon.config.fireMode == BeamWeaponConfig.BeamFireMode.ChargeBurst)
                {
                    // Show charge progress bar
                    float chargePercent = beamWeapon.GetCurrentChargePercent();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Charge Level");
                    
                    // Progress bar for charge
                    Rect progressRect = EditorGUILayout.GetControlRect();
                    EditorGUI.ProgressBar(progressRect, chargePercent, $"{chargePercent * 100:F0}%");
                    
                    EditorGUILayout.EndHorizontal();
                    
                    float damageMultiplier = 1f + chargePercent * (beamWeapon.config.maxChargeDamageMultiplier - 1f);
                    EditorGUILayout.LabelField($"Current Damage Multiplier: {damageMultiplier:F2}x");
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