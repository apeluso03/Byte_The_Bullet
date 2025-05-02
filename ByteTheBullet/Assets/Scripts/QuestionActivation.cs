using UnityEngine;

public class InteractableQuestionObject : MonoBehaviour
{
    public QuestionData question;
    public QuestionManager questionManager;

    private bool playerInRange = false;

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            questionManager.DisplayQuestion(question);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            // Optional: Show "Press E to interact" UI here
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            // Optional: Hide interaction prompt here
        }
    }
}