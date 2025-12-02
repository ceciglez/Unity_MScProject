using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.Collections.Generic;

/// <summary>
/// OBSOLETE VERSION - Applies Stylized Grass Shader material to Mapbox tiles near the player.
/// Uses the Stylized Grass Shader asset from Staggart Creations.
/// Documentation: https://staggart.xyz/unity/stylized-grass-shader/sgs-docs/
/// </summary>
public class StylizedGrassManager_OLD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The AbstractMap component")]
    public AbstractMap map;
    
    [Tooltip("The player transform")]
    public Transform player;
    
    [Header("Materials")]
    [Tooltip("Grass material for tiles near player (uses Stylized Grass Shader)")]
    public Material grassMaterial;
    
    [Tooltip("Base terrain material for distant tiles (no grass)")]
    public Material baseMaterial;
    
    [Header("Grass Settings")]
    [Tooltip("Only apply grass to tiles within this distance from player (meters)")]
    [Range(50f, 300f)]
    public float grassRenderDistance = 100f;
    
    [Tooltip("How often to check and update materials (seconds)")]
    [Range(0.5f, 5f)]
    public float updateInterval = 1f;
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private float lastUpdateTime;
    private Dictionary<UnityTile, bool> tileGrassState = new Dictionary<UnityTile, bool>();
    
    void Start()
    {
        // Auto-find references
        if (map == null)
            map = FindObjectOfType<AbstractMap>();
            
        if (player == null)
        {
            // Try to find player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                var characterController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
                if (characterController != null)
                    playerObj = characterController.gameObject;
            }
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("[StylizedGrassManager] No player found!");
        }
        
        if (grassMaterial == null)
        {
            Debug.LogError("[StylizedGrassManager] No grass material assigned!");
            enabled = false;
            return;
        }
        
        if (baseMaterial == null)
        {
            Debug.LogError("[StylizedGrassManager] No base material assigned!");
            enabled = false;
            return;
        }
        
        // Subscribe to tile events
        if (map != null)
        {
            map.OnTileFinished += OnTileFinished;
        }
    }
    
    void OnDestroy()
    {
        if (map != null)
        {
            map.OnTileFinished -= OnTileFinished;
        }
    }
    
    void OnTileFinished(UnityTile tile)
    {
        // When a new tile loads, check if it should have grass
        UpdateTileMaterial(tile);
    }
    
    void Update()
    {
        if (player == null || map == null)
            return;
            
        // Periodically update all tiles
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.time;
            UpdateAllTiles();
        }
    }
    
    void UpdateAllTiles()
    {
        // Get all tiles
        UnityTile[] tiles = map.GetComponentsInChildren<UnityTile>();
        
        int grassCount = 0;
        int baseCount = 0;
        
        foreach (UnityTile tile in tiles)
        {
            bool hasGrass = UpdateTileMaterial(tile);
            if (hasGrass)
                grassCount++;
            else
                baseCount++;
        }
        
        if (debugMode)
        {
            Debug.Log($"[StylizedGrassManager] Tiles with grass: {grassCount}, without: {baseCount}");
        }
    }
    
    bool UpdateTileMaterial(UnityTile tile)
    {
        if (tile == null || player == null)
            return false;
            
        // Check distance to player
        float distance = Vector3.Distance(tile.transform.position, player.position);
        bool shouldHaveGrass = distance <= grassRenderDistance;
        
        // Check current state
        bool currentlyHasGrass = false;
        if (tileGrassState.TryGetValue(tile, out currentlyHasGrass))
        {
            // No change needed
            if (currentlyHasGrass == shouldHaveGrass)
                return currentlyHasGrass;
        }
        
        // Update material
        MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material targetMaterial = shouldHaveGrass ? grassMaterial : baseMaterial;
            renderer.sharedMaterial = targetMaterial;
            tileGrassState[tile] = shouldHaveGrass;
            
            if (debugMode)
            {
                string materialName = shouldHaveGrass ? "GRASS" : "BASE";
                Debug.Log($"[StylizedGrassManager] Tile {tile.name} ({distance:F1}m away) -> {materialName} material");
            }
        }
        
        return shouldHaveGrass;
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;
            
        // Draw grass render distance sphere
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        Gizmos.DrawSphere(player.position, grassRenderDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, grassRenderDistance);
    }
}
