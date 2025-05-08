using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GK;
using System.Linq;
public class roomGenerator : MonoBehaviour
{
    [SerializeField] float minZ, maxZ, minX, maxX;
    [SerializeField] int maxBlocks = 5;
    [SerializeField] GameObject Room, BigRoom, FloatingBall;
    List<Vector3> roomLocations = new List<Vector3>();
    List<GameObject> rooms= new List<GameObject>();
    List<GameObject> balls = new List<GameObject>();
    [SerializeField] Button button;
    [SerializeField] float minDistance = 8f;
    [SerializeField] Material lineRenderMaterial;
    List<Vector3> triangulatedVertices = new();
    int ballHeight = 8;

    void Start()
    {
     if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
        generateRooms();
        
    }

    void OnClick()
    {
        foreach (GameObject room in rooms)
        {
            Destroy(room);
        }
        foreach (GameObject ball in balls)
        {
            Destroy(ball);
        }
        rooms.Clear();
        balls.Clear();
        roomLocations.Clear();
        generateRooms();
        
    }

    void generateRooms()
    {
        for (int i = 0; i < maxBlocks; i++)
        {
            GameObject roomType = Room;
            Vector3 pos = generateValidPosition();
            GameObject newRoom = Instantiate(roomType, pos, Quaternion.identity);
            rooms.Add(newRoom);
        }
        generateBalls();
    }

    void generateBalls()
    {
        for (int i = 0; i < maxBlocks; i++)
        {
            Vector3 pos = roomLocations[i];
            Vector3 newPos = new Vector3(pos.x, ballHeight, pos.z);
            GameObject newBall = Instantiate(FloatingBall, newPos, Quaternion.identity);
            balls.Add(newBall);
        }
        connectLines();
    }

    Vector3 generateValidPosition()
    {
        Vector3 pos;
        bool validPos;
        do{
            pos = new Vector3((int)Random.Range(minX/5, maxX/5) * 5, 3.5f, (int)Random.Range(minZ/5, maxZ/5) * 5);
            validPos = true;

            foreach (Vector3 roomPos in roomLocations)
            {
                if (Vector3.Distance(roomPos, pos) < minDistance)
                {
                    validPos = false;
                    break;
                }
            }
        }while(!validPos);
        roomLocations.Add(pos);
        return pos;
    }

    void connectLines(){
        List<Vector3> triangleCords = calcDelulu();
        Dictionary<Vector3, GameObject> balls = getBalls();
        for(int i = 0; i<triangleCords.Count; i += 3){
            for(int j = 0; j < 3; j++){
                GameObject ball = balls[triangleCords[i+j]];
                LineRenderer lineRenderer = ball.GetComponent<LineRenderer>();
                lineRenderer.material = lineRenderMaterial;
                // Set the width
                lineRenderer.startWidth = 0.3f;
                lineRenderer.endWidth = 0.3f;

                // Set the color
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;
                int count = lineRenderer.positionCount;
                if(count <3 && j != 2){
                    GameObject temp = balls[triangleCords[i+j+1]];
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPosition(0, ball.transform.position);
                    lineRenderer.SetPosition(1, temp.transform.position);
                    lineRenderer.SetPosition(2, ball.transform.position);
                }else if(count < 3){
                    GameObject temp = balls[triangleCords[i+j-2]];
                    lineRenderer.positionCount = 3;
                    lineRenderer.SetPosition(0, ball.transform.position);
                    lineRenderer.SetPosition(1, temp.transform.position);
                    lineRenderer.SetPosition(2, ball.transform.position);
                }else if(count >= 3 && j !=2){
                    GameObject temp = balls[triangleCords[i+j+1]];
                    lineRenderer.positionCount += 3;
                    lineRenderer.SetPosition(count + 1, temp.transform.position);
                    lineRenderer.SetPosition(count + 2, ball.transform.position);
                }else if(count >= 3){
                    GameObject temp = balls[triangleCords[i+j-2]];
                    lineRenderer.positionCount += 3;
                    lineRenderer.SetPosition(count + 1, temp.transform.position);
                    lineRenderer.SetPosition(count + 2, ball.transform.position);
                }else{
                    Debug.Log(ball.transform.position);
                    Debug.Log(lineRenderer.positionCount);
                    Debug.Log("this is gonna make me end it all");
                }
            }
        }

        RemoveZeroPositionsFromLineRenderers();
    }

    void RemoveZeroPositionsFromLineRenderers()
{
    foreach (GameObject ball in balls)
    {
        LineRenderer lineRenderer = ball.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            // Get all positions from the LineRenderer
            int positionCount = lineRenderer.positionCount;
            List<Vector3> positions = new List<Vector3>(positionCount);
            for (int i = 0; i < positionCount; i++)
            {
                positions.Add(lineRenderer.GetPosition(i));
            }

            // Filter out Vector3.zero positions
            List<Vector3> filteredPositions = positions.FindAll(pos => pos != Vector3.zero);

            // Set the filtered positions back to the LineRenderer
            lineRenderer.positionCount = filteredPositions.Count;
            for (int i = 0; i < filteredPositions.Count; i++)
            {
                lineRenderer.SetPosition(i, filteredPositions[i]);
            }
        }
    }
}
    Dictionary<Vector3, GameObject> getBalls()
    {
        Dictionary<Vector3, GameObject> closestBalls = new();
        foreach (GameObject ball in balls)
        {
            closestBalls.Add(ball.transform.position, ball);   
        }
        return closestBalls;
    }

    List<Vector3> calcDelulu(){
        DelaunayCalculator delaunay = new();
        List<Vector2> newVertices = new();
        foreach (Vector3 roomPos in roomLocations)
        {
            newVertices.Add(new Vector2(roomPos.x, roomPos.z));
        }
        DelaunayTriangulation triangulation = delaunay.CalculateTriangulation(newVertices);
        triangulatedVertices = triangulation.Vertices.Select(v => new Vector3(v.x, ballHeight, v.y)).ToList();
        List<int> triangles = triangulation.Triangles;
        List<Vector3> triangleCords = new();
        for(int i = 0; i < triangles.Count; i++){
            triangleCords.Add(triangulatedVertices[triangles[i]]);
        }
        return triangleCords;
    }

}
