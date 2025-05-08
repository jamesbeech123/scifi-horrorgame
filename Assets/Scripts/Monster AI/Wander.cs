using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Wander : State
{
    [Header("Wander Parameters")]
    private float wanderRadius;
    private Coroutine wanderCoroutine;
    private Vector3 destinationPosition;
    private bool destinationReached = false;

    // Constructor
    public Wander(MonsterAI monsterAI, NavMeshAgent agent, GameObject player, float wanderRadius, Vector3 destinationPosition) : base(monsterAI, agent, player)
    {
        this.wanderRadius = wanderRadius;
        this.destinationPosition = destinationPosition;
    }

    public override void EnterState()
    {
        Debug.Log("WANDERING");
        wanderCoroutine = monsterAI.StartCoroutine(WanderCoroutine());
    }

    public override void UpdateState()
    {
        if (destinationReached)
        {
            monsterAI.ProvideFeedback("No Transition");
            monsterAI.ChangeState(new Wander(monsterAI, navMeshAgent, player, wanderRadius, monsterAI.RequestWanderParameter()));
        }
        else if (monsterAI.isPlayerDetected)
        {
            monsterAI.ProvideFeedback("Chase");
            monsterAI.ChangeState(new Chase(monsterAI, navMeshAgent, player));
        }
        else if ((monsterAI.wasPlayerDetectedThisFrame || monsterAI.monsterHeardPlayer) && !monsterAI.isPlayerDetected)
        {
            monsterAI.ProvideFeedback("Investigate");
            monsterAI.ChangeState(new Investigate(monsterAI, navMeshAgent, player,
            monsterAI.GetSearchRadius(), monsterAI.GetMaxInvestigationTimer(), monsterAI.GetMaxSearchTokens()));
        }
    }

    public override void ExitState()
    {
        if (wanderCoroutine != null)
        {
            monsterAI.StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
        }
    }

    // Go to destination, pause momentarily when destination reached
    private IEnumerator WanderCoroutine()
    {
        while (true)
        {
            NavMeshHit navHit;

            do
            {
                NavMesh.SamplePosition(destinationPosition, out navHit, wanderRadius, NavMesh.AllAreas);
            }
            while (!navHit.hit);

            navMeshAgent.SetDestination(navHit.position);

            while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
            {
                yield return null;
            }

            destinationReached = true;

            yield break;
        }
    }
}