using UnityEngine;

public class WallBlockRemover : MonoBehaviour
{
    [Header("Blocks to Remove")]
    public GameObject block1;
    public GameObject block2;

    private bool alreadyRemoved = false;

    /// <summary>
    /// Call this method when the player answers correctly.
    /// </summary>
    public void RemoveBlocks()
    {
        if (alreadyRemoved) return;

        if (block1 != null)
            Destroy(block1);

        if (block2 != null)
            Destroy(block2);

        alreadyRemoved = true;
        Debug.Log("Blocks removed!");
    }
}