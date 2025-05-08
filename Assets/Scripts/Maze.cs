using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using static UnityEngine.Random;

public class MapLocation {
    public int x, z;

    public MapLocation(int _x, int _z){
        x = _x;
        z = _z;
    }

    public override bool Equals(object obj) {
        if (obj is MapLocation other)
            return x == other.x && z == other.z;
        return false;
    }

    public override int GetHashCode() {
        return x.GetHashCode() ^ z.GetHashCode();
    }

}

public class Maze : MonoBehaviour
{
    public GameObject startRoom, endRoom;
    GameObject mazeArea;
    public List<MapLocation> directions = new(){
        new MapLocation(0, 1),
        new MapLocation(0, -1),
        new MapLocation(1, 0),
        new MapLocation(-1, 0)
    };
    public int width = 30, depth = 30;
    public byte[,] map;
    public int scale = 6, minEdges = 6, maxObjRooms = 4, minDistanceBetweenObjRooms = 6, maxResets = 10;

    public GameObject straight, fourWay, threeWay, deadEnd, cornerTurn, placeholder, mainRoom, operatingRoomPrefab, reactorRoomPrefab, finalRoomPrefab;

    public List<MapLocation> edgeDeadEnds = new();
    List<GameObject> rooms = new();
    enum Direction{UP, DOWN, LEFT, RIGHT}
    int placed = 0;
    Dictionary<MapLocation, GameObject> placedRooms = new();
    [SerializeField] GameObject mainNavLimiter, playerSpawnerObject, itemSpawnerObject, monsterSpawnerObject;
    PlayerSpawner playerSpawnerScript;
    MonsterSpawner monsterSpawnerScript;
    WeightedItemSpawner itemSpawnerScript;
    Dictionary<MapLocation, Direction> objRoomDirections = new();
    Dictionary<MapLocation, GameObject> deadEndObjects = new();
    [SerializeField] List<GameObject> objectives = new();
    private GameObject navLimiter;
    bool IsSpawnerSet = false;


    void Start()
    {
        mazeArea = new GameObject("Maze Area");
        NavMeshSurface navMeshSurface = SetupNavMeshSurface();

        SetupSpawners();

        if(minEdges < maxObjRooms)
        {
            Debug.LogError("Minimum edges must be greater than maximum objective rooms as that means there will be no space for the objective rooms");
            return;
        }

        InitializeMap();
        GenerateLocations(Range(1, width), Range(1, depth));

        if(InstantiateHallways())
        {
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.BuildNavMesh();
            Debug.Log("Number of rooms placed: " + rooms.Count);

            Destroy(navLimiter);

            NotifyMazeReady();
        }
        else
        {
            Debug.LogError("Error instantiating rooms");
            ResetMap();
        }
    }

    NavMeshSurface SetupNavMeshSurface()
    {
        NavMeshSurface navMeshSurface = mazeArea.AddComponent<NavMeshSurface>();

        navLimiter = Instantiate(mainNavLimiter,
                                 new Vector3((width + 10) * scale / 2, 2.1f, (depth + 10) * scale / 2),
                                 Quaternion.identity);
        navLimiter.transform.localScale = new Vector3(width + 10, 1, depth + 10);
        navLimiter.transform.parent = mazeArea.transform;
        return navMeshSurface;
    }

    void SetupSpawners()
    {
        if(IsSpawnerSet) return;
        // Get and setup player spawner
        playerSpawnerScript = playerSpawnerObject.GetComponent<PlayerSpawner>();
        if(playerSpawnerScript != null)
        {
            playerSpawnerScript.SetEvents(this);
            Debug.Log("Player spawner set up");
        }

        // Get and setup weighted item spawner
        itemSpawnerScript = itemSpawnerObject.GetComponent<WeightedItemSpawner>();
        if(itemSpawnerScript != null)
        {
            itemSpawnerScript.SetEvents(this);
            Debug.Log("Item spawner set up");
        }
        

        // Get and setup monster spawner
        monsterSpawnerScript = monsterSpawnerObject.GetComponent<MonsterSpawner>();
        if (monsterSpawnerScript != null)
        {
            monsterSpawnerScript.SetEvents(this);
            Debug.Log("Monster spawner set up");
        }
        IsSpawnerSet = true;
    }

    // Possible parameter for difficulty setting 
    // (int size = (int)Difficulty.EASY)
    // Make it work.
    void InitializeMap(){
        map = new byte[width, depth];
        for(int z = 0; z < depth; z++)
            for(int x = 0; x < width; x++)
            {
                map[x,z] = 1;
            }
    }

    void GenerateLocations(int x, int z)
    {
        if(SurroundingNeighbours(x, z) >= 2) return;

        map[x,z] = 0;
        directions.Shuffle();
        GenerateLocations(x + directions[0].x, z + directions[0].z);
        GenerateLocations(x + directions[1].x, z + directions[1].z);
        GenerateLocations(x + directions[2].x, z + directions[2].z);
        GenerateLocations(x + directions[3].x, z + directions[3].z);
    }
    bool InstantiateHallways(){
        for(int z = 0; z < depth; z++)
            for(int x = 0; x < width; x++)
            {
                GameObject room = null;
                if(map[x,z] == 1){
                    continue;
                }
                else //Straight section
                if(Search2D(x, z, new int[]{2, 0, 2, 1, 0, 1, 2, 0, 2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(straight, pos, Quaternion.identity);
                    room.transform.Rotate(0, 90, 0);
                }else if(Search2D(x, z, new int[]{2, 1, 2, 0, 0, 0, 2, 1, 2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(straight, pos, Quaternion.identity);
                }else // Four way section
                if (Search2D(x, z, new int[]{1, 0, 1, 0, 0, 0, 1, 0, 1})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(fourWay, pos, Quaternion.identity);
                }else //Dead end section
                if (Search2D(x, z, new int[]{2,1,2,0,0,1,2,1,2})){
                    if(z == 1 || z == depth - 2 || x == 1 || x == width - 2){
                        edgeDeadEnds.Add(new MapLocation(x, z));
                    }else{
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(deadEnd, pos, Quaternion.identity);
                        deadEndObjects.Add(new MapLocation(x, z), room);
                    }
                }else if (Search2D(x, z, new int[]{2,1,2,1,0,0,2,1,2})){
                    if(z == 1 || z == depth - 2 || x == 1 || x == width - 2){
                        edgeDeadEnds.Add(new MapLocation(x, z));
                    }else{
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(deadEnd, pos, Quaternion.identity);
                        room.transform.Rotate(0, 180, 0);
                        deadEndObjects.Add(new MapLocation(x, z), room);
                    }
                }else if (Search2D(x, z, new int[]{2,1,2,1,0,1,2,0,2})){
                    if(z == 1 || z == depth - 2 || x == 1 || x == width - 2){
                        edgeDeadEnds.Add(new MapLocation(x, z));
                    }else{
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(deadEnd, pos, Quaternion.identity);
                        room.transform.Rotate(0, 270, 0);
                        deadEndObjects.Add(new MapLocation(x, z), room);
                    }
                }else if (Search2D(x, z, new int[]{2,0,2,1,0,1,2,1,2})){
                    if(z == 1 || z == depth - 2 || x == 1 || x == width - 2){
                        edgeDeadEnds.Add(new MapLocation(x, z));
                    }else{
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(deadEnd, pos, Quaternion.identity);
                        room.transform.Rotate(0, 90, 0);
                        deadEndObjects.Add(new MapLocation(x, z), room);
                    }
                }else //Corner section
                if(Search2D(x, z, new int[]{2, 1, 2, 0, 0, 1, 1, 0, 2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(cornerTurn, pos, Quaternion.identity);
                    room.transform.Rotate(0, 270, 0);
                } else if(Search2D(x,z,new int[]{2,1,2,1,0,0,2,0,1})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(cornerTurn, pos, Quaternion.identity);
                    room.transform.Rotate(0, 180, 0);
                }else if(Search2D(x,z,new int[]{2,0,1,1,0,0,2,1,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(cornerTurn, pos, Quaternion.identity);
                    room.transform.Rotate(0, 90, 0);
                }else if(Search2D(x,z,new int[]{1,0,2,0,0,1,2,1,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(cornerTurn, pos, Quaternion.identity);
                }else //Three way section
                if(Search2D(x,z,new int[]{1,0,1,0,0,0,2,1,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(threeWay, pos, Quaternion.identity);
                    room.transform.Rotate(0, 90, 0);
                }else if(Search2D(x,z,new int[]{2,1,2,0,0,0,1,0,1})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(threeWay, pos, Quaternion.identity);
                    room.transform.Rotate(0, 270, 0);
                }else if(Search2D(x,z,new int[]{1,0,2,0,0,1,1,0,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(threeWay, pos, Quaternion.identity);
                }else if(Search2D(x,z,new int[]{2,0,1,1,0,0,2,0,1})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(threeWay, pos, Quaternion.identity);
                    room.transform.Rotate(0, 180, 0);
                }
                if(room != null){
                    rooms.Add(room);
                    room.transform.parent = mazeArea.transform;
                    // DisableRenders(room);
                }
            }
        return InstantiateEdgeHallways();
    }

    public event Action OnMapReset;
    
    void ResetMap(){
        if(OnMapReset != null){
            OnMapReset.Invoke();
        }
        if(maxResets <= 0){
            Debug.LogError("Max resets reached");
            return;
        }
        maxResets--;
        foreach(GameObject room in rooms)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }

        foreach(GameObject room in deadEndObjects.Values)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }
        deadEndObjects.Clear();

        // if (playerSpawnerScript.IsPlayerSpawned()) playerSpawnerScript.DespawnpPlayer();
        // if (itemSpawnerScript != null) itemSpawnerScript.DespawnItems();
        rooms.Clear();
        edgeDeadEnds.Clear();
        placedRooms.Clear();
        objRoomDirections.Clear();

        placed = 0;
        map = null;
        Destroy(mazeArea);
        Debug.Log("Resetting map");
        Start();
    }
    bool InstantiateEdgeHallways(){

        if (edgeDeadEnds.Count <= minEdges) {
            Debug.LogError("Not enough edge dead ends to place objective rooms...resetting map");
            return false;
        }

        edgeDeadEnds.Shuffle();
        
        foreach(MapLocation loc in edgeDeadEnds){
            
            int x = loc.x;
            int z = loc.z;
            if(IsSpaceClear(x,z)){
                GameObject room = null;
                if (placed >= maxObjRooms){
                    break;
                }
                if(loc.x == 1){

                    if(Search2D(x, z, new int[]{2, 1, 2, 2, 0, 0, 2, 1, 2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(straight, pos, Quaternion.identity);
                    }
                    if(Search2D(x, z, new int[]{2, 1, 2, 2, 0, 1, 2, 0, 2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                        room.transform.Rotate(0, 270, 0);
                    }else if(Search2D(x,z,new int[]{2,0,2,2,0,1,2,1,2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                    }

                    Vector3 pos2 = new Vector3 ((loc.x - 1) * scale, 0, loc.z * scale);
                    GameObject dead = Instantiate(straight, pos2, Quaternion.identity);
                    dead.transform.Rotate(0, 180, 0);
                    rooms.Add(dead);
                    dead.transform.parent = mazeArea.transform;
                    objRoomDirections.Add(loc, Direction.LEFT);

                }else if(loc.x == width - 2){

                    if(Search2D(x, z, new int[]{2, 1, 2, 0, 0, 2, 2, 1, 2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(straight, pos, Quaternion.identity);
                    }else if(Search2D(x,z,new int[]{2,0,2,1,0,2,2,1,2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                        room.transform.Rotate(0, 90, 0);

                    }else if(Search2D(x,z,new int[]{2,1,2,1,0,2,2,0,2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                        room.transform.Rotate(0, 180, 0);
                    }

                    Vector3 pos2 = new Vector3 ((loc.x + 1) * scale, 0, loc.z * scale);
                    GameObject str = Instantiate(straight, pos2, Quaternion.identity);
                    rooms.Add(str);
                    str.transform.parent = mazeArea.transform;
                    objRoomDirections.Add(loc, Direction.RIGHT);

                }else if(loc.z == 1){

                    if(Search2D(x, z, new int[]{2, 0, 2, 1, 0, 1, 2, 2, 2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(straight, pos, Quaternion.identity);
                        room.transform.Rotate(0, 90, 0);
                    }else if(Search2D(x,z,new int[]{2,1,2,1,0,0,2,2,2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                        room.transform.Rotate(0, 180, 0);
                    }else if(Search2D(x, z, new int[]{2, 1, 2, 0, 0, 1, 2, 2, 2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                        room.transform.Rotate(0, 270, 0);
                    }

                    Vector3 pos2 = new Vector3 (loc.x * scale, 0, (loc.z - 1 )* scale);
                    GameObject dead = Instantiate(straight, pos2, Quaternion.identity);
                    dead.transform.Rotate(0, 90, 0);
                    rooms.Add(dead);
                    dead.transform.parent = mazeArea.transform;
                    objRoomDirections.Add(loc, Direction.DOWN);

                }else if(loc.z == depth - 2){

                    if(Search2D(x, z, new int[]{2, 2, 2, 1, 0, 1, 2, 0, 2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(straight, pos, Quaternion.identity);
                        room.transform.Rotate(0, 90, 0);
                    }else if(Search2D(x,z,new int[]{2,2,2,0,0,1,2,1,2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                    }else if(Search2D(x,z,new int[]{2,2,2,1,0,0,2,1,2})){
                        Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                        room = Instantiate(cornerTurn, pos, Quaternion.identity);
                        room.transform.Rotate(0, 90, 0);
                    }

                    Vector3 pos2 = new Vector3 (loc.x * scale, 0, (loc.z + 1 )* scale);
                    GameObject dead = Instantiate(straight, pos2, Quaternion.identity);
                    dead.transform.Rotate(0, 270, 0);
                    rooms.Add(dead);
                    dead.transform.parent = mazeArea.transform;
                    objRoomDirections.Add(loc, Direction.UP);

                }
                if(room != null){
                    rooms.Add(room);
                    room.transform.parent = mazeArea.transform;
                    // DisableRenders(room);
                    placed++;
                }

            }
        }
        return ReplaceDeadEnds();
    }
    bool ReplaceDeadEnds(){

        GameObject room = null;

        foreach(MapLocation loc in objRoomDirections.Keys.ToList()){
            edgeDeadEnds.Remove(loc);
        }

        foreach(MapLocation loc in edgeDeadEnds){
        
            int x = loc.x;
            int z = loc.z;

            if (Search2D(x, z, new int[]{2,1,2,0,0,1,2,1,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(deadEnd, pos, Quaternion.identity);
            }else if (Search2D(x, z, new int[]{2,1,2,1,0,0,2,1,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(deadEnd, pos, Quaternion.identity);
                    room.transform.Rotate(0, 180, 0);
            }else if (Search2D(x, z, new int[]{2,1,2,1,0,1,2,0,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(deadEnd, pos, Quaternion.identity);
                    room.transform.Rotate(0, 270, 0);
            }else if (Search2D(x, z, new int[]{2,0,2,1,0,1,2,1,2})){
                    Vector3 pos = new Vector3 (x * scale, 0, z * scale);
                    room = Instantiate(deadEnd, pos, Quaternion.identity);
                    room.transform.Rotate(0, 90, 0);
            }
            if(room != null){
                rooms.Add(room);
                room.transform.parent = mazeArea.transform;
                // DisableRenders(room);
            }
        }

        return PlaceRooms();
    }

    bool IsSpaceClear(int x, int z)
    {
        if (objRoomDirections.Keys.Count == 0)
            return true;
        
        foreach (MapLocation placedLoc in objRoomDirections.Keys)
        {
            if (Mathf.Abs(placedLoc.x - x) < minDistanceBetweenObjRooms &&
                Mathf.Abs(placedLoc.z - z) < minDistanceBetweenObjRooms)
            {
                // Debug.Log("Space not clear at " + x + " " + z);
                return false;
            }
        }
        // Debug.Log("Space is clear at " + x + " " + z);
        return true;
    }

    bool PlaceRooms(){
        Dictionary<MapLocation, Tuple<Vector3, Quaternion>> roomPositions = new Dictionary<MapLocation, Tuple<Vector3, Quaternion>>();
        foreach(KeyValuePair<MapLocation, Direction> entry in objRoomDirections){
            
            int x = entry.Key.x;
            int z = entry.Key.z;

            Vector3 pos = new Vector3 (x * scale, 0, z * scale);
            Quaternion rotation = Quaternion.Euler(0, 0, 0);
            switch(entry.Value){
                case Direction.DOWN:
                    pos = new Vector3 (x * scale, 0, (z - 2) * scale);
                    rotation = Quaternion.Euler(0, 90, 0);
                    break;
                case Direction.UP:
                    pos = new Vector3 (x * scale, 0, (z + 2) * scale);
                    rotation = Quaternion.Euler(0, 270, 0);
                    break;
                case Direction.RIGHT:
                    pos = new Vector3 ((x + 2) * scale, 0, z * scale);
                    break;
                case Direction.LEFT:
                    pos = new Vector3 ((x - 2) * scale, 0, z * scale);
                    rotation = Quaternion.Euler(0, 180, 0);
                    break;
            }
            
            roomPositions.Add(entry.Key, new Tuple<Vector3, Quaternion>(pos, rotation));
        }

        List<MapLocation> roomLocs = roomPositions.Keys.ToList().ShuffleAndReturn();
        MapLocation startRoomLoc = roomLocs[0];
        MapLocation endRoomLoc = null;
        Tuple<Vector3, Quaternion> startRoomPos = roomPositions[startRoomLoc];
        Tuple<Vector3, Quaternion> endRoomPos = null;

        float maxDistance = 0;
        foreach(MapLocation room in roomLocs){
            if(room != startRoomLoc){
                float distance = Vector3.Distance(new Vector3(room.x, 0, room.z), new Vector3(startRoomLoc.x, 0, startRoomLoc.z));
                if(distance > maxDistance){
                    maxDistance = distance;
                    endRoomLoc = room;
                    endRoomPos = roomPositions[room];
                }
            }
        }

        startRoom = Instantiate(mainRoom, startRoomPos.Item1, startRoomPos.Item2);
        startRoom.tag = "StartRoom";
        Vector3 endRoomPosition = endRoomPos.Item1;
        switch(endRoomPos.Item2.eulerAngles.y){
            case 0:
                endRoomPosition = new Vector3(endRoomPos.Item1.x + 6, 0, endRoomPos.Item1.z);
                break;
            case 90:
                endRoomPosition = new Vector3(endRoomPos.Item1.x, 0, endRoomPos.Item1.z - 6);
                break;
            case 180:
                endRoomPosition = new Vector3(endRoomPos.Item1.x - 6, 0, endRoomPos.Item1.z);
                break;
            case 270:
                endRoomPosition = new Vector3(endRoomPos.Item1.x, 0, endRoomPos.Item1.z + 6);
                break;
        }
        endRoom = Instantiate(finalRoomPrefab, endRoomPosition, endRoomPos.Item2);
        endRoom.tag = "EndRoom";
        Debug.Log("End room rotation: " + endRoom.transform.rotation.eulerAngles.y);
        startRoom.transform.parent = mazeArea.transform;
        endRoom.transform.parent = mazeArea.transform;

        bool toggle = true;

        foreach(MapLocation room in roomLocs) {
            if(room != startRoomLoc && room != endRoomLoc) {
                if(toggle) {
                    placedRooms[room] = Instantiate(operatingRoomPrefab, roomPositions[room].Item1, roomPositions[room].Item2);
                    placedRooms[room].tag = "OperatingRoom";
                }
                else {
                    placedRooms[room] = Instantiate(reactorRoomPrefab, roomPositions[room].Item1, roomPositions[room].Item2);
                    placedRooms[room].tag = "ReactorRoom";
                }
                toggle = !toggle;
                placedRooms[room].transform.parent = mazeArea.transform;
                rooms.Add(placedRooms[room]);
            }
        }

        placedRooms.Add(startRoomLoc, startRoom);
        placedRooms.Add(endRoomLoc, endRoom);
        rooms.Add(startRoom);
        rooms.Add(endRoom);
        
        return placeObjectives();
    }

    bool placeObjectives(){
        List<MapLocation> DELoc = deadEndObjects.Keys.ToList().ShuffleAndReturn();
        for(int i = 0; i < objectives.Count; i++){
            if(DELoc.Count == 0) break;
            int index = Range(0, DELoc.Count);
            MapLocation loc = DELoc[index];
            GameObject room = deadEndObjects[loc];
            if(room != null){
                GameObject obj = Instantiate(objectives[i], room.transform.position, room.transform.rotation);
                obj.transform.parent = room.transform;
                obj.tag = "Objective";
                Debug.Log("Objective " + i + " placed at " + room.transform.position);
                DELoc.RemoveAt(index);
            }
        }

        return true;
    }

    bool Search2D(int c, int r, int[] pattern){

        if(c <= 0 || c >= width - 1 || r <= 0 || r >= depth - 1) return false;
        
        return (map[c-1, r+1] == pattern[0] || pattern[0] == 2) &&
           (map[c, r+1] == pattern[1] || pattern[1] == 2) &&
           (map[c+1, r+1] == pattern[2] || pattern[2] == 2) &&
           (map[c-1, r] == pattern[3] || pattern[3] == 2) &&
           (map[c, r] == pattern[4] || pattern[4] == 2) &&
           (map[c+1, r] == pattern[5] || pattern[5] == 2) &&
           (map[c-1, r-1] == pattern[6] || pattern[6] == 2) &&
           (map[c, r-1] == pattern[7] || pattern[7] == 2) &&
           (map[c+1, r-1] == pattern[8] || pattern[8] == 2);
    }

    int SurroundingNeighbours(int x, int z){
        int count = 0;
        if(x <= 0 || x >= width - 1 || z <= 0 || z >= depth - 1) return 5;
        if(map[x-1, z] == 0) count ++;
        if(map[x+1, z] == 0) count ++;
        if(map[x, z-1] == 0) count ++;
        if(map[x, z+1] == 0) count ++;
        return count;
    }

    public event Action OnMazeReady;

    void NotifyMazeReady()
    {
        // Debug.LogWarning("Inside Notify Maze Ready in Maze.cs");
        
        if (OnMazeReady != null)
        {
            // Debug.Log("Maze is ready");
            OnMazeReady.Invoke();
        }
    }

    public Transform[] GetHallways()
    {
        return rooms.Select(room => room.transform).ToArray();
    }
}

