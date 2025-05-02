using UnityEngine;

[CreateAssetMenu(fileName = "NewQuestion", menuName = "Quiz/Question")]
public class QuestionData : ScriptableObject
{
    [TextArea]
    public string questionText;
    public Sprite questionImage;
    public string[] answerOptions = new string[4];
    public int correctAnswerIndex; // 0 to 3
}