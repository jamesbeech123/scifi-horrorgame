using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    private Animator elevatorAnimator;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered the elevator, EXIT GAME");
            CloseElevator();
            
        }
    }

    void CloseElevator()
    {
        if (elevatorAnimator != null)
        {
            elevatorAnimator.SetTrigger("ElevatorClose");
        }
        else
        {
            Debug.LogWarning("Elevator Animator not assigned!");
        }
    }

    void Start()
    {
        elevatorAnimator = gameObject.GetComponent<Animator>();
    }
}
