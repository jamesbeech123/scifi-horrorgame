using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class RLMonsterAI : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Material winFloor;
    [SerializeField] private Material loseFloor;
    [SerializeField] private MeshRenderer floor;

    private Rigidbody rb;
    private Vector3 initialAgentPosition;
    private Vector3 initialTargetPosition;
    private float previousDistanceToTarget;
    private Detection detectionScript;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        initialAgentPosition = transform.localPosition;
        initialTargetPosition = targetTransform.localPosition;
        detectionScript = GetComponent<Detection>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent position
        transform.localPosition = initialAgentPosition;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset target position to a random location
        float randomX = Random.Range(-10f, 15f);
        float randomZ = Random.Range(-20f, 2f);
        targetTransform.localPosition = new Vector3(randomX, targetTransform.localPosition.y, randomZ);

        // Reset distance tracker
        previousDistanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position and rotation
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localRotation);

        // Detection script information (normalized where needed)
        //sensor.AddObservation(detectionScript.playerDetection / 100f);
        //sensor.AddObservation(detectionScript.isWithinCentralVision ? 1f : 0f);
        //sensor.AddObservation(detectionScript.isWithinPeripheralVision ? 1f : 0f);
        //sensor.AddObservation(detectionScript.getPlayerPosition());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveRotate = actions.ContinuousActions[0]; // Rotation input
        float moveForward = actions.ContinuousActions[1]; // Forward/backward input

        float moveSpeed = 5f;

        // Apply movement and rotation
        rb.MovePosition(transform.position + transform.forward * moveForward * moveSpeed * Time.deltaTime);
        transform.Rotate(0f, moveRotate * moveSpeed, 0f, Space.Self);

        // Calculate the current distance to target
        float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

        // Reward or penalize based on distance to target
        if (distanceToTarget < previousDistanceToTarget)
        {
            AddReward(0.01f); // Reward for progress
        }
        else
        {
            AddReward(-0.01f); // Penalty for moving away
        }

        previousDistanceToTarget = distanceToTarget;

        // Add small penalty for each step to encourage efficiency
        AddReward(-0.001f);

        // Check if the agent has reached the target
        if (distanceToTarget < 1.5f)
        {
            SetReward(1f); // Big reward for reaching the target
            floor.material = winFloor;
            EndEpisode();
        }

        // Check if the agent falls off or encounters invalid conditions
        if (transform.localPosition.y < 0)
        {
            SetReward(-1f);
            floor.material = loseFloor;
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

        continuousActions[0] = Input.GetAxisRaw("Horizontal");  // Rotation
        continuousActions[1] = Input.GetAxisRaw("Vertical");    // Forward/Backward movement

        Debug.Log($"Heuristic Inputs - Horizontal: {continuousActions[0]}, Vertical: {continuousActions[1]}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            SetReward(1f); // Reward for reaching the target
            floor.material = winFloor;
            EndEpisode();
        }
        else if (other.CompareTag("Wall"))
        {
            SetReward(-1f); // Penalty for hitting a wall
            floor.material = loseFloor;
            EndEpisode();
        }
    }
}
