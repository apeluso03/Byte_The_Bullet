using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ClickToAdvanceText : MonoBehaviour
{
    public Text uiText; // Assign in Inspector
    [TextArea(3, 10)]
    public string[] textScreens;
    public string nextSceneName = "GameStartScene";

    public AudioClip clickSound;
    private AudioSource audioSource;

    private int currentIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (textScreens.Length > 0)
            uiText.text = textScreens[0];
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isTransitioning)
        {
            if (clickSound != null && audioSource != null)
                audioSource.PlayOneShot(clickSound);

            currentIndex++;

            if (currentIndex < textScreens.Length)
            {
                uiText.text = textScreens[currentIndex];
            }
            else
            {
                isTransitioning = true;
                StartCoroutine(PlaySoundThenLoadScene());
            }
        }
    }

    private IEnumerator PlaySoundThenLoadScene()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
            yield return new WaitForSeconds(clickSound.length);
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
