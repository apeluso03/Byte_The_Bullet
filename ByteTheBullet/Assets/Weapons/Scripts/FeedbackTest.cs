using UnityEngine;

public class FeedbackTest : MonoBehaviour
{
    [Header("Camera Shake")]
    public bool enableCameraShake = true;
    public float shakeDuration = 0.2f;
    public float shakeIntensity = 0.5f;
    
    [Header("Recoil")]
    public bool enableRecoil = true;
    public float recoilDistance = 0.2f;
} 