using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Investigate : State
{
    private Coroutine investigateCoroutine;
    private Coroutine goToLocationCoroutineMain;
    private Coroutine goToLocationCoroutine;
    private Coroutine investigationTimerCoroutine;

    private float searchRadius;             // Radius within which the monster will search for the player
    private float MAX_INVESTIGATION_TIMER;  // Time to investigate the search radius
    private int MAX_SEARCH_TOKENS;          // Number of times the monster will search for the player
    private float navigationPadding = 2f;   // Padding for navigation

    private bool tokensDepleted = false;
    private bool timerExpired = false;
    private bool noHidingSpotsLeft = false;
    private bool noTokensOrSpotsLeftOrTimerIsOut => tokensDepleted || timerExpired || noHidingSpotsLeft;

    public Investigate(MonsterAI monsterAI, UnityEngine.AI.NavMeshAgent agent, GameObject player,
    float searchRadius, float maxInvestigationTimer, int maxSearchTokens) : base(monsterAI, agent, player) 
    {
        this.searchRadius = searchRadius;
        this.MAX_INVESTIGATION_TIMER = maxInvestigationTimer;
        this.MAX_SEARCH_TOKENS = maxSearchTokens;
    }

    public override void EnterState()
    {
        Debug.Log("INVESTIGATING");
        investigateCoroutine = monsterAI.StartCoroutine(InvestigateCoroutine());
    }

    public override void UpdateState()
    {
        if (noTokensOrSpotsLeftOrTimerIsOut)
        {
            monsterAI.ProvideFeedback("Not Found");
            monsterAI.SetPlayerDetectionPercentage(0);
            monsterAI.ChangeState(new Wander(monsterAI, navMeshAgent, player, monsterAI.GetWanderRadius(), monsterAI.RequestWanderParameter()));
        }
        else if ((monsterAI.wasPlayerDetectedThisFrame || monsterAI.monsterHeardPlayer) && !monsterAI.isPlayerDetected) {
            monsterAI.ProvideFeedback("Player Noise");
            monsterAI.ChangeState(new Investigate(monsterAI, navMeshAgent, player,
            monsterAI.GetSearchRadius(), monsterAI.GetMaxInvestigationTimer(), monsterAI.GetMaxSearchTokens()));
        }
        else if (((monsterAI.wasPlayerDetectedThisFrame || monsterAI.monsterHeardPlayer) && monsterAI.isPlayerDetected) || monsterAI.isPlayerFoundHiding)
        {
            monsterAI.ProvideFeedback("Chase");
            monsterAI.ChangeState(new Chase(monsterAI, navMeshAgent, player));
        }
    }

    public override void ExitState()
    {
        if (goToLocationCoroutineMain != null) monsterAI.StopCoroutine(goToLocationCoroutineMain); goToLocationCoroutineMain = null;
        if (goToLocationCoroutine != null) monsterAI.StopCoroutine(goToLocationCoroutine); goToLocationCoroutine = null;

        if (investigationTimerCoroutine != null) monsterAI.StopCoroutine(investigationTimerCoroutine); investigationTimerCoroutine = null;

        if (investigateCoroutine != null) monsterAI.StopCoroutine(investigateCoroutine); investigateCoroutine = null;
    }

    /// <summary>
    /// Coroutine to investigate the player's last seen position
    /// </summary>
    private IEnumerator InvestigateCoroutine()
    {
        float tokensLeft = MAX_SEARCH_TOKENS;
        
        // Go to player's last seen position
        Debug.Log("Going to player's last seen position");
        yield return GoToLocation(monsterAI.playerLastSeenPosition);

        // Get the hiding spots within search radius
        List<GameObject> hidingSpots = GetHidingSpots();
        Debug.Log("Hiding spots found: " + hidingSpots.Count);

        if (hidingSpots.Count <= 0)
        {
            Debug.Log("No hiding spots found/left.");
            noHidingSpotsLeft = true;
            yield break;
        }
        
        // Start timer to investigate the search radius
        StartInvestigationTimer(MAX_INVESTIGATION_TIMER);

        while (!timerExpired && tokensLeft > 0 && hidingSpots.Count > 0)
        {
            // Request hiding spot from the RL model
            GameObject hidingSpotToCheck = monsterAI.RequestInvestigateParameter(hidingSpots);
            
            if (hidingSpotToCheck == null)
            {
                Debug.LogWarning("No hiding spot to check returned from RL model.");
                break;
            }
            
            // Remove the chosen spot from the list so it's not checked again
            hidingSpots.Remove(hidingSpotToCheck);
            
            // Go to hiding spot
            goToLocationCoroutineMain = monsterAI.StartCoroutine(GoToLocation(hidingSpotToCheck.transform.position));
            yield return goToLocationCoroutineMain;

            // Interact with hiding spot and deplete token
            monsterAI.SetIsPlayerFoundHiding(CheckHidingSpot(hidingSpotToCheck));

            tokensLeft--;
            Debug.Log($"Tokens remaining: {tokensLeft}, Hiding spots left: {hidingSpots.Count}");
            
            yield return new WaitForSeconds(0.5f);
        }

        noHidingSpotsLeft = hidingSpots.Count <= 0;
        tokensDepleted = (tokensLeft <= 0);
        yield return new WaitForSeconds(0.5f);
    }

    private bool CheckHidingSpot(GameObject hidingSpot)
    {
        return hidingSpot.GetComponent<HidingSpot>().MonsterInteract(player);
    }

    /// <summary>
    /// Get all hiding spots within the search radius
    /// </summary>s
    /// <returns>List of hiding spots</returns>
    private List<GameObject> GetHidingSpots()
    {
        Collider[] hitColliders = Physics.OverlapSphere(monsterAI.transform.position, searchRadius);
        List<GameObject> hidingSpots = new List<GameObject>();
        
        foreach (var hitCollider in hitColliders)
        {
            // Check if object is a hiding spot
            if (hitCollider.CompareTag("HidingSpot"))
            {
                hidingSpots.Add(hitCollider.gameObject);
            }
        }
        
        // Sort by distance to last seen position
        hidingSpots.Sort((a, b) => 
        Vector3.Distance(a.transform.position, monsterAI.playerLastSeenPosition)
        .CompareTo(
            Vector3.Distance(b.transform.position, monsterAI.playerLastSeenPosition)
        ));

        return hidingSpots;
    }

    /* -------------------------------------------------------------------------- */
    /*                          START INVESTIGATION TIMER                         */
    /* -------------------------------------------------------------------------- */

    private void StartInvestigationTimer(float duration)
    {
        if (investigationTimerCoroutine != null)
        {
            monsterAI.StopCoroutine(investigationTimerCoroutine);
            investigationTimerCoroutine = null;
        }
        investigationTimerCoroutine = monsterAI.StartCoroutine(InvestigationTimer(duration));
    }

    private IEnumerator InvestigationTimer(float duration)
    {
        timerExpired = false;
        float timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        timerExpired = true;
        Debug.Log("Investigation timer expired.");
    }

    /* -------------------------------------------------------------------------- */
    /*                               GO TO LOCATION                               */
    /* -------------------------------------------------------------------------- */

    private IEnumerator GoToLocation(Vector3 location)
    {
        if (goToLocationCoroutine != null) monsterAI.StopCoroutine(goToLocationCoroutine); goToLocationCoroutine = null;

        goToLocationCoroutine = monsterAI.StartCoroutine(GoToLocationCoroutine(location));
        yield return goToLocationCoroutine;
    }

    private IEnumerator GoToLocationCoroutine(Vector3 location)
    {
        // Check if there is a hiding spot within 0.1f radius
        Collider[] hitColliders = Physics.OverlapSphere(location, 0.1f);
        GameObject hidingSpot = null;
    
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("HidingSpot"))
            {
                hidingSpot = hitCollider.gameObject;
                break;
            }
        }
    
        if (hidingSpot != null)
        {
            // Go to the front side of the hiding spot
            Vector3 directionToHidingSpot = (hidingSpot.transform.position - monsterAI.transform.position).normalized;
            Vector3 frontSidePosition = hidingSpot.transform.position - directionToHidingSpot * navigationPadding;
    
            navMeshAgent.SetDestination(frontSidePosition);
            while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
            {
                yield return null;
            }
    
            // Wait for 0.3 seconds
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            // Go to the location directly
            navMeshAgent.SetDestination(location);
            while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
            {
                yield return null;
            }
        }
    }
}