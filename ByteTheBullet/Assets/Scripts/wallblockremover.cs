using UnityEngine;
using UnityEngine.UI;

public class WallBlockRemover : MonoBehaviour
{
    public Text scoreText;

    [Header("Blocks to Remove")]
    public GameObject block1;
    public GameObject block2;
    public GameObject block3;
    public GameObject block4;
    public GameObject block5;
    public GameObject block6;
    public GameObject block7;
    public GameObject block8;

    private bool alreadyRemoved = false;

    /// <summary>
    /// Call this method when the player answers correctly.
    /// </summary>
    public void RemoveBlocks()
    {
        // Remove blocks based on score
        if (scoreText.text == "1")
        {
            Debug.Log("Score is 1");
            if (block1 != null)
                Destroy(block1);
            if (block2 != null)
                Destroy(block2);
        }

        if (scoreText.text == "2")
        {
            Debug.Log("Score is 2");
            if (block3 != null)
                Destroy(block3);
            if (block4 != null)
                Destroy(block4);
        }

        if (scoreText.text == "5")
        {
            Debug.Log("Score is 5");
            if (block7 != null)
                Destroy(block7);
            if (block8 != null)
                Destroy(block8);
        }

        // Also check if the current room center is null (Final Boss Room)
            CameraSnap camSnap = Camera.main.GetComponent<CameraSnap>();
        if (camSnap != null && camSnap.GetCurrentRoomCenter() == null)
        {
            Debug.Log("Final Boss Room active — removing blocks 5 and 6");
            if (block5 != null)
                Destroy(block5);
            if (block6 != null)
                Destroy(block6);
        }

        Debug.Log("Blocks removed!");
    }
}
