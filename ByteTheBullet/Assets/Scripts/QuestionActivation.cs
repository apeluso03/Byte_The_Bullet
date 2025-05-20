using UnityEngine;
using UnityEngine.UI;

public class InteractableQuestionObject : MonoBehaviour
{
    public QuestionManager questionManager;

    private bool playerInRange = false;

    void Start()
    {}

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !questionManager.panel.activeSelf)
        {
            QuestionData randomQuestion = QuestionPool.Instance.GetRandomQuestion();

            if (randomQuestion != null)
            {
                questionManager.DisplayQuestion(randomQuestion);
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
            Debug.Log("Player entered range");
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
