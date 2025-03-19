using UnityEngine;

public class WeaponFollower : MonoBehaviour
{
    [Header("Following Settings")]
    public Vector3 positionOffset = new Vector3(0.3f, 0.0f, 0);
    public Vector3 leftPositionOffset = new Vector3(-0.3f, 0.0f, 0); // Offset when facing left
    public float rotationSpeed = 15f;
    
    [Header("Attachment Options")]
    public bool instantFollow = true; // Toggle between instant and smooth following
    public float followSpeed = 10f; // Only used when instantFollow is false
    
    [Header("Gungeon Style Settings")]
    public bool maintainUpDirection = true;
    public float maxRotationAngle = 75f;
    
    [Header("Dash Effects")]
    public bool hideWeaponDuringDash = true;
    public float hideAfterDashTime = 0.2f; // How long to keep weapon hidden after dash ends
    
    [Header("Layer Settings")]
    public bool changeLayerBasedOnMovement = true;
    public string frontLayerName = "InFrontOfPlayer";
    public string backLayerName = "BehindPlayer";
    
    protected Transform playerTransform;
    protected SpriteRenderer weaponRenderer;
    protected Transform socketTransform;
    
    // Tracking variables
    private bool isFacingRight = true;
    private SpriteRenderer playerSpriteRenderer;
    private MoveScript playerMovement;
    private float dashHideTimer = 0f;
    private bool isWeaponHidden = false;
    private Vector2 lastMovementDirection;
    
    // Public accessor for other scripts to check if weapon is hidden
    public bool IsWeaponHidden() => isWeaponHidden;
    
    public void SetPlayer(Transform player)
    {
        playerTransform = player;
        
        // Get player sprite renderer if possible
        playerSpriteRenderer = player.GetComponent<SpriteRenderer>();
        
        // Also try to get from child if not on player directly
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = player.GetComponentInChildren<SpriteRenderer>();
            
        // Get movement script for dash detection
        playerMovement = player.GetComponent<MoveScript>();
    }
    
    public void SetSocket(Transform socket)
    {
        socketTransform = socket;
        Debug.Log("Weapon socket set to: " + (socket != null ? socket.name : "null"));
    }
    
    private void Start()
    {
        weaponRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void LateUpdate()
    {
        if (playerTransform == null) return;
            
        // Use socket if available, otherwise use player transform
        Transform targetTransform = socketTransform != null ? socketTransform : playerTransform;
        
        // Check player facing direction
        CheckPlayerDirection();
        
        // Update weapon visibility based on dash state
        UpdateWeaponVisibility();
        
        // Update weapon sorting layer based on movement
        UpdateWeaponSortingLayer();
        
        // Position and aim weapon
        FollowTarget(targetTransform);
        GungeonStyleAiming();
    }
    
    private void CheckPlayerDirection()
    {
        // Check if we can detect player direction
        if (playerSpriteRenderer != null)
        {
            isFacingRight = !playerSpriteRenderer.flipX;
        }
        // Alternative check using player movement component
        else if (playerMovement != null)
        {
            isFacingRight = playerMovement.IsFacingRight();
            
            // Also store movement direction for layer changes
            if (playerMovement.GetMovementDirection() != Vector2.zero)
            {
                lastMovementDirection = playerMovement.GetMovementDirection();
            }
        }
    }
    
    private void UpdateWeaponVisibility()
    {
        if (!hideWeaponDuringDash || weaponRenderer == null) return;
        
        bool isDashing = (playerMovement != null) && playerMovement.IsDashing();
        
        // If dashing, reset the hide timer
        if (isDashing)
        {
            dashHideTimer = hideAfterDashTime;
            weaponRenderer.enabled = false;
            isWeaponHidden = true;
        }
        // If not dashing but timer is still active
        else if (dashHideTimer > 0)
        {
            dashHideTimer -= Time.deltaTime;
            weaponRenderer.enabled = false;
            isWeaponHidden = true;
        }
        // Show the weapon when not dashing and timer expired
        else
        {
            weaponRenderer.enabled = true;
            isWeaponHidden = false;
        }
    }
    
    private void UpdateWeaponSortingLayer()
    {
        if (!changeLayerBasedOnMovement || weaponRenderer == null) return;
        
        bool isMovingUp = false;
        
        // Check if we're moving upward
        if (playerMovement != null)
        {
            isMovingUp = playerMovement.GetMovementDirection().y > 0.1f;
        }
        else
        {
            // Fallback to checking if mouse/aim is pointing upward
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 aimDirection = (mousePos - transform.position).normalized;
            isMovingUp = aimDirection.y > 0.5f;
        }
        
        // Change sorting layer based on movement direction
        if (isMovingUp)
        {
            // When moving up, put weapon behind player
            if (weaponRenderer.sortingLayerName != backLayerName)
            {
                weaponRenderer.sortingLayerName = backLayerName;
            }
        }
        else
        {
            // Otherwise, keep weapon in front of player
            if (weaponRenderer.sortingLayerName != frontLayerName)
            {
                weaponRenderer.sortingLayerName = frontLayerName;
            }
        }
    }
    
    private void FollowTarget(Transform target)
    {
        // Choose offset based on facing direction
        Vector3 currentOffset = isFacingRight ? positionOffset : leftPositionOffset;
        
        // Calculate target position
        Vector3 targetPos = target.position + currentOffset;
        
        // Use direct positioning or smooth following
        if (instantFollow)
        {
            // Snap directly to position - no hovering effect
            transform.position = targetPos;
        }
        else
        {
            // Use smooth follow only if instantFollow is disabled
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        }
    }
    
    private void GungeonStyleAiming()
    {
        // Get mouse position in world space
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        // Calculate direction to mouse
        Vector3 direction = mousePos - transform.position;
        
        // Calculate raw angle to mouse
        float rawAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Clamp angle if maintainUpDirection is true (Gungeon style)
        float finalAngle = rawAngle;
        
        if (maintainUpDirection)
        {
            // For right-facing: Clamp between -maxRotationAngle and +maxRotationAngle
            // For left-facing: Clamp between 180-maxRotationAngle and 180+maxRotationAngle
            if (isFacingRight)
            {
                // When facing right, constrain vertical aim
                if (rawAngle > maxRotationAngle && rawAngle < 180)
                    finalAngle = maxRotationAngle;
                else if (rawAngle < -maxRotationAngle && rawAngle > -180)
                    finalAngle = -maxRotationAngle;
            }
            else
            {
                // When facing left, flip the constraints
                // Normalize angle to -180 to 180 range
                float normalizedAngle = rawAngle;
                if (normalizedAngle > 180) normalizedAngle -= 360;
                
                // Apply constraints, but for left-facing direction
                if (normalizedAngle > 0 && normalizedAngle < 180 - maxRotationAngle)
                    finalAngle = 180 - maxRotationAngle;
                else if (normalizedAngle < 0 && normalizedAngle > -180 + maxRotationAngle)
                    finalAngle = -180 + maxRotationAngle;
            }
        }
        
        // Apply rotation - keep smooth rotation for aiming
        Quaternion targetRotation = Quaternion.Euler(0, 0, finalAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}