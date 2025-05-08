using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GK;
using System.Linq;
using Unity.VisualScripting;
public class NewGridAttempt : MonoBehaviour
{
    /// <summary>
    /// Debug variables for visualizing the Delaunay triangulation and corner rooms.
    /// </summary>
    [SerializeField] bool DebugTriangulation = false;
    /// <summary>
    /// Debug variables for visualizing the corner rooms.
    /// </summary>
    [SerializeField] bool DebugCornerRooms = false;
    /// <summary>
    /// Variables for the area in which the rooms are generated.
    /// </summary>
    [SerializeField] int roomGenerationAreaMinZ, roomGenerationAreaMaxZ, roomGenerationAreaMinX, roomGenerationAreaMaxX;
    /// <summary>
    /// The number of rooms to generate.
    /// </summary>
    [SerializeField] int roomsToGenerate = 5;
    /// <summary>
    /// The prefabs to be instantiated.
    /// </summary>
    [SerializeField] GameObject Room, BigRoom, FloatingBall, CornerRoom;
    /// <summary>
    /// Lists of available grid cell locations, room locations, triangulated vertices, corner room cords, rooms, balls and corner rooms.
    /// </summary>
    List<Vector3> availableGridCellLocations= new(), roomLocations= new(), triangulatedVertices= new(), cornerCords= new();
    List<GameObject> rooms= new(), balls= new(), cornerRooms= new();
    /// <summary>
    /// The button to reset the generation.
    /// </summary>
    [SerializeField] Button button;
    /// <summary>
    /// The minimum distance between rooms.
    /// </summary>
    [SerializeField] float minDistance = 50f;
    /// <summary>
    /// The material for the LineRenderers.
    /// </summary>
    [SerializeField] Material lineRenderMaterial; //DEBUG
    /// <summary>
    /// The height of the balls.
    /// </summary>
    int ballHeight = 14; //Debug

    /// <summary>
    /// Distance matrix
    /// </summary>
    Dictionary<List<Vector2>, float> distanceMatrix;

    List<Vector3> triangleCords = new();


    /// <summary>
    /// Starts the generation of the grid and rooms.
    /// </summary>
    void Start() {
        if (button != null) {
                button.onClick.AddListener(OnClick);
        }
        generateGrid();

        generateRooms();
    }

    /// <summary>
    /// Generates a grid of available locations for rooms to be placed.
    /// Starts at the minimum x and z values and increments by 5 until the maximum x and z values are reached.
    /// WARNING: Uses preset y value of 2.5 for all grid cells. Designed for rooms with a height of 5. MUST BE REVISED.
    /// </summary>
    void generateGrid() {
        for (int x = roomGenerationAreaMinX; x <= roomGenerationAreaMaxX; x += 25) {
            for (int z = roomGenerationAreaMinZ; z <= roomGenerationAreaMaxZ; z += 25) {
                availableGridCellLocations.Add(new Vector3(x, 0, z));
            }
        }
    }

    /// <summary>
    /// Destroys all rooms, balls, corner rooms and clears all lists.
    /// </summary>
    void OnClick() {
        generateGrid();
        foreach (GameObject room in rooms) {
            Destroy(room);
        }
        foreach (GameObject ball in balls) {
            Destroy(ball);
        }
        foreach (GameObject cornerRoom in cornerRooms) {
            Destroy(cornerRoom);
        }
        rooms.Clear();
        balls.Clear();
        cornerRooms.Clear();
        roomLocations.Clear();
        cornerCords.Clear();
        generateRooms();
    }

    /// <summary>
    /// Chooses random coordinates from available grid cell and places a room there.
    /// Removes the chosen coordinates from the available grid cell list.
    /// </summary>
    void generateRooms() {
        for (int i = 0; i < roomsToGenerate; i++) {
            Vector3 roomCoordinates = generateValidPosition(); // Use generateValidPosition to find a valid location
            if (roomCoordinates == Vector3.zero) break; // If no valid position is found, break the loop
    
            GameObject roomType = Room;
            GameObject newRoom = Instantiate(roomType, roomCoordinates, Quaternion.identity);
            rooms.Add(newRoom);
    
            availableGridCellLocations.Remove(roomCoordinates); // Remove the used location
        }
    
        if(DebugTriangulation || DebugCornerRooms) {
            generateBalls();
        }
        generateDistanceMatrix();
        Debug.Log("Distance matrix: " + distanceMatrix.Count);
        Debug.Log(distanceMatrix.Values.ToCommaSeparatedString());
    }
    
    /// <summary>
    /// Generates a valid position for a room to be placed.
    /// </summary>
    /// <returns>
    /// A valid position for a room to be placed.
    /// </returns>
    Vector3 generateValidPosition() {
        Vector3 pos;
        bool validPos = false;
        int attempts = 0;
    
        while (!validPos && attempts < availableGridCellLocations.Count) {
            pos = availableGridCellLocations[Random.Range(0, availableGridCellLocations.Count)];
            validPos = true;
    
            foreach (Vector3 roomPos in roomLocations) {
                if (Vector3.Distance(roomPos, pos) < minDistance) {
                    validPos = false;
                    break;
                }
            }
    
            if (validPos) {
                roomLocations.Add(pos);
                return pos;
            }
    
            attempts++;
        }
    
        return Vector3.zero;
    }

    /// <summary>
    /// Calculates the Delaunay triangulation of the room locations.
    /// </summary>
    /// <returns>
    /// A list of Vector3 coordinates of the triangulated vertices. 
    /// </returns>
    List<Vector3> calcDelulu() {
        DelaunayCalculator delaunay = new();
        List<Vector2> newVertices = new();
        foreach (Vector3 roomPos in roomLocations) {
            newVertices.Add(new Vector2(roomPos.x, roomPos.z));
        }
        DelaunayTriangulation triangulation = delaunay.CalculateTriangulation(newVertices);
        triangulatedVertices = triangulation.Vertices.Select(v => new Vector3(v.x, ballHeight, v.y)).ToList();
        List<int> triangles = triangulation.Triangles;
        List<Vector3> triangleCords = new();
        for (int i = 0; i < triangles.Count; i++) {
            triangleCords.Add(triangulatedVertices[triangles[i]]);
        }
        return triangleCords;
    }

    /// <summary>
    /// Generates floating balls at the triangulated vertices.
    /// </summary>
    void generateBalls() {
        for (int i = 0; i < roomLocations.Count; i++)
        {
            Vector3 pos = roomLocations[i];
            Vector3 newPos = new Vector3(pos.x, ballHeight, pos.z);
            GameObject newBall = Instantiate(FloatingBall, newPos, Quaternion.identity);
            balls.Add(newBall);
        }
        if(DebugTriangulation) {
            connectLines();
        }
        if(DebugCornerRooms) {
            createCornerRooms();
        }
    }

    /// <summary>
    /// Connects the floating balls with lines to visualize the Delaunay triangulation.
    /// </summary>
    void connectLines() {
        List<Vector3> triangleCords = calcDelulu();
        Dictionary<Vector3, GameObject> balls = getBalls();
        for(int i = 0; i<triangleCords.Count; i += 3) {
            for(int j = 0; j < 3; j++) {
                GameObject ball = balls[triangleCords[i+j]];
                LineRenderer lineRenderer = ball.GetComponent<LineRenderer>();
                lineRenderer.material = lineRenderMaterial;
                // Set the width
                lineRenderer.startWidth = 0.5f;
                lineRenderer.endWidth = 0.5f;

                // Set the color
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;
                int count = lineRenderer.positionCount;
                if (count <3 && j != 2) {
                    GameObject temp = balls[triangleCords[i+j+1]];
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPosition(0, ball.transform.position);
                    lineRenderer.SetPosition(1, temp.transform.position);
                    lineRenderer.SetPosition(2, ball.transform.position);
                } else if (count < 3) {
                    GameObject temp = balls[triangleCords[i+j-2]];
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPosition(0, ball.transform.position);
                    lineRenderer.SetPosition(1, temp.transform.position);
                    lineRenderer.SetPosition(2, ball.transform.position);
                } else if (count >= 3 && j !=2) {
                    GameObject temp = balls[triangleCords[i+j+1]];
                    lineRenderer.positionCount += 3;
                    lineRenderer.SetPosition(count + 1, temp.transform.position);
                    lineRenderer.SetPosition(count + 2, ball.transform.position);
                } else if (count >= 3) {
                    GameObject temp = balls[triangleCords[i+j-2]];
                    lineRenderer.positionCount += 3;
                    lineRenderer.SetPosition(count + 1, temp.transform.position);
                    lineRenderer.SetPosition(count + 2, ball.transform.position);
                } else {
                    Debug.Log(ball.transform.position);
                    Debug.Log(lineRenderer.positionCount);
                    Debug.Log("this is gonna make me end it all");
                }
            }
        }

        RemoveZeroPositionsFromLineRenderers();
    }

    /// <summary>
    /// Removes Vector3.zero positions from the LineRenderers.
    /// </summary>
    void RemoveZeroPositionsFromLineRenderers() {
        foreach (GameObject ball in balls) {
            LineRenderer lineRenderer = ball.GetComponent<LineRenderer>();
            if (lineRenderer != null) {
                // Get all positions from the LineRenderer
                int positionCount = lineRenderer.positionCount;
                List<Vector3> positions = new List<Vector3>(positionCount);
                for (int i = 0; i < positionCount; i++) {
                    positions.Add(lineRenderer.GetPosition(i));
                }

                // Filter out Vector3.zero positions
                List<Vector3> filteredPositions = positions.FindAll(pos => pos != Vector3.zero);

                // Set the filtered positions back to the LineRenderer
                lineRenderer.positionCount = filteredPositions.Count;
                for (int i = 0; i < filteredPositions.Count; i++) {
                    lineRenderer.SetPosition(i, filteredPositions[i]);
                }
            }
        }
    }

    /// <summary>
    /// Returns a dictionary of the balls and their positions.
    /// </summary>
    /// <returns>
    /// A dictionary of the balls and their positions.
    /// </returns>
    Dictionary<Vector3, GameObject> getBalls() {
        Dictionary<Vector3, GameObject> closestBalls = new();
        foreach (GameObject ball in balls) {
            closestBalls.Add(ball.transform.position, ball);   
        }
        return closestBalls;
    }

    /// <summary>
    /// Calculates the Manhattan distance between two positions.
    /// </summary>
    /// <returns>
    /// A list of Vector3 coordinates of the Manhattan distance between the two positions.
    /// </returns>
    List<Vector3> manhattanDistance(Vector3 pos1, Vector3 pos2) {
        return new List<Vector3> { new Vector3(pos1.x, 0, pos2.z), new Vector3(pos2.x, 0, pos1.z) };
    }

    /// <summary>
    /// Creates corner rooms at the corners of the triangles. Checks if the corner rooms are already occupied by other rooms.
    /// Also checks if rooms are on the same axes in either direction to avoid creating corner rooms on the same axis.
    /// WARNING: Creates corners in places where they are not needed. MUST BE LOOKED AT AND FIXED.
    /// </summary>
    void createCornerRooms() {
        triangleCords = calcDelulu();
        for(int i = 0; i<triangleCords.Count; i += 3) {
            Vector3 pos1 = triangleCords[i];
            Vector3 pos2 = triangleCords[i+1];
            Vector3 pos3 = triangleCords[i+2];
            List<Vector3> corner1Pos = manhattanDistance(pos1, pos2);
            List<Vector3> corner2Pos = manhattanDistance(pos2, pos3);
            if((pos1.x != pos2.x || pos1.z != pos2.z) && !roomLocations.Contains(corner1Pos[0])) {
                GameObject corner1 = Instantiate(CornerRoom, corner1Pos[0], Quaternion.identity);
                cornerCords.Add(corner1.transform.position);
                cornerRooms.Add(corner1);
            }else if((pos1.x != pos2.x || pos1.z != pos2.z) && !roomLocations.Contains(corner1Pos[1])) {
                GameObject corner1 = Instantiate(CornerRoom, corner1Pos[1], Quaternion.identity);
                cornerCords.Add(corner1.transform.position);
                cornerRooms.Add(corner1);
            }
            if((pos2.x != pos3.x || pos2.z != pos3.z) && !roomLocations.Contains(corner2Pos[0]) ) {
                GameObject corner2 = Instantiate(CornerRoom, corner2Pos[0], Quaternion.identity);
                cornerCords.Add(corner2.transform.position);
                cornerRooms.Add(corner2);
            }else if((pos2.x != pos3.x || pos2.z != pos3.z) && !roomLocations.Contains(corner2Pos[1])) {
                GameObject corner2 = Instantiate(CornerRoom, corner2Pos[1], Quaternion.identity);
                cornerCords.Add(corner2.transform.position);
                cornerRooms.Add(corner2);
            }
        }
    }

    void generateDistanceMatrix() {
        if(triangleCords.Count == 0) {
            triangleCords = calcDelulu();
        }
        distanceMatrix = new();
        for(int i = 0; i<triangleCords.Count; i += 3) {
            Vector2 pos1 = new Vector2(triangleCords[i].x, triangleCords[i].z);
            Vector2 pos2 = new Vector2(triangleCords[i+1].x, triangleCords[i+1].z);
            Vector2 pos3 = new Vector2(triangleCords[i+2].x, triangleCords[i+2].z);
            
            List<Vector2> key1 = new List<Vector2> { pos1, pos2 };
            List<Vector2> key2 = new List<Vector2> { pos2, pos3 };
            List<Vector2> key3 = new List<Vector2> { pos1, pos3 };


            float distance1 = Vector2.Distance(pos1, pos2);
            float distance2 = Vector2.Distance(pos2, pos3);
            float distance3 = Vector2.Distance(pos1, pos3);
            distanceMatrix.Add(key1, distance1);
            distanceMatrix.Add(key2, distance2);
            distanceMatrix.Add(key3, distance3);
        }
    }
}