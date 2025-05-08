using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    // Instead of a bool flag and a stored position, store a reference to the player that is hiding.
    private GameObject currentHider;
    private Vector3 hiderOriginalPosition;

    // Called when the player interacts with this hiding spot.
    // If unoccupied, hide the player. If occupied and the same player interacts, unhide them.
    public void Interact(GameObject player)
    {
        if (currentHider == null)
        {
            HidePlayer(player);
        }
        else if (currentHider == player)
        {
            UnhidePlayer(player);
        }
        else
        {
            Debug.Log("This spot is already occupied by another player.");
        }
    }

    public void HidePlayer(GameObject player)
    {
        if (currentHider == null)
        {
            currentHider = player;
            // Optionally have a component on the player to toggle a hiding state.
            // For now, we change its layer and store its original position.
            player.layer = LayerMask.NameToLayer("Ignore Raycast");
            hiderOriginalPosition = player.transform.position;
            player.transform.position = transform.position;
            Debug.Log("Player is now hiding.");
        }
        else
        {
            Debug.Log("This hiding spot is already occupied.");
        }
    }

    public void UnhidePlayer(GameObject player)
    {
        if (currentHider == player)
        {
            // Reset the player's layer and position.
            player.layer = LayerMask.NameToLayer("Default");
            player.transform.position = hiderOriginalPosition;
            Debug.Log("Player is no longer hiding.");
            currentHider = null;
        }
        else
        {
            Debug.Log("Player was not hiding here.");
        }
    }

    public bool MonsterInteract(GameObject player)
    {
        Debug.Log("Monster is interacting with hiding spot");
        if (currentHider != null)
        {
            Debug.Log("Player was hiding, now uncovered.");
            UnhidePlayer(currentHider);
            return true;
        }
        else
        {
            Debug.Log("No player is hiding here.");
            return false;
        }
    }
}
