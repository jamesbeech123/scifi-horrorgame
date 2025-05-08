using UnityEngine;

public class KeycardReaderInteractor : MonoBehaviour, IInteractable
{
    public GameObject reader;  
    public GameMaster game;
    public ItemCollectible keycard;
    private Material ExitLightMaterialOn;

    public void Interact(GameObject player)
    {
        // Check if the player has a keycard
        if (InventorySystem.Instance.HasItem("keycard"))
        {
            // Update the model
            Renderer renderer = reader.GetComponent<MeshRenderer>();
            Material[] materials = renderer.materials;
            materials[2] = ExitLightMaterialOn;
            renderer.materials = materials;
            InventorySystem.Instance.RemoveItem(keycard);
            

            // Trigger the checklist to open the door
            game.CompleteObjective();
            Debug.Log("Keycard swiped and step completed.");
        }
        else
        {
            Debug.Log("No keycard in inventory.");
        }
    }
}
