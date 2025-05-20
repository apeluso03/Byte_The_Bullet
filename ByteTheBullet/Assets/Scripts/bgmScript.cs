using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class bgmScript : MonoBehaviour
{
    public AudioClip bgmClip; // Assign in Inspector
    private AudioSource audioSource;

    void Awake()
    {
        // Ensure only one BGM instance persists (optional)
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        audioSource.Play();
    }
}
