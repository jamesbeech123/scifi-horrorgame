using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.IO;
using System.Collections.Generic;

public class TrainingManager : MonoBehaviour
{
    [Header("Training Settings")]
    [SerializeField] private RL rlAgent;
    [SerializeField] private MonsterAI monsterAI;
    [SerializeField] private GameObject player;
    [SerializeField] private int totalEpisodes = 1000;
    [SerializeField] private int episodesBetweenSaves = 100;
    [SerializeField] private string qTableSavePath = "qtable.txt";

    [Header("Randomization Settings")]
    [SerializeField] private Vector2 randomizationArea = new Vector2(10f, 10f);
    [SerializeField] private float minDistanceFromMonster = 5f;
    [SerializeField] private bool randomizePlayerEachEpisode = true;

    [Header("Hyperparameters")]
    [SerializeField] private float initialExplorationRate = 0.9f;
    [SerializeField] private float minExplorationRate = 0.1f;
    [SerializeField] private float explorationDecayRate = 0.995f;

    [Header("Visualization")]
    [SerializeField] private bool showTrainingProgress = true;
    [SerializeField] private int progressUpdateInterval = 10;

    private int currentEpisode = 0;
    private float trainingStartTime;
    private float totalRewardThisEpisode;
    private int stepsThisEpisode;
    private NavMeshAgent playerNavAgent;

    private Dictionary<string, float> rewardTable = new Dictionary<string, float>
    {
        {"Investigate", 0.05f},
        {"Not Found", -0.05f},
        {"Chase", 0.1f},
        {"No Transition", -0.005f},
        {"Player Noise", 0.05f},
        {"Fake Noise", -0.05f},
        {"Player Hit", 0.1f},
        {"Player Escaped", -0.2f},
    };

    void Start()
    {
        InitializeReferences();
        StartCoroutine(TrainingRoutine());
    }

    private void InitializeReferences()
    {
        if (rlAgent == null) rlAgent = FindObjectOfType<RL>();
        if (monsterAI == null) monsterAI = FindObjectOfType<MonsterAI>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (rlAgent == null || monsterAI == null || player == null)
        {
            Debug.LogError("Missing required references!");
            enabled = false;
            return;
        }

        playerNavAgent = player.GetComponent<NavMeshAgent>();
    }

    IEnumerator TrainingRoutine()
    {
        trainingStartTime = Time.time;
        Debug.Log("Starting training...");

        for (currentEpisode = 0; currentEpisode < totalEpisodes; currentEpisode++)
        {
            yield return StartCoroutine(RunEpisode());
            
            if (showTrainingProgress && currentEpisode % progressUpdateInterval == 0)
            {
                LogTrainingProgress();
            }

            if (currentEpisode % episodesBetweenSaves == 0 && currentEpisode > 0)
            {
                rlAgent.SaveQTable(Path.Combine(Application.persistentDataPath, qTableSavePath));
            }
        }

        FinalizeTraining();
    }

    IEnumerator RunEpisode()
    {
        // Initialize episode
        totalRewardThisEpisode = 0f;
        stepsThisEpisode = 0;
        rlAgent.explorationRate = Mathf.Max(minExplorationRate,
            initialExplorationRate * Mathf.Pow(explorationDecayRate, currentEpisode));

        // Reset environment
        yield return StartCoroutine(ResetEnvironment());

        // Run episode until completion
        while (!IsEpisodeComplete())
        {
            stepsThisEpisode++;
            yield return null;
        }
    }

    private bool IsEpisodeComplete()
    {
        return stepsThisEpisode >= 1000; 
    }

    private void LogTrainingProgress()
    {
        float progress = (float)currentEpisode / totalEpisodes * 100f;
        float timePerEpisode = (Time.time - trainingStartTime) / (currentEpisode + 1);
        float estimatedTimeRemaining = timePerEpisode * (totalEpisodes - currentEpisode);
        float avgReward = stepsThisEpisode > 0 ? totalRewardThisEpisode / stepsThisEpisode : 0f;

        Debug.Log($"Episode {currentEpisode}/{totalEpisodes} ({progress:F1}%) | " +
                $"Exploration: {rlAgent.explorationRate:F2} | " +
                $"Avg Reward: {avgReward:F3} | " +
                $"ETA: {estimatedTimeRemaining / 60:F1} minutes");
    }

    IEnumerator ResetEnvironment()
    {
        rlAgent.SetMonsterAI(monsterAI);
        monsterAI.ResetMonster();

        if (randomizePlayerEachEpisode)
        {
            RandomizePlayerPosition();
            if (playerNavAgent != null)
            {
                SetRandomNavMeshDestination();
            }
        }

        yield return null;
    }

    private void RandomizePlayerPosition()
    {
        int attempts = 0;
        const int maxAttempts = 30;

        do
        {
            Vector2 randomCircle = Random.insideUnitCircle * Random.Range(1f, randomizationArea.magnitude);
            Vector3 randomPoint = new Vector3(
                transform.position.x + randomCircle.x,
                0,
                transform.position.z + randomCircle.y
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, randomizationArea.magnitude, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, monsterAI.transform.position) >= minDistanceFromMonster)
                {
                    player.transform.position = hit.position;
                    return;
                }
            }
            attempts++;
        } while (attempts < maxAttempts);

        Debug.LogWarning("Failed to find valid random position for player");
        player.transform.position = Vector3.zero;
    }

    private void SetRandomNavMeshDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * randomizationArea.magnitude;
        randomDirection += player.transform.position;
        
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, randomizationArea.magnitude, NavMesh.AllAreas))
        {
            playerNavAgent.SetDestination(hit.position);
        }
    }

    private void FinalizeTraining()
    {
        rlAgent.SaveQTable(Path.Combine(Application.persistentDataPath, qTableSavePath));
        Debug.Log($"Training completed in {Time.time - trainingStartTime} seconds");
    }

    public void ProcessFeedback(List<string> feedbackList)
    {
        foreach (string feedback in feedbackList)
        {
            if (rewardTable.TryGetValue(feedback, out float reward))
            {
                RecordReward(reward);
            }
        }
    }

    private void RecordReward(float reward)
    {
        totalRewardThisEpisode += reward;
    }

    [ContextMenu("Start Training")]
    public void StartTraining()
    {
        StartCoroutine(TrainingRoutine());
    }

    [ContextMenu("Stop Training")]
    public void StopTraining()
    {
        StopAllCoroutines();
        rlAgent.SaveQTable(Path.Combine(Application.persistentDataPath, qTableSavePath));
        Debug.Log("Training stopped and Q-table saved");
    }
}