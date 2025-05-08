using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum SpawnPointType {
    Fuse,
    KeyCard
}

public class SpawnPoint : MonoBehaviour{
    public SpawnPointType spawnPointType;
    public bool isOccupied = false;
    public GameObject item;

    public void SetOccupied(bool occupied){
        isOccupied = occupied;
    }

    public void SetItem(GameObject item){
        this.item = item;
    }

    public GameObject GetItem(){
        return item;
    }
}

public class SpawnPointManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
