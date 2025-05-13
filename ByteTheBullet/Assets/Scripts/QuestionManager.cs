using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuestionManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public Text questionText;
    public Image questionImage;
    public Button[] answerButtons;
    public Button dismissButton;
    public Text feedbackText;

    [Header("Audio")]
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    private AudioSource audioSource;

    private int correctAnswerIndex;
    public QuestionData currentQuestion;

    void Start()
    {
        panel.SetActive(false);
        feedbackText.text = "";
        dismissButton.gameObject.SetActive(false);
        audioSource = GetComponent<AudioSource>();

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
            answerButtons[i].interactable = true;
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
            audioSource.PlayOneShot(correctSound);
            FindObjectOfType<WallBlockRemover>().RemoveBlocks();

            foreach (Button btn in answerButtons)
            {
                btn.interactable = false;
            }

            dismissButton.gameObject.SetActive(true);
        }
        else
        {
            feedbackText.text = "Incorrect!";
            audioSource.PlayOneShot(incorrectSound);
            FindObjectOfType<PlayerHealth>().TakeDamage(1);

            answerButtons[selectedIndex].interactable = false;
        }
    }

    void HideQuestion()
    {
        panel.SetActive(false);
        dismissButton.gameObject.SetActive(false);
        feedbackText.text = "";
    }
}
