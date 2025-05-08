using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] GameObject monsterPrefab;
    private GameObject monsterRoom;
    [SerializeField] GameObject mazeObject;
    private GameObject monster;

    public void SetEvents(Maze maze)
    {
        maze.OnMazeReady += HandleMazeReady;
        maze.OnMapReset += HandleMapReset;
    }

    private void HandleMapReset()
    {
        // Debug.Log("Inside HandleMapReset");
        // GameObject player = GameObject.FindWithTag("Player");
        if (IsMonsterSpawned())
        {
            DespawnpMonster();
            Debug.Log("Monster despawned");
        }
    }

    private void HandleMazeReady()
    {
        // Debug.Log("Inside HandleMazeReady");
        monsterRoom = GameObject.FindWithTag("ReactorRoom");
        if (monsterRoom == null)
        {
            Debug.LogError("Start room not found!");
            return;
        }
        // Debug.Log($"Player will be spawned at {monsterRoom.transform.position}");
        monster = GameObject.FindWithTag("Monster");
        if (monster == null)
        {
            monster = Instantiate(monsterPrefab, new Vector3(monsterRoom.transform.position.x, monsterRoom.transform.position.y + 1, monsterRoom.transform.position.z), Quaternion.identity);
            monster.tag = "Monster";
        }
        // Debug.Log($"Player spawned at {player.transform.position} with tag {player.tag}");
    }

    public void DespawnpMonster()
    {
        if (monster != null) Destroy(monster);
    }


    public bool IsMonsterSpawned()
    {
        return monster != null;
    }

}
