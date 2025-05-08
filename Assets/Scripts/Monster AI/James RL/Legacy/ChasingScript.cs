using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ChasingScript : Agent
{
    public Transform player;
    public float moveSpeed = 5f;
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
        player.localPosition = new Vector3(randomX, player.localPosition.y, randomZ);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add AI's position
        sensor.AddObservation(transform.position);

        // Add Player's position
        sensor.AddObservation(player.position);

        // Add relative position to the player
        sensor.AddObservation(player.position - transform.position);

        //Ai's Speed
        sensor.AddObservation(GetComponent<Rigidbody>().velocity);

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveRotate = actions.ContinuousActions[0]; // Left/Right movement
        float moveForward = actions.ContinuousActions[1]; // Forward/Backward movement

        // Apply movement
        rb.MovePosition(transform.position + transform.forward * moveForward * moveSpeed * Time.deltaTime);
        transform.Rotate(0f, moveRotate * moveSpeed, 0f, Space.Self);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float reward = 1f / (distanceToPlayer + 1f); // Reward increases as distance decreases
        SetReward(reward);

    }


    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Reward AI for catching the player
            SetReward(1.0f);
            EndEpisode();
        }
    }
}
