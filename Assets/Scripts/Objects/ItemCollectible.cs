using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCollectible : MonoBehaviour, IInteractable
{

    public string itemName;
    public int itemQuantity = 1;
    public Sprite itemIcon;

    public ItemCollectible()
    {
        itemName = "None";
        itemIcon = null;
    }

    public void Interact(GameObject player)
    {
        //Debug.Log("Interacting with item");
        CollectItem();
    }

    internal void Use()
    {
        throw new NotImplementedException();
    }

    private void CollectItem()
    {
        Debug.Log($"Collected {itemName} x{itemQuantity}");

        InventorySystem.Instance.AddItem(this);
        Debug.Log(InventorySystem.Instance.HasItem(itemName));

        gameObject.SetActive(false);
    }
}
