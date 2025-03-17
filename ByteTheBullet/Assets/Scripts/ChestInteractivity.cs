using UnityEngine;

public class ChestInteraction : MonoBehaviour
{
    public Animator animator; // Reference to the Animator component
    private bool isOpen = false; // Track whether the chest is open
    private bool isPlayerNear = false; // Track if the player is near the chest

    void Update()
    {
        // Check if the player is near the chest and presses the "F" key
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            ToggleChest();
        }
    }

    void ToggleChest()
    {
        // Toggle the chest state
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen); // Update the Animator parameter
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player enters the trigger area
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Check if the player exits the trigger area
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}