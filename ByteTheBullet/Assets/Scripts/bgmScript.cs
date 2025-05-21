using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class bgmScript : MonoBehaviour
{
    public static bgmScript instance; // Singleton
    public AudioClip bgmClip;

    private AudioSource audioSource;

    void Awake()
    {
        // Singleton pattern: Destroy duplicates
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Destroy this if one already exists
            return;
        }

        instance = this;
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
