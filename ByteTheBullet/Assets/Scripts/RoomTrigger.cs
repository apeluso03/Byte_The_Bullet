using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class RoomTrigger : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    private bool hasSpawned = false;
    public float spawnDelay = 0.5f; // Delay in seconds before enemies spawn

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Camera snapping or follow mode
            CameraSnap camSnap = Camera.main.GetComponent<CameraSnap>();
            if (camSnap != null)
            {
                if (CompareTag("FinalBossRoom"))
                {
                    camSnap.SetRoom(null); // Let camera follow player
                }
                if (CompareTag("GameEnd"))
                {
                    SceneManager.LoadScene("GameEnd");
                }
                else if (CompareTag("RoomMiddle"))
                {
                    camSnap.SetRoom(transform); // Snap to room center
                }
            }

            // Enemy spawning with delay
            if (!hasSpawned)
            {
                StartCoroutine(SpawnEnemiesWithDelay());
                hasSpawned = true;
            }
        }
    }

    IEnumerator SpawnEnemiesWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay); // Wait for the delay

        // After delay, spawn enemies
        foreach (Transform point in spawnPoints)
        {
            Instantiate(enemyPrefab, point.position, Quaternion.identity);
        }
    }
}
