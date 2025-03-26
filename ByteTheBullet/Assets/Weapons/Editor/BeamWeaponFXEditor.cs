using UnityEngine;
using UnityEditor;

namespace Weapons.Editor
{
    [CustomEditor(typeof(BeamWeaponFX))]
    public class BeamWeaponFXEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Reference to the BeamWeaponFX component
            BeamWeaponFX beamFX = (BeamWeaponFX)target;
            
            EditorGUI.BeginChangeCheck();
            
            // Draw the default inspector properties
            DrawDefaultInspector();
            
            bool changed = EditorGUI.EndChangeCheck();
            
            // Add info box for beam settings
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Adjust Beam Section Distance and Section Overlap to control how beam sections are placed. Enable Debug Beam Sections to visualize section placement in the Scene view.", MessageType.Info);
            
            // Show quick test controls
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Test Controls", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            float newWidth = EditorGUILayout.Slider("Test Beam Width", beamFX.beamWidth, 0.05f, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(beamFX, "Change Beam Width");
                beamFX.beamWidth = newWidth;
                changed = true;
            }
            
            EditorGUI.BeginChangeCheck();
            float newDistance = EditorGUILayout.Slider("Test Section Distance", beamFX.beamSectionDistance, 0.1f, 5.0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(beamFX, "Change Section Distance");
                beamFX.beamSectionDistance = newDistance;
                changed = true;
            }
            
            EditorGUI.BeginChangeCheck();
            float newOverlap = EditorGUILayout.Slider("Test Section Overlap", beamFX.sectionOverlap, 0f, 0.9f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(beamFX, "Change Section Overlap");
                beamFX.sectionOverlap = newOverlap;
                changed = true;
            }
            
            // When values change and play mode is active, force an update
            if (changed && Application.isPlaying)
            {
                // Force visuals to update immediately
                EditorUtility.SetDirty(beamFX);
            }
            
            // Add button to reset beam sections
            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Beam Sections"))
            {
                beamFX.ResetBeamSections();
            }
            
            // Add a test button for play mode
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Test Beam Animation"))
                {
                    Vector3 startPos = beamFX.transform.position;
                    Vector3 endPos = startPos + Vector3.right * 10f;
                    beamFX.UpdateBeamMiddleAnimation(startPos, endPos);
                }
            }
        }
    }
}