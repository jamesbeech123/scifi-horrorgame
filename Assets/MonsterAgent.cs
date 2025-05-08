using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MonsterAgent : Agent
{
    [SerializeField] private Transform targetTransform;

    private Rigidbody rb;
    private Vector3 playerLastPosition;
    private bool playerVisible;
    private int secondsSinceLastSeen;
    private Vector3 directionToPlayer;
    private bool isObserving;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0f, 1f, 0f);

        float randomX = Random.Range(-10f, 15f);
        float randomZ = Random.Range(-20f, 2f);
        targetTransform.localPosition = new Vector3(randomX, targetTransform.localPosition.y, randomZ);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];

        switch (action)
        {
            case 1:
                Debug.Log("OBSERVE");
                Observe();
                break;
        }

        // Only move if not observing
        if (!isObserving)
        {
            float moveRotate = actions.ContinuousActions[0]; // Rotation
            float moveForward = actions.ContinuousActions[1]; // Forward

            float moveSpeed = 5f;

            rb.MovePosition(transform.position + transform.forward * moveForward * moveSpeed * Time.deltaTime);
            transform.Rotate(0f, moveRotate * moveSpeed, 0f, Space.Self);

            AddReward(0.001f * moveForward);
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

        float previousDistance = Vector3.Distance(transform.position, targetTransform.position);
        if (distanceToTarget < previousDistance)
        {
            AddReward(0.01f); // Small reward for getting closer
        }

        // Reward system
        SetReward(-0.01f);  // A small negative penalty for every step
        if (distanceToTarget < 2f)  // Close to the target
        {
            AddReward(0.5f);  // Positive reward when near the target
            EndEpisode();
        }
    }

    private void Observe()
    {
        // Set the observing flag to true
        isObserving = true;

        // Pause movement for 1 second
        StartCoroutine(PauseMovement(1f));

        // Detect player's general direction
        directionToPlayer = (targetTransform.position - transform.position).normalized;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToPlayer, out hit))
        {
            if (hit.transform == targetTransform)
            {
                AddReward(0.2f); // Detected player while observing
                playerLastPosition = targetTransform.position;
            }
        }
        else
        {
            AddReward(-0.05f); // Stopped unnecessarily
        }
    }

    private IEnumerator PauseMovement(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Reset the observing flag after the pause
        isObserving = false;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localRotation);
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(playerLastPosition);
        sensor.AddObservation(playerVisible);
        sensor.AddObservation(secondsSinceLastSeen);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Only allow movement if not observing
        if (!isObserving)
        {
            continuousActions[0] = Input.GetAxisRaw("Horizontal");  // Rotation (A/D or Arrow keys)
            continuousActions[1] = Input.GetAxisRaw("Vertical");    // Forward movement (W or Arrow keys)
        }

        if (Input.GetKey(KeyCode.Space)) discreteActionsOut[0] = 1; // Press Space to Observe
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SetReward(+1f);
            EndEpisode();
        }
        if (other.CompareTag("Wall"))
        {
            SetReward(-1f);
            EndEpisode();
        }
    }

    // Draw Gizmos in the Unity Editor
    private void OnDrawGizmos()
    {
        if (transform != null && targetTransform != null)
        {
            // Draw a line from the monster to the player
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetTransform.position);

            // Draw the direction to the player
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, directionToPlayer * 5f); // Scale the direction for visibility
        }
    }
}