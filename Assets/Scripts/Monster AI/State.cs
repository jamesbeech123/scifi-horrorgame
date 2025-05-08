using UnityEngine;
using UnityEngine.AI;

public abstract class State : MonoBehaviour
{
    protected MonsterAI monsterAI; // This is the state machine
    protected GameObject player;
    protected NavMeshAgent navMeshAgent;

    public State(MonsterAI monsterAI, NavMeshAgent agent, GameObject player)
    {
        this.monsterAI = monsterAI;
        this.navMeshAgent = agent;
        this.player = player;
    }

    // Method to enter the state
    public abstract void EnterState();

    // Method to update the state
    public abstract void UpdateState();

    // Method to exit the state
    public abstract void ExitState();
}