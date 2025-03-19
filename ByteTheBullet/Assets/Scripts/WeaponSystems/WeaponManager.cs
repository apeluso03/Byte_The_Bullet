using UnityEngine;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Setup")]
    public Transform weaponSocket;
    public GameObject[] startingWeapons;
    
    [Header("References")]
    public PlayerAim aimController;
    
    // Inventory management
    private List<GameObject> weaponInventory = new List<GameObject>();
    private List<WeaponBase> instantiatedWeapons = new List<WeaponBase>();
    private WeaponBase currentWeapon;
    private int currentWeaponIndex = 0;
    
    void Start()
    {
        // Get reference to aim controller if not set
        if (aimController == null)
            aimController = GetComponent<PlayerAim>();
            
        // Create weapon socket if needed
        if (weaponSocket == null && aimController != null && aimController.aimPivot != null)
        {
            GameObject socketObj = new GameObject("WeaponSocket");
            socketObj.transform.SetParent(aimController.aimPivot);
            socketObj.transform.localPosition = new Vector3(0.3f, 0, 0);
            weaponSocket = socketObj.transform;
        }
        
        // Add starting weapons
        foreach (GameObject weaponPrefab in startingWeapons)
        {
            if (weaponPrefab != null)
                AddWeaponToInventory(weaponPrefab);
        }
        
        // Equip first weapon if available
        if (instantiatedWeapons.Count > 0)
            EquipWeapon(0);
    }
    
    void Update()
    {
        if (currentWeapon == null) return;
        
        // Only handle weapon inputs if the current weapon isn't a StrictSemiAutoWeapon
        if (!(currentWeapon is StrictSemiAutoWeapon))
        {
            // Handle weapon inputs
            if (Input.GetButton("Fire1"))
                currentWeapon.Fire();
        }
        // The StrictSemiAutoWeapon will handle its own input in its Update method
        
        if (Input.GetKeyDown(KeyCode.R))
            currentWeapon.Reload();
            
        // Weapon switching
        if (Input.GetKeyDown(KeyCode.Q))
            CycleWeapon(-1);
            
        if (Input.GetKeyDown(KeyCode.E))
            CycleWeapon(1);
            
        // Number keys for direct weapon selection
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) && i < instantiatedWeapons.Count)
            {
                EquipWeapon(i);
                break;
            }
        }
    }
    
    public void AddWeaponDirectly(GameObject weaponPrefab)
    {
        // Skip if we already have this weapon
        WeaponBase prefabWeapon = weaponPrefab.GetComponent<WeaponBase>();
        if (prefabWeapon == null)
        {
            Debug.LogError("Weapon prefab doesn't have WeaponBase component!");
            return;
        }
        
        // Check if we already have a weapon with this ID
        foreach (WeaponBase weapon in instantiatedWeapons)
        {
            if (weapon.weaponID == prefabWeapon.weaponID)
                return;
        }
        
        // Add to inventory
        weaponInventory.Add(weaponPrefab);
        
        // Instantiate weapon
        GameObject weaponInstance = Instantiate(weaponPrefab);
        WeaponBase weaponComponent = weaponInstance.GetComponent<WeaponBase>();
        
        if (weaponComponent != null)
        {
            // Add to instantiated weapons list
            instantiatedWeapons.Add(weaponComponent);
            
            // Deactivate initially
            weaponInstance.SetActive(false);
        }
        else
        {
            Debug.LogError("Weapon prefab doesn't have WeaponBase component!");
            Destroy(weaponInstance);
        }
    }
    
    public void AddWeaponToInventory(GameObject weaponPrefab)
    {
        AddWeaponDirectly(weaponPrefab);
    }
    
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= instantiatedWeapons.Count)
            return;
            
        // Deactivate current weapon
        if (currentWeapon != null)
            currentWeapon.Deactivate();
            
        // Update index
        currentWeaponIndex = index;
        
        // Activate new weapon
        currentWeapon = instantiatedWeapons[currentWeaponIndex];
        currentWeapon.Initialize(transform);
        
        // Setup weapon position
        if (weaponSocket != null)
        {
            // Don't parent to socket - this can cause scaling issues
            // currentWeapon.transform.SetParent(weaponSocket, false);
            // currentWeapon.transform.localPosition = Vector3.zero;
            
            // Instead, tell the weapon follower about the socket
            WeaponFollower follower = currentWeapon.GetComponent<WeaponFollower>();
            if (follower != null)
            {
                follower.SetPlayer(transform);
                follower.SetSocket(weaponSocket);
                
                // Debug message to confirm socket is being used
                Debug.Log("Weapon equipped at socket: " + weaponSocket.name);
            }
        }
        else
        {
            Debug.LogWarning("No weapon socket assigned - weapon will follow player center");
        }
        
        Debug.Log("Equipped weapon: " + currentWeapon.weaponName);
    }
    
    public void CycleWeapon(int direction)
    {
        if (instantiatedWeapons.Count <= 1)
            return;
            
        int newIndex = currentWeaponIndex + direction;
        
        // Wrap around
        if (newIndex < 0)
            newIndex = instantiatedWeapons.Count - 1;
        else if (newIndex >= instantiatedWeapons.Count)
            newIndex = 0;
            
        EquipWeapon(newIndex);
    }
    
    public bool HasWeapon(int weaponID)
    {
        foreach (WeaponBase weapon in instantiatedWeapons)
        {
            if (weapon.weaponID == weaponID)
                return true;
        }
        return false;
    }
    
    public string GetCurrentAmmoText()
    {
        if (currentWeapon != null)
            return currentWeapon.GetAmmoText();
            
        return "";
    }
    
    // Returns the current active weapon
    public WeaponBase GetCurrentWeapon()
    {
        return currentWeapon;
    }
} 