using UnityEngine;
using UnityEditor;
using Weapons;

[CustomEditor(typeof(ShotgunWeapon))]
public class ShotgunWeaponEditor : Editor
{
    // Preview settings
    private bool showPreview = true;
    private float previewLength = 5f;
    
    // Section foldouts
    private bool showPelletSettings = true;
    private bool showFireSettings = true;
    private bool showFeedbackSettings = true;
    private bool showAmmoSettings = true;
    //private bool showSoundSettings = true;
    
    // Add this as a class field at the top of the ShotgunWeaponEditor class
    private float[] pelletAngles;
    private int lastPelletCount = -1;
    private float lastSpreadAngle = -1;
    
    public override void OnInspectorGUI()
    {
        ShotgunWeapon shotgun = (ShotgunWeapon)target;
        
        // Custom header
        EditorGUILayout.Space(5);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Malice's Shotgun Config", headerStyle);
        EditorGUILayout.Space(10);
        
        // Fire Point Field with Create button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Fire Point");
        
        shotgun.firePoint = (Transform)EditorGUILayout.ObjectField(shotgun.firePoint, typeof(Transform), true);
        
        if (GUILayout.Button("Create", GUILayout.Width(60)))
        {
            shotgun.CreateFirePoint();
            EditorUtility.SetDirty(shotgun);
        }
        EditorGUILayout.EndHorizontal();
        
        if (shotgun.firePoint == null)
        {
            EditorGUILayout.HelpBox("No FirePoint assigned! Projectiles will spawn from the weapon's center.", MessageType.Warning);
        }
        
        EditorGUILayout.Space(10);
        
        // MOVED: Firing Settings goes here, right after the Fire Point
        showFireSettings = EditorGUILayout.Foldout(showFireSettings, "Firing Settings", true, EditorStyles.foldoutHeader);
        if (showFireSettings)
        {
            EditorGUI.indentLevel++;
            
            // Move firing type to the top
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Firing Type", "How the shotgun fires when player inputs are received"));
            shotgun.firingType = (ShotgunWeapon.FiringType)EditorGUILayout.EnumPopup(shotgun.firingType);
            EditorGUILayout.EndHorizontal();
            
            // Then show fire sound below it
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Fire Sound", "Sound played when firing"));
            shotgun.shotgunFireSound = (AudioClip)EditorGUILayout.ObjectField(
                shotgun.shotgunFireSound, typeof(AudioClip), false, GUILayout.Width(150));
            GUI.enabled = shotgun.shotgunFireSound != null;
            if (GUILayout.Button("►", GUILayout.Width(25)))
            {
                PlayClip(shotgun.shotgunFireSound, shotgun.soundVolume);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Different settings based on firing type
            switch (shotgun.firingType)
            {
                case ShotgunWeapon.FiringType.SemiAuto:
                case ShotgunWeapon.FiringType.Auto:
                    shotgun.shotgunFireRate = EditorGUILayout.Slider(new GUIContent("Fire Rate", "Shots per second"), shotgun.shotgunFireRate, 0.5f, 10f);
                    break;
                    
                case ShotgunWeapon.FiringType.Burst:
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Burst Settings", EditorStyles.boldLabel);
                    
                    shotgun.burstCount = EditorGUILayout.IntSlider(
                        new GUIContent("Burst Count", "Number of shots in each burst"), 
                        shotgun.burstCount, 2, 6);
                        
                    shotgun.burstDensity = EditorGUILayout.Slider(
                        new GUIContent("Burst Density", "Timing between shots in a burst (lower = tighter pattern)"), 
                        shotgun.burstDensity, 0.05f, 0.3f);
                        
                    shotgun.timeBetweenBursts = EditorGUILayout.Slider(
                        new GUIContent("Time Between Bursts", "Delay between consecutive bursts"), 
                        shotgun.timeBetweenBursts, 0.2f, 2.0f);
                        
                    shotgun.continuousBurst = EditorGUILayout.Toggle(
                        new GUIContent("Continuous Burst", "When enabled, holding the fire button will continuously fire bursts"), 
                        shotgun.continuousBurst);
                        
                    // Add visual burst pattern preview
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Burst Pattern");
                    Rect previewRect = GUILayoutUtility.GetRect(200, 30);
                    DrawBurstPatternPreview(previewRect, shotgun.burstCount, shotgun.burstDensity);
                    EditorGUILayout.EndHorizontal();
                    
                    break;
                    
                case ShotgunWeapon.FiringType.PumpAction:
                    shotgun.pumpDelay = EditorGUILayout.Slider(
                        new GUIContent("Pump Delay", "Time to pump the shotgun (seconds)"), 
                        shotgun.pumpDelay, 0.2f, 1.5f);
                        
                    // Add pump sound within pump action settings
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Pump Sound", "Sound played when pumping the shotgun"));
                    shotgun.pumpSound = (AudioClip)EditorGUILayout.ObjectField(
                        shotgun.pumpSound, typeof(AudioClip), false, GUILayout.Width(150));
                    GUI.enabled = shotgun.pumpSound != null;
                    if (GUILayout.Button("►", GUILayout.Width(25)))
                    {
                        PlayClip(shotgun.pumpSound, shotgun.soundVolume);
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                        
                    EditorGUILayout.HelpBox("In pump action mode, press R to manually pump after firing.", MessageType.Info);
                    break;
            }
            
            // Sound customization settings at the bottom of firing section
        EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Sound Customization", EditorStyles.boldLabel);
            
            shotgun.soundVolume = EditorGUILayout.Slider(
                new GUIContent("Sound Volume", "Volume multiplier for all weapon sounds"),
                shotgun.soundVolume, 0f, 1f);
            
            shotgun.randomizePitch = EditorGUILayout.Toggle(
                new GUIContent("Randomize Pitch", "Add slight pitch variation for more natural sound"),
                shotgun.randomizePitch);
                
            if (shotgun.randomizePitch)
            {
                EditorGUI.indentLevel++;
                shotgun.pitchVariation = EditorGUILayout.Slider(
                    new GUIContent("Pitch Variation", "Amount of random variation in pitch"),
                    shotgun.pitchVariation, 0f, 0.3f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);
        
        // Ammunition Settings
        showAmmoSettings = EditorGUILayout.Foldout(showAmmoSettings, "Ammunition", true, EditorStyles.foldoutHeader);
        if (showAmmoSettings)
        {
            EditorGUI.indentLevel++;
            
            // Magazine Size
            shotgun.magazineSize = EditorGUILayout.IntSlider(
                new GUIContent("Magazine Size", "How many shots before reloading"),
                shotgun.magazineSize, 1, 30);
            
            // Move Max Reserve to be right below Magazine Size
            int newMaxReserve = EditorGUILayout.IntSlider(
                new GUIContent("Max Reserve", "Maximum amount of reserve ammunition"),
                shotgun.MaxReserveAmmo, 12, 200);
            if (newMaxReserve != shotgun.MaxReserveAmmo)
            {
                shotgun.MaxReserveAmmo = newMaxReserve;
                
                // If current reserve ammo is higher than new max, clamp it
                if (shotgun.ReserveAmmo > shotgun.MaxReserveAmmo)
                    shotgun.ReserveAmmo = shotgun.MaxReserveAmmo;
                
                EditorUtility.SetDirty(shotgun);
            }
            
            EditorGUILayout.Space(5);
            
            // Current Ammo Display
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Current Ammo");
            
            // Make a custom progress bar for current ammo
            Rect ammoRect = GUILayoutUtility.GetRect(100, 18);
            float ammoPercentage = shotgun.magazineSize > 0 ? (float)shotgun.CurrentAmmo / shotgun.magazineSize : 0;
            EditorGUI.ProgressBar(ammoRect, ammoPercentage, $"{shotgun.CurrentAmmo} / {shotgun.magazineSize}");
            
            // Add a quick "Fill" button
            if (GUILayout.Button("Fill", GUILayout.Width(60)))
            {
                shotgun.FillAmmo(true, false);
                EditorUtility.SetDirty(shotgun);
            }
            EditorGUILayout.EndHorizontal();
            
            // Reserve Ammo Display
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Reserve Ammo");
            
            // Similar progress bar, but for reserve ammo
            Rect reserveRect = GUILayoutUtility.GetRect(100, 18);
            float reservePercentage = shotgun.MaxReserveAmmo > 0 ? (float)shotgun.ReserveAmmo / shotgun.MaxReserveAmmo : 0;
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.8f);
            EditorGUI.ProgressBar(reserveRect, reservePercentage, $"{shotgun.ReserveAmmo} / {shotgun.MaxReserveAmmo}");
            GUI.backgroundColor = originalColor;
            
            // Add fill button
            if (GUILayout.Button("Fill", GUILayout.Width(60)))
            {
                shotgun.ReserveAmmo = shotgun.MaxReserveAmmo;
                EditorUtility.SetDirty(shotgun);
            }
            EditorGUILayout.EndHorizontal();
            
            // Reload Time
            shotgun.reloadTime = EditorGUILayout.Slider(
                new GUIContent("Reload Time", "Time in seconds to reload"),
                shotgun.reloadTime, 0.5f, 3.0f);
            
            // Auto-reload toggle
            shotgun.autoReloadWhenEmpty = EditorGUILayout.Toggle(
                new GUIContent("Auto-Reload When Empty", "Automatically reload when magazine is empty"),
                shotgun.autoReloadWhenEmpty);
            
            EditorGUILayout.Space(5);
            
            // Then add reload and empty sounds
            EditorGUILayout.LabelField("Ammunition Sounds", EditorStyles.boldLabel);
            
            // Reload sound
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Reload Sound", "Sound played when reloading"));
            shotgun.shotgunReloadSound = (AudioClip)EditorGUILayout.ObjectField(
                shotgun.shotgunReloadSound, typeof(AudioClip), false, GUILayout.Width(150));
            GUI.enabled = shotgun.shotgunReloadSound != null;
            if (GUILayout.Button("►", GUILayout.Width(25)))
            {
                PlayClip(shotgun.shotgunReloadSound, shotgun.soundVolume);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Empty sound
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Empty Sound", "Sound played when out of ammo"));
            shotgun.shotgunEmptySound = (AudioClip)EditorGUILayout.ObjectField(
                shotgun.shotgunEmptySound, typeof(AudioClip), false, GUILayout.Width(150));
            GUI.enabled = shotgun.shotgunEmptySound != null;
            if (GUILayout.Button("►", GUILayout.Width(25)))
            {
                PlayClip(shotgun.shotgunEmptySound, shotgun.soundVolume);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Helpful buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Full Ammo"))
            {
                shotgun.FillAmmo(true, true);
                EditorUtility.SetDirty(shotgun);
            }
            
            if (GUILayout.Button("Empty Magazine"))
            {
                shotgun.EmptyMagazine();
                EditorUtility.SetDirty(shotgun);
            }

            // Add this new button for emptying reserve ammo
            if (GUILayout.Button("Empty Reserve"))
            {
                shotgun.ReserveAmmo = 0;
                EditorUtility.SetDirty(shotgun);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);
        
        // Pellet Settings
        showPelletSettings = EditorGUILayout.Foldout(showPelletSettings, "Pellet Settings", true, EditorStyles.foldoutHeader);
        if (showPelletSettings)
        {
            EditorGUI.indentLevel++;
            
            // Make sure these fields are in the correct order:
            shotgun.pelletCount = EditorGUILayout.IntSlider("Pellet Count", shotgun.pelletCount, 1, 20);
            shotgun.pelletSpeed = EditorGUILayout.Slider("Pellet Speed", shotgun.pelletSpeed, 1f, 30f);
            shotgun.spreadAngle = EditorGUILayout.Slider("Spread Angle", shotgun.spreadAngle, 0f, 90f);
            
            // Make the toggle very visible with a space before it:
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);
            shotgun.randomPelletPaths = EditorGUILayout.Toggle(
                new GUIContent("Random Paths", "When enabled, pellets follow random paths within the spread. When disabled, pellets are evenly distributed."),
                shotgun.randomPelletPaths);
            EditorGUILayout.Space(5);
            
            // Add prefab field and toggle
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Pellet Appearance", EditorStyles.boldLabel);
            
            // Prefab field
            shotgun.pelletPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Pellet Prefab", "Optional custom prefab for pellets. If assigned, uses this instead of generating basic circular pellets"),
                shotgun.pelletPrefab, typeof(GameObject), false);
            
            // Apply settings toggle
            if (shotgun.pelletPrefab != null)
            {
                shotgun.applySettingsToPrefab = EditorGUILayout.Toggle(
                    new GUIContent("Apply Settings To Prefab", "When enabled, the color and size settings below will be applied to your prefab"),
                    shotgun.applySettingsToPrefab);
                
                EditorGUILayout.HelpBox("If your prefab has special materials or shaders, you may want to disable 'Apply Settings' to preserve its appearance.", MessageType.Info);
            }
            
            EditorGUI.BeginDisabledGroup(shotgun.pelletPrefab != null && !shotgun.applySettingsToPrefab);
            
            // Original appearance settings
            shotgun.pelletSize = EditorGUILayout.Slider("Pellet Size", shotgun.pelletSize, 0.05f, 1f);
            shotgun.pelletColor = EditorGUILayout.ColorField("Pellet Color", shotgun.pelletColor);
            
            EditorGUI.EndDisabledGroup();
            
            // Visual representation of pellet spread in Inspector
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Spread Preview:", EditorStyles.boldLabel);

            // Create a rect for our visualization
            Rect spreadRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, 80);

            EditorGUI.DrawRect(spreadRect, new Color(0.2f, 0.2f, 0.2f)); // Dark background

            // Draw the spread visualization - pass the randomPelletPaths value
            DrawSpreadVisualization(spreadRect, shotgun.spreadAngle, shotgun.pelletCount, shotgun.pelletColor, shotgun.randomPelletPaths);
            
            EditorGUI.indentLevel--;
            
            // Preset buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Tight Spread"))
            {
                shotgun.pelletCount = 6;
                shotgun.spreadAngle = 15f;
                EditorUtility.SetDirty(shotgun);
            }
            if (GUILayout.Button("Standard"))
            {
                shotgun.pelletCount = 8;
                shotgun.spreadAngle = 30f;
                EditorUtility.SetDirty(shotgun);
            }
            if (GUILayout.Button("Wide Spread"))
            {
                shotgun.pelletCount = 12;
                shotgun.spreadAngle = 45f;
                EditorUtility.SetDirty(shotgun);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(10);
        
        // Feedback Settings
        showFeedbackSettings = EditorGUILayout.Foldout(showFeedbackSettings, "Feedback Settings", true, EditorStyles.foldoutHeader);
        if (showFeedbackSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Screen Shake", EditorStyles.boldLabel);
            
            shotgun.enableScreenShake = EditorGUILayout.Toggle(
                new GUIContent("Enable Screen Shake", "Toggle screen shake effect when firing"),
                shotgun.enableScreenShake);
                
            if (shotgun.enableScreenShake)
            {
                EditorGUI.indentLevel++;
                
                shotgun.screenShakeIntensity = EditorGUILayout.Slider(
                    new GUIContent("Shake Intensity", "How strong the screen shake effect is"),
                    shotgun.screenShakeIntensity, 0f, 0.5f);
                    
                shotgun.screenShakeDuration = EditorGUILayout.Slider(
                    new GUIContent("Shake Duration", "How long the screen shake lasts (in seconds)"),
                    shotgun.screenShakeDuration, 0.1f, 1.0f);
                    
                shotgun.screenShakeFrequency = EditorGUILayout.Slider(
                    new GUIContent("Shake Frequency", "How fast the screen shakes (higher = more jittery)"),
                    shotgun.screenShakeFrequency, 10f, 40f);
                    
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
            
            shotgun.enableMuzzleFlash = EditorGUILayout.Toggle(
                new GUIContent("Enable Muzzle Flash", "Show a muzzle flash when firing"),
                shotgun.enableMuzzleFlash);
                
            if (shotgun.enableMuzzleFlash)
            {
                EditorGUI.indentLevel++;
                
                shotgun.muzzleFlashPrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Muzzle Flash Prefab", "Prefab to spawn as muzzle flash"),
                    shotgun.muzzleFlashPrefab, typeof(GameObject), false);
                    
                shotgun.muzzleFlashDuration = EditorGUILayout.Slider(
                    new GUIContent("Flash Duration", "How long the muzzle flash appears (in seconds)"),
                    shotgun.muzzleFlashDuration, 0.05f, 0.5f);
                    
                shotgun.muzzleFlashScale = EditorGUILayout.Slider(
                    new GUIContent("Flash Scale", "Size multiplier for the muzzle flash"),
                    shotgun.muzzleFlashScale, 0.5f, 3f);
                    
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Physical Feedback", EditorStyles.boldLabel);
            
            shotgun.recoilForce = EditorGUILayout.Slider(
                new GUIContent("Recoil Force", "How much the shotgun kicks back when firing"),
                shotgun.recoilForce, 0f, 0.15f);
                
            shotgun.recoilDuration = EditorGUILayout.Slider(
                new GUIContent("Recoil Duration", "How long the recoil animation lasts (in seconds)"),
                shotgun.recoilDuration, 0.1f, 0.5f);
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space(10);
        
        // Preview settings
        showPreview = EditorGUILayout.Foldout(showPreview, "Spread Visualization", true, EditorStyles.foldoutHeader);
        if (showPreview)
        {
            previewLength = EditorGUILayout.Slider("Preview Length", previewLength, 1f, 10f);
            
            // Only keep the new tip explaining the preview vs gameplay behavior
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Note: The preview shows a stable sample pattern. During gameplay, each shot will generate a unique random pattern within your spread cone.", MessageType.Info);
        }
        
        // Apply changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(shotgun);
            SceneView.RepaintAll();
        }
    }
    
    // Draw visual representation of the shotgun spread in the scene view
    private void OnSceneGUI()
    {
        ShotgunWeapon shotgun = (ShotgunWeapon)target;
        
        if (!showPreview)
            return;
        
        // Determine the position and forward direction to use for visualization
        Vector3 originPos = shotgun.firePoint != null ? shotgun.firePoint.position : shotgun.transform.position;
        Vector3 originDir = shotgun.firePoint != null ? shotgun.firePoint.right : shotgun.transform.right;
        
        // Draw main fire direction
        Handles.color = Color.blue;
        Handles.ArrowHandleCap(
            0,
            originPos,
            Quaternion.LookRotation(originDir),
            previewLength * 0.5f,
            EventType.Repaint
        );
        
        // Calculate spread bounds
        float halfAngle = shotgun.spreadAngle / 2;
        Vector3 upperBound = Quaternion.Euler(0, 0, halfAngle) * originDir;
        Vector3 lowerBound = Quaternion.Euler(0, 0, -halfAngle) * originDir;
        
        // Draw spread cone bounds
        Handles.color = Color.yellow;
        Handles.DrawLine(originPos, originPos + upperBound * previewLength);
        Handles.DrawLine(originPos, originPos + lowerBound * previewLength);
        
        // Draw arc at the end of the preview length
        Handles.DrawWireArc(
            originPos,
            Vector3.forward,
            lowerBound,
            shotgun.spreadAngle,
            previewLength
        );
        
        // Draw sample pellet paths
        Handles.color = shotgun.pelletColor;

        // Check if we need to regenerate the random pattern
        if (pelletAngles == null || 
            lastPelletCount != shotgun.pelletCount || 
            lastSpreadAngle != shotgun.spreadAngle)
        {
            // Only regenerate the random pattern when parameters change
            pelletAngles = new float[shotgun.pelletCount];
            Random.InitState(shotgun.pelletCount * 1000 + Mathf.RoundToInt(shotgun.spreadAngle * 10));
            
            for (int i = 0; i < shotgun.pelletCount; i++)
            {
                pelletAngles[i] = Random.Range(-halfAngle, halfAngle);
            }
            
            lastPelletCount = shotgun.pelletCount;
            lastSpreadAngle = shotgun.spreadAngle;
        }

        // Now use the stable random angles to draw the pellets
        for (int i = 0; i < shotgun.pelletCount; i++)
        {
            float randomAngle = pelletAngles[i];
            Vector3 direction = Quaternion.Euler(0, 0, randomAngle) * originDir;
            
            // Draw pellet path
            Handles.DrawLine(
                originPos,
                originPos + direction * previewLength
            );
            
            // Draw pellet at the end
            float pelletRadius = shotgun.pelletSize * 0.2f;
            Handles.DrawSolidDisc(
                originPos + direction * previewLength,
                Vector3.forward,
                pelletRadius
            );
        }
        
        // Add a label showing the spread angle
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 12;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.background = MakeTexture(1, 1, new Color(0, 0, 0, 0.5f));
        
        string firingModeText = "";
        switch (shotgun.firingType)
        {
            case ShotgunWeapon.FiringType.SemiAuto: firingModeText = "Semi-Auto"; break;
            case ShotgunWeapon.FiringType.Auto: firingModeText = "Automatic"; break;
            case ShotgunWeapon.FiringType.Burst: firingModeText = $"Burst ({shotgun.burstCount})"; break;
            case ShotgunWeapon.FiringType.PumpAction: firingModeText = "Pump Action"; break;
        }
        
        Handles.Label(
            originPos + originDir * (previewLength + 1.5f) + new Vector3(0, 0.5f, 0),
            $"Spread: {shotgun.spreadAngle}°\nPellets: {shotgun.pelletCount}\nFiring: {firingModeText}",
            labelStyle
        );
    }
    
    // Helper method to create a texture for the label background
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    // Helper method to draw a visual representation of the burst pattern
    private void DrawBurstPatternPreview(Rect rect, int burstCount, float burstDensity)
    {
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
        
        // Calculate spacing factor based on burst density
        // Invert the relationship: lower density = closer shots
        float spacingFactor = burstDensity * 10f; // Scale to make differences more apparent
        
        // Base position for first shot
        float startX = rect.x + 10;
        
        ShotgunWeapon shotgun = (ShotgunWeapon)target;
        float availableWidth = rect.width - 25; // Leave some margin
        float burstWidth = (burstCount - 1) * spacingFactor * 20; // Width of a single burst
        float cooldownWidth = Mathf.Min(shotgun.timeBetweenBursts * 30, availableWidth * 0.3f); // Width of cooldown
        
        // Draw first burst
        DrawSingleBurst(rect, startX, burstCount, spacingFactor, burstDensity);
        
        // Draw cooldown period
        float cooldownStartX = startX + burstWidth + 15;
        float cooldownEndX = cooldownStartX + cooldownWidth;
        
        // Draw cooldown line
        Handles.color = new Color(0.3f, 0.3f, 0.9f, 0.8f);
        Handles.DrawLine(
            new Vector3(cooldownStartX, rect.y + 15, 0),
            new Vector3(cooldownEndX, rect.y + 15, 0)
        );
        
        // Draw cooldown marker
        EditorGUI.DrawRect(new Rect(cooldownEndX - 3, rect.y + 10, 6, 10), new Color(0.3f, 0.3f, 0.9f));
        
        // Draw second burst (partial, to show pattern continues)
        if (cooldownEndX + 25 < rect.x + rect.width)
        {
            DrawSingleBurst(rect, cooldownEndX + 10, Mathf.Min(3, burstCount), spacingFactor, burstDensity, true);
        }
        
        // Add labels for clarity
        GUI.color = Color.white;
        float textY = rect.y + rect.height - 5;
        
        // Show density value
        EditorGUI.LabelField(
            new Rect(rect.x + 5, textY, 100, 20),
            burstDensity < 0.15f ? "Dense" : burstDensity > 0.25f ? "Sparse" : "Medium"
        );
        
        // Show cooldown info
        GUI.contentColor = new Color(0.7f, 0.7f, 1.0f);
        EditorGUI.LabelField(
            new Rect(cooldownStartX, rect.y, cooldownWidth, 20),
            $"{shotgun.timeBetweenBursts:F2}s"
        );
        
        // Show burst timing info
        float totalTime = burstDensity * (burstCount - 1);
        string timeInfo = $"Burst: {totalTime:F2}s";
        
        GUI.contentColor = Color.white;
        EditorGUI.LabelField(
            new Rect(rect.x + rect.width - 80, textY, 75, 20),
            timeInfo
        );
    }
    
    // Helper method to draw a single burst pattern
    private void DrawSingleBurst(Rect rect, float startX, int burstCount, float spacingFactor, float burstDensity, bool faded = false)
    {
        float alpha = faded ? 0.5f : 1.0f;
        
        for (int i = 0; i < burstCount; i++)
        {
            // Position each shot based on density (closer together for low density)
            float xPos = startX + (i * spacingFactor * 20);
            
            // Stop if we exceed the rect width
            if (xPos > rect.x + rect.width - 15)
                break;
            
            // Color gradient from green (fast/dense) to red (slow/sparse)
            Color shotColor = Color.Lerp(Color.green, Color.red, burstDensity / 0.3f);
            shotColor.a = alpha;
            
            // Draw the shot box
            EditorGUI.DrawRect(new Rect(xPos - 5, rect.y + 10, 10, 10), shotColor);
            
            // Connect with line if not first shot
            if (i > 0)
            {
                float prevX = startX + ((i-1) * spacingFactor * 20);
                Handles.color = new Color(Color.gray.r, Color.gray.g, Color.gray.b, alpha);
                Handles.DrawLine(
                    new Vector3(prevX + 5, rect.y + 15, 0),
                    new Vector3(xPos - 5, rect.y + 15, 0)
                );
            }
        }
    }
    
    private void DrawSpreadVisualization(Rect rect, float spreadAngle, int pelletCount, Color pelletColor, bool useRandomPaths)
    {
        // Adjust constants based on spread angle
        float centerX = rect.x + rect.width * 0.2f; 
        float centerY = rect.y + rect.height * 0.5f;
        float radius = rect.height * 0.1f;
        
        // Scale maxDistance to ensure the spread fits within the rect
        // For wider angles, we need to use a shorter distance
        float maxDistance = rect.width * 0.75f;
        
        // Calculate how far the spread can go vertically
        float halfAngleRad = spreadAngle * 0.5f * Mathf.Deg2Rad;
        float verticalExtent = maxDistance * Mathf.Sin(halfAngleRad);
        
        // If the vertical extent would exceed our rect, scale down the distance
        float maxAllowedVerticalExtent = rect.height * 0.45f; // Leave some margin
        if (verticalExtent > maxAllowedVerticalExtent)
        {
            maxDistance = maxAllowedVerticalExtent / Mathf.Sin(halfAngleRad);
        }
        
        // Draw the background
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
        
        // Draw the "gun" source point
        EditorGUI.DrawRect(new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2), Color.white);
        
        // Calculate spread boundaries
        Vector2 upperLine = new Vector2(Mathf.Cos(-halfAngleRad), Mathf.Sin(-halfAngleRad)) * maxDistance;
        Vector2 lowerLine = new Vector2(Mathf.Cos(halfAngleRad), Mathf.Sin(halfAngleRad)) * maxDistance;
        
        // Draw the spread cone lines
        Handles.color = Color.yellow;
        Handles.DrawLine(
            new Vector3(centerX, centerY, 0), 
            new Vector3(centerX + upperLine.x, centerY + upperLine.y, 0)
        );
        Handles.DrawLine(
            new Vector3(centerX, centerY, 0),
            new Vector3(centerX + lowerLine.x, centerY + lowerLine.y, 0)
        );
        
        // Draw arc at the end of the cone
        Vector3 arcCenter = new Vector3(centerX, centerY, 0);
        Handles.DrawWireArc(
            arcCenter,
            Vector3.forward,
            new Vector3(upperLine.x, upperLine.y, 0).normalized,
            spreadAngle,
            maxDistance
        );
        
        // Draw simulated pellets
        Handles.color = pelletColor;

        if (useRandomPaths)
        {
            // Random pattern (existing code)
            System.Random rng = new System.Random(pelletCount * 1000 + Mathf.RoundToInt(spreadAngle * 10));
            
            for (int i = 0; i < pelletCount; i++)
            {
                // Use a more realistic distribution for shotgun
                float edgeBias = Mathf.Pow((float)rng.NextDouble(), 0.7f);
                float anglePercentage = (edgeBias * 2f - 1f);
                float angle = anglePercentage * halfAngleRad;
                
                // Distance calculation
                float distance = maxDistance * (0.9f + 0.1f * (float)rng.NextDouble());
                
                float pelletX = centerX + Mathf.Cos(angle) * distance;
                float pelletY = centerY + Mathf.Sin(angle) * distance;
                
                // Only draw if pellet would be inside our rect
                if (pelletX >= rect.x && pelletX <= rect.x + rect.width && 
                    pelletY >= rect.y && pelletY <= rect.y + rect.height)
                {
                    // Draw the pellet
                    float pelletSize = 3f + (float)rng.NextDouble() * 2f;
                    Handles.DrawSolidDisc(
                        new Vector3(pelletX, pelletY, 0),
                        Vector3.forward,
                        pelletSize
                    );
                }
            }
        }
        else
        {
            // Fixed pattern - evenly distributed
            for (int i = 0; i < pelletCount; i++)
            {
                float angle;
                if (pelletCount <= 1)
                {
                    angle = 0; // Single pellet fires straight
                }
                else
                {
                    // Distribute evenly across the spread
                    float step = spreadAngle / (pelletCount - 1);
                    angle = -spreadAngle/2 + (i * step);
                }
                
                angle *= Mathf.Deg2Rad; // Convert to radians
                
                // Use full distance
                float distance = maxDistance;
                
                float pelletX = centerX + Mathf.Cos(angle) * distance;
                float pelletY = centerY + Mathf.Sin(angle) * distance;
                
                // Only draw if pellet would be inside our rect
                if (pelletX >= rect.x && pelletX <= rect.x + rect.width && 
                    pelletY >= rect.y && pelletY <= rect.y + rect.height)
                {
                    // Draw the pellet - fixed size for even pattern
                    float pelletSize = 4f;
                    Handles.DrawSolidDisc(
                        new Vector3(pelletX, pelletY, 0),
                        Vector3.forward,
                        pelletSize
                    );
                }
            }
        }
        
        // Add angle text
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        
        Rect labelRect = new Rect(rect.x + rect.width - 60, rect.y + 5, 50, 20);
        EditorGUI.LabelField(labelRect, $"{spreadAngle}°", labelStyle);
        
        // Add pellet count text
        Rect countRect = new Rect(rect.x + rect.width - 60, rect.y + rect.height - 25, 50, 20);
        EditorGUI.LabelField(countRect, $"{pelletCount}x", labelStyle);
    }

    // Replace the PlayClip method with this version:
    private void PlayClip(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;
        
        // Select the clip in the project window to make it visible in inspector
        Selection.activeObject = clip;
        
        // Focus the inspector window
        EditorUtility.FocusProjectWindow();
        
        // Show a message
        Debug.Log($"Selected audio clip: {clip.name} for preview");
    }
} 