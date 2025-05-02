using UnityEngine;

public class CameraSnap : MonoBehaviour
{
    public Transform player;
    private Transform currentRoomCenter;

    public float snapSpeed = 5f;
    public float snapThreshold = 0.05f;

    void Start()
    {
        FindStartingRoom();
    }

    void Update()
    {
        if (currentRoomCenter == null) return;

        Vector3 targetPos = new Vector3(
            currentRoomCenter.position.x,
            currentRoomCenter.position.y,
            transform.position.z
        );

        if (Vector3.Distance(transform.position, targetPos) > snapThreshold)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                Time.deltaTime * snapSpeed
            );
        }
    }

    public void SetRoom(Transform roomCenter)
    {
        if (roomCenter != null)
        {
            currentRoomCenter = roomCenter;
        }
    }

    private void FindStartingRoom()
    {
        if (player == null)
        {
            Debug.LogWarning("Player not assigned to CameraSnap.");
            return;
        }

        Collider2D[] hits = Physics2D.OverlapPointAll(player.position);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("RoomMiddle"))
            {
                SetRoom(hit.transform);
                Debug.Log("CameraSnap: Found starting room: " + hit.name);
                return;
            }
        }

        Debug.LogWarning("CameraSnap: No RoomMiddle found at player start position.");
    }
}
