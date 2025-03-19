using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    [Header("Aiming Settings")]
    public float rotationSpeed = 10f; // How fast the aim rotates
    public Transform aimPivot; // The pivot point for aiming (usually a child of the player)
    public float aimDistance = 0.5f; // Distance of the aim indicator from the player
    public GameObject aimIndicator; // Optional visual indicator for aiming direction
    public Camera playerCamera; // Reference to the camera - assign this in the inspector

    [Header("Input Settings")]
    public bool useMouseAiming = true; // Whether to use mouse for aiming
    public string horizontalAimAxis = "RightStickHorizontal"; // Controller right stick X
    public string verticalAimAxis = "RightStickVertical"; // Controller right stick Y
    
    // Private variables
    private Vector2 aimDirection = Vector2.right; // Default aim direction
    private MoveScript playerMovement; // Reference to the player movement script
    private Camera mainCamera; // Reference to the main camera
    private bool isCameraFound = false; // Flag to track if camera was found
    private WeaponManager weaponManager;
    
    void Start()
    {
        // Get references
        playerMovement = GetComponent<MoveScript>();
        weaponManager = GetComponent<WeaponManager>();
        
        // Setup camera
        if (playerCamera != null)
        {
            mainCamera = playerCamera;
            isCameraFound = true;
        }
        else if ((mainCamera = Camera.main) != null)
        {
            isCameraFound = true;
        }
        else
        {
            Debug.LogError("No camera found! Please assign a camera to the playerCamera field.");
            useMouseAiming = false;
        }
        
        // Display sorting layer setup instructions
        CheckAndCreateSortingLayers();
        
        // Setup aim pivot
        if (aimPivot == null)
        {
            GameObject pivotObj = new GameObject("AimPivot");
            pivotObj.transform.SetParent(transform);
            pivotObj.transform.localPosition = Vector3.zero;
            aimPivot = pivotObj.transform;
        }
        
        // Setup aim indicator
        if (aimIndicator == null && aimPivot != null)
        {
            GameObject indicatorObj = new GameObject("AimIndicator");
            indicatorObj.transform.SetParent(aimPivot);
            indicatorObj.transform.localPosition = new Vector3(aimDistance, 0, 0);
            indicatorObj.AddComponent<SpriteRenderer>();
            
            aimIndicator = indicatorObj;
        }
    }
    
    void Update()
    {
        HandleAiming();
        
        // Handle weapon sprite flipping based on aim angle
        if (weaponManager != null)
        {
            // Use GetCurrentWeapon() instead of accessing currentWeapon directly
            WeaponBase currentWeapon = weaponManager.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                SpriteRenderer weaponRenderer = currentWeapon.GetComponent<SpriteRenderer>();
                if (weaponRenderer != null)
                {
                    // Get the current aim angle
                    float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                    bool shouldFlip = Mathf.Abs(angle) > 90;
                    
                    // Flip weapon sprite
                    weaponRenderer.flipY = shouldFlip;
                    
                    // Change the layer based on aiming direction
                    bool isAimingUp = aimDirection.y > 0.5f;
                    weaponRenderer.sortingLayerName = isAimingUp ? "BehindPlayer" : "InFrontOfPlayer";
                }
            }
        }
    }
    
    void HandleAiming()
    {
        // Get aim direction
        if (useMouseAiming && isCameraFound)
        {
            Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = transform.position.z;
            aimDirection = (mousePosition - transform.position).normalized;
        }
        else
        {
            float horizontalInput = Input.GetAxis(horizontalAimAxis);
            float verticalInput = Input.GetAxis(verticalAimAxis);
            
            if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            {
                aimDirection = new Vector2(horizontalInput, verticalInput).normalized;
            }
        }
        
        // Rotate aim pivot
        if (aimDirection.sqrMagnitude > 0 && aimPivot != null)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            aimPivot.rotation = Quaternion.Lerp(
                aimPivot.rotation, 
                Quaternion.Euler(0, 0, angle), 
                rotationSpeed * Time.deltaTime
            );
            
            // Update player facing direction
            if (playerMovement != null && Mathf.Abs(aimDirection.x) > 0.3f)
            {
                bool shouldFaceRight = aimDirection.x > 0;
                
                if (shouldFaceRight && !playerMovement.IsFacingRight())
                {
                    playerMovement.FlipSprite();
                }
                else if (!shouldFaceRight && playerMovement.IsFacingRight())
                {
                    playerMovement.FlipSprite();
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(aimDirection * aimDistance));
    }

    // Helper function to make sure sorting layers exist
    private bool CheckAndCreateSortingLayers()
    {
        Debug.LogWarning(
            "Please create two sorting layers in Unity:\n" +
            "1. 'BehindPlayer' (lower value than Default)\n" +
            "2. 'InFrontOfPlayer' (higher value than Default)\n" +
            "Edit > Project Settings > Tags and Layers > Sorting Layers"
        );
        return true;
    }
    
    // Public method to get the current aim direction
    public Vector2 GetAimDirection()
    {
        return aimDirection;
    }
}