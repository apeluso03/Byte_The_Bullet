using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class ButtonSoundAndScene : MonoBehaviour
{
    public AudioClip clickSound;
    public string sceneToLoad;

    private AudioSource audioSource;
    private Button button;
    private bool hasClicked = false;

    void Start()
    {
        button = GetComponent<Button>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        button.onClick.AddListener(HandleClick);
    }

    void HandleClick()
    {
        if (hasClicked) return;
        hasClicked = true;

        StartCoroutine(PlaySoundThenLoadScene());
    }

    private IEnumerator PlaySoundThenLoadScene()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
            yield return new WaitForSeconds(clickSound.length);
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
