using UnityEngine;
using System.Collections.Generic;

// Attach this script to the Player (PlayerController) GameObject.
// It adds a SphereCollider (trigger) used as a proximity detector for hallways.
// Nearby hallway GameObjects (with specific tags) are detected via Physics.OverlapSphere.
// When a hallway is within or touching the detection sphere, its renderers, lights, 
// and other expensive components are enabled. When it's outside, those components are disabled
// while keeping colliders active for AI navigation.
[RequireComponent(typeof(SphereCollider))]
public class HallwayActivator : MonoBehaviour
{
    [Tooltip("Radius of the detection sphere around the player.")]
    [SerializeField] private float detectionRadius = 10f;

    [Tooltip("Valid tags for hallways. Only GameObjects with one of these tags will be processed.")]
    public string[] validTags = new string[] { "4Way", "3Way", "Corner", "Straight", "DeadEnd" };

    // (Optional) If you want to further filter by layer, set the hallway layer mask here.
    [Tooltip("Layer mask for hallway objects.")]
    [SerializeField] private LayerMask hallwayLayerMask;

    // Internal reference to the SphereCollider used for detection
    private SphereCollider detectionSphere;
    // Track which hallways are currently active (player is near them)
    private HashSet<GameObject> activeHallways = new HashSet<GameObject>();
    // Reusable buffer for OverlapSphere results to minimize allocations (adjust size as needed)
    private Collider[] overlapResults = new Collider[500];

    void Start()
    {
        // Configure the SphereCollider as a trigger with the desired radius.
        detectionSphere = GetComponent<SphereCollider>();
        detectionSphere.isTrigger = true;
        detectionSphere.radius = detectionRadius;
    }

    void Update()
    {
        // Use OverlapSphereNonAlloc for performance (avoid garbage allocation)
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectionSphere.radius, overlapResults, hallwayLayerMask);
        // Debug.Log("Hallway count: " + hitCount);
        // Set to track hallways found in this frame
        HashSet<GameObject> currentNearby = new HashSet<GameObject>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapResults[i];
            if (col == null) continue;

            // Skip colliders that belong to the player itself
            if (col.transform.IsChildOf(transform))
                continue;

            // Check if the GameObject's tag is one of the valid hallway tags.
            if (!System.Array.Exists(validTags, tag => col.gameObject.CompareTag(tag)))
                continue;

            // Debug.Log("Player is near hallway: " + col.gameObject.name);

            // Get the hallway object. Adjust this if your hallway structure differs.
            GameObject hallwayObject = col.gameObject;

            // Add to the current set of nearby hallways
            currentNearby.Add(hallwayObject);

            // If this hallway wasn't active before, enable its expensive components.
            if (!activeHallways.Contains(hallwayObject))
            {
                SetHallwayActive(hallwayObject, true);
                activeHallways.Add(hallwayObject);
            }
        }

        // Disable hallways that are no longer nearby.
        List<GameObject> toDisable = new List<GameObject>();
        foreach (GameObject hall in activeHallways)
        {
            if (!currentNearby.Contains(hall))
            {
                SetHallwayActive(hall, false);
                toDisable.Add(hall);
                // Debug.Log("Player is no longer near hallway: " + hall.name);
            }
        }
        foreach (GameObject hall in toDisable)
        {
            activeHallways.Remove(hall);
            // Debug.Log("Removing hallway from active set: " + hall.name);
        }
    }

    /// <summary>
    /// Enables or disables the rendering and other expensive components of a hallway.
    /// Colliders remain active for physics/AI navigation.
    /// </summary>
    private void SetHallwayActive(GameObject hallway, bool active)
    {
        // Toggle all renderers (MeshRenderer, SkinnedMeshRenderer, etc.)
        foreach (Renderer rend in hallway.GetComponentsInChildren<Renderer>())
        {
            rend.enabled = active;
        }
        // Toggle all lights in the hallway.
        foreach (Light light in hallway.GetComponentsInChildren<Light>())
        {
            light.enabled = active;
        }
        // Toggle particle systems (or other components) as needed.
        foreach (ParticleSystem ps in hallway.GetComponentsInChildren<ParticleSystem>())
        {
            var emission = ps.emission;
            emission.enabled = active;
            if (active)
                ps.Play();
            else
                ps.Pause();
        }
        // (Extend here to handle other components as needed.)
    }
}
