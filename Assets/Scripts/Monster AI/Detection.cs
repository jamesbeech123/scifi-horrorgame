using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Detection : MonoBehaviour
{   

    #region Variables

    [Header("Player Settings")]
    private GameObject playerObject;
    private Transform playerTransform;
    private FirstPersonAIO firstPersonAIO;
    private Vector3 playerPosition = Vector3.zero; // Position of the player updated by the player's threat level event handler
    private int playerThreatLevel = 0; // Threat level of the player (0-3)
    private float playerSprintSpeed; // Sprint speed of the player

    [Header("Player Detection Status")]
    private float playerDetection = 0f; // Detection percentage of the player (0-100)
    private float lastDetectionTime = 0f; // Time of the last player detection
    private float detectionDecreaseTimer = 0f; // Timer for decreasing player detection
    public float detectionTimeoutDuration = 3f; // Duration after which the player detection will time out
    public const float detectionDecreaseInterval = 1f; // Every X seconds, the player detection will decrease by detectionDrecreaseRate
    public float detectionDecreaseRate = 10f; // Rate at which the player detection decreases
    public int centralVisionDetectionRate = 5; // Detection rate within central vision
    public int peripheralVisionDetectionRate = 1; // Detection rate within peripheral vision
    private bool isWithinCentralVision = false; // Flag to check if the player is within central vision
    private bool isWithinPeripheralVision = false; // Flag to check if the player is within peripheral vision
    private bool isPlayerDetected = false; // Flag to check if the player is detected
    
    [Header("Monster Sensor Parameters")]
    [Range(0, 360)]
    public float visionRadius  = 100f; // Range of monster FoV
    public float visionAngle = 40f; // Angle of monster FoV (includes central and peripheral vision)
    public float centralVision = 20f; // Center of monster FoV, detection will be most accurate in this area
    public float certainDetectionRadius = 10f; // Range at which the monster is certain to detect the player, defines how close the player must be to be detected IMMEDIATELY
    public float hearingRadius = 25f; // Noise detection radius of the monster

    [Header("Wander Parameters")]
    private Coroutine wanderCoroutine;
    public float wanderRadius = 50f; // Radius within which the monster will wander around

    [Header("Hearing Parameters")]
    private Coroutine hearingCoroutine;

    [Header("Chase Parameters")]
    public int monsterAttackDamage = 30; // Damage dealt by the monster when attacking the player
    
    [Header("Investigation Parameters")]
    private Coroutine investigationCoroutine;
    private bool isInvestigating = false; // Flag to check if the monster is investigating
    private float searchRadius = 0f; // Radius within which the monster will search for the player
    private Vector3 lastSeenPosition = Vector3.zero; // Last seen position of the player
    
    [Header("Monster Components")]
    private NavMeshAgent navMeshAgent;

    [Header("Monster UI Components")]
    private TMP_Text detectionPercentageText;


    #endregion

    #region Default Functions

    private void Start()
    {
        detectionPercentageText = GameObject.FindGameObjectsWithTag("PlayerDetectionPercentageTextUI")[0].GetComponent<TMP_Text>(); // Player detection percentage text UI
        navMeshAgent = GetComponent<NavMeshAgent>(); // NavMeshAgent component of the monster

        // Player related variables
        playerObject = GameObject.FindGameObjectWithTag("Player"); // Player object
        playerTransform = playerObject.transform; // Player transform
        firstPersonAIO = playerObject.GetComponent<FirstPersonAIO>(); // Subscribe to player threat level event handler
        firstPersonAIO.ThreatLevelChanged += OnThreatLevelChanged;
        playerSprintSpeed = firstPersonAIO.sprintSpeed;

        StartCoroutine(DetectionHandler()); // Called every 0.5 seconds to handle player detection
    }


    private void FixedUpdate()
    {
        MonsterVisionSensor(); // Constantly scan for the player

        // If the player is detected, stop wandering and chase the player
        if (playerDetection == 0 && wanderCoroutine == null)
        {
            wanderCoroutine = StartCoroutine(MonsterWander());
        }
        // If the player is not detected, stop chasing the player and wander around
        else if (playerDetection > 0 && wanderCoroutine != null)
        {
            StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
        }

        bool wasPlayerDetected = isPlayerDetected;
        isPlayerDetected = playerDetection >= 100;

        // If the player was detected but now is not, start investigating
        if (wasPlayerDetected && !isPlayerDetected && !isInvestigating)
        {
            if (investigationCoroutine != null)
            {
                StopCoroutine(investigationCoroutine);
            }
            investigationCoroutine = StartCoroutine(MonsterInvestigate());
        }
    }

    #endregion

    #region Monster Detection Functions

    /// <summary>
    /// Scans for the player within the monster's field of view.
    /// If the player is detected, the monster will chase the player.
    /// If the player is not detected, the monster will wander around.
    /// </summary>
    private void MonsterVisionSensor()
    {
        Vector3 direction;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
    
        // Check if the player is within the certain detection radius
        if (distanceToPlayer <= certainDetectionRadius)
        {
            // Check if the player is within the vision angle
            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);
    
            if (angleToPlayer <= visionAngle / 2)
            {
                // Player is within vision angle and certain detection radius
                playerDetection = 100;
                MonsterChasePlayer(playerTransform.position);
                return;
            }
            else if (playerThreatLevel == 2)
            {
                // Player is within certain detection radius but outside vision angle, and is running
                playerDetection = 100;
                MonsterChasePlayer(playerTransform.position);
                return;
            }
        }
    
        // Loop through angles within the vision angle range
        for (float angle = -visionAngle / 2; angle < visionAngle / 2; angle += 1f)
        {
            // Calculate the direction based on the current angle
            direction = Quaternion.Euler(0, angle, 0) * transform.forward;
    
            // Perform a raycast in the calculated direction
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, visionRadius))
            {
                // Check if the raycast hit the player
                if (hit.collider.CompareTag("Player"))
                {
                    // Increase player detection level when within central vision
                    if (Mathf.Abs(angle) <= centralVision / 2)
                    {
                        isWithinCentralVision = true;
                        // Draw a green debug ray if the player is detected within central vision
                        Debug.DrawRay(transform.position, direction * visionRadius, Color.green);
                    }
                    // Increase player detection level when within peripheral vision
                    else
                    {
                        isWithinPeripheralVision = true;
                        // Draw a pink debug ray if the player is detected within peripheral vision
                        Debug.DrawRay(transform.position, direction * visionRadius, Color.magenta);
                    }
    
                    // If player detection level reaches 100, chase the player
                    if (playerDetection >= 100)
                    {
                        MonsterChasePlayer(playerTransform.position);
                    }
                }
                else
                {
                    // Draw a red debug ray if the raycast hit something other than the player within central vision
                    if (Mathf.Abs(angle) <= centralVision / 2)
                    {
                        Debug.DrawRay(transform.position, direction * visionRadius, Color.red);
                    }
                    // Draw an orange debug ray if the raycast hit something other than the player within peripheral vision
                    else
                    {
                        Debug.DrawRay(transform.position, direction * visionRadius, Color.yellow);
                    }
                }
            }
            else
            {
                // Draw a red debug ray if the raycast did not hit anything within central vision
                if (Mathf.Abs(angle) <= centralVision / 2)
                {
                    Debug.DrawRay(transform.position, direction * visionRadius, Color.red);
                }
                // Draw an orange debug ray if the raycast did not hit anything within peripheral vision
                else
                {
                    Debug.DrawRay(transform.position, direction * visionRadius, Color.yellow);
                }
            }
        }
    }


    /// <summary>
    /// If the player makes noise and it originates from within the hearing radius, the monster will set the destination of the NavMeshAgent to playerPosition.
    /// </summary>
    /// <param name="playerPosition">Position of the player</param>
    private IEnumerator MonsterHearingSensor()
    {
        while (playerPosition != Vector3.zero)
        {
            // Check if the player is within the hearing radius
            if (Vector3.Distance(transform.position, playerPosition) <= hearingRadius)
            {
                navMeshAgent.SetDestination(playerPosition);
                Debug.Log("Monster heard the player!");
    
                while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
                {
                    yield return null;
                }
    
                yield return new WaitForSeconds(2f); // We must initiate looking around or similar behavior like after chase here. This is a placeholder.
            }
    
            yield return new WaitForSeconds(0.5f);
        }
    }


    /// <summary>
    /// Every 0.5 seconds, the player detection level will be updated based on the detection rate.
    /// Multiple detection sensors can call this function with different detection rates.
    /// When the time comes, all detection rates will be summed up and the player detection level will be updated.
    /// The player detection level will decrease by a preset amount every second if the player is not detected for some time.
    /// If the player detection level reaches 100, the monster will chase the player.
    /// </summary>
        private IEnumerator DetectionHandler()
    {
        while (true)
        {
            bool playerDetected = isWithinCentralVision || isWithinPeripheralVision;
    
            if (playerDetection >= 0 && playerDetection < 100)
            {
                if (isWithinCentralVision)
                {
                    playerDetection += centralVisionDetectionRate;
                    lastDetectionTime = Time.time; // Update last detection time
                }
                else if (isWithinPeripheralVision)
                {
                    playerDetection += peripheralVisionDetectionRate;
                    lastDetectionTime = Time.time; // Update last detection time
                }
            }
    
            // Decrease player detection after X seconds of no detection
            if (!playerDetected && Time.time - lastDetectionTime >= detectionTimeoutDuration)
            {
                detectionDecreaseTimer += 0.5f; // Increment timer by 0.5 seconds
                if (detectionDecreaseTimer >= detectionDecreaseInterval)
                {
                    playerDetection -= detectionDecreaseRate; // Decrease detection by detectionDecreaseRate every second
                    if (playerDetection < 0)
                    {
                        playerDetection = 0;
                    }
                    detectionDecreaseTimer = 0f;
                }
            }
            else
            {
                detectionDecreaseTimer = 0f; // Reset the timer if detection occurs
            }
    
            // If player detection level reaches 100, chase the player
            if (playerDetection >= 100)
            {
                MonsterChasePlayer(playerTransform.position);
            }
    
            detectionPercentageText.text = $"Player Detection: {playerDetection}%"; // Update the player detection percentage text UI
    
            isWithinCentralVision = false; // Reset central vision flag
            isWithinPeripheralVision = false; // Reset peripheral vision flag
            yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds
        }
    }

    #endregion

    #region Monster State Functions

    #region Wander
    /// <summary>
    /// Coroutine for the monster to wander around
    /// The monster will choose a random destination on the navmesh and move towards it
    /// The monster will wait for a short duration before choosing a new destination
    /// The coroutine will loop indefinitely and only stop when the player is detected
    /// </summary>
    /// <returns></returns>
    private IEnumerator MonsterWander()
    {
        while (true)
        {
            NavMeshHit navHit;
    
            do
            {
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius; // Random direction within the wander radius
                randomDirection += transform.position; // Add the random direction to the monster's position
    
                NavMesh.SamplePosition(randomDirection, out navHit, wanderRadius, NavMesh.AllAreas); // Sample a position on the navmesh
            }
            while (!navHit.hit);
    
            navMeshAgent.SetDestination(navHit.position); // Set the destination of the NavMeshAgent to the sampled position
    
            // Wait until the monster reaches the destination or a new destination is needed
            while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
            {
                yield return null;
            }
    
            // Wait for a short duration before choosing a new destination
            yield return new WaitForSeconds(2f);
        }
    }
    #endregion

    #region Investigate
    // TODO: Implement investigation
    private IEnumerator MonsterInvestigate()
    {
        
        isInvestigating = true;
        float investigationTimer = 0f;
        lastSeenPosition = playerPosition; 
        bool reachedLastSeenPosition = false;

        // Start the investigation timer
        while (!reachedLastSeenPosition)
        {
            investigationTimer += Time.deltaTime;

            // Check if the monster has reached the last seen position or within a 0.5f radius of it
            if (Vector3.Distance(transform.position, lastSeenPosition) <= 0.5f)
            {
                reachedLastSeenPosition = true;
            }
            Debug.Log("Reached last seen position: " + reachedLastSeenPosition);

            yield return null;
        }
        Debug.Log("Investigating...");
        // Calculate the search radius based on the player's sprint speed and the investigation timer
        searchRadius = firstPersonAIO.sprintSpeed * investigationTimer;
        Debug.Log($"Search radius: {searchRadius}");

        // Generate 1 or 2 tokens randomly
        int tokens = UnityEngine.Random.Range(1, 3);

        // Look for interactables within the search radius
        Collider[] interactables = Physics.OverlapSphere(lastSeenPosition, searchRadius, LayerMask.GetMask("Interactable"));

        while (tokens > 0 && interactables.Length > 0)
        {
            // Interact with a random interactable
            int randomIndex = UnityEngine.Random.Range(0, interactables.Length);
            Collider interactable = interactables[randomIndex];

            // Perform interaction (e.g., check hiding spots)
            InteractWith(interactable);

            // Remove the interacted object from the list
            List<Collider> interactableList = new List<Collider>(interactables);
            interactableList.RemoveAt(randomIndex);
            interactables = interactableList.ToArray();

            tokens--;
            yield return new WaitForSeconds(1f); // Wait for a short duration before the next interaction
        }

        // If tokens are depleted and the player was not found, go back to wander stage or yield break
        if (tokens == 0)
        {
            Debug.Log("Tokens depleted, going back to wander stage.");
            StartCoroutine(MonsterWander());
        }
        else
        {
            yield break;
        }

        isInvestigating = false;
    }
    #endregion

    #region Chase
    /// <summary>
    /// Chases the player by setting the destination of the NavMeshAgent to the player's position
    /// </summary>
    private void MonsterChasePlayer(Vector3 playerPosition)
    {
        if (playerTransform != null)
        {
            navMeshAgent.SetDestination(playerTransform.position);
        }

        if  (Vector3.Distance(transform.position, playerTransform.position) <= 2f)
        {
            //TODO: Implement attack
            //Attack(); 
        }
    }

    //TODO: Implement stalking 
    //TODO: Implement after chase

    #endregion

    #endregion
    
    #region Event Handlers

    /// <summary>
    /// Event handler for when the player's threat level changes
    /// </summary>
    /// <param name="sender">Player Object</param>
    /// <param name="e">Player Position</param>
    private void OnThreatLevelChanged(object sender, FirstPersonAIO.ThreatLevelEventArgs e)
    {
        playerPosition = e.PlayerPosition;
        playerThreatLevel = e.ThreatLevel;
        
        Debug.Log($"Received player position: {playerPosition}");

        if (hearingCoroutine != null)
        {
            StopCoroutine(hearingCoroutine);
        }
        hearingCoroutine = StartCoroutine(MonsterHearingSensor());
    }

    #endregion

    #region Debug Draw Functions
    
    private void OnDrawGizmos()
    {
        // Draw the hearing radius as a blue circle
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        // Draw the vision radius as a white circle
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // Draw the vision angle as a yellow cone
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward * visionRadius;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, visionAngle, visionRadius);
        #endif

        // Draw the central vision angle as a red cone
        float centralVisionAngle = centralVision; // Assuming centralVision is defined elsewhere
        Gizmos.color = Color.red;
        Vector3 centralLeftBoundary = Quaternion.Euler(0, -centralVisionAngle / 2, 0) * forward;
        Vector3 centralRightBoundary = Quaternion.Euler(0, centralVisionAngle / 2, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + centralLeftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + centralRightBoundary);
        #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, centralLeftBoundary, centralVisionAngle, visionRadius);
        #endif

        // Draw the certain detection radius as a green circle
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, certainDetectionRadius);

        // Draw the search radius gizmo if investigating
        if (isInvestigating)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastSeenPosition, searchRadius);
        }
    }
    #endregion

    //TODO: PLACEHOLDER
    private void InteractWith(Collider interactable)
    {
        // Implement the interaction logic here
        Debug.Log($"Interacting with {interactable.name}");
    }
}