using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerminalInteractor : MonoBehaviour, IInteractable
{
    public GameMaster game;

    public void Interact(GameObject player)
    {
        game.CompleteObjective();
        Debug.Log("Terminal interacted and step completed.");

    }
}
