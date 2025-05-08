using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FogOfWar : MonoBehaviour
{
    [Header("Fog Texture Settings")]
    public int textureWidth = 128;
    public int textureHeight = 128;
    public float revealRadius = 6f;

    [Header("Maze and Minimap Settings")]
    public Rect mazeBounds = new Rect(0, 0, 180, 180);
    public RawImage minimapRawImage;   
    public Canvas minimapPanelObject;  
    public Image playerMarkerUIObject, obstacleMarkerUIObject;
    Maze maze;

    RectTransform minimapPanel;
    RectTransform playerMarkerUI;

    Texture2D fogTexture;
    Color fogColor = new Color(0, 0, 0, 1);

    void Awake()
    {
        if (minimapRawImage == null)
            minimapRawImage = GameObject.FindWithTag("Minimap")?.GetComponent<RawImage>();
        if (minimapPanelObject == null)
            minimapPanelObject = GameObject.FindWithTag("MinimapGrid")?.GetComponent<Canvas>();
        minimapPanel = GameObject.FindWithTag("MinimapGrid")?.GetComponent<RectTransform>();
        if (playerMarkerUIObject == null)
            playerMarkerUIObject = GameObject.FindWithTag("PlayerMinimap")?.GetComponent<Image>();
        playerMarkerUI = playerMarkerUIObject?.GetComponent<RectTransform>();
    }

    void Start()
    {
        // Create and initialize the fog texture once
        fogTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        ClearFog();
        fogTexture.Apply();
        maze = GameObject.FindWithTag("MazeSpawner")?.GetComponent<Maze>();
        if (minimapRawImage != null)
            minimapRawImage.texture = fogTexture;

        if(maze == null)
        {
            Debug.LogError("Maze object not found in Fog of War Script!");
            // return;
        }
    }

    // Fill entire texture with opaque fog
    void ClearFog()
    {
        Color[] colors = new Color[textureWidth * textureHeight];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = fogColor;
        }
        fogTexture.SetPixels(colors);
    }

    // Reveals the area around the given world position.
    public void RevealAtPosition(Vector3 worldPos)
    {
        // Convert world position to normalized coordinates on the maze bounds (assuming XZ plane)
        float normalizedX = Mathf.Clamp01((worldPos.x - mazeBounds.xMin) / mazeBounds.width);
        float normalizedY = Mathf.Clamp01((worldPos.z - mazeBounds.yMin) / mazeBounds.height);

        // Convert normalized position to texture pixel coordinates
        int texX = Mathf.RoundToInt(normalizedX * textureWidth);
        int texY = Mathf.RoundToInt(normalizedY * textureHeight);

        // Calculate the reveal radius in pixel space
        int pixelRadius = Mathf.RoundToInt(revealRadius * (textureWidth / mazeBounds.width));

        // Iterate over a square area and reveal pixels within a circular radius
        for (int y = -pixelRadius; y <= pixelRadius; y++)
        {
            int sampleY = texY + y;
            if (sampleY < 0 || sampleY >= textureHeight)
                continue;

            for (int x = -pixelRadius; x <= pixelRadius; x++)
            {
                int sampleX = texX + x;
                if (sampleX < 0 || sampleX >= textureWidth)
                    continue;

                float dist = Mathf.Sqrt(x * x + y * y);
                if (dist <= pixelRadius)
                {
                    // Hard reveal inside the radius (alpha set to zero)
                    fogTexture.SetPixel(sampleX, sampleY, new Color(0, 0, 0, 0));
                }
            }
        }
        // Apply the changes once per reveal call.
        fogTexture.Apply();
    }

    // Updates the player marker UI position based on world coordinates.
    void UpdatePlayerMarker(Vector3 worldPos)
    {
        if (playerMarkerUI == null || minimapPanel == null)
            return;

        // Convert world position to normalized coordinates.
        float normalizedX = Mathf.Clamp01((worldPos.x - mazeBounds.xMin) / mazeBounds.width);
        float normalizedY = Mathf.Clamp01((worldPos.z - mazeBounds.yMin) / mazeBounds.height);

        // Calculate marker's position relative to the minimap panel dimensions.
        float markerPosX = normalizedX * minimapPanel.rect.width;
        float markerPosY = normalizedY * minimapPanel.rect.height;

        playerMarkerUI.anchoredPosition = new Vector2(markerPosX, markerPosY);
    }

    void Update()
    {
        // Use the object's transform to update the fog and marker;
        // alternatively, you could reference a separate player transform.
        Vector3 playerPos = transform.position;

        // Reveal the fog area around the player's position
        RevealAtPosition(playerPos);

        // Update the player's marker overlay position on the minimap.
        UpdatePlayerMarker(playerPos);
    }

    public RectTransform GetMinimapPanel()
    {
        return minimapPanel;
    }
}
