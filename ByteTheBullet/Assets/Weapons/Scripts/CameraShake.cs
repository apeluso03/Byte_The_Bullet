using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("Default intensity of camera shake")]
    [Range(0.01f, 2.0f)]
    public float defaultShakeIntensity = 0.5f;
    
    [Tooltip("Default duration of camera shake in seconds")]
    [Range(0.1f, 1.0f)]
    public float defaultShakeDuration = 0.2f;
    
    [Tooltip("How quickly the shake effect fades out - higher = faster falloff")]
    [Range(0.5f, 5.0f)]
    public float shakeFalloff = 2.0f;
    
    [Tooltip("Whether to allow multiple shakes to stack")]
    public bool allowMultipleShakes = true;
    
    [Header("Debug")]
    [SerializeField] private bool isShaking = false;
    [SerializeField] private int activeShakes = 0;
    
    // Reference to store the original camera position
    private Vector3 originalPosition;
    
    // Singleton instance for easy access
    private static CameraShake _instance;
    public static CameraShake Instance { get { return _instance; } }
    
    private void Awake()
    {
        // Setup singleton pattern
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Store original position
        originalPosition = transform.localPosition;
    }
    
    /// <summary>
    /// Trigger a camera shake with default settings
    /// </summary>
    public void ShakeCamera()
    {
        ShakeCamera(defaultShakeIntensity, defaultShakeDuration);
    }
    
    /// <summary>
    /// Trigger a camera shake with custom intensity
    /// </summary>
    /// <param name="intensity">How strong the shake effect should be</param>
    public void ShakeCamera(float intensity)
    {
        ShakeCamera(intensity, defaultShakeDuration);
    }
    
    /// <summary>
    /// Trigger a camera shake with custom intensity and duration
    /// </summary>
    /// <param name="intensity">How strong the shake effect should be</param>
    /// <param name="duration">How long the shake effect should last in seconds</param>
    public void ShakeCamera(float intensity, float duration)
    {
        // If multiple shakes aren't allowed and we're already shaking, don't start a new shake
        if (!allowMultipleShakes && isShaking)
            return;
            
        StartCoroutine(ShakeCameraCoroutine(intensity, duration));
    }
    
    /// <summary>
    /// Trigger a weapon-based camera shake with intensity scaling
    /// </summary>
    /// <param name="baseIntensity">Base intensity of the shake</param>
    /// <param name="multiplier">Factor to multiply base intensity by (e.g. bullet count)</param>
    /// <param name="duration">How long the shake effect should last in seconds</param>
    public void ShakeFromWeapon(float baseIntensity, float multiplier, float duration)
    {
        float scaledIntensity = baseIntensity * multiplier;
        ShakeCamera(scaledIntensity, duration);
    }
    
    /// <summary>
    /// Coroutine to perform the actual camera shake
    /// </summary>
    private IEnumerator ShakeCameraCoroutine(float intensity, float duration)
    {
        isShaking = true;
        activeShakes++;
        
        // Store current position as start position for this shake
        Vector3 startPosition = transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float remainingTime = duration - elapsed;
            float damper = Mathf.Pow(remainingTime / duration, shakeFalloff);
            
            // Create a random shake offset
            float x = Random.Range(-1f, 1f) * intensity * damper;
            float y = Random.Range(-1f, 1f) * intensity * damper;
            
            // Apply the shake offset to the current position
            transform.localPosition = startPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Only reset position if this is the last active shake
        activeShakes--;
        if (activeShakes <= 0)
        {
            transform.localPosition = originalPosition;
            isShaking = false;
            activeShakes = 0;
        }
    }
    
    /// <summary>
    /// Stop all active camera shakes and reset position
    /// </summary>
    public void StopAllShakes()
    {
        StopAllCoroutines();
        transform.localPosition = originalPosition;
        isShaking = false;
        activeShakes = 0;
    }
    
    /// <summary>
    /// Reset camera position when disabled
    /// </summary>
    private void OnDisable()
    {
        StopAllShakes();
    }
} 