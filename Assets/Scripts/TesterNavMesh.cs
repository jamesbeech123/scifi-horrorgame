using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class TesterNavMesh : MonoBehaviour
{
    GameObject testArea;

    // Start is called before the first frame update
    void Start()
    {
        testArea = new GameObject("Test Area");
        SpawnTestArea();
        // BuildMesh();    
    }

    void SpawnTestArea()
    {
        testArea.transform.position = new Vector3(0, 0, 0);

        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.transform.position = new Vector3(0, 0, 0);
        testCube.transform.localScale = new Vector3(6, 6, 6);
        testCube.transform.parent = testArea.transform;

        GameObject testCube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube1.transform.position = new Vector3(6, 0, 0);
        testCube1.transform.localScale = new Vector3(6, 6, 6);
        testCube1.transform.parent = testArea.transform;

        GameObject testCube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube2.transform.position = new Vector3(12, 0, 0);
        testCube2.transform.localScale = new Vector3(6, 6, 6);
        testCube2.transform.parent = testArea.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void BuildMesh(){
        NavMeshSurface navMeshSurface = testArea.AddComponent<NavMeshSurface>();
        navMeshSurface.collectObjects = CollectObjects.Children;
        navMeshSurface.BuildNavMesh();
    }
}
