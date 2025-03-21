using UnityEngine;

public class WeaponAiming : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform weaponPointLeft;
    public Transform weaponPointRight;
    
    [Header("Weapon Settings")]
    public Vector3 weaponScale = new Vector3(1, 1, 1);
    [Tooltip("Position of the grip relative to weapon's pivot (RIGHT hand)")]
    public Vector2 rightHandGripPoint;
    [Tooltip("Position of the grip relative to weapon's pivot (LEFT hand)")]
    public Vector2 leftHandGripPoint;
    
    [Header("Switching Settings")]
    [Tooltip("Switch hands when mouse crosses the vertical line through the player's center")]
    public bool swapAtCenterLine = true;
    [Tooltip("How far left/right the player needs to aim to switch hands (only if not using center line)")]
    public float handSwitchThreshold = 0.5f;

    [Header("Debug")]
    public bool showDebugLines = true;
    public Color debugColor = Color.yellow;

    [Header("Hand Visuals")]
    public GameObject leftHandVisual;
    public GameObject rightHandVisual;
    
    [Header("Weapon Layering")]
    [Tooltip("Weapon appears behind character when aiming up")]
    public bool hideWeaponWhenAimingUp = true;
    [Tooltip("How far up the player needs to aim for weapon to go behind them")]
    public float upDirectionThreshold = 0.8f;
    [Tooltip("Small buffer to prevent flickering when aiming near the threshold")]
    public float layerSwitchBuffer = 0.05f;
    public string behindPlayerLayer = "BehindPlayer";
    public string inFrontOfPlayerLayer = "InFrontOfPlayer";
    
    // Private variables
    private Transform currentHand;
    private bool isUsingLeftHand = false; // Track which hand we're using
    
    // For handling weapon state
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // References to renderers
    private SpriteRenderer weaponRenderer;
    private Vector3 aimDirection; // Store aim direction for other methods to use

    void Start()
    {
        // Initialize with right hand
        currentHand = weaponPointRight;
        isUsingLeftHand = false;
        
        // Set initial scale
        transform.localScale = weaponScale;
        
        // Get the weapon's sprite renderer
        weaponRenderer = GetComponent<SpriteRenderer>();
        
        // Initialize hand visibility
        UpdateHandVisibility();
        
        // Store initial state
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // Get mouse position and direction
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        aimDirection = (mousePosition - player.position).normalized;
        
        // First check if we need to switch hands
        SwitchHands(mousePosition);
        
        // Then rotate and position the weapon
        UpdateWeaponTransform(aimDirection);
        
        // Update weapon sorting layer based on aim direction
        UpdateWeaponLayering();
        
        // Debug visualization during gameplay
        if (showDebugLines)
            DrawDebugInfo(mousePosition);
    }
    
    void SwitchHands(Vector3 mousePosition)
    {
        bool handChanged = false;
        
        if (swapAtCenterLine)
        {
            // Switch hands based on mouse crossing the center line
            bool mouseIsLeft = mousePosition.x < player.position.x;
            
            if (mouseIsLeft && currentHand != weaponPointLeft)
            {
                currentHand = weaponPointLeft;
                isUsingLeftHand = true;
                handChanged = true;
            }
            else if (!mouseIsLeft && currentHand != weaponPointRight)
            {
                currentHand = weaponPointRight;
                isUsingLeftHand = false;
                handChanged = true;
            }
        }
        else
        {
            // Get direction to mouse
            Vector3 aimDirection = (mousePosition - player.position).normalized;
            
            // Threshold-based switching
            if (aimDirection.x < -handSwitchThreshold && currentHand != weaponPointLeft)
            {
                currentHand = weaponPointLeft;
                isUsingLeftHand = true;
                handChanged = true;
            }
            else if (aimDirection.x > handSwitchThreshold && currentHand != weaponPointRight)
            {
                currentHand = weaponPointRight;
                isUsingLeftHand = false;
                handChanged = true;
            }
        }
        
        // Update hand visibility if the hand changed
        if (handChanged)
            UpdateHandVisibility();
    }
    
    void UpdateWeaponTransform(Vector3 aimDirection)
    {
        // Calculate rotation to face the mouse
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Choose the appropriate grip point based on current hand
        Vector2 gripPoint = isUsingLeftHand ? leftHandGripPoint : rightHandGripPoint;
        
        // Apply scale based on which hand is being used
        if (isUsingLeftHand)
        {
            transform.localScale = new Vector3(weaponScale.x, -weaponScale.y, weaponScale.z);
            
            // For left hand, negate the X value to make it go in the expected direction
            // but keep the Y flipped as before
            Vector3 localGripOffset = new Vector3(-gripPoint.x, -gripPoint.y, 0);
            
            // Convert local offset to world space using the weapon's rotation
            Vector3 worldGripOffset = transform.TransformDirection(localGripOffset);
            
            // Position the weapon
            transform.position = currentHand.position + worldGripOffset;
        }
        else
        {
            transform.localScale = weaponScale;
            
            // Right hand works normally
            Vector3 localGripOffset = new Vector3(gripPoint.x, gripPoint.y, 0);
            Vector3 worldGripOffset = transform.TransformDirection(localGripOffset);
            transform.position = currentHand.position + worldGripOffset;
        }
        
        // Save the state for next frame
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
    
    void DrawDebugInfo(Vector3 mousePosition)
    {
        if (!Application.isPlaying) return;
        
        // Draw line from hand to weapon
        Debug.DrawLine(currentHand.position, transform.position, debugColor);
        
        // Draw weapon forward direction
        Debug.DrawLine(transform.position, transform.position + transform.right * 0.5f, Color.blue);
        
        // Draw grip point
        Vector2 gripPoint = isUsingLeftHand ? leftHandGripPoint : rightHandGripPoint;
        Vector3 localGripPos;
        
        if (isUsingLeftHand)
            localGripPos = new Vector3(-gripPoint.x, -gripPoint.y, 0); // Flip both X and Y for left hand
        else
            localGripPos = new Vector3(gripPoint.x, gripPoint.y, 0);
        
        Vector3 worldGripPos = transform.TransformPoint(localGripPos);
        Debug.DrawLine(transform.position, worldGripPos, Color.red);
        
        // Draw the center line of the player (vertical Y axis)
        if (swapAtCenterLine)
        {
            Debug.DrawLine(
                new Vector3(player.position.x, player.position.y - 2, 0),
                new Vector3(player.position.x, player.position.y + 2, 0),
                Color.magenta
            );
        }
    }
    
    // Visual debugging in editor
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;
        
        // Draw right hand grip point
        Gizmos.color = Color.green;
        Vector3 rightGripWorld = transform.TransformPoint(new Vector3(rightHandGripPoint.x, rightHandGripPoint.y, 0));
        Gizmos.DrawSphere(rightGripWorld, 0.05f);
        Gizmos.DrawLine(transform.position, rightGripWorld);
        
        // Draw left hand grip point
        Gizmos.color = Color.red;
        Vector3 leftGripWorld = transform.TransformPoint(new Vector3(leftHandGripPoint.x, leftHandGripPoint.y, 0));
        Gizmos.DrawSphere(leftGripWorld, 0.05f);
        Gizmos.DrawLine(transform.position, leftGripWorld);
        
        // Draw the center line of the player if using centerline switching
        if (swapAtCenterLine)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                new Vector3(player.position.x, player.position.y - 2, 0),
                new Vector3(player.position.x, player.position.y + 2, 0)
            );
        }
    }

    // Update which hand is visible
    void UpdateHandVisibility()
    {
        // Only proceed if we have hand visuals assigned
        if (leftHandVisual != null && rightHandVisual != null)
        {
            // Show only the hand that's holding the weapon
            leftHandVisual.SetActive(isUsingLeftHand);
            rightHandVisual.SetActive(!isUsingLeftHand);
        }
    }

    // New method to handle weapon layering
    void UpdateWeaponLayering()
    {
        if (weaponRenderer == null) return;
        
        // Track the current layer to avoid unnecessary switches
        string currentLayer = weaponRenderer.sortingLayerName;
        string targetLayer = currentLayer;
        
        if (hideWeaponWhenAimingUp)
        {
            if (currentLayer == behindPlayerLayer && aimDirection.y < (upDirectionThreshold - layerSwitchBuffer))
            {
                // Switch to front layer only when clearly below threshold
                targetLayer = inFrontOfPlayerLayer;
            }
            else if (currentLayer == inFrontOfPlayerLayer && aimDirection.y > upDirectionThreshold)
            {
                // Switch to behind layer when above threshold
                targetLayer = behindPlayerLayer;
            }
        }
        else
        {
            // If feature is disabled, always show in front
            targetLayer = inFrontOfPlayerLayer;
        }
        
        // Only change layer if needed
        if (currentLayer != targetLayer)
        {
            weaponRenderer.sortingLayerName = targetLayer;
        }
    }

    public void SetWeaponVisible(bool visible)
    {
        if (weaponRenderer != null)
        {
            Debug.Log($"Setting weapon visibility to: {visible}");
            weaponRenderer.enabled = visible;
        }
        
        // Optionally hide hands too
        if (leftHandVisual != null)
        {
            Debug.Log($"Setting left hand visibility to: {visible && isUsingLeftHand}");
            leftHandVisual.SetActive(visible && isUsingLeftHand);
        }
        if (rightHandVisual != null)
        {
            Debug.Log($"Setting right hand visibility to: {visible && !isUsingLeftHand}");
            rightHandVisual.SetActive(visible && !isUsingLeftHand);
        }
    }
}