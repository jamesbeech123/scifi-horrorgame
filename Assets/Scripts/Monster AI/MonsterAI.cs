using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    [Header("Player Related")]
    private GameObject player;
    public FirstPersonAIO firstPersonAIO {get; private set;}    // FirstPersonAIO.cs.
    private int playerThreatLevel = 0;                          // Threat level of the player (0-3).
    private int gameProgress = 0;                               

    [Header("Detection Parameters")]

    // Detection timers
    private float timePastSinceLastDetectionTime = 0f;                          // How long ago the player was detected?
    private float playerDetectionPercentage = 0f;                               // Detection percentage of the player (0-100).
    private float detectionDecreaseTimer = 0f;                                  // Detection percentage will decrease when this timer reaches detectionDecreaseInterval.
    public const float DETECTION_DECREASE_FREQUENCY = 1f;                       // Every time detectionDecreaseTimer reaches this value, the detection percentage will decrease.
    public const float TIME_REQUIRED_TO_START_DECREASING_DETECTION = 3f;        // When this many seconds pass without detection, the detection percentage will start to decrease.

    public Vector3 playerLastSeenPosition {get; private set; } = Vector3.zero;  // Last seen position of the player.

    // Detection rates
    [Header("Detection Rates")]
    public int centralVisionDetectionRate = 28;         // Detection rate within central vision
    public int peripheralVisionDetectionRate = 12;      // Detection rate within peripheral vision
    public float detectionDecreaseRate = 10f;           // Rate at which the player detection decreases

    // Detection flags
    private bool isWithinCentralVision = false;                     // Flag to check if the player is within central vision
    private bool isWithinPeripheralVision = false;                  // Flag to check if the player is within peripheral vision
    private bool wasWithinCentralVision = false;                    // Used in DetectionHandler() to overcome the limitations of the coroutine, local copy the value of isWithinCentralVision
    private bool wasWithinPeripheralVision = false;                 // Used in DetectionHandler() to overcome the limitations of the coroutine, local copy the value of isWithinPeripheralVision
    public bool isPlayerDetected {get; private set; } = false;      // Flag to check if the detection percentage is 100
    public bool wasPlayerDetectedThisFrame {get; private set; }     // Was the player detected this frame?
    public bool monsterHeardPlayer {get; private set;} = false;     // Did the monster hear the player?
    public bool isPlayerFoundHiding {get; private set; } = false;   // Is the player found hiding?

    [Header("Sensor Parameters")]
    [Range(0, 360)]
    public float visionRadius  = 10f;                   // Range of monster FoV
    [Range(0, 180)]
    public float visionAngle = 40f;                     // Angle of monster FoV (includes central and peripheral vision)
    [Range(0, 180)]
    public float centralVision = 20f;                   // Center of monster FoV, detection will be most accurate in this area
    [Range(0, 90)]
    public float certainDetectionRadius = 10f;          // Defines how close the player must be to be detected instantly if within vision or loud enough
    [Range(0, 90)]
    public float hearingRadius = 25f;                   // Noise detection radius of the monster

    [Header("Components")]
    private NavMeshAgent navMeshAgent;
    private TextMeshProUGUI UItext;

    private State currentFSMState;
    
    [Header("Wander State")]
    [SerializeField][Range(0, 30)]
    private float wanderRadius = 10f;

    [Header("Investigate State")]
    [SerializeField][Range(0, 30)]
    private float searchRadius = 12f;
    [SerializeField][Range(0, 30)]
    private float MAX_INVESTIGATION_TIMER = 10f;
    [SerializeField][Range(0, 10)]
    private int MAX_SEARCH_TOKENS = 3;

    [Header("Chase State")]
    private bool isSlowDownActive = false;
    public int attackDamage {get; private set;} = 1; // Chase.cs will use this value to decrease player health

    private RL model;
    private List<string> currentFSMOutcome = new List<string>();

    /* -------------------------------------------------------------------------- */
    /*                              DEFAULT FUNCTIONS                             */
    /* -------------------------------------------------------------------------- */

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player"); // Player object


        //UItext = GameObject.FindGameObjectWithTag("StateText").GetComponent<TextMeshProUGUI>(); // UI text object
        model = GetComponent<RL>();
        model.SetMonsterAI(this);
    }

    void Start()
    {
        firstPersonAIO = player.GetComponent<FirstPersonAIO>();
        firstPersonAIO.ThreatLevelChanged += OnThreatLevelChanged; // Subscribe to threat level event handler

        StartCoroutine(CheckVisionAngle());
        StartCoroutine(DetectionHandler());

        Vector3 initialWanderParameter = RequestWanderParameter();

        // Start the state machine with the Wander state
        ChangeState(new Wander(this, navMeshAgent, player, wanderRadius, initialWanderParameter)); 
    }

    void FixedUpdate()
    {        
        isPlayerDetected = playerDetectionPercentage >= 100;

        wasPlayerDetectedThisFrame = isWithinCentralVision || isWithinPeripheralVision;
        
        isWithinCentralVision = false;
        isWithinPeripheralVision = false;

        if (wasPlayerDetectedThisFrame || monsterHeardPlayer) playerLastSeenPosition = player.transform.position;
        if (isPlayerFoundHiding) playerDetectionPercentage = 100; playerLastSeenPosition = player.transform.position;

        currentFSMState?.UpdateState();

        if (!wasPlayerDetectedThisFrame)
        {
            isPlayerFoundHiding = false;
            monsterHeardPlayer = false; 
        }

    }

    /* -------------------------------------------------------------------------- */
    /*                                STATE MACHINE                               */
    /* -------------------------------------------------------------------------- */

    public void ChangeState(State newState)
    {
        model.EvaluateAndUpdate(currentFSMOutcome);
        currentFSMOutcome.Clear();
        currentFSMState?.ExitState();
        currentFSMState = newState;
        //UItext.text = newState.GetType().Name;
        currentFSMState.EnterState();
    }
    
    /* -------------------------------------------------------------------------- */
    /*                              MONSTER AFTER-HIT                             */
    /* -------------------------------------------------------------------------- */
    /// <summary>
    /// Slows down the monster for a certain duration
    /// </summary>
    /// <param name="slowDownRate">Rate at which the monster will slow down (e.g 0.3 is 30% slow rate)</param>
    public void SlowDownMonster(float slowDownRate, float duration)
    {
        if (isSlowDownActive)
        {
            return;
        }
        StartCoroutine(SlowDownMonsterCoroutine(slowDownRate, duration));
    }
    
    /// <summary>
    /// Slow down the monster for a certain duration
    /// </summary>
    /// <param name="slowDownRate">Rate at which the monster will slow down (e.g 0.3 is 30% slow rate)</param>
    /// <param name="duration">Duration of the slow down effect</param>
    private IEnumerator SlowDownMonsterCoroutine(float slowDownRate, float duration)
    {
        isSlowDownActive = true;
        float effectDuration = duration;
        float originalSpeed = navMeshAgent.speed;
        float newSpeed = originalSpeed * (1 - slowDownRate);
        navMeshAgent.speed = newSpeed;
    
        while (effectDuration > 0)
        {
            effectDuration -= Time.deltaTime;
            yield return null;
        }
    
        navMeshAgent.speed = originalSpeed;
        isSlowDownActive = false;
    }

    /* -------------------------------------------------------------------------- */
    /*                                  RL MODEL                                  */
    /* -------------------------------------------------------------------------- */

    /// <summary>
    /// Used to enumerate the FSM states.
    /// </summary>
    /// <returns>Enumeration of the current FSM state.</returns>
    private int EnumerateCurrentFSMState()
    {
        switch (currentFSMState)
        {
            case Wander:
                return 0;
            case Investigate:
                return 1;
            case Chase:
                return 2;
        }
        return -1;
    }

    /// <summary>
    /// Called to request a wander parameter for Wander() instantiation.
    /// </summary>
    /// <returns>A position to go to.</returns>
    public Vector3 RequestWanderParameter()
    {
        return model.ChooseWanderParameter();
    }

    /// <summary>
    /// Called to request a hiding spot to check for Investigate().
    /// </summary>
    /// <param name="hidingSpots"></param>
    /// <returns>A game object to investigate.</returns>
    public GameObject RequestInvestigateParameter(List<GameObject>hidingSpots)
    {
        return model.ChooseInvestigateParameter(hidingSpots);
    }

    /// <summary>
    /// Local compilaton of feedbacks, output is used later to update the model.
    /// Called by states when notable events occur during their lifecycle.
    /// </summary>
    /// <param name="feedback">A description of what happened (e.g. "Player Hit", "Player Found")</param>
    public void ProvideFeedback(string feedback)
    {
        // Add single feedback item to the outcomes list
        if (!string.IsNullOrEmpty(feedback))
        {
            currentFSMOutcome.Add(feedback);
        }
    }

    /* -------------------------------------------------------------------------- */
    /*                         EVENT HANDLERS & LISTENERS                         */
    /* -------------------------------------------------------------------------- */

    private void OnThreatLevelChanged(object sender, FirstPersonAIO.ThreatLevelEventArgs e)
    {
        playerThreatLevel = e.ThreatLevel;
        
        // Debug.Log($"Received player position: {e.PlayerPosition}");

        // Run hearing check
        CheckHearingDetection(e.PlayerPosition);
    }

    /* -------------------------------------------------------------------------- */
    /*                             DETECTION FUNCTIONS                            */
    /* -------------------------------------------------------------------------- */

    private IEnumerator CheckVisionAngle()
    {
        while (true)
        {
            Vector3 direction;

            for (float angle = -visionAngle / 2; angle < visionAngle / 2; angle += 1f)
            {
                direction = Quaternion.Euler(0, angle, 0) * transform.forward;

                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, visionRadius))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        if (Mathf.Abs(angle) <= centralVision / 2)
                        {
                            isWithinCentralVision = true;
                            wasWithinCentralVision = true;
                            Debug.DrawRay(transform.position, direction * visionRadius, Color.green);
                        }
                        else
                        {
                            isWithinPeripheralVision = true;
                            wasWithinPeripheralVision = true;
                            Debug.DrawRay(transform.position, direction * visionRadius, Color.green);
                        }
                        CheckCertainDetection(hit.collider.transform.position);
                    }

                    else
                    {
                        // Paint the ray red if it does not hit anything within central vision
                        if (Mathf.Abs(angle) <= centralVision / 2)
                        {
                            Debug.DrawRay(transform.position, direction * visionRadius, Color.red);
                        }
                        // Paint the ray yellow if it does not hit anything within peripheral vision
                        else
                        {
                            Debug.DrawRay(transform.position, direction * visionRadius, Color.yellow);
                        }
                    }
                }
                else
                {
                    // Paint the ray red if it does not hit anything within central vision
                    if (Mathf.Abs(angle) <= centralVision / 2)
                    {
                        Debug.DrawRay(transform.position, direction * visionRadius, Color.red);
                    }
                    // Paint the ray yellow if it does not hit anything within peripheral vision
                    else
                    {
                        Debug.DrawRay(transform.position, direction * visionRadius, Color.yellow);
                    }
                }
            }

            yield return null;
        }
    }

    private void CheckCertainDetection(Vector3 PositionToCheck)
    {
        if (PositionToCheck == null || PositionToCheck == Vector3.zero)
        {
            Debug.LogError("Position to check is null or zero");
        }

        float distanceToPositionToCheck = Vector3.Distance(transform.position, PositionToCheck);

        if (distanceToPositionToCheck <= certainDetectionRadius || (distanceToPositionToCheck <= certainDetectionRadius && playerThreatLevel == 2))
        {
            playerDetectionPercentage = 100;
        }
    }

    private void CheckHearingDetection(Vector3 noisePosition)
    {
        if(noisePosition != null)
        {
            if (Vector3.Distance(transform.position, noisePosition) <= hearingRadius)
            {
                monsterHeardPlayer = true;
                CheckCertainDetection(noisePosition);
            }
        }
    }

    private IEnumerator DetectionHandler()
    {
        while (true)
        {
            // DETECTION INCREASE
            if (playerDetectionPercentage >= 0 && playerDetectionPercentage < 100)
            {
                if (wasWithinCentralVision)
                {
                    wasWithinCentralVision = false; // Reset central vision flag
                    playerDetectionPercentage += centralVisionDetectionRate;
                    timePastSinceLastDetectionTime = Time.time; // Update last detection time
                }
                else if (wasWithinPeripheralVision)
                {
                    wasWithinPeripheralVision = false; // Reset peripheral vision flag
                    playerDetectionPercentage += peripheralVisionDetectionRate;
                    timePastSinceLastDetectionTime = Time.time; // Update last detection time
                }
            }

            // DETECTION DECREASE
            if (!wasPlayerDetectedThisFrame && Time.time - timePastSinceLastDetectionTime >= TIME_REQUIRED_TO_START_DECREASING_DETECTION)
            {
                detectionDecreaseTimer += 0.5f; // Increment timer by 0.5 seconds
                if (detectionDecreaseTimer >= DETECTION_DECREASE_FREQUENCY)
                {
                    playerDetectionPercentage -= detectionDecreaseRate; // Decrease detection by detectionDecreaseRate every second
                    detectionDecreaseTimer = 0f;
                }
            }
            else
            {
                detectionDecreaseTimer = 0f; // Reset the timer if detection occurs
            }

            // Clamp the detection percentage between 0 and 100
            playerDetectionPercentage = Mathf.Clamp(playerDetectionPercentage, 0, 100);

            yield return new WaitForSeconds(0.5f);
        }
    }

    /* -------------------------------------------------------------------------- */
    /*                               GETTER & SETTER                              */
    /* -------------------------------------------------------------------------- */

    public float GetWanderRadius()
    {
        return wanderRadius;
    }
    public float GetSearchRadius()
    {
        return searchRadius;
    }
    public float GetMaxInvestigationTimer()
    {
        return MAX_INVESTIGATION_TIMER;
    }
    public int GetMaxSearchTokens()
    {
        return MAX_SEARCH_TOKENS;
    }
    public Vector3 GetPlayerLastSeenPosition()
    {
        return playerLastSeenPosition;
    }
    public int GetGameProgress()
    {
        return gameProgress;
    }
    public int GetCurrentFSMState()
    {
        return EnumerateCurrentFSMState();
    }
    public void SetIsPlayerFoundHiding(bool isHiding)
    {
        isPlayerFoundHiding = isHiding;
    }
    public void SetPlayerDetectionPercentage(float newPercentage)
    {
        playerDetectionPercentage = newPercentage;
    }

    /* -------------------------------------------------------------------------- */
    /*                                TRAINING METHODS                            */
    /* -------------------------------------------------------------------------- */

    public void ResetMonster()
    {
        transform.position = Vector3.zero;
    }

    /* -------------------------------------------------------------------------- */
    /*                                DRAW & GIZMO                                */
    /* -------------------------------------------------------------------------- */

    private void OnDrawGizmos()
    {
        // VISION RADIUS - White circle
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, visionRadius);
    
        // VISION ANGLE - Yellow cone
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward * visionRadius;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle / 2, 0) * forward;
    
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, leftBoundary, visionAngle, visionRadius);
        #endif
    
        // CENTRAL VISION ANGLE - Red cone
        Gizmos.color = Color.red;
        Vector3 centralLeftBoundary = Quaternion.Euler(0, -centralVision / 2, 0) * forward;
        Vector3 centralRightBoundary = Quaternion.Euler(0, centralVision / 2, 0) * forward;
    
        Gizmos.DrawLine(transform.position, transform.position + centralLeftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + centralRightBoundary);
        #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireArc(transform.position, Vector3.up, centralLeftBoundary, centralVision, visionRadius);
        #endif
    
        // CERTAIN DETECTION RADIUS - Green circle
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, certainDetectionRadius);

        // HEARING RADIUS - Blue circle
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerLastSeenPosition, 0.5f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}