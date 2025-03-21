using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [Header("Dependencies")]
    public Animator animator;
    public WeaponAiming weaponAiming;
    public PlayerMovement playerMovement; // Reference to your player movement script
    
    [Header("Settings")]
    [Tooltip("How strongly the aim direction affects running animations (0-1)")]
    [Range(0, 1)]
    public float aimInfluence = 1.0f;
    
    // Animator parameters
    private readonly string horizontalParam = "LookX";
    private readonly string verticalParam = "LookY";
    private readonly string isMovingParam = "IsMoving";
    
    // Direction vector cache
    [HideInInspector]
    public Vector2 lastLookDirection = Vector2.down; // Default to down
    
    // New parameters for dodge animations
    private readonly string dodgeTriggerParam = "Dodge";
    private readonly string dodgeXParam = "DodgeX";
    private readonly string dodgeYParam = "DodgeY";
    
    // Reference to dodge component
    private PlayerDodge playerDodge;
    
    void Start()
    {
        // Ensure we have the animator reference
        if (animator == null)
            animator = GetComponent<Animator>();
            
        // Ensure we have the weapon aiming reference
        if (weaponAiming == null)
            weaponAiming = GetComponentInChildren<WeaponAiming>();
            
        // Ensure we have the player movement reference
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        // Get dodge component reference
        playerDodge = GetComponent<PlayerDodge>();
    }
    
    void Update()
    {
        // Get mouse position and calculate aim direction from player
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        Vector3 aimDirection = (mousePosition - transform.position).normalized;
        
        // Check if player is moving and not dodging
        bool isMoving = playerMovement.moveDirection.magnitude > 0.1f;
        bool isDodging = playerDodge != null && playerDodge.IsDodging();
        
        // Always update the look direction
        lastLookDirection = new Vector2(aimDirection.x, aimDirection.y);
        
        // Skip animation updates if we're dodging (controlled by PlayerDodge)
        if (!isDodging)
        {
            // The key difference: ALWAYS use aim direction for animation, 
            // regardless of whether moving or not - this is the Gungeon style
            Vector2 directionToUse = lastLookDirection;
            
            // Update animator parameters
            animator.SetFloat(horizontalParam, directionToUse.x);
            animator.SetFloat(verticalParam, directionToUse.y);
            animator.SetBool(isMovingParam, isMoving);
        }
    }
}