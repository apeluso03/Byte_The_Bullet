using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public Image[] weaponSlots;
    public Image selectionIndicator;
    
    void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<PlayerInventory>();
        }
        
        if (playerInventory != null)
        {
            playerInventory.OnWeaponChanged += UpdateSelection;
            
            // Initial update
            UpdateUI();
        }
    }
    
    void UpdateUI()
    {
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (i < playerInventory.weaponSlots.Length && 
                playerInventory.weaponSlots[i] != null)
            {
                // Get sprite from weapon
                SpriteRenderer weaponRenderer = playerInventory.weaponSlots[i].GetComponent<SpriteRenderer>();
                if (weaponRenderer != null)
                {
                    weaponSlots[i].sprite = weaponRenderer.sprite;
                    weaponSlots[i].color = Color.white;
                }
            }
            else
            {
                weaponSlots[i].sprite = null;
                weaponSlots[i].color = new Color(1, 1, 1, 0.3f); // Semi-transparent
            }
        }
        
        UpdateSelection(playerInventory.currentWeaponIndex);
    }
    
    void UpdateSelection(int selectedIndex)
    {
        if (selectionIndicator != null && selectedIndex >= 0 && selectedIndex < weaponSlots.Length)
        {
            selectionIndicator.rectTransform.position = weaponSlots[selectedIndex].rectTransform.position;
        }
    }
}