// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class GameMaster : MonoBehaviour
// {
//     public GameObject mapMaker; // Reference to the Maze GameObject
//     private Maze mazeComponent; // Reference to the Maze.cs Component
    
//     public GameObject monsterPrefab;    // Reference to the Monster Prefab
//     private GameObject monster;         // Reference to the Monster GameObject

//     void Awake()
//     {
//         mazeComponent = mapMaker.GetComponent<Maze>();
//     }

//     void Start()
//     {   
//         if (mazeComponent != null)
//         {
//             mazeComponent.OnMazeReady += HandleMazeReady;
//         }
//     }

//     /// <summary>
//     /// Spawns a monster in the maze at a random position
//     /// </summary>
//     private void SpawnMonster()
//     {
//         if (monsterPrefab == null)
//         {
//             Debug.LogError("Monster prefab not assigned in GameMaster!");
//             return;
//         }
    
//         // Get maze scale and dimensions from maze component
//         int mazeScale = mazeComponent.scale;
//         int mazeWidth = mazeComponent.width;
//         int mazeDepth = mazeComponent.depth;
    
//         // Calculate a random position within the maze bounds
//         float xPos = UnityEngine.Random.Range(2, mazeWidth - 2) * mazeScale;
//         float zPos = UnityEngine.Random.Range(2, mazeDepth - 2) * mazeScale;
//         Vector3 spawnPosition = new Vector3(xPos, 0, zPos);
    
//         // Sample a valid position on the NavMesh near our random point
//         UnityEngine.AI.NavMeshHit hit;
//         if (UnityEngine.AI.NavMesh.SamplePosition(spawnPosition, out hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
//         {
//             // Instantiate the monster slightly above the ground to prevent clipping
//             monster = Instantiate(monsterPrefab, hit.position + Vector3.up * 0.5f, Quaternion.identity);
//             Debug.Log($"Monster spawned at {hit.position}");
//         }
//         else
//         {
//             Debug.LogWarning("Could not find valid NavMesh position to spawn monster!");
//         }
//     }

//     /* -------------------------------------------------------------------------- */
//     /*                                EVENT HANDLERS                              */
//     /* -------------------------------------------------------------------------- */

//     /// <summary>
//     /// Event handler for when the maze is generated. Called inside Start() after map is ready.
//     /// </summary>
//     private void HandleMazeReady()
//     {
//         Debug.Log("Maze generation completed!");
//         SpawnMonster();
//     }
// }