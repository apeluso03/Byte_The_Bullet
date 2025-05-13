using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 4;
    public WeaponAiming[] weaponSlots;
    public int currentWeaponIndex = 0;
    
    [Header("References")]
    public Transform weaponPointLeft;
    public Transform weaponPointRight;
    
    // Events
    public event Action<int> OnWeaponChanged;
    
    void Start()
    {
        // Initialize weapon array if not already set in inspector
        if (weaponSlots == null || weaponSlots.Length != maxSlots)
        {
            weaponSlots = new WeaponAiming[maxSlots];
        }
        
        // Setup all weapons to have the correct hand points
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
            {
                // Set references and initially disable
                SetupWeapon(weaponSlots[i], false);
            }
        }
        
        // Equip the first weapon if available
        if (weaponSlots[currentWeaponIndex] != null)
        {
            EquipWeapon(currentWeaponIndex);
        }
    }
    
    void Update()
    {
        // Handle weapon switching with number keys (1-4)
        for (int i = 0; i < maxSlots; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i < weaponSlots.Length)
            {
                EquipWeapon(i);
            }
        }
        
        // Or with mousewheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int newIndex = currentWeaponIndex;
            
            if (scroll > 0)
                newIndex = (newIndex + 1) % maxSlots;
            else
                newIndex = (newIndex - 1 + maxSlots) % maxSlots;
                
            EquipWeapon(newIndex);
        }
        
        // Force weapon equipping with F1-F4 keys
        if (Input.GetKeyDown(KeyCode.F1)) ForceEquipWeapon(0);
        if (Input.GetKeyDown(KeyCode.F2)) ForceEquipWeapon(1);
        if (Input.GetKeyDown(KeyCode.F3)) ForceEquipWeapon(2);
        if (Input.GetKeyDown(KeyCode.F4)) ForceEquipWeapon(3);
        
        // Validate inventory slots every frame to catch when they're cleared
        ValidateInventorySlots();

        // Drop current weapon with G key
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentWeapon();
        }
    }
    
    void ValidateInventorySlots()
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
            {
                // Check if the GameObject is still active in hierarchy
                if (weaponSlots[i].gameObject == null)
                {
                    Debug.LogError($"Weapon in slot {i} has been destroyed!");
                    weaponSlots[i] = null;
                }
                else if (!weaponSlots[i].gameObject.activeInHierarchy && i == currentWeaponIndex)
                {
                    Debug.LogError($"Current weapon in slot {i} is inactive when it should be active!");
                    weaponSlots[i].gameObject.SetActive(true);
                }
            }
        }
    }
    
    private void SetupWeapon(WeaponAiming weapon, bool active)
    {
        if (weapon == null) return;
        
        Debug.Log($"Setting up weapon: {weapon.name}, active: {active}");
        
        // Ensure weapon is properly parented to the player
        if (weapon.transform.parent != transform)
        {
            weapon.transform.SetParent(transform);
        }
        
        // Set hand points references
        weapon.player = transform;
        
        if (weaponPointLeft != null && weaponPointRight != null)
        {
            weapon.weaponPointLeft = weaponPointLeft;
            weapon.weaponPointRight = weaponPointRight;
        }
        else
        {
            Debug.LogError("Weapon points are not assigned in PlayerInventory!");
        }
        
        // Set equipped state and activation state
        weapon.isEquipped = active;
        weapon.gameObject.SetActive(active);
        
        Debug.Log($"Weapon {weapon.name} setup complete - Active: {active}, IsEquipped: {weapon.isEquipped}");
    }
    
    public void EquipWeapon(int index)
    {
        Debug.Log($"Attempting to equip weapon at slot {index}");
        
        if (index < 0 || index >= maxSlots)
        {
            Debug.LogError($"Invalid weapon index: {index}");
            return;
        }
        
        // Deactivate current weapon
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponSlots.Length && 
            weaponSlots[currentWeaponIndex] != null)
        {
            Debug.Log($"Deactivating current weapon at slot {currentWeaponIndex}");
            weaponSlots[currentWeaponIndex].gameObject.SetActive(false);
            weaponSlots[currentWeaponIndex].isEquipped = false;
        }
        
        // Save the current index
        currentWeaponIndex = index;
        
        // Activate new weapon if available
        if (weaponSlots[index] != null)
        {
            Debug.Log($"Activating weapon at slot {index}: {weaponSlots[index].name}");
            SetupWeapon(weaponSlots[index], true);
        }
        else
        {
            Debug.Log($"No weapon in slot {index} to equip");
        }
        
        // Notify listeners
        OnWeaponChanged?.Invoke(index);
    }
    
    public bool AddWeapon(WeaponAiming weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("Attempted to add null weapon to inventory");
            return false;
        }
        
        Debug.Log($"Adding weapon: {weapon.name} to inventory");
        
        // Find first empty slot
        for (int i = 0; i < maxSlots; i++)
        {
            if (weaponSlots[i] == null)
            {
                Debug.Log($"Adding weapon {weapon.name} to slot {i}");
                
                // Set the parent to this object to prevent destruction
                weapon.transform.SetParent(transform);
                
                // Add weapon to inventory array
                weaponSlots[i] = weapon;
                
                // Setup the weapon (this will handle activation state)
                SetupWeapon(weapon, i == currentWeaponIndex);
                
                // If we don't have a weapon equipped, equip this one
                if (currentWeaponIndex < 0 || weaponSlots[currentWeaponIndex] == null)
                {
                    Debug.Log($"No current weapon, equipping {weapon.name} at slot {i}");
                    EquipWeapon(i);
                }
                
                Debug.Log($"After adding, slot {i} contains: {(weaponSlots[i] != null ? weaponSlots[i].name : "NULL")}");
                
                return true;
            }
        }
        
        // No space found
        Debug.Log("No empty slots in inventory");
        return false;
    }

/*     void OnGUI()
    {
        // Only show in play mode
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;
        
        GUI.Box(new Rect(10, 10, 250, 150), "Inventory Debug");
        
        // Show weapon points status
        string wpStatus = $"Weapon Points: Left: {(weaponPointLeft != null ? "Set" : "NULL")}, Right: {(weaponPointRight != null ? "Set" : "NULL")}";
        GUI.Label(new Rect(20, 40, 230, 20), wpStatus, style);
        
        // Show current weapon index
        GUI.Label(new Rect(20, 60, 230, 20), $"Current Weapon Index: {currentWeaponIndex}", style);
        
        // Show weapon slots
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            string slotText = $"Slot {i}: ";
            
            if (weaponSlots[i] != null)
            {
                slotText += $"{weaponSlots[i].name} ({(weaponSlots[i].gameObject.activeSelf ? "Active" : "Inactive")})";
                
                // Check if it's the equipped weapon
                if (i == currentWeaponIndex)
                    slotText += " [EQUIPPED]";
            }
            else
            {
                slotText += "Empty";
            }
            
            GUI.Label(new Rect(20, 80 + (i * 20), 230, 20), slotText, style);
        }
        
        // Add key instruction
        GUI.Label(new Rect(20, 170, 230, 20), "Press 1-4 to switch weapons", style);
    } */

    public void ForceEquipWeapon(int index)
    {
        if (index < 0 || index >= maxSlots) return;
        
        if (weaponSlots[index] != null)
        {
            Debug.Log($"Force equipping weapon at slot {index}");
            EquipWeapon(index);
        }
        else
        {
            Debug.Log($"No weapon in slot {index} to force equip");
        }
    }

    public void DropCurrentWeapon(){
        if (currentWeaponIndex >= 0 && weaponSlots[currentWeaponIndex] != null){
            WeaponAiming currentWeapon = weaponSlots[currentWeaponIndex];
            
            // Create pickup prefab
            GameObject pickupPrefab = Resources.Load<GameObject>("WeaponPickup");
            if (pickupPrefab != null)
            {
                // Spawn pickup slightly in front of player
                Vector2 dropPos = transform.position + (Vector3)(Vector2.right * transform.localScale.x);
                GameObject pickup = Instantiate(pickupPrefab, dropPos, Quaternion.identity);
                
                // Set the weapon prefab reference
                SimpleWeaponPickup pickupScript = pickup.GetComponent<SimpleWeaponPickup>();
                if (pickupScript != null)
                {
                    // Get the WeaponAiming component from the current weapon's GameObject
                    WeaponAiming weaponPrefabComponent = currentWeapon.gameObject.GetComponent<WeaponAiming>();
                    pickupScript.weaponPrefab = weaponPrefabComponent;
                }
            }

            // Remove weapon from inventory
            Destroy(currentWeapon.gameObject);
            weaponSlots[currentWeaponIndex] = null;
            
            // Find next available weapon
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null)
                {
                    EquipWeapon(i);
                    break;
                }
            }
            
            // Notify listeners
            OnWeaponChanged?.Invoke(currentWeaponIndex);
        }
    }
}