using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSizeAdjuster : MonoBehaviour
{
    [Header("Camera Size Settings")]
    [SerializeField] private float targetVerticalSize = 8f; // Half-height of the camera view
    [SerializeField] private bool applyOnStart = true;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        if (applyOnStart)
        {
            ApplyCameraSize();
        }
    }

    /// <summary>
    /// Applies the desired vertical camera size.
    /// </summary>
    public void ApplyCameraSize()
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = targetVerticalSize;
            Debug.Log($"Camera vertical size set to {targetVerticalSize}");
        }
        else
        {
            Debug.LogWarning("Camera is not orthographic. This script is for 2D orthographic cameras only.");
        }
    }
}
