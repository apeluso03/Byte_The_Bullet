using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuestionManager : MonoBehaviour
{
    public GameObject panel;
    public Text questionText;
    public Image questionImage;
    public Button[] answerButtons;

    public Button dismissButton;

    public Text feedbackText;

    private int correctAnswerIndex;

    public QuestionData currentQuestion;

    void Start()
    {
        panel.SetActive(false);
        feedbackText.text = "";
        dismissButton.gameObject.SetActive(false);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
        }

        dismissButton.onClick.AddListener(HideQuestion);
    }

    public void DisplayQuestion(QuestionData question)
    {
        currentQuestion = question;

        panel.SetActive(true);
        dismissButton.gameObject.SetActive(false);
        questionText.text = question.questionText;

        questionImage.sprite = question.questionImage;
        questionImage.gameObject.SetActive(question.questionImage != null);

        correctAnswerIndex = question.correctAnswerIndex;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].interactable = true; // Re-enable buttons each time
            answerButtons[i].GetComponentInChildren<Text>().text = question.answerOptions[i];
            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
        }

        feedbackText.text = "";
    }


    private void OnAnswerClicked(int selectedIndex)
    {
        if (selectedIndex == correctAnswerIndex)
        {
            feedbackText.text = "Correct!";
            FindObjectOfType<WallBlockRemover>().RemoveBlocks();
            if (currentQuestion.correctAnswerIndex == 3) { 
                SceneManager.LoadScene("ThankPlayer");
            }
        }
        else
        {
            feedbackText.text = "Incorrect!";
            FindObjectOfType<PlayerHealth>().TakeDamage(1);
        }

        foreach (Button btn in answerButtons)
        {
            btn.interactable = false;
        }
        dismissButton.gameObject.SetActive(true);
    }

    void HideQuestion()
    {
        panel.SetActive(false);
        dismissButton.gameObject.SetActive(false);
        feedbackText.text = "";
    }
}