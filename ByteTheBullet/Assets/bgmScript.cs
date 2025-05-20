using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMScript : MonoBehaviour
{
    public AudioClip bgmClip;        // Assign in Inspector
    [Range(0f, 1f)]
    public float volume = 0.1f;      // Adjustable volume (0 to 1)

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        audioSource.Play();
    }
}
