using UnityEngine;

interface IInteractable
{
    public void Interact(GameObject player);
}

public class Interactor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform InteractorSource;
    public float InteractorRange;
    public GameObject InteractPrompt;

    private KeyCode KeyInteract = KeyCode.E;

    private void Start()
    {
        InteractPrompt = GameObject.FindGameObjectWithTag("InteractUI");
    }



    void Update()
    {
            //Creates a ray from the specified source and slightly offsets it forward to avoid player collider
            Vector3 rayStartPosition = InteractorSource.transform.position +InteractorSource.forward * 0.2f;            
            Debug.DrawRay(rayStartPosition, InteractorSource.forward * InteractorRange, Color.green, 1f);
            Ray r = new Ray(rayStartPosition, InteractorSource.forward);


            int layerMask = ~LayerMask.GetMask("IgnoreRaycast");
            if (Physics.Raycast(r, out RaycastHit hitInfo, InteractorRange, layerMask))
            {
                //If Ray Collides with an Interactable
                if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
                {
                    //Enables Prompt
                    InteractPrompt.gameObject.SetActive(true);

                    //Checks whether interact key is pressed
                    if (Input.GetKeyDown(KeyInteract))
                    {
             
                        interactObj.Interact(this.gameObject);
                    }


            }
            else
            {
                InteractPrompt.gameObject.SetActive(false);
            }
            }
        
    }
}
