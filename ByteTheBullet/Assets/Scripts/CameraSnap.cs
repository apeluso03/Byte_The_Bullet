using UnityEngine;

public class CameraSnap : MonoBehaviour
{
    public Transform player;
    private Transform currentRoomCenter;

    public float snapSpeed = 5f;
    public float zoomSpeed = 3f;

    private Camera cam;

    public float defaultZoom;     // Normal room zoom (initialized in Start)
    public float followZoom = 8f; // Zoomed out when following player
    private float targetZoom;

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main camera not found.");
            return;
        }

        defaultZoom = cam.orthographicSize;  // Initialize here
        targetZoom = defaultZoom;

        FindStartingRoom();
    }

    void LateUpdate()
    {
        if (player == null || cam == null) return;

        Vector3 targetPos;

        if (currentRoomCenter == null)
        {
            // Follow the player and zoom out
            targetPos = new Vector3(player.position.x, player.position.y, transform.position.z);
            targetZoom = followZoom;
        }
        else
        {
            // Snap to room center and zoom in
            targetPos = new Vector3(currentRoomCenter.position.x, currentRoomCenter.position.y, transform.position.z);
            targetZoom = defaultZoom;
        }

        // Smooth camera move
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * snapSpeed);

        // Smooth zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
    }

    public void SetRoom(Transform roomCenter)
    {
        currentRoomCenter = roomCenter;

        if (roomCenter == null)
        {
            Debug.Log("CameraSnap: Entered Final Boss Room — following player and zooming out.");
        }
    }

    private void FindStartingRoom()
    {
        if (player == null) return;

        Collider2D[] hits = Physics2D.OverlapPointAll(player.position);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("RoomMiddle"))
            {
                SetRoom(hit.transform);
                return;
            }

            if (hit.CompareTag("FinalBossRoom"))
            {
                SetRoom(null); // triggers follow + zoom out
                return;
            }
        }
    }

    public Transform GetCurrentRoomCenter()
    {
        return currentRoomCenter;
    }

}

