using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingAI : MonoBehaviour
{
    Dictionary<Vector2, List<Vector2>> possiblePaths= new();

    // Start is called before the first frame update
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize(List<Vector3> paths){
        // for(int i=0; i<paths.Count; i++){
        //     for(int j=0; j<paths.Count; j++){
        //         if(i!=j){
        //             Vector3[] path = new Vector3[2];
        //             path[0] = paths[i];
        //             path[1] = paths[j];
        //             possiblePaths.Add(path);
        //         }
        //     }
        // }
    }
}
