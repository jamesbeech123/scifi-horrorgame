using UnityEngine;
using UnityEngine.UI;

public class ObjectiveCollisionHandler : MonoBehaviour
{
    public GameObject objectiveMarkerPrefab;

    private FogOfWar fogOfWar;

    private GameObject objectiveMarkerInstance;

    public GameObject objectiveIndicatorPrefab;

    public Vector2 markerShift;

    public bool useRandomShift = false;
    public Vector2 randomShiftRange = new Vector2(20, 20);  

    private GameObject indicatorInstance;

    void Start()
    {
        fogOfWar = FindObjectOfType<FogOfWar>();
        if (fogOfWar == null)
        {
            Debug.LogError("FogOfWar reference not found in ObjectiveCollisionHandler.");
        }


        RectTransform minimapPanel = fogOfWar.GetMinimapPanel();
        if (minimapPanel == null)
        {
            Debug.LogError("Minimap panel not found.");
            return;
        }

        if (objectiveIndicatorPrefab == null)
        {
            Debug.LogError("Objective Indicator Prefab is not assigned on " + gameObject.name);
            return;
        }
        
        // If using a random shift, compute it once.
        if (useRandomShift)
        {
            markerShift = new Vector2(Random.Range(-randomShiftRange.x, randomShiftRange.x), 
                                      Random.Range(-randomShiftRange.y, randomShiftRange.y));
        }
        
        // Instantiate the UI marker as a child of the minimap panel.
        indicatorInstance = Instantiate(objectiveIndicatorPrefab, minimapPanel);
        UpdateIndicatorPosition();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 objPos = transform.position;
            float normalizedX = Mathf.Clamp01((objPos.x - fogOfWar.mazeBounds.xMin) / fogOfWar.mazeBounds.width);
            float normalizedY = Mathf.Clamp01((objPos.z - fogOfWar.mazeBounds.yMin) / fogOfWar.mazeBounds.height);

            RectTransform minimapPanel = fogOfWar.GetMinimapPanel();
            if (minimapPanel == null)
            {
                Debug.LogError("Minimap panel not found.");
                return;
            }

            float markerPosX = normalizedX * minimapPanel.rect.width;
            float markerPosY = normalizedY * minimapPanel.rect.height;

            if (objectiveMarkerPrefab != null && objectiveMarkerInstance == null)
            {
                objectiveMarkerInstance = Instantiate(objectiveMarkerPrefab, minimapPanel);
                RectTransform markerRect = objectiveMarkerInstance.GetComponent<RectTransform>();
                markerRect.anchoredPosition = new Vector2(markerPosX, markerPosY);
            }

            Destroy(indicatorInstance);
        }

    }

    void UpdateIndicatorPosition()
    {
        Vector3 objPos = transform.position;
        float normalizedX = Mathf.Clamp01((objPos.x - fogOfWar.mazeBounds.xMin) / fogOfWar.mazeBounds.width);
        float normalizedY = Mathf.Clamp01((objPos.z - fogOfWar.mazeBounds.yMin) / fogOfWar.mazeBounds.height);

        RectTransform minimapPanel = fogOfWar.GetMinimapPanel();
        float basePosX = normalizedX * minimapPanel.rect.width;
        float basePosY = normalizedY * minimapPanel.rect.height;

        Vector2 finalPos = new Vector2(basePosX, basePosY) + markerShift;

        RectTransform indicatorRect = indicatorInstance.GetComponent<RectTransform>();
        indicatorRect.anchoredPosition = finalPos;
    }
}