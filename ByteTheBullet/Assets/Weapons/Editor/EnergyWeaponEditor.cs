using UnityEngine;
using UnityEditor;
using Weapons;
using System.Collections;

namespace Weapons.Editor
{
    [CustomEditor(typeof(EnergyWeapon))]
    public class EnergyWeaponEditor : UnityEditor.Editor
    {
        private bool showEnergySettings = true;
        private bool showBlasterSettings = false;
        private bool showChargeSettings = false;
        private bool showBeamSettings = false;
        private bool showBeamVisualSettings = false;
        private bool showBeamPhysicsSettings = false;
        private bool showBeamCurveSettings = false;
        private bool showBeamPresets = false;
        
        public override void OnInspectorGUI()
        {
            EnergyWeapon energyWeapon = (EnergyWeapon)target;
            
            // Draw the script reference field
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            
            // Draw weapon type dropdown first for better organization
            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("energyType"));
            
            serializedObject.ApplyModifiedProperties();
            
            // Core Energy Settings
            EditorGUILayout.Space(10);
            showEnergySettings = EditorGUILayout.Foldout(showEnergySettings, "Energy Core Settings", true, EditorStyles.foldoutHeader);
            
            if (showEnergySettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxEnergy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("currentEnergy"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("energyRegenRate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("energyColor"));
                
                // Basic weapon settings
                EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fireRate"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("firePoint"));
                
                EditorGUI.indentLevel--;
            }
            
            // Draw settings specific to the selected weapon type
            switch (energyWeapon.energyType)
            {
                case EnergyWeapon.EnergyType.Blaster:
                    DrawBlasterSettings();
                    break;
                
                case EnergyWeapon.EnergyType.Charge:
                    DrawChargeSettings();
                    break;
                
                case EnergyWeapon.EnergyType.Beam:
                    DrawBeamSettings(energyWeapon);
                    break;
            }
            
            // Audio & Effects (always visible)
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Audio & Effects", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shootSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadSound"));
            
            if (energyWeapon.energyType == EnergyWeapon.EnergyType.Beam)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamStartSound"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamLoopSound"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamEndSound"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamFX"));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("muzzleFlashPrefab"));
            }
            
            // Apply test controls in play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Test Controls (Play Mode Only)", EditorStyles.boldLabel);
                
                if (energyWeapon.energyType == EnergyWeapon.EnergyType.Beam)
                {
                    if (GUILayout.Button(energyWeapon.IsFiring ? "Stop Beam" : "Start Beam"))
                    {
                        if (energyWeapon.IsFiring)
                        {
                            energyWeapon.StopBeam();
                        }
                        else
                        {
                            energyWeapon.StartBeam();
                        }
                    }
                    
                    if (energyWeapon.IsFiring)
                    {
                        if (GUILayout.Button("Simulate 360° Spin Test"))
                        {
                            // Start a spin test
                            EditorCoroutineUtility.StartCoroutine(SimulateBeamSpin(energyWeapon), this);
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Test Fire"))
                    {
                        energyWeapon.Shoot();
                    }
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawBlasterSettings()
        {
            EditorGUILayout.Space(10);
            showBlasterSettings = EditorGUILayout.Foldout(showBlasterSettings, "Blaster Settings", true, EditorStyles.foldoutHeader);
            
            if (showBlasterSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("energyCostPerShot"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("energyProjectileSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSize"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawChargeSettings()
        {
            EditorGUILayout.Space(10);
            showChargeSettings = EditorGUILayout.Foldout(showChargeSettings, "Charge Settings", true, EditorStyles.foldoutHeader);
            
            if (showChargeSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("energyCostPerShot"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fullChargeEnergyCost"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxEnergyChargeTime"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fullChargeDamageMultiplier"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeEffectPrefab"));
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawBeamSettings(EnergyWeapon energyWeapon)
        {
            // Main beam settings
            EditorGUILayout.Space(10);
            showBeamSettings = EditorGUILayout.Foldout(showBeamSettings, "Beam Settings", true, EditorStyles.foldoutHeader);
            
            if (showBeamSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamRange"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamWidth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamDamagePerSecond"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamEnergyCostPerSecond"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useChargeEffect"));
                
                if (energyWeapon.useChargeEffect)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chargeTime"));
                }
                
                // Beam visibility toggle
                if (GUILayout.Button(energyWeapon.hideBeamColor ? "Show Beam Color" : "Hide Beam Color"))
                {
                    Undo.RecordObject(energyWeapon, "Toggle Beam Visibility");
                    energyWeapon.ToggleBeamVisibility();
                    EditorUtility.SetDirty(energyWeapon);
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Beam Visual Settings
            EditorGUILayout.Space(10);
            showBeamVisualSettings = EditorGUILayout.Foldout(showBeamVisualSettings, "Beam Visual Settings", true, EditorStyles.foldoutHeader);
            
            if (showBeamVisualSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamSegments"));
                EditorGUILayout.HelpBox("Higher values create a smoother curve but may impact performance.", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("straightSectionLength"));
                EditorGUILayout.HelpBox("Length of the straight section coming out of the weapon.", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionPointCount"));
                EditorGUILayout.HelpBox("How many points to use for blending between straight and curved sections.", MessageType.None);
                
                EditorGUI.indentLevel--;
            }
            
            // Beam Physics Settings
            EditorGUILayout.Space(10);
            showBeamPhysicsSettings = EditorGUILayout.Foldout(showBeamPhysicsSettings, "Beam Physics Settings", true, EditorStyles.foldoutHeader);
            
            if (showBeamPhysicsSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("beamFollowSpeed"));
                EditorGUILayout.HelpBox("Lower values create more lag. Higher values make the beam follow aim changes more quickly.", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("tipLagMultiplier"));
                EditorGUILayout.HelpBox("Higher values create more dramatic whip effects at the beam tip.", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ropeStiffness"));
                EditorGUILayout.HelpBox("How stiff the beam is (lower = more floppy).", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationIterations"));
                EditorGUILayout.HelpBox("Number of simulation iterations (higher = more stable).", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("segmentDistance"));
                EditorGUILayout.HelpBox("Distance between points in the beam.", MessageType.None);
                
                EditorGUI.indentLevel--;
            }
            
            // Beam Curve Settings
            EditorGUILayout.Space(10);
            showBeamCurveSettings = EditorGUILayout.Foldout(showBeamCurveSettings, "Beam Curve Settings", true, EditorStyles.foldoutHeader);
            
            if (showBeamCurveSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waveAmplitude"));
                EditorGUILayout.HelpBox("Amplitude of the wave motion.", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("waveFrequency"));
                EditorGUILayout.HelpBox("Speed of the wave motion.", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothingPasses"));
                EditorGUILayout.HelpBox("Number of smoothing passes (higher = smoother curves).", MessageType.None);
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("smoothingStrength"));
                EditorGUILayout.HelpBox("Smoothing factor (higher = more smoothing).", MessageType.None);
                
                EditorGUI.indentLevel--;
            }
            
            // Beam Presets
            EditorGUILayout.Space(10);
            showBeamPresets = EditorGUILayout.Foldout(showBeamPresets, "Beam Presets", true, EditorStyles.foldoutHeader);
            
            if (showBeamPresets)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.HelpBox("Select a preset to quickly configure the beam behavior:", MessageType.Info);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Enter the Gungeon Style"))
                {
                    Undo.RecordObject(energyWeapon, "Apply Beam Preset");
                    energyWeapon.beamSegments = 24;
                    energyWeapon.beamFollowSpeed = 8f;
                    energyWeapon.tipLagMultiplier = 1.5f;
                    energyWeapon.ropeStiffness = 0.65f;
                    energyWeapon.waveAmplitude = 0.5f;
                    energyWeapon.waveFrequency = 1.0f;
                    EditorUtility.SetDirty(energyWeapon);
                }
                
                if (GUILayout.Button("Super Whippy"))
                {
                    Undo.RecordObject(energyWeapon, "Apply Beam Preset");
                    energyWeapon.beamSegments = 32;
                    energyWeapon.beamFollowSpeed = 5f;
                    energyWeapon.tipLagMultiplier = 2.5f;
                    energyWeapon.ropeStiffness = 0.4f;
                    energyWeapon.waveAmplitude = 0.8f;
                    energyWeapon.waveFrequency = 1.5f;
                    EditorUtility.SetDirty(energyWeapon);
                }
                
                if (GUILayout.Button("Quick Response"))
                {
                    Undo.RecordObject(energyWeapon, "Apply Beam Preset");
                    energyWeapon.beamSegments = 16;
                    energyWeapon.beamFollowSpeed = 12f;
                    energyWeapon.tipLagMultiplier = 1.0f;
                    energyWeapon.ropeStiffness = 0.8f;
                    energyWeapon.waveAmplitude = 0.3f;
                    energyWeapon.waveFrequency = 0.8f;
                    EditorUtility.SetDirty(energyWeapon);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }
        
        // Simulate a 360° spin to test the beam curve
        private IEnumerator SimulateBeamSpin(EnergyWeapon energyWeapon)
        {
            Transform firePoint = energyWeapon.firePoint;
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
    
    // Create a utility class with a different name to avoid conflicts
    public static class EditorCoroutineUtility
    {
        public static void StartCoroutine(IEnumerator routine, Object owner)
        {
            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= update;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                    EditorApplication.update -= update;
                }
            };
            
            EditorApplication.update += update;
        }
    }
} 