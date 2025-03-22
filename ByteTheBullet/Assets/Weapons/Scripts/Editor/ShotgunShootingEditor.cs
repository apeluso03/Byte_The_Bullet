using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ShotgunShooting))]
public class ShotgunShootingEditor : Editor
{
    // Property groups
    private SerializedProperty fireMode;
    private SerializedProperty pelletCount;
    private SerializedProperty spreadAngle;
    private SerializedProperty projectileSpeed;
    private SerializedProperty projectilePrefab;
    
    // Semi-auto properties
    private SerializedProperty semiAutoFireDelay;
    
    // Full-auto properties
    private SerializedProperty fullAutoFireRate;
    
    // Burst properties
    private SerializedProperty burstCount;
    private SerializedProperty burstDelay;
    private SerializedProperty burstCooldown;
    
    // Charged shot properties
    private SerializedProperty chargeTime;
    private SerializedProperty maxChargedPelletCount;
    private SerializedProperty chargedShotCooldown;
    private SerializedProperty chargingMaterial;
    private SerializedProperty fullyChargedColor;
    
    // Effect properties
    private SerializedProperty muzzleFlash;
    private SerializedProperty shootSound;
    private SerializedProperty pumpSound;
    private SerializedProperty chargeSound;
    private SerializedProperty chargeReleaseSound;
    private SerializedProperty shellEjection;
    private SerializedProperty shellCasingPrefab;
    private SerializedProperty shellEjectionForce;
    
    // References
    private SerializedProperty firePoint;
    
    private void OnEnable()
    {
        // Common properties
        fireMode = serializedObject.FindProperty("fireMode");
        pelletCount = serializedObject.FindProperty("pelletCount");
        spreadAngle = serializedObject.FindProperty("spreadAngle");
        projectileSpeed = serializedObject.FindProperty("projectileSpeed");
        projectilePrefab = serializedObject.FindProperty("projectilePrefab");
        firePoint = serializedObject.FindProperty("firePoint");
        
        // Semi-auto properties
        semiAutoFireDelay = serializedObject.FindProperty("semiAutoFireDelay");
        
        // Full-auto properties
        fullAutoFireRate = serializedObject.FindProperty("fullAutoFireRate");
        
        // Burst properties
        burstCount = serializedObject.FindProperty("burstCount");
        burstDelay = serializedObject.FindProperty("burstDelay");
        burstCooldown = serializedObject.FindProperty("burstCooldown");
        
        // Charged shot properties
        chargeTime = serializedObject.FindProperty("chargeTime");
        maxChargedPelletCount = serializedObject.FindProperty("maxChargedPelletCount");
        chargedShotCooldown = serializedObject.FindProperty("chargedShotCooldown");
        chargingMaterial = serializedObject.FindProperty("chargingMaterial");
        fullyChargedColor = serializedObject.FindProperty("fullyChargedColor");
        
        // Effect properties
        muzzleFlash = serializedObject.FindProperty("muzzleFlash");
        shootSound = serializedObject.FindProperty("shootSound");
        pumpSound = serializedObject.FindProperty("pumpSound");
        chargeSound = serializedObject.FindProperty("chargeSound");
        chargeReleaseSound = serializedObject.FindProperty("chargeReleaseSound");
        shellEjection = serializedObject.FindProperty("shellEjection");
        shellCasingPrefab = serializedObject.FindProperty("shellCasingPrefab");
        shellEjectionForce = serializedObject.FindProperty("shellEjectionForce");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Draw the script field
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((ShotgunShooting)target), typeof(MonoScript), false);
        }
        
        EditorGUILayout.Space();
        
        // Common settings
        EditorGUILayout.LabelField("Shotgun Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(pelletCount);
        EditorGUILayout.PropertyField(spreadAngle);
        EditorGUILayout.PropertyField(projectileSpeed);
        EditorGUILayout.PropertyField(projectilePrefab);
        
        EditorGUILayout.Space();
        
        // Fire mode selection
        EditorGUILayout.LabelField("Fire Type", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(fireMode, new GUIContent("Fire Mode"));
        bool fireModeChanged = EditorGUI.EndChangeCheck();
        
        EditorGUILayout.Space();
        
        // Draw settings based on selected fire mode
        ShotgunShooting.FireMode currentMode = (ShotgunShooting.FireMode)fireMode.enumValueIndex;
        
        // Semi-auto settings
        if (currentMode == ShotgunShooting.FireMode.SemiAuto)
        {
            EditorGUILayout.LabelField("Semi-Auto Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(semiAutoFireDelay, new GUIContent("Fire Delay"));
            EditorGUILayout.PropertyField(pumpSound);
        }
        
        // Full-auto settings
        else if (currentMode == ShotgunShooting.FireMode.FullAuto)
        {
            EditorGUILayout.LabelField("Full-Auto Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fullAutoFireRate, new GUIContent("Fire Rate"));
        }
        
        // Burst fire settings
        else if (currentMode == ShotgunShooting.FireMode.Burst)
        {
            EditorGUILayout.LabelField("Burst Fire Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(burstCount);
            EditorGUILayout.PropertyField(burstDelay);
            EditorGUILayout.PropertyField(burstCooldown);
        }
        
        // Charged shot settings
        else if (currentMode == ShotgunShooting.FireMode.Charged)
        {
            EditorGUILayout.LabelField("Charged Shot Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(chargeTime);
            EditorGUILayout.PropertyField(maxChargedPelletCount);
            EditorGUILayout.PropertyField(chargedShotCooldown);
            EditorGUILayout.PropertyField(chargingMaterial);
            EditorGUILayout.PropertyField(fullyChargedColor);
            EditorGUILayout.PropertyField(chargeSound);
            EditorGUILayout.PropertyField(chargeReleaseSound);
        }
        
        EditorGUILayout.Space();
        
        // Common effects settings
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(muzzleFlash);
        EditorGUILayout.PropertyField(shootSound);
        EditorGUILayout.PropertyField(shellEjection);
        EditorGUILayout.PropertyField(shellCasingPrefab);
        EditorGUILayout.PropertyField(shellEjectionForce);
        
        EditorGUILayout.Space();
        
        // References
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(firePoint);
        
        serializedObject.ApplyModifiedProperties();
        
        // If fire mode changed, inform the user about the change
        if (fireModeChanged)
        {
            EditorUtility.SetDirty(target);
            EditorGUILayout.HelpBox("Fire mode changed. Settings have been updated for the selected mode.", MessageType.Info);
        }
    }
} 