using UnityEngine;

public class FuseBoxInteractor : MonoBehaviour, IInteractable
{
    public GameObject fuseboxWithFuse;  // The model of the fusebox with the fuse inserted
    public GameObject fuseboxWithoutFuse; // The default model without the fuse
    public GameMaster game;
    public ItemCollectible fuse;

    public void Interact(GameObject player)
    {
        // Check if the player has a fuse
        if (InventorySystem.Instance.HasItem("fuse"))
        {
            // Update the fusebox model to show the fuse inserted
            fuseboxWithFuse.SetActive(true);
            fuseboxWithoutFuse.SetActive(false);
            InventorySystem.Instance.RemoveItem(fuse);
            

            // Trigger the checklist to open the door
            game.CompleteObjective();
            Debug.Log("Fuse inserted and step completed.");
        }
        else
        {
            Debug.Log("No fuse in inventory.");
        }
    }
}
