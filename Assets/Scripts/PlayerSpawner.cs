using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;
    private GameObject startRoom;
    [SerializeField] GameObject mazeObject;
    private GameObject player;

    public void SetEvents(Maze maze)
    {
        maze.OnMazeReady += HandleMazeReady;
        maze.OnMapReset += HandleMapReset;
    }

    private void HandleMapReset()
    {
        // Debug.Log("Inside HandleMapReset");
        // GameObject player = GameObject.FindWithTag("Player");
        if (IsPlayerSpawned())
        {
            DespawnpPlayer();
            Debug.Log("Player despawned");
        }
    }

    private void HandleMazeReady()
    {
        // Debug.Log("Inside HandleMazeReady");
        startRoom = GameObject.FindWithTag("StartRoom");
        if (startRoom == null)
        {
            Debug.LogError("Start room not found!");
            return;
        }
        // Debug.Log($"Player will be spawned at {startRoom.transform.position}");
        player = GameObject.FindWithTag("Player");
        if(player == null)
        {
            player = Instantiate(playerPrefab, new Vector3(startRoom.transform.position.x, startRoom.transform.position.y + 1, startRoom.transform.position.z), Quaternion.identity);
            player.tag = "Player";
        }
        // Debug.Log($"Player spawned at {player.transform.position} with tag {player.tag}");
    }

    public void DespawnpPlayer()
    {
        if (player != null) Destroy(player);
    }

    public bool IsPlayerSpawned()
    {
        return player != null;
    }

}
