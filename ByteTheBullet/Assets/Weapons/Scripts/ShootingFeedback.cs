using UnityEngine;

/// <summary>
/// Handles visual feedback for weapon firing including camera shake.
/// Attach this to weapon objects alongside their shooting scripts.
/// </summary>
public class ShootingFeedback : MonoBehaviour
{
    [Header("Camera Shake Settings")]
    [Tooltip("Whether to enable camera shake when this weapon fires")]
    public bool enableCameraShake = true;
    
    [Tooltip("Base intensity of camera shake")]
    [Range(0.05f, 2.0f)]
    public float shakeIntensity = 0.5f;
    
    [Tooltip("Duration of camera shake in seconds")]
    [Range(0.05f, 1.0f)]
    public float shakeDuration = 0.2f;
    
    [Header("Weapon-Specific Modifiers")]
    [Tooltip("For shotguns: scale shake based on pellet count")]
    public bool scaleWithPelletCount = true;
    
    [Tooltip("For charged weapons: scale shake based on charge level")]
    public bool scaleWithChargeLevel = true;
    
    [Tooltip("Max multiplier when fully charged or max pellets")]
    [Range(1.0f, 3.0f)]
    public float maxMultiplier = 2.0f;
    
    [Header("Debug")]
    [SerializeField] private string lastFeedbackInfo = "";
    
    /// <summary>
    /// Trigger camera shake with the configured settings
    /// </summary>
    public void TriggerCameraShake()
    {
        if (!enableCameraShake || CameraShake.Instance == null) return;
        
        CameraShake.Instance.ShakeCamera(shakeIntensity, shakeDuration);
        lastFeedbackInfo = $"Basic shake: {shakeIntensity:F2} intensity, {shakeDuration:F2}s";
    }
    
    /// <summary>
    /// Trigger camera shake with pellet-based scaling (for shotguns)
    /// </summary>
    /// <param name="pellets">Current number of pellets fired</param>
    /// <param name="maxPellets">Base/max pellet count for the weapon</param>
    public void TriggerShotgunShake(int pellets, int maxPellets)
    {
        if (!enableCameraShake || CameraShake.Instance == null) return;
        
        float multiplier = scaleWithPelletCount ? 
            Mathf.Lerp(1.0f, maxMultiplier, (float)pellets / maxPellets) : 1.0f;
            
        CameraShake.Instance.ShakeFromWeapon(shakeIntensity, multiplier, shakeDuration);
        lastFeedbackInfo = $"Shotgun shake: {pellets} pellets, {multiplier:F2}x multiplier";
    }
    
    /// <summary>
    /// Trigger camera shake with charge-based scaling
    /// </summary>
    /// <param name="chargePercent">Charge level from 0.0 to 1.0</param>
    public void TriggerChargedShake(float chargePercent)
    {
        if (!enableCameraShake || CameraShake.Instance == null) return;
        
        float multiplier = scaleWithChargeLevel ? 
            Mathf.Lerp(1.0f, maxMultiplier, chargePercent) : 1.0f;
            
        CameraShake.Instance.ShakeFromWeapon(shakeIntensity, multiplier, shakeDuration);
        lastFeedbackInfo = $"Charged shake: {chargePercent:P0} charge, {multiplier:F2}x multiplier";
    }
    
    /// <summary>
    /// Trigger camera shake with a custom intensity multiplier
    /// </summary>
    /// <param name="customMultiplier">Multiplier for the base intensity</param>
    public void TriggerCustomShake(float customMultiplier)
    {
        if (!enableCameraShake || CameraShake.Instance == null) return;
        
        CameraShake.Instance.ShakeCamera(shakeIntensity * customMultiplier, shakeDuration);
        lastFeedbackInfo = $"Custom shake: {customMultiplier:F2}x multiplier";
    }
} 