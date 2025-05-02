using UnityEngine;
using System.Collections;
using System;

public class CameraShake : MonoBehaviour
{
    public void ShakeCamera(float intensity, float duration)
    {
        //Do nothing
    }
}
        /*
        StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            transform.localPosition = new Vector3(x, y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        //transform.localPosition = originalPosition;
    }
} */