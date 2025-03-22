using UnityEngine;
using System.Collections;

// This is a specialized debug version - create this as a NEW script file
public class ShotgunShootingDebug : MonoBehaviour
{
    [Header("DEBUG CONTROLS")]
    [Tooltip("CRITICAL: Set to true to enable full auto testing")]
    public bool enableFullAutoMode = true;
    
    [Tooltip("CRITICAL: Set how many shots per second")]
    public float shotsPerSecond = 10f;
    
    [Tooltip("How many pellets per shot")]
    public int pelletCount = 8;
    
    [Tooltip("Projectile to spawn")]
    public GameObject projectilePrefab;
    
    [Header("Debug Output")]
    [Tooltip("Shows what's happening")]
    [TextArea(4, 6)]
    public string debugOutput = "No shots fired yet";
    
    // Private tracking variables
    private float nextFireTime = 0;
    private int shotsFired = 0;
    private bool fireButtonHeld = false;
    private int updateCallCount = 0;
    private float lastShootTime = 0;
    
    // Use a coroutine to log without spamming
    private int loggingFrequency = 10; // Only log every X frames
    
    void Start()
    {
        // Basic initialization
        if (projectilePrefab == null)
        {
            debugOutput = "ERROR: No projectile prefab assigned!";
            Debug.LogError("ShotgunShootingDebug: No projectile prefab assigned!");
        }
        else
        {
            debugOutput = "Ready to fire. HOLD left mouse button for full auto.";
            Debug.Log("ShotgunShootingDebug initialized. HOLD left mouse button for full auto.");
        }
        
        // Create firepoint if needed
        if (transform.Find("FirePoint") == null)
        {
            Transform firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = new Vector3(0.5f, 0, 0);
        }
        
        // Start logging coroutine
        StartCoroutine(LogStatus());
    }
    
    void Update()
    {
        // Count updates
        updateCallCount++;
        
        // Track input methods
        bool mouseInput = Input.GetMouseButton(0);  // Direct mouse button check
        bool fire1Input = Input.GetButton("Fire1"); // Indirect mapped input check
        
        // Store previous state to detect changes
        bool wasHeld = fireButtonHeld;
        
        // Update current state
        fireButtonHeld = mouseInput || fire1Input;
        
        // Check for input changes for logging
        if (fireButtonHeld && !wasHeld)
        {
            debugOutput = "MOUSE BUTTON PRESSED!";
            Debug.Log("Mouse button pressed down at " + Time.time);
        }
        else if (!fireButtonHeld && wasHeld)
        {
            debugOutput = "MOUSE BUTTON RELEASED!";
            Debug.Log("Mouse button released at " + Time.time);
        }
        
        // Basic firing check
        if (enableFullAutoMode && fireButtonHeld && Time.time >= nextFireTime)
        {
            FireShot();
            float interval = 1f / shotsPerSecond;
            nextFireTime = Time.time + interval;
            
            debugOutput = $"FIRED SHOT #{shotsFired} at {Time.time:F3}. Next shot in {interval:F3}s";
            Debug.Log($"FIRED SHOT #{shotsFired} at time {Time.time:F3}. Next shot in {interval:F3}s");
        }
    }
    
    private IEnumerator LogStatus()
    {
        while (true)
        {
            // Log the current status every second
            bool mouseInput = Input.GetMouseButton(0);
            bool fire1Input = Input.GetButton("Fire1");
            
            Debug.Log($"STATUS: Time={Time.time:F2}, NextFire={nextFireTime:F2}, Mouse={mouseInput}, " +
                     $"Fire1={fire1Input}, CanFire={Time.time >= nextFireTime}, Shots={shotsFired}");
                     
            yield return new WaitForSeconds(1.0f);
        }
    }
    
    private void FireShot()
    {
        // Basic safety check
        if (projectilePrefab == null)
        {
            Debug.LogError("Cannot fire - no projectile prefab!");
            return;
        }
        
        // Get the fire point
        Transform firePoint = transform.Find("FirePoint");
        if (firePoint == null)
        {
            Debug.LogError("Cannot fire - no FirePoint child found!");
            return;
        }
        
        shotsFired++;
        lastShootTime = Time.time;
        
        // Spawn the pellets
        for (int i = 0; i < pelletCount; i++)
        {
            // Add some spread
            float spread = Random.Range(-15f, 15f);
            Quaternion rotation = Quaternion.Euler(0, 0, spread) * firePoint.rotation;
            
            // Create the projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, rotation);
            
            // Add velocity - try both 2D and 3D
            Rigidbody2D rb2d = projectile.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.linearVelocity = rotation * Vector3.right * 15f;
            }
            else
            {
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = rotation * Vector3.right * 15f;
                }
            }
            
            // Make sure it destroys itself
            Destroy(projectile, 5f);
        }
    }
    
    // Simple helper to display debug info in scene view
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Transform firePoint = transform.Find("FirePoint");
            if (firePoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(firePoint.position, 0.05f);
                Gizmos.DrawRay(firePoint.position, firePoint.right * 0.5f);
            }
        }
    }
} 