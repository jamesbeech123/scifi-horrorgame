using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Chase : State
{
    private Coroutine chaseCoroutine;
    private float timeSinceLastSpotted = 0;
    private MonsterAnimator animator;

    public Chase(MonsterAI monsterAI, NavMeshAgent agent, GameObject player) : base(monsterAI, agent, player) { }

    public override void EnterState()
    {
        Debug.Log("CHASING");
        chaseCoroutine = monsterAI.StartCoroutine(ChaseCoroutine());
    }

    public override void UpdateState()
    {
        timeSinceLastSpotted += Time.deltaTime;
        if ((monsterAI.wasPlayerDetectedThisFrame || monsterAI.monsterHeardPlayer) && monsterAI.isPlayerDetected)
        {
            timeSinceLastSpotted = 0;
        }

        else if ((!monsterAI.wasPlayerDetectedThisFrame || !monsterAI.monsterHeardPlayer) && monsterAI.isPlayerDetected && timeSinceLastSpotted > 0.2f)
        {
            Debug.Log("Lost player, investigating.");
            monsterAI.ProvideFeedback("Player Escaped");
            monsterAI.ChangeState(new Investigate(monsterAI, navMeshAgent, player,
            monsterAI.GetSearchRadius(), monsterAI.GetMaxInvestigationTimer(), monsterAI.GetMaxSearchTokens()));
        }
    }

    public override void ExitState()
    {
        if (chaseCoroutine != null)
        {
            monsterAI.StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
    }

    private IEnumerator ChaseCoroutine()
    {
        while (monsterAI.isPlayerDetected)
        {
            // Implementation for updating the chase state
            while (Vector3.Distance(monsterAI.transform.position, monsterAI.playerLastSeenPosition) > 2f)
            {
                navMeshAgent.SetDestination(monsterAI.playerLastSeenPosition);
                yield return null;
            }

            if (Vector3.Distance(monsterAI.transform.position, player.transform.position) <= 2f)
            {
                Debug.Log("Player caught!");
                AttackPlayer();
            }
            yield return null;
        }
    }

    private void AttackPlayer()
    {
        Debug.Log("Attacking player.");
        monsterAI.ProvideFeedback("Player Hit");
        //Attack player
        //monsterAI.firstPersonAIO.ChangeHealthState(monsterAI.attackDamage);

        // Slow down the monster by 30% for 3 seconds
        monsterAI.SlowDownMonster(0.1f, 3f);

        // Speed up the player by 25% for 3 seconds
        //monsterAI.firstPersonAIO.SpeedUpPlayer(0.5f, 3f);

        // When attacked the player successfully, play scream animation and slow down the monster by 30% for 3 seconds.
        // FIXME: JAAAAAAAAAMEEES DO IT PLS IDK ANYTHING ABOUT ANIMATIONS
        animator.Scream();
    }
}