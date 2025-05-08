using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour
{
    public GameObject mapMaker;
    public GameObject monsterPrefab;
    private Maze mazeComponent;
    private int objectivesCompleted;
    private Animator exitDoor;
    public GameObject[] exitLights;
    private Material ExitLightMaterialOn;
    public int totalObjectives;

    void Start()
    {
        //Number of Objectives Needed to Open the Exit Door
        totalObjectives = 1;

        objectivesCompleted = 0;
        exitLights = GameObject.FindGameObjectsWithTag("ExitLights");
        ExitLightMaterialOn = Resources.Load<Material>("ExitLightMaterialOn");


    }


    public void CompleteObjective()
    {
        UpdateExitLights();
        
    }

    public void UpdateExitLights()
    {
        if (objectivesCompleted < exitLights.Length) 
        {
            GameObject currentLight = exitLights[objectivesCompleted];

            //Replaces the Red Light Material with the Green Light Material
            Renderer renderer = currentLight.GetComponent<MeshRenderer>();
            Material[] materials = renderer.materials;
            materials[1] = ExitLightMaterialOn;
            renderer.materials = materials;
        }
        objectivesCompleted++;


        if (objectivesCompleted == totalObjectives)
        {
            OpenExitDoor();
        }
    }

    private void OpenExitDoor()
    {
        GameObject.FindGameObjectWithTag("ExitDoor").GetComponent<Animator>().SetTrigger("DoorOpen");
    }

}