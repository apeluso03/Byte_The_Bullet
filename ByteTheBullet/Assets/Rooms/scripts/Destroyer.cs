using UnityEngine;

public class Destroyer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(other.gameObject, 0.5f);
    }

}
