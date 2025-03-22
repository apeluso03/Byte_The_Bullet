#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// Simple attribute to make fields read-only in the inspector
public class ReadOnlyAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Save previous GUI state
        EditorGUI.BeginDisabledGroup(true);
        
        // Draw the property
        EditorGUI.PropertyField(position, property, label);
        
        // Restore GUI state
        EditorGUI.EndDisabledGroup();
    }
}
#endif 