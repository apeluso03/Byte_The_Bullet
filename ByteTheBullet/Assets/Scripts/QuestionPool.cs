using System.Collections.Generic;
using UnityEngine;

public class QuestionPool : MonoBehaviour
{
    public static QuestionPool Instance;

    public List<QuestionData> allQuestions = new List<QuestionData>();

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public QuestionData GetRandomQuestion()
    {
        if (allQuestions.Count == 0) return null;

        int index = Random.Range(0, allQuestions.Count);
        QuestionData selected = allQuestions[index];
        allQuestions.RemoveAt(index);
        return selected;
    }
}
