using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_Controller : MonoBehaviour
{
    public AIState currentState;

    // Start is called before the first frame update
    void Start()
    {

        currentState = AIState.Wander;
    }

    // Update is called once per frame
    void Update()
    {
        //Handles the current AI behavior based on what the current state is
        switch (currentState)
        {
            case AIState.Wander:
                HandleWanderState();
                break;
            case AIState.Investigate:
                HandleInvestigateState();
                break;
            case AIState.Chase:
                HandleChaseState();
                break;
            case AIState.Stalk:
                HandleStalkState();
                break;
            case AIState.AfterChase:
                HandleAfterChaseState();
                break;
        }
    }

    //Changes the state to make the AI Wander
    void HandleWanderState()
    {
        // Debug.Log("AI is wandering...");
    }

    //Changes the state to make the AI Investigate
    void HandleInvestigateState()
    {
        Debug.Log("AI is investigate...");
    }

    //Changes the state to make the AI Chase
    void HandleChaseState()
    {
        Debug.Log("AI is chasing...");
    }

    //Changes the state to make the AI Stalk
    void HandleStalkState()
    {
        Debug.Log("AI is stalk...");
    }

    //Changes the state to make the AI Post Chase
    void HandleAfterChaseState()
    {
        Debug.Log("AI is afterChase...");
    }

    //Changes the state to the passed newState
    void ChangeState(AIState newState)
    {
        currentState = newState;
        Debug.Log("AI State Switched to: "+currentState);
    }
}
