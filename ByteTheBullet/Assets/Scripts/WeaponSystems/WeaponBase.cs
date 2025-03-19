using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Properties")]
    public string weaponName;
    public Sprite weaponIcon;
    public int weaponID;
    
    [Header("Transform")]
    public Transform muzzlePoint;
    
    [Header("Components")]
    protected SpriteRenderer weaponRenderer;
    protected Animator weaponAnimator;
    protected AudioSource audioSource;
    
    protected Transform playerTransform;
    protected bool isActive = false;
    
    // Add a protection flag at the class level
    protected bool processingFireRequest = false;
    
    // Called when weapon is equipped
    public virtual void Initialize(Transform player)
    {
        playerTransform = player;
        
        // Get required components
        weaponRenderer = GetComponent<SpriteRenderer>();
        weaponAnimator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Enable and position the weapon
        gameObject.SetActive(true);
        isActive = true;
        
        // Start following the player
        StartFollowing();
    }
    
    // Called when weapon is unequipped
    public virtual void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
    
    // Implement in child class
    protected virtual void StartFollowing()
    {
        // Position near player
        transform.position = playerTransform.position;
    }
    
    // Update for positioning, aiming, etc.
    protected virtual void Update()
    {
        if (!isActive || playerTransform == null)
            return;
            
        // Basic following - override in WeaponFollower
        transform.position = playerTransform.position;
    }
    
    // Modify the Fire method to prevent recursive calls
    public virtual void Fire()
    {
        // Prevent re-entry
        if (processingFireRequest)
        {
            return;
        }
        
        processingFireRequest = true;
        
        // Implement your firing logic here or in derived classes
        
        processingFireRequest = false;
    }
    
    // Implement in specific weapons
    public virtual void Reload() { }
    public virtual string GetAmmoText() { return ""; }
}