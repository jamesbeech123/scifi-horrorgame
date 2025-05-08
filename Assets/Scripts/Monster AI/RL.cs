using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Reinforcement Learning controller for monster AI using Q-learning algorithm
/// </summary>
[RequireComponent(typeof(MonsterAI))]
public class RL : MonoBehaviour
{
    /* -------------------------------------------------------------------------- */
    /*                              HYPER PARAMETERS                              */
    /* -------------------------------------------------------------------------- */
    private static Dictionary<(GameState, int), float> qTable = new Dictionary<(GameState, int), float>(); // Q-Table
    [SerializeField] private float learningRate = 0.1f;     // Rate of new information overwriting old information
    [SerializeField] private float discountFactor = 0.9f;   // Importance of future rewards
    [SerializeField][Range(0f, 1f)] public float explorationRate = 0.9f;  // Rate of experimenting instead of choosing the best action
    [SerializeField] private int cellSize = 2;              // Size of the grid cells
    [SerializeField] private bool visualizeGrid = true;     // Debug visualization flag
    [SerializeField] private bool trainingMode = true;     // Toggle for training mode with more randomization
    [SerializeField] private string qTablePath = "qtable.txt";

    /* -------------------------------------------------------------------------- */
    /*                            REWARD/PENALTY TABLE                            */
    /* -------------------------------------------------------------------------- */
    [System.Serializable]
    public class RewardEntry
    {
        public string key;
        public float value;
    }

    [SerializeField]
    private List<RewardEntry> rewardEntries = new List<RewardEntry>
{
    new RewardEntry { key = "Investigate", value = 0.05f },
    new RewardEntry { key = "Not Found", value = -0.05f },
    new RewardEntry { key = "Chase", value = -0.1f },
    new RewardEntry { key = "No Transition", value = -0.005f },
    new RewardEntry { key = "Player Noise", value = 0.05f },
    new RewardEntry { key = "Fake Noise", value = -0.05f },
    new RewardEntry { key = "Player Hit", value = 0.1f },
    new RewardEntry { key = "Player Escaped", value = -0.2f },
};

    private Dictionary<string, float> rewardTable;

    //[System.Serializable]
    //public class RewardDictionary : SerializableDictionary<string, float> { }
    //[SerializeField]
    //private RewardDictionary rewardTable = new RewardDictionary
    //{
    //    {"Investigate", 0.05f},     // Reached Investigate State
    //    {"Not Found", -0.05f},      // Player Not Found in Hiding
    //    {"Chase", 0.1f},            // Reached Chase State
    //    {"No Transition", -0.005f},  // No State Transition
    //    {"Player Noise", 0.05f},    // Noise Caused by Player
    //    {"Fake Noise", -0.05f},     // Noise Caused by Bait
    //    {"Player Hit", 0.1f},       // Player Hit by Monster
    //    {"Player Escaped", -0.2f},  // Player Escaped the Chase
    //};

    /* -------------------------------------------------------------------------- */
    /*                             EVALUATION PROCESS                             */
    /* -------------------------------------------------------------------------- */
    private GameState previousState;
    private int actionTaken = -1; // 0 = Wander, 1 = Investigate
    private GameState currentState;

    /* -------------------------------------------------------------------------- */
    /*                          GAMEOBJECT AND COMPONENTS                         */
    /* -------------------------------------------------------------------------- */
    private MonsterAI monsterAI;
    private List<Vector3> possibleDestinations; // List of possible destinations for the monster to move to
    private Transform gridVisualization;        // Container for grid visualization objects

    [Header("Q-Table Settings")]
    [SerializeField] private TextAsset defaultQTable;
    [SerializeField] private string savedQTableName = "qtable.txt";
    private bool qTableLoaded = false;
    [SerializeField] bool debugMode = false; // Debug mode for logging

    /* -------------------------------------------------------------------------- */
    /*                              DEFAULT FUNCTIONS                             */
    /* -------------------------------------------------------------------------- */
    void Awake()
    {
        monsterAI = GetComponent<MonsterAI>();
        InitializeRewardTable();
        possibleDestinations = GenerateNavMeshGrid(cellSize);

        if (visualizeGrid)
        {
            CreateGridVisualization();
        }

        InitializeQTable(); // Load Q-table here

        currentState = GetGameState();
        previousState = currentState;

        // Lower exploration rate if not in training mode
        if (!trainingMode)
        {
            explorationRate = Mathf.Min(explorationRate, 0.2f);
        }
    }

    void Start()
    {
        LoadQTable(Application.persistentDataPath + "/QTable.txt");
    }

    private void InitializeRewardTable()
    {
        rewardTable = new Dictionary<string, float>();
        foreach (var entry in rewardEntries)
        {
            if (!rewardTable.ContainsKey(entry.key))
            {
                rewardTable.Add(entry.key, entry.value);
            }
            else
            {
                Debug.LogWarning($"Duplicate reward key: {entry.key}");
            }
        }
    }

    void InitializeQTable()
    {
        string savedPath = Path.Combine(Application.persistentDataPath, savedQTableName);

        // Try loading saved Q-table first
        if (File.Exists(savedPath))
        {
            try
            {
                LoadQTable(savedPath);
                qTableLoaded = true;
                if(debugMode) Debug.Log("Successfully loaded saved Q-table");
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load saved Q-table: {e.Message}");
            }
        }

        // Fall back to default Q-table
        if (defaultQTable != null)
        {
            try
            {
                string tempPath = Path.Combine(Application.persistentDataPath, "temp_qtable.txt");
                File.WriteAllText(tempPath, defaultQTable.text);
                LoadQTable(tempPath);
                qTableLoaded = true;
                if(debugMode) Debug.Log("Loaded default Q-table from resources");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load default Q-table: {e.Message}");
            }
        }

        if (!qTableLoaded)
        {
            Debug.LogWarning("No Q-table available - starting with empty table");
            qTable = new Dictionary<(GameState, int), float>();
        }
    }

    /* -------------------------------------------------------------------------- */
    /*                             PARAMETER SELECTION                            */
    /* -------------------------------------------------------------------------- */

    /// <summary>
    /// Selects a destination for the monster to wander to based on either exploration or exploitation.
    /// If in training mode, always chooses a random destination.
    /// If exploring (random chance based on explorationRate), chooses a random destination.
    /// If exploiting, chooses the destination with the highest Q-value for the current game state.
    /// </summary>
    /// <returns>A Vector3 position on the NavMesh for the monster to move towards</returns>
    public Vector3 ChooseWanderParameter()
    {
        // Save previous state before updating to new state
        previousState = currentState;
        currentState = GetGameState();

        // Set the action being taken
        actionTaken = 0;

        Vector3 chosenDestination;

        // In training mode, always choose a random destination for better exploration
        if (trainingMode || UnityEngine.Random.value < explorationRate)
        {
            // Explore: choose a random destination
            int randomIndex = UnityEngine.Random.Range(0, possibleDestinations.Count);
            chosenDestination = possibleDestinations[randomIndex];
        }
        else
        {
            // Exploit: choose the best destination based on Q-values
            float maxQValue = float.MinValue;
            int bestDestIndex = 0;

            if (possibleDestinations.Count > 0)
            {
                // Evaluate each possible destination
                for (int i = 0; i < possibleDestinations.Count; i++)
                {
                    // Create a temporary state representing being at this destination
                    GameState potentialState = new GameState(
                        DiscretePosition(possibleDestinations[i]),
                        currentState.PlayerPosition,
                        currentState.GameProgress,
                        currentState.CurrentFSMState
                    );

                    float qValue = 0;
                    if (qTable.ContainsKey((potentialState, 0)))
                    {
                        qValue = qTable[(potentialState, 0)];
                    }

                    if (qValue > maxQValue)
                    {
                        maxQValue = qValue;
                        bestDestIndex = i;
                    }
                }

                chosenDestination = possibleDestinations[bestDestIndex];
            }
            else
            {
                // Fallback if no destinations
                chosenDestination = transform.position;
            }
        }

        // Highlight the chosen destination if visualization is enabled
        if (visualizeGrid)
        {
            VisualizeChoice(chosenDestination);
        }

        return chosenDestination;
    }

    /// <summary>
    /// Selects a hiding spot for the monster to investigate based on either exploration or exploitation.
    /// If exploring (random chance based on explorationRate), chooses a random hiding spot.
    /// If exploiting, chooses the hiding spot with the highest Q-value for the current game state.
    /// </summary>
    /// <param name="hidingSpots">List of possible hiding spots for the monster to investigate</param>
    /// <returns>A GameObject representing the hiding spot to investigate, or null if the list is empty</returns>
    public GameObject ChooseInvestigateParameter(List<GameObject> hidingSpots)
    {
        // Save previous state before updating to new state
        previousState = currentState;
        currentState = GetGameState();

        // Set the action being taken
        actionTaken = 1;

        if (hidingSpots == null || hidingSpots.Count == 0)
        {
            Debug.LogWarning("No hiding spots available to investigate");
            return null;
        }

        if (UnityEngine.Random.value < explorationRate || trainingMode)
        {
            // Explore: choose a random hiding spot
            return hidingSpots[UnityEngine.Random.Range(0, hidingSpots.Count)];
        }
        else
        {
            // Exploit: choose the best hiding spot based on Q-values
            float maxQValue = float.MinValue;
            int bestSpotIndex = 0;

            // Evaluate each hiding spot
            for (int i = 0; i < hidingSpots.Count; i++)
            {
                // Create a temporary state representing being at this hiding spot
                GameState potentialState = new GameState(
                    DiscretePosition(hidingSpots[i].transform.position),
                    currentState.PlayerPosition,
                    currentState.GameProgress,
                    currentState.CurrentFSMState
                );

                float qValue = 0;
                if (qTable.ContainsKey((potentialState, 1)))
                {
                    qValue = qTable[(potentialState, 1)];
                }

                if (qValue > maxQValue)
                {
                    maxQValue = qValue;
                    bestSpotIndex = i;
                }
            }

            return hidingSpots[bestSpotIndex];
        }
    }

    /* -------------------------------------------------------------------------- */
    /*                             SECONDARY FUNCTIONS                            */
    /* -------------------------------------------------------------------------- */

    /// <summary>
    /// Generates a grid of cells for the RL model to choose as potential wander destination.
    /// </summary>
    /// <param name="cellSize">Size of the cells.</param>
    /// <returns>List of cell positions.</returns>
    private List<Vector3> GenerateNavMeshGrid(int cellSize)
    {
        List<Vector3> destinations = new List<Vector3>();
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        // Determine the bounds of the navmesh
        Vector3 minBounds = navMeshData.vertices[0];
        Vector3 maxBounds = navMeshData.vertices[0];
        foreach (Vector3 vertex in navMeshData.vertices)
        {
            minBounds = Vector3.Min(minBounds, vertex);
            maxBounds = Vector3.Max(maxBounds, vertex);
        }

        if(debugMode) Debug.Log($"NavMesh Grid Generation - Bounds: Min={minBounds}, Max={maxBounds}, Cell Size={cellSize}");

        // Create a grid of points within the bounds
        for (float x = minBounds.x; x <= maxBounds.x; x += cellSize)
        {
            for (float z = minBounds.z; z <= maxBounds.z; z += cellSize)
            {
                Vector3 point = new Vector3(x, 0, z);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(point, out hit, cellSize, NavMesh.AllAreas))
                {
                    destinations.Add(hit.position);
                }
            }
        }

        if(debugMode) Debug.Log($"NavMesh Grid Generation - Points generated: {destinations.Count}");

        if (destinations.Count == 0)
        {
            Debug.LogError("No valid destinations found on the NavMesh!");
        }

        return destinations;
    }

    /// <summary>
    /// Creates visual markers for grid points when debug visualization is enabled
    /// </summary>
    private void CreateGridVisualization()
    {
        // Remove existing visualization if it exists
        if (gridVisualization != null)
        {
            Destroy(gridVisualization.gameObject);
        }

        // Create new container for grid markers
        gridVisualization = new GameObject("GridVisualization").transform;

        // Create a sphere for each grid point
        foreach (Vector3 point in possibleDestinations)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.position = point;
            marker.transform.localScale = Vector3.one * 0.5f;
            marker.GetComponent<Renderer>().material.color = Color.yellow;
            marker.transform.parent = gridVisualization;
            Destroy(marker.GetComponent<Collider>()); // Remove collider as we don't need it
        }
    }

    /// <summary>
    /// Highlights the chosen destination in the visualization grid
    /// </summary>
    /// <param name="chosenPosition">The position that was selected</param>
    private void VisualizeChoice(Vector3 chosenPosition)
    {
        if (gridVisualization == null) return;

        // Reset all points to yellow first
        foreach (Transform child in gridVisualization)
        {
            child.GetComponent<Renderer>().material.color = Color.yellow;
        }

        // Find the closest grid point to our chosen destination
        Transform closest = null;
        float closestDistance = float.MaxValue;

        foreach (Transform child in gridVisualization)
        {
            float distance = Vector3.Distance(child.position, chosenPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = child;
            }
        }

        // Color the closest point red
        if (closest != null)
        {
            closest.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    /// <summary>
    /// Get the current state of the game. Includes monster's position, player's last seen position, 
    /// game progress and FSM state.
    /// </summary>
    /// <returns>The current state of the game.</returns>
    private GameState GetGameState()
    {
        // Discretize positions to create manageable state space
        Vector3 monsterPosition = DiscretePosition(transform.position);
        Vector3 playerPosition = DiscretePosition(monsterAI.GetPlayerLastSeenPosition());
        int gameProgress = monsterAI.GetGameProgress();
        int currentFSMState = monsterAI.GetCurrentFSMState();

        return new GameState(monsterPosition, playerPosition, gameProgress, currentFSMState);
    }

    /* -------------------------------------------------------------------------- */
    /*                             AFTERMATH FUNCTIONS                            */
    /* -------------------------------------------------------------------------- */

    /// <summary>
    /// Entry point for evaluation process.
    /// </summary>
    /// <param name="feedbackList">List of important events that occurred during the game.</param>
    public void EvaluateAndUpdate(List<string> feedbackList)
    {
        UpdateQTable(previousState, actionTaken, currentState, CalculateReward(feedbackList));
    }

    /// <summary>
    /// Calculates the total reward based on a list of feedback events.
    /// Each feedback string corresponds to a key in the rewardTable dictionary.
    /// The method sums up all the corresponding reward values for each valid feedback.
    /// </summary>
    /// <param name="feedbackList">List of feedback strings to evaluate</param>
    /// <returns>The total calculated reward value (positive for rewards, negative for penalties)</returns>
    private float CalculateReward(List<string> feedbackList)
    {
        float totalReward = 0f;

        // Process each feedback item and look up its reward value
        foreach (string feedback in feedbackList)
        {
            if (rewardTable.TryGetValue(feedback, out float value))
            {
                totalReward += value;
                if(debugMode) Debug.Log($"Reward applied: {feedback} = {value}");
            }
            else
            {
                Debug.LogWarning($"Unknown feedback type: {feedback}");
            }
        }

        if(debugMode) Debug.Log($"Total reward: {totalReward}");
        return totalReward;
    }

    /// <summary>
    /// Updates the Q-table based on the received reward and transitions between states.
    /// Uses the Q-learning formula: Q(s,a) = Q(s,a) + learningRate * [reward + discountFactor * maxQ(s',a') - Q(s,a)]
    /// </summary>
    /// <param name="previousState">The state before the action was taken</param>
    /// <param name="actionId">The ID of the action taken (0 for wander, 1 for investigate)</param>
    /// <param name="currentState">The state after the action was taken</param>
    /// <param name="reward">The reward received for the action</param>
    public void UpdateQTable(GameState previousState, int actionId, GameState currentState, float reward)
    {
        // Initialize Q-value if it doesn't exist yet
        if (!qTable.ContainsKey((previousState, actionId)))
        {
            qTable[(previousState, actionId)] = 0f;
        }

        // Find the maximum Q-value for the current state across all possible actions
        float maxNextQValue = float.MinValue;

        // Check Q-value for wandering in the current state
        if (qTable.ContainsKey((currentState, 0)))
        {
            maxNextQValue = Mathf.Max(maxNextQValue, qTable[(currentState, 0)]);
        }

        // Check Q-value for investigating in the current state
        if (qTable.ContainsKey((currentState, 1)))
        {
            maxNextQValue = Mathf.Max(maxNextQValue, qTable[(currentState, 1)]);
        }

        // If no Q-values exist for the current state, initialize with 0
        if (maxNextQValue == float.MinValue)
        {
            maxNextQValue = 0f;
        }

        // Apply the Q-learning formula
        float oldQValue = qTable[(previousState, actionId)];
        float newQValue = oldQValue + learningRate * (reward + discountFactor * maxNextQValue - oldQValue);

        // Update the Q-table
        qTable[(previousState, actionId)] = newQValue;

        if(debugMode) Debug.Log($"Q-Table updated: State={previousState}, Action={actionId}, " +
                  $"OldQ={oldQValue}, NewQ={newQValue}, Reward={reward}");
    }

    void OnApplicationQuit()
    {
        SaveQTable(Application.persistentDataPath + "/QTable.txt");
    }


    /* -------------------------------------------------------------------------- */
    /*                               MISC FUNCTIONS                               */
    /* -------------------------------------------------------------------------- */

    /// <summary>
    /// Used in MonsterAI.cs to assign the correct MonsterAI instance.
    /// </summary>
    /// <param name="monsterAI">MonsterAI.cs instance.</param>
    public void SetMonsterAI(MonsterAI monsterAI)
    {
        this.monsterAI = monsterAI;
    }

    /// <summary>
    /// Discretizes a position by rounding to the nearest grid cell
    /// </summary>
    /// <param name="position">The original position</param>
    /// <returns>The discretized position</returns>
    private Vector3 DiscretePosition(Vector3 position)
    {
        // Round positions to reduce state space
        float x = Mathf.Round(position.x / cellSize) * cellSize;
        float y = Mathf.Round(position.y / cellSize) * cellSize;
        float z = Mathf.Round(position.z / cellSize) * cellSize;
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Gets the current Q-table for inspection or saving
    /// </summary>
    /// <returns>The Q-table dictionary</returns>
    public Dictionary<(GameState, int), float> GetQTable()
    {
        return qTable;
    }

    /// <summary>
    /// Saves the Q-table to a file
    /// </summary>
    /// <param name="path">File path to save to</param>
    public void SaveQTable(string path)
    {
        if(debugMode) Debug.Log($"Saving Q-table to: {path}");
        try
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (var entry in qTable)
                {
                    GameState state = entry.Key.Item1;
                    writer.WriteLine($"{state.MonsterPosition}|{state.PlayerPosition}|" +
                                   $"{state.GameProgress}|{state.CurrentFSMState}|" +
                                   $"{entry.Key.Item2}|{entry.Value}");
                }
            }
            if(debugMode) Debug.Log("Q-table saved successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Q-table: {e.Message}");
        }
    }

    /// <summary>
    /// Loads the Q-table from a file
    /// </summary>
    /// <param name="path">File path to load from</param>
    public void LoadQTable(string path)
    {
        try
        {
            qTable.Clear();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length == 6)
                    {
                        Vector3 monsterPos = ParseVector3(parts[0]);
                        Vector3 playerPos = ParseVector3(parts[1]);
                        var state = new GameState(
                            monsterPos,
                            playerPos,
                            int.Parse(parts[2]),
                            int.Parse(parts[3])
                        );
                        qTable[(state, int.Parse(parts[4]))] = float.Parse(parts[5]);
                    }
                }
            }
            if(debugMode) Debug.Log("Q-table loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load Q-table: {e.Message}");
        }
    }

    /// <summary>
    /// Helper method to parse a Vector3 from string
    /// </summary>
    private Vector3 ParseVector3(string s)
    {
        string[] parts = s.Trim('(', ')').Split(',');
        return new Vector3(
            float.Parse(parts[0]),
            float.Parse(parts[1]),
            float.Parse(parts[2])
        );
    }
}

/// <summary>
/// Represents the current state of the game, including the positions of the monster and player, 
/// the game progress, and the current state of the monster's finite state machine.
/// Implements IEquatable for proper dictionary comparison.
/// </summary>
public struct GameState : IEquatable<GameState>
{
    public Vector3 MonsterPosition { get; }
    public Vector3 PlayerPosition { get; }
    public int GameProgress { get; }
    public int CurrentFSMState { get; }

    public GameState(Vector3 monsterPos, Vector3 playerPos, int progress, int fsmState)
    {
        MonsterPosition = monsterPos;
        PlayerPosition = playerPos;
        GameProgress = progress;
        CurrentFSMState = fsmState;
    }

    public bool Equals(GameState other)
    {
        return MonsterPosition == other.MonsterPosition &&
               PlayerPosition == other.PlayerPosition &&
               GameProgress == other.GameProgress &&
               CurrentFSMState == other.CurrentFSMState;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + MonsterPosition.GetHashCode();
            hash = hash * 23 + PlayerPosition.GetHashCode();
            hash = hash * 23 + GameProgress.GetHashCode();
            hash = hash * 23 + CurrentFSMState.GetHashCode();
            return hash;
        }
    }
}