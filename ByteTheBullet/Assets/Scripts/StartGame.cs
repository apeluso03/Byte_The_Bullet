using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public void startGame()
    {
        // Destroy background music if it exists
        var bgm = FindObjectOfType<bgmScript>();
        if (bgm != null) Destroy(bgm.gameObject);

        // Destroy any persistent camera
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<CameraSnap>() != null)
        {
            Destroy(cam.gameObject);
        }

        // Reset any static game state (optional)

        // Finally, load the scene
        SceneManager.LoadScene("Dungeon");
    }
}
