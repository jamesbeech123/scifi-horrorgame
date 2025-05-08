using System;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Sprite icon;
    private Image inventoryIcon;
    ItemCollectible item;

    private void Start()
    {
        inventoryIcon = transform.Find("InventoryIcon").GetComponent<Image>();

    }


    public void AddItem(ItemCollectible newItem)
    {
        item = newItem;
        icon = item.itemIcon;
        inventoryIcon.sprite = icon;
        inventoryIcon.enabled = true;
    }

    public void ClearSlot()
    {
        item = null;
        icon = null;
    }

    public void OnRemoveButton()
    {
        //Inventory.instance.RemoveItem(item);
    }

    public void UseItem()
    {
        if (item != null)
        {
            item.Use();
        }
    }

   
}