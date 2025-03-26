using UnityEngine;
using UnityEditor;

namespace Weapons.Editor
{
    [CustomEditor(typeof(BeamWeapon))]
    public class BeamWeaponEditor : UnityEditor.Editor
    {
        private bool showFreezeRaySettings = true;
        
        public override void OnInspectorGUI()
        {
            BeamWeapon beamWeapon = (BeamWeapon)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            // Beam visibility toggle
            EditorGUILayout.Space(10);
            if (GUILayout.Button(beamWeapon.config.hideBeamColor ? "Show Beam Color" : "Hide Beam Color"))
            {
                Undo.RecordObject(beamWeapon, "Toggle Beam Visibility");
                beamWeapon.ToggleBeamVisibility();
                EditorUtility.SetDirty(beamWeapon);
            }
            
            // Freeze Ray Presets
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
                
                SerializedProperty configProperty = serializedObject.FindProperty("config");
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