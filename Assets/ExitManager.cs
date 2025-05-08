using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitManager : MonoBehaviour
{
    public GameObject elevatorDoor; 

    private Animator elevatorAnimator;

    void Start()
    {
        if (elevatorDoor != null)
        {
            elevatorAnimator = elevatorDoor.GetComponent<Animator>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            Debug.Log("Player entered the hallway, opening elevator...");
            OpenElevator();
        }
    }

    void OpenElevator()
    {
        if (elevatorAnimator != null)
        {
            elevatorAnimator.SetTrigger("ElevatorOpen"); 
        }
        else
        {
            Debug.LogWarning("Elevator Animator not assigned!");
        }
    }
}
