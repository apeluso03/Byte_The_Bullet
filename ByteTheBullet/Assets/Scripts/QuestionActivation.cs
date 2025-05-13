using UnityEngine;
using UnityEngine.UI;

public class InteractableQuestionObject : MonoBehaviour
{
    public QuestionManager questionManager;
    public GameObject interactionPromptUI; // Assign this in the Inspector

    private bool playerInRange = false;

    void Start()
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInRange && !questionManager.panel.activeSelf && interactionPromptUI != null)
        {
            interactionPromptUI.SetActive(true);
        }
        else if (interactionPromptUI != null && (questionManager.panel.activeSelf || !playerInRange))
        {
            interactionPromptUI.SetActive(false);
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !questionManager.panel.activeSelf)
        {
            QuestionData randomQuestion = QuestionPool.Instance.GetRandomQuestion();

            if (randomQuestion != null)
            {
                questionManager.DisplayQuestion(randomQuestion);
                interactionPromptUI.SetActive(false); // Hide prompt when interacting
            }
            else
            {
                Debug.LogWarning("No more questions available in the pool.");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionPromptUI != null)
                interactionPromptUI.SetActive(false);
        }
    }
}
