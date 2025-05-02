using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CameraSnap camSnap = Camera.main.GetComponent<CameraSnap>();
            if (camSnap != null)
            {
                camSnap.SetRoom(transform);
            }
        }
    }
}