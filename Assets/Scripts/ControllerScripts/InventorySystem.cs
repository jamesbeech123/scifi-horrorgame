using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;
    public static InventoryUI inventoryUI;

    public List<ItemCollectible> inventory = new List<ItemCollectible>();
    internal Action onItemChangedCallback;

    private void Awake()
    {
        inventoryUI = GameObject.FindGameObjectWithTag("InventoryUI").GetComponent<InventoryUI>();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemCollectible item)
    {

        inventory.Add(item);
        inventoryUI.UpdateUI();
        Debug.Log("Added item: " + item.itemName);
    }

    public void RemoveItem(ItemCollectible item)
    {
        inventory.Remove(item);
        inventoryUI.UpdateUI();
        Debug.Log("Removed item: " + item.itemName);
    }

    public bool HasItem(String itemName)
    {
        foreach (ItemCollectible item in inventory)
        {
            if (item.itemName == itemName)  // Check if the itemName matches
            {
                return true;
            }
        }
        return false;  // Return false if no match found
    }

    public void DisplayInventory()
    {
        foreach (ItemCollectible item in inventory)
        {
            Debug.Log("Item in inventory: " + item.itemName);
        }
    }
}
