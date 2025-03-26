using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WeaponAiming))]
public class WeaponAimingEditor : Editor
{
    private static Vector2[] presetDirections = new Vector2[]
    {
        new Vector2(1, 0),    // Right
        new Vector2(0, 1),    // Up
        new Vector2(-1, 0),   // Left
        new Vector2(0, -1),   // Down
        new Vector2(1, 1).normalized,    // Up-Right
        new Vector2(-1, 1).normalized,   // Up-Left
        new Vector2(-1, -1).normalized,  // Down-Left
        new Vector2(1, -1).normalized    // Down-Right
    };
    
    private static string[] directionNames = new string[]
    {
        "Right", "Up", "Left", "Down", 
        "Up-Right", "Up-Left", "Down-Left", "Down-Right"
    };
    
    private const int previewSize = 200;
    private bool showPreview = true;
    private SerializedProperty rightHandGripPoint;
    private SerializedProperty leftHandGripPoint;
    
    private void OnEnable()
    {
        // Get serialized properties we need direct access to
        rightHandGripPoint = serializedObject.FindProperty("rightHandGripPoint");
        leftHandGripPoint = serializedObject.FindProperty("leftHandGripPoint");
    }
    
    public override void OnInspectorGUI()
    {
        WeaponAiming weaponAiming = (WeaponAiming)target;
        serializedObject.Update();
        
        // References section
        EditorGUILayout.PropertyField(serializedObject.FindProperty("player"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponPointLeft"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponPointRight"));
        
        // Handle missing references warnings
        bool missingReferences = false;
        string missingMessage = "Missing references: ";
        
        if (weaponAiming.player == null)
        {
            missingReferences = true;
            missingMessage += "Player, ";
        }
        if (weaponAiming.weaponPointLeft == null)
        {
            missingReferences = true;
            missingMessage += "Left Hand, ";
        }
        if (weaponAiming.weaponPointRight == null)
        {
            missingReferences = true;
            missingMessage += "Right Hand, ";
        }
        
        if (missingReferences)
        {
            EditorGUILayout.HelpBox(missingMessage.TrimEnd(',', ' '), MessageType.Warning);
        }
        
        // Hand visuals section
        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftHandVisual"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightHandVisual"));
        
        // Draw hand switching property groups
        EditorGUILayout.PropertyField(serializedObject.FindProperty("swapAtCenterLine"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("handSwitchThreshold"));
        
        // Weapon layering section
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hideWeaponWhenAimingUp"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("upDirectionThreshold"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("layerSwitchBuffer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("behindPlayerLayer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("inFrontOfPlayerLayer"));
        
        // GRIP POINTS SECTION - moved below weapon layering
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("GRIP POINT SETTINGS", EditorStyles.boldLabel);
        
        // Store original label width
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        
        // RIGHT HAND GRIP
        EditorGUILayout.LabelField("Right Hand Grip", EditorStyles.boldLabel);
        
        // X coordinate with fine-tuning buttons
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 15f; // Make the label narrow
        EditorGUILayout.PropertyField(rightHandGripPoint.FindPropertyRelative("x"), new GUIContent("X"));
        EditorGUIUtility.labelWidth = oldLabelWidth; // Restore label width
        
        GUILayout.Space(5); // Add some spacing
        if (GUILayout.Button("+", GUILayout.Width(25))) rightHandGripPoint.FindPropertyRelative("x").floatValue += 0.1f;
        if (GUILayout.Button("-", GUILayout.Width(25))) rightHandGripPoint.FindPropertyRelative("x").floatValue -= 0.1f;
        EditorGUILayout.EndHorizontal();
        
        // Y coordinate with fine-tuning buttons
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 15f; // Make the label narrow
        EditorGUILayout.PropertyField(rightHandGripPoint.FindPropertyRelative("y"), new GUIContent("Y"));
        EditorGUIUtility.labelWidth = oldLabelWidth; // Restore label width
        
        GUILayout.Space(5); // Add some spacing
        if (GUILayout.Button("+", GUILayout.Width(25))) rightHandGripPoint.FindPropertyRelative("y").floatValue += 0.1f;
        if (GUILayout.Button("-", GUILayout.Width(25))) rightHandGripPoint.FindPropertyRelative("y").floatValue -= 0.1f;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // LEFT HAND GRIP
        EditorGUILayout.LabelField("Left Hand Grip", EditorStyles.boldLabel);
        
        // X coordinate with fine-tuning buttons
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 15f; // Make the label narrow
        EditorGUILayout.PropertyField(leftHandGripPoint.FindPropertyRelative("x"), new GUIContent("X"));
        EditorGUIUtility.labelWidth = oldLabelWidth; // Restore label width
        
        GUILayout.Space(5); // Add some spacing
        if (GUILayout.Button("+", GUILayout.Width(25))) leftHandGripPoint.FindPropertyRelative("x").floatValue += 0.1f;
        if (GUILayout.Button("-", GUILayout.Width(25))) leftHandGripPoint.FindPropertyRelative("x").floatValue -= 0.1f;
        EditorGUILayout.EndHorizontal();
        
        // Y coordinate with fine-tuning buttons
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 15f; // Make the label narrow
        EditorGUILayout.PropertyField(leftHandGripPoint.FindPropertyRelative("y"), new GUIContent("Y"));
        EditorGUIUtility.labelWidth = oldLabelWidth; // Restore label width
        
        GUILayout.Space(5); // Add some spacing
        if (GUILayout.Button("+", GUILayout.Width(25))) leftHandGripPoint.FindPropertyRelative("y").floatValue += 0.1f;
        if (GUILayout.Button("-", GUILayout.Width(25))) leftHandGripPoint.FindPropertyRelative("y").floatValue -= 0.1f;
        EditorGUILayout.EndHorizontal();
        
        // Add copy buttons for convenience
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Right → Left"))
        {
            // For right to left, we need to mirror the X coordinate (negate it)
            leftHandGripPoint.FindPropertyRelative("x").floatValue = -rightHandGripPoint.FindPropertyRelative("x").floatValue;
            // Y coordinate typically stays the same
            leftHandGripPoint.FindPropertyRelative("y").floatValue = rightHandGripPoint.FindPropertyRelative("y").floatValue;
            
            // Automatically switch to preview left hand
            weaponAiming.useLeftHandInEditor = true;
            
            // If we're aiming left/right, mirror the aim direction
            if (Mathf.Abs(weaponAiming.editorAimDirection.x) > 0.01f)
            {
                // Make sure it aims to the left (negative X)
                if (weaponAiming.editorAimDirection.x > 0)
                {
                    weaponAiming.editorAimDirection.x = -weaponAiming.editorAimDirection.x;
                }
            }
            
            serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Copy Left → Right"))
        {
            // For left to right, we need to mirror the X coordinate (negate it)
            rightHandGripPoint.FindPropertyRelative("x").floatValue = -leftHandGripPoint.FindPropertyRelative("x").floatValue;
            // Y coordinate typically stays the same
            rightHandGripPoint.FindPropertyRelative("y").floatValue = leftHandGripPoint.FindPropertyRelative("y").floatValue;
            
            // Automatically switch to preview right hand
            weaponAiming.useLeftHandInEditor = false;
            
            // If we're aiming left/right, mirror the aim direction
            if (Mathf.Abs(weaponAiming.editorAimDirection.x) > 0.01f)
            {
                // Make sure it aims to the right (positive X)
                if (weaponAiming.editorAimDirection.x < 0)
                {
                    weaponAiming.editorAimDirection.x = -weaponAiming.editorAimDirection.x;
                }
            }
            
            serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
            SceneView.RepaintAll();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        // Add our preview section
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("WEAPON PREVIEW", EditorStyles.boldLabel);
        
        // Toggle for enabling preview mode
        bool wasPreviewMode = weaponAiming.editorPreviewMode;
        weaponAiming.editorPreviewMode = EditorGUILayout.Toggle("Enable Preview Mode", weaponAiming.editorPreviewMode);
        
        if (weaponAiming.editorPreviewMode != wasPreviewMode)
        {
            // Force repaint to update the preview
            SceneView.RepaintAll();
        }
        
        if (weaponAiming.editorPreviewMode)
        {
            // Hand selection with automatic direction adjustment
            bool previousHandSetting = weaponAiming.useLeftHandInEditor;
            weaponAiming.useLeftHandInEditor = EditorGUILayout.Toggle("Use Left Hand", weaponAiming.useLeftHandInEditor);
            
            // If the hand setting changed, update the direction to point the correct way
            if (previousHandSetting != weaponAiming.useLeftHandInEditor)
            {
                // Check if we're aiming left/right (check the X component)
                if (Mathf.Abs(weaponAiming.editorAimDirection.x) > 0.01f)
                {
                    // If switching hands, adjust aim direction to mirror horizontally
                    weaponAiming.editorAimDirection.x = -weaponAiming.editorAimDirection.x;
                    
                    // Force a Scene view repaint
                    SceneView.RepaintAll();
                }
            }
            
            // Direction selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Aim Direction");
            
            // Preset buttons for directions (just the main 4 for simplicity)
            for (int i = 0; i < 4; i++)
            {
                // Check if this is the current direction
                bool isActive = (weaponAiming.editorAimDirection.normalized - presetDirections[i]).sqrMagnitude < 0.01f;
                
                // Use a different style for the active direction
                GUIStyle buttonStyle = isActive ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                
                if (GUILayout.Button(directionNames[i], buttonStyle))
                {
                    weaponAiming.editorAimDirection = presetDirections[i];
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Preview window
            EditorGUILayout.Space();
            showPreview = EditorGUILayout.Foldout(showPreview, "Show Preview Window", true);
            
            if (showPreview)
            {
                Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize);
                DrawPreviewTexture(previewRect, weaponAiming);
            }
            
            if (GUILayout.Button("Apply to Scene View"))
            {
                SceneView.RepaintAll();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // DEBUG SETTINGS - moved below preview
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("DEBUG SETTINGS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugLines"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugColor"));
        
        EditorGUILayout.EndVertical();
        
        serializedObject.ApplyModifiedProperties();
        
        // Force refresh when values change
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void DrawPreviewTexture(Rect rect, WeaponAiming weaponAiming)
    {
        // Check if we have the required components
        if (weaponAiming.player == null || 
            weaponAiming.weaponPointLeft == null || 
            weaponAiming.weaponPointRight == null)
        {
            EditorGUI.HelpBox(rect, "Missing required references", MessageType.Error);
            return;
        }
        
        // Get sprite renderer
        SpriteRenderer spriteRenderer = weaponAiming.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            EditorGUI.HelpBox(rect, "No sprite found", MessageType.Error);
            return;
        }
        
        // Create a preview background
        Texture2D background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 1f));
        background.Apply();
        
        // Draw the background
        GUI.DrawTexture(rect, background);
        
        // Get necessary values for preview
        Transform previewHand = weaponAiming.useLeftHandInEditor ? 
            weaponAiming.weaponPointLeft : weaponAiming.weaponPointRight;
        Vector2 gripPoint = weaponAiming.useLeftHandInEditor ? 
            weaponAiming.leftHandGripPoint : weaponAiming.rightHandGripPoint;
        
        // Get sprite texture
        Texture2D spriteTexture = AssetPreview.GetAssetPreview(spriteRenderer.sprite);
        if (spriteTexture == null)
        {
            spriteTexture = spriteRenderer.sprite.texture;
        }
        
        // Calculate rotation based on aim direction
        float angle = Mathf.Atan2(weaponAiming.editorAimDirection.y, weaponAiming.editorAimDirection.x) * Mathf.Rad2Deg;
        
        // Calculate center of preview
        Vector2 center = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
        
        // Draw hand position indicator
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(center, Vector3.forward, 5f);
        
        // Draw aim direction line
        Handles.color = Color.blue;
        Handles.DrawLine(center, center + weaponAiming.editorAimDirection.normalized * 40f);
        
        // Calculate grip offset
        Vector2 localGripOffset;
        if (weaponAiming.useLeftHandInEditor)
        {
            localGripOffset = new Vector2(-gripPoint.x, -gripPoint.y);
        }
        else
        {
            localGripOffset = gripPoint;
        }
        
        // Rotate grip offset
        float radians = angle * Mathf.Deg2Rad;
        Vector2 rotatedOffset = new Vector2(
            localGripOffset.x * Mathf.Cos(radians) - localGripOffset.y * Mathf.Sin(radians),
            localGripOffset.x * Mathf.Sin(radians) + localGripOffset.y * Mathf.Cos(radians)
        );
        
        // Calculate final weapon position
        Vector2 weaponPosition = center - rotatedOffset;
        
        // Draw grip connection line
        Handles.color = Color.yellow;
        Handles.DrawLine(center, weaponPosition);
        
        // Create a matrix to position and rotate the sprite
        Matrix4x4 matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, weaponPosition);
        
        // Calculate sprite size for display
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size * 30f; // Scale for preview
        
        // Apply Y-flipping if using left hand
        if (weaponAiming.useLeftHandInEditor)
        {
            spriteSize.y = -spriteSize.y;
        }
        
        // Draw the sprite
        Rect spriteRect = new Rect(
            weaponPosition.x - spriteSize.x / 2, 
            weaponPosition.y - spriteSize.y / 2, 
            spriteSize.x, 
            spriteSize.y
        );
        
        GUI.DrawTexture(spriteRect, spriteTexture);
        
        // Reset rotation
        GUI.matrix = matrix;
        
        // Draw labels
        GUI.color = Color.yellow;
        GUI.Label(new Rect(rect.x + 5, rect.y + 5, 100, 20), "Hand Point");
        
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + rect.width - 120, rect.y + rect.height - 40, 120, 40), 
            $"Grip: {(weaponAiming.useLeftHandInEditor ? "LEFT" : "RIGHT")}\nAngle: {angle:F0}°");
    }
    
    // Scene view preview improvements
    private void OnSceneGUI()
    {
        WeaponAiming weaponAiming = (WeaponAiming)target;
        
        if (weaponAiming.player == null || 
            weaponAiming.weaponPointLeft == null || 
            weaponAiming.weaponPointRight == null)
        {
            return;
        }
        
        // If the weapon is in preview mode, add some additional handles
        if (weaponAiming.editorPreviewMode)
        {
            Transform previewHand = weaponAiming.useLeftHandInEditor ? 
                weaponAiming.weaponPointLeft : weaponAiming.weaponPointRight;
                
            Vector2 gripPoint = weaponAiming.useLeftHandInEditor ? 
                weaponAiming.leftHandGripPoint : weaponAiming.rightHandGripPoint;
                
            // Get sprite renderer
            SpriteRenderer spriteRenderer = weaponAiming.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            
            // Draw handle for grip point adjustment
            EditorGUI.BeginChangeCheck();
            
            Vector3 worldGripPoint;
            if (weaponAiming.useLeftHandInEditor)
            {
                // For left hand, we need to handle the flipping
                worldGripPoint = weaponAiming.transform.TransformPoint(
                    new Vector3(-gripPoint.x, -gripPoint.y, 0)
                );
            }
            else
            {
                worldGripPoint = weaponAiming.transform.TransformPoint(
                    new Vector3(gripPoint.x, gripPoint.y, 0)
                );
            }
            
            Vector3 newWorldGripPoint = Handles.PositionHandle(worldGripPoint, Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(weaponAiming, "Adjust Grip Point");
                
                // Convert back to local space
                Vector3 newLocalGripPoint = weaponAiming.transform.InverseTransformPoint(newWorldGripPoint);
                
                if (weaponAiming.useLeftHandInEditor)
                {
                    weaponAiming.leftHandGripPoint = new Vector2(-newLocalGripPoint.x, -newLocalGripPoint.y);
                }
                else
                {
                    weaponAiming.rightHandGripPoint = new Vector2(newLocalGripPoint.x, newLocalGripPoint.y);
                }
            }
        }
    }
} 