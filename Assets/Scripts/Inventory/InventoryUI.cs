using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.XR.CoreUtils;

public class InventoryUI : MonoBehaviour
{

    InventorySystem inventory;
    InventorySlot[] slots;
    GameObject player;

    private bool UILOADED;

    CanvasGroup uiCanvasGroup;

    private void Start()
    {
        UILOADED = false;

        uiCanvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (uiCanvasGroup == null)
        {
            uiCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        StartCoroutine(WaitForPlayer());
    }

    IEnumerator WaitForPlayer()
    {
        // Wait until player is found
        while (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            yield return null;  // Wait for next frame
        }

        // Once player is found, initialize UI
        LoadUI();
    }

    void LoadUI()
    {
        UILOADED=true;
        // Get the InventorySystem from the player
        inventory = GameObject.FindGameObjectWithTag("InventorySystem").GetComponent<InventorySystem>();

        if (inventory == null)
        {
            Debug.LogError("InventorySystem component not found! Make sure InventorySystem is attached.");
            return;
        }

        // Initialize inventory UI slots
        slots = gameObject.GetComponentsInChildren<InventorySlot>();

        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("No InventorySlot components found! Make sure InventorySlot components exist in the children of this GameObject.");
            return;
        }

        // Make sure the InventorySystem instance exists
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("InventorySystem.Instance is null. Ensure InventorySystem exists in the scene.");
            return;
        }

        // Subscribe to inventory update callback
        inventory.onItemChangedCallback += UpdateUI;

        Debug.Log($"Found {slots.Length} inventory slots.");
    }

    void Update()
    {
        if (UILOADED && Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("I pressed!");
            ToggleVisibility();
        }
    }

    void ToggleVisibility()
    {
        // Toggle alpha: alpha 0 = invisible, 1 = visible.
        if (uiCanvasGroup.alpha == 1)
        {
            uiCanvasGroup.alpha = 0;
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            uiCanvasGroup.alpha = 1;
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.inventory.Count)
            {
                Debug.Log("ADDING TO INVENTORY: " + inventory.inventory[i].itemName);
                slots[i].AddItem(inventory.inventory[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }
}
