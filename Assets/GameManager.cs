//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class GameManager : MonoBehaviour
//{
//    public GameObject monsterPrefab; // Assign your monster prefab in the Inspector
//    public GameObject player; // Assign the player GameObject in the Inspector
//    public int numMonsters = 10; // Number of monsters to spawn
//    public Transform spawnArea; // Define a spawn area in the scene
//    public int episodeDuration = 10; //Defines the length of the episode in seconds

//    private List<RL> monsters = new List<RL>();

//    void Start()
//    {
//        // Spawn monsters
//        for (int i = 0; i < numMonsters; i++)
//        {
//            Vector3 spawnPosition = GetRandomSpawnPosition();
//            GameObject monster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
//            RL monsterRL = monster.GetComponent<RL>();
//            monsterRL.player = player;
//            monsterRL.gameManager = this;
//            monsters.Add(monsterRL);
//        }

//        // Start the first episode
//        StartCoroutine(RunEpisodes());
//    }

//    public Vector3 GetRandomSpawnPosition()
//    {
//        // Define a random spawn position within the spawn area
//        float x = UnityEngine.Random.Range(spawnArea.position.x - spawnArea.localScale.x / 2, spawnArea.position.x + spawnArea.localScale.x / 2);
//        float z = UnityEngine.Random.Range(spawnArea.position.z - spawnArea.localScale.z / 2, spawnArea.position.z + spawnArea.localScale.z / 2);
//        return new Vector3(x, 0, z);
//    }

//    private IEnumerator RunEpisodes()
//    {
//        while (true)
//        {
//            // Run each episode for a fixed duration
//            yield return new WaitForSeconds(episodeDuration); // Episode duration

//            // Update Q-tables for all monsters
//            foreach (RL monster in monsters)
//            {
//                monster.UpdateQTableFromBuffer();
//            }

//            // Reset the episode
//            ResetEpisode();
//        }
//    }

//    public void ResetEpisode()
//    {
//        // Randomize player and monster positions
//        player.transform.position = GetRandomSpawnPosition();
//        foreach (RL monster in monsters)
//        {
//            monster.ResetMonster();
//        }
//    }
//}