using UnityEngine;
using System.Collections;

public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge Settings")]
    public float dodgeSpeed = 15f;
    public float dodgeDuration = 0.2f;
    public float dodgeCooldown = 0.8f;
    [Tooltip("Multiplier for dodge distance when performing an idle dodge")]
    public float idleDodgeDistanceMultiplier = 1.2f;
    
    [Header("Dodge Direction Controls")]
    [Tooltip("When idle, dodge in the direction you're aiming")]
    public bool idleDodgeInAimDirection = true;
    [Tooltip("Allow dodge to go backward (if false, will dodge to the sides instead)")]
    public bool allowBackwardDodge = false;
    [Tooltip("Minimum angle (in degrees) between aim and move direction to be considered a backward dodge")]
    public float backwardDodgeAngle = 120f;
    
    [Header("VFX")]
    public GameObject dodgeTrailEffect;
    public float trailDuration = 0.3f;
    
    [Header("References")]
    public WeaponAiming weaponAiming; // Make it serialized so you can assign it in the inspector
    
    // Dependencies
    private PlayerMovement playerMovement;
    private CharacterAnimator characterAnimator;
    private Animator animator;
    private Rigidbody2D rb;
    
    // State tracking
    private bool isDodging = false;
    private float cooldownTimer = 0f;
    private Vector2 dodgeDirection;
    
    // Add these fields
    private PlayerInventory inventory;
    private WeaponAiming currentWeapon;
    
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        characterAnimator = GetComponent<CharacterAnimator>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // Get inventory component
        inventory = GetComponent<PlayerInventory>();
        
        // Listen for weapon changes if inventory exists
        if (inventory != null)
        {
            inventory.OnWeaponChanged += HandleWeaponChanged;
        }
        else
        {
            Debug.LogWarning("PlayerInventory not found! Weapon effects during dodge may not work correctly.");
            
            // Fallback to direct search
            currentWeapon = GetComponentInChildren<WeaponAiming>();
        }
        
        // Try to get the reference if not assigned in inspector
        if (weaponAiming == null)
        {
            weaponAiming = GetComponentInChildren<WeaponAiming>();
            if (weaponAiming == null)
            {
                weaponAiming = FindAnyObjectByType<WeaponAiming>();
                Debug.LogWarning("WeaponAiming not found in children, searching scene...");
            }
        }
        
        Debug.Log("WeaponAiming reference: " + (weaponAiming != null));
        if (weaponAiming == null)
        {
            Debug.Log("No WeaponAiming found - weapon effects during dodge will be ignored");
        }
    }
    
    void Update()
    {
        // Update cooldown
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;
            
        // Check for dodge input
        if (Input.GetKeyDown(KeyCode.Space) && cooldownTimer <= 0 && !isDodging)
        {
            StartDodge();
        }
    }
    
    void StartDodge()
    {
        // Don't allow dodging if already dodging
        if (isDodging) return;
        
        isDodging = true;
        cooldownTimer = dodgeCooldown;
        
        // Get aim direction for reference
        Vector2 aimDirection = characterAnimator.lastLookDirection.normalized;
        
        // *** REVISED DODGE DIRECTION LOGIC FOR PROPER ROLL ***
        
        // Get the movement direction if any
        bool isMoving = playerMovement.moveDirection.sqrMagnitude > 0.01f;
        Vector2 inputDirection = isMoving ? playerMovement.moveDirection.normalized : Vector2.zero;
        
        // Determine dodge direction
        if (isMoving)
        {
            // Get perpendicular vector (90 degrees) to aim direction (for side rolls)
            Vector2 aimPerp = new Vector2(aimDirection.y, -aimDirection.x);
            
            // Calculate how much of the input is in the aim direction
            float forwardComponent = Vector2.Dot(inputDirection, aimDirection);
            
            // Calculate how much of the input is perpendicular to aim direction
            float sideComponent = Vector2.Dot(inputDirection, aimPerp);
            
            // Never allow backward rolls - only forward or sideways
            if (forwardComponent < 0) 
            {
                // If trying to move backward, only use the side component
                if (Mathf.Abs(sideComponent) > 0.2f)
                {
                    // Dodge sideways
                    dodgeDirection = Mathf.Sign(sideComponent) * aimPerp;
                }
                else
                {
                    // Dodge perpendicular to aim direction (choose right by default)
                    dodgeDirection = aimPerp;
                }
            }
            else if (Mathf.Abs(sideComponent) > 0.7f && forwardComponent < 0.3f)
            {
                // Strong side input with minimal forward input - do a side roll
                dodgeDirection = Mathf.Sign(sideComponent) * aimPerp;
            }
            else
            {
                // Mix of forward and side input - blend for a diagonal roll
                // But ensure the forward component is positive (no backward rolling)
                dodgeDirection = (Mathf.Max(0, forwardComponent) * aimDirection) + 
                                (sideComponent * aimPerp);
                dodgeDirection.Normalize();
            }
        }
        else
        {
            // When idle, dodge forward in aim direction or to the sides
            if (idleDodgeInAimDirection)
            {
                dodgeDirection = aimDirection;
            }
            else
            {
                // If not dodging in aim direction when idle, choose right by default
                dodgeDirection = new Vector2(aimDirection.y, -aimDirection.x);
            }

            // Apply multiplier for idle dodges
            dodgeDirection *= idleDodgeDistanceMultiplier;
        }
        
        // Normalize the final direction
        dodgeDirection.Normalize();
        
        // ANIMATION DIRECTION LOGIC
        // Map the physical dodge direction to one of our 6 available animations
        Vector2 animDirection = MapToAvailableAnimDirection(dodgeDirection);
        
        // Set dodge animation parameters
        animator.SetTrigger("Dodge");
        animator.SetFloat("DodgeX", animDirection.x);
        animator.SetFloat("DodgeY", animDirection.y);
        
        // Show dodge info for debugging
        Debug.Log($"Dodge Direction: {dodgeDirection}, Anim Direction: {animDirection}");
        
        // Spawn trail effect if available
        if (dodgeTrailEffect != null)
        {
            GameObject trail = Instantiate(dodgeTrailEffect, transform.position, Quaternion.identity);
            Destroy(trail, trailDuration);
        }
        
        // Start the dodge coroutine
        StartCoroutine(PerformDodge());
    }
    
    // Maps any direction to one of our 6 available animation directions
    Vector2 MapToAvailableAnimDirection(Vector2 direction)
    {
        // Define our 6 available animation directions
        Vector2[] availableDirections = new Vector2[]
        {
            new Vector2(0, 1),      // Up
            new Vector2(0.7f, 0.7f), // Up-Right
            new Vector2(1, 0),      // Right
            new Vector2(0, -1),     // Down
            new Vector2(-1, 0),     // Left
            new Vector2(-0.7f, 0.7f) // Up-Left
        };
        
        // For down-right and down-left direction, we'll map to right and left
        // since we don't have specific animations for them
        if (direction.y < -0.3f && direction.x > 0.3f)
        {
            // Down-right maps to right animation
            return new Vector2(1, 0);
        }
        else if (direction.y < -0.3f && direction.x < -0.3f)
        {
            // Down-left maps to left animation
            return new Vector2(-1, 0);
        }
        
        // For all other directions, find the closest match
        float maxDot = -1;
        Vector2 bestDirection = direction;
        
        foreach (var availDir in availableDirections)
        {
            float dot = Vector2.Dot(direction, availDir);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestDirection = availDir;
            }
        }
        
        return bestDirection;
    }
    
    IEnumerator PerformDodge()
    {
        // Hide the weapon
        if (currentWeapon != null)
        {
            Debug.Log("Hiding weapon");
            currentWeapon.SetWeaponVisible(false);
        }
        
        // Save original state
        bool wasMovementEnabled = playerMovement.enabled;
        
        // Temporarily disable normal movement
        playerMovement.enabled = false;
        
        // Freeze the rigidbody constraints to prevent external forces
        RigidbodyConstraints2D originalConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Store original velocity to restore after dodge
        Vector2 originalVelocity = rb.linearVelocity;
        
        // Apply dodge velocity - FORCE the player in dodge direction only
        rb.linearVelocity = dodgeDirection * dodgeSpeed;
        
        // Wait for dodge duration
        float elapsedTime = 0;
        while (elapsedTime < dodgeDuration)
        {
            // IMPORTANT: Force the velocity to be exactly in the dodge direction
            // This prevents any other forces from affecting the dodge
            rb.linearVelocity = dodgeDirection * dodgeSpeed;
            
            // Explicitly ignore any input during the dodge
            playerMovement.moveDirection = Vector2.zero;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // End dodge state
        isDodging = false;
        
        // Wait for 0.1 seconds before showing the weapon again
        yield return new WaitForSeconds(0.1f);
        
        // Show the weapon again
        if (currentWeapon != null)
        {
            Debug.Log("Showing weapon");
            currentWeapon.SetWeaponVisible(true);
        }
        
        // Restore original movement control
        rb.linearVelocity = Vector2.zero; // Stop completely after dodge
        rb.constraints = originalConstraints; // Restore original constraints
        playerMovement.enabled = wasMovementEnabled;
        
        // Reset animator parameters
        animator.ResetTrigger("Dodge");
    }
    
    // Public method to check if dodging
    public bool IsDodging()
    {
        return isDodging;
    }
    
    // Method to handle weapon changes
    private void HandleWeaponChanged(int index)
    {
        // Update current weapon reference when weapon changes
        if (inventory != null && index >= 0 && index < inventory.weaponSlots.Length)
        {
            currentWeapon = inventory.weaponSlots[index];
        }
    }
    
    // Don't forget to clean up event listeners
    void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnWeaponChanged -= HandleWeaponChanged;
        }
    }
}