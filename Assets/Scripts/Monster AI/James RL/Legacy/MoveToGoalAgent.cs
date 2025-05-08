using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Material winFloor;
    [SerializeField] private Material loseFloor;
    [SerializeField] private MeshRenderer floor;

    private Rigidbody rb;

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
        
        float moveRotate = actions.ContinuousActions[0]; //Rotation
        float moveForward = actions.ContinuousActions[1]; //Forward

        float moveSpeed = 5f;

        rb.MovePosition(transform.position + transform.forward * moveForward * moveSpeed * Time.deltaTime);
        transform.Rotate(0f, moveRotate * moveSpeed, 0f, Space.Self);

        AddReward(0.001f * moveForward);


        // Debugging the current position
        Debug.Log($"Current Position: {transform.localPosition}");

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



    public override void CollectObservations(VectorSensor sensor)
    {

        sensor.AddObservation(transform.localRotation);
        sensor.AddObservation(transform.localPosition);
        
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

       
        continuousActions[0] = Input.GetAxisRaw("Horizontal");  // Left/Right movement (A/D or Arrow keys)
        continuousActions[1] = Input.GetAxisRaw("Vertical");    // Forward/Backward movement (W/S or Arrow keys)

    
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            SetReward(+1f);
            floor.material = winFloor;
            EndEpisode();
        }
        if (other.CompareTag("Wall"))
        {
            SetReward(-1f);
            floor.material = loseFloor;
            EndEpisode();
        }

    }
    
}
