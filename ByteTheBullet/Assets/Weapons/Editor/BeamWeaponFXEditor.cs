using UnityEngine;
using UnityEditor;
using Weapons;

namespace Weapons.Editor
{
    [CustomEditor(typeof(BeamWeaponFX))]
    public class BeamWeaponFXEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Reference to the BeamWeaponFX component
            BeamWeaponFX beamFX = (BeamWeaponFX)target;
            
            // Draw the default inspector properties
            DrawDefaultInspector();
            
            // Add info box for beam settings
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Adjust Beam Section Distance and Section Overlap to control how beam sections are placed. Enable Debug Beam Sections to visualize section placement in the Scene view.", MessageType.Info);
            
            // Add button to reset beam sections
            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Beam Sections"))
            {
                beamFX.ResetBeamSections();
            }
        }
    }
} 