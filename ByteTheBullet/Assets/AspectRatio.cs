using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    public float targetAspectRatio = 4f / 3f;

    private Camera cam;
    private float lastWidth;
    private float lastHeight;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateCameraViewport();
    }

    void Update()
    {
        // Only recalculate if screen size changed (including fullscreen toggle)
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            UpdateCameraViewport();
        }
    }

    void UpdateCameraViewport()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspectRatio;

        if (scaleHeight < 1.0f)
        {
            cam.rect = new Rect(
                0f,
                (1.0f - scaleHeight) / 2.0f,
                1.0f,
                scaleHeight
            );
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            cam.rect = new Rect(
                (1.0f - scaleWidth) / 2.0f,
                0f,
                scaleWidth,
                1.0f
            );
        }
    }
}
