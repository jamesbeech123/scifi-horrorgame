using UnityEngine;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

[System.Serializable]
public class WeightedItem
{
    public GameObject prefab;
    public float weight;
}

public class WeightedItemSpawner : MonoBehaviour
{
    public Maze maze;
    public Transform[] hallways;
    public float spawnThreshold = 0.5f;
    public float noiseScale = 1.0f;

    public List<WeightedItem> items;
    GameObject allItems;

    public void SetEvents(Maze maze)
    {
        maze.OnMazeReady += SpawnItemsInHallways;
        maze.OnMapReset += DespawnItems;
    }

    public void DespawnItems()
    {
        // foreach (GameObject child in allItems.transform)
        // {
        //     Destroy(child);
        // }
        hallways = null;
        
        Destroy(allItems);
        Debug.Log("All items despawned");
    }

    void SpawnItemsInHallways()
    {
        allItems = new GameObject("AllItems");
        hallways = maze.GetHallways();
        Debug.Log("Hallways: " + hallways.Length);

        float totalWeight = 0f;
        foreach (WeightedItem wi in items)
        {
            totalWeight += wi.weight;
        }

        float offset = 0.3f;
        foreach (Transform hallway in hallways)
        {
            // Debug.Log("Hallway position: " + hallway.position);
            float noiseValue = Mathf.PerlinNoise((hallway.position.x + offset) * noiseScale, (hallway.position.z + offset) * noiseScale);
            if (noiseValue >= spawnThreshold)
            {
                float rand = Random.value * totalWeight;
                float cumulative = 0f;
                GameObject itemToSpawn = null;
                foreach (WeightedItem wi in items)
                {
                    cumulative += wi.weight;
                    if (rand <= cumulative)
                    {
                        itemToSpawn = wi.prefab;
                        break;
                    }
                }
                // Debug.Log("Item to be spawned: " + itemToSpawn);
                if (itemToSpawn != null)
                {
                    Vector3 pos = new Vector3(Random.Range(hallway.position.x-1f , hallway.position.x + 1f), hallway.position.y, Random.Range(hallway.position.z-1f , hallway.position.z + 1f));
                    GameObject thing = Instantiate(itemToSpawn, pos, hallway.transform.rotation);
                    thing.transform.parent = allItems.transform;
                }
            }
        }
        Debug.Log("All items spawned, number of items: " + allItems.transform.childCount);
    }
}
