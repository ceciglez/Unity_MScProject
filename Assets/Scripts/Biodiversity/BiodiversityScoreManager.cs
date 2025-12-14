using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Calculates biodiversity metrics using Simpson's Diversity Index across spatial cells
// Creates hotspot data for visual effects and scoring systems
// Analyzes species distribution from iNaturalist observations
//
// DEVELOPMENT NOTE:
// - Implementation aided by Claude Sonnet 3.5 for Simpson's Index calculations and spatial analysis
// - Biodiversity scoring concept and integration design developed independently

[System.Serializable]
public class BiodiversityHotspot
{
    public Vector3 position;
    public float simpsonsIndex;
    public float radius;
    public int speciesCount;
    public int totalObservations;
}

public class BiodiversityScoreManager : MonoBehaviour
{
    [Header("Biodiversity Calculation")]
    [Tooltip("Size of each biodiversity calculation cell in Unity units")]
    public float cellSize = 50f;
    
    [Tooltip("Maximum distance to consider observations (affects calculation area)")]
    public float maxCalculationDistance = 200f;
    
    [Tooltip("How often to recalculate biodiversity scores (in seconds)")]
    public float updateInterval = 3f;
    
    [Header("Simpson's Index Saturation")]
    [Tooltip("Minimum saturation for low diversity areas")]
    [Range(0f, 1f)]
    public float minSaturation = 0.1f;
    
    [Tooltip("Maximum saturation for high diversity areas")]
    [Range(1f, 5f)]
    public float maxSaturation = 3.0f;

    // ...existing fields...

    public float GetSimpsonsIndexAtPosition(Vector3 position)
    {
        // Convert world position to grid cell
        Vector2Int cell = new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
        if (biodiversityGrid != null && biodiversityGrid.TryGetValue(cell, out float simpsonsIndex))
        {
            return simpsonsIndex;
        }
        // Fallback: return average or current global
        if (averageDiversityIndex > 0f)
            return averageDiversityIndex;
        return currentGlobalSaturation > 0f ? currentGlobalSaturation : 1f;
    }
    
    [Tooltip("How smoothly diversity changes between areas")]
    [Range(0.1f, 10f)]
    public float diversitySmoothness = 2f;
    
    [Header("Spotlight Effect")]
    [Tooltip("Enable dramatic spotlight effect around hotspots")]
    public bool useSpotlightEffect = true;
    
    [Tooltip("Brightness boost for biodiversity hotspots")]
    [Range(1f, 10f)]
    public float spotlightIntensity = 4f;
    
    [Header("Debugging")]
    public bool showDebugGizmos = true;
    public bool enableDebugLogging = true;
    public bool forceConstantUpdates = true; // For testing
    
    [Header("Material Assignment (For Debugging)")]
    [Tooltip("Manually assign materials that should receive biodiversity effects")]
    public Material[] testMaterials = new Material[0];
    [Tooltip("Test renderers to apply effects to")]
    public Renderer[] testRenderers = new Renderer[0];
    [SerializeField] private int totalObservationsFound = 0;
    [SerializeField] private int totalSpeciesFound = 0;
    [SerializeField] private float averageDiversityIndex = 0f;
    [SerializeField] private float currentGlobalSaturation = 1f;
    
    // Private fields
    private Dictionary<Vector2Int, float> biodiversityGrid = new Dictionary<Vector2Int, float>(); // Simpson's Index values
    private Dictionary<Vector2Int, Dictionary<string, int>> speciesCountGrid = new Dictionary<Vector2Int, Dictionary<string, int>>(); // Species counts per cell
    private Dictionary<Vector2Int, int> totalObservationsGrid = new Dictionary<Vector2Int, int>(); // Total observations per cell
    private List<ObservationDisplay> allObservations = new List<ObservationDisplay>();
    private List<BiodiversityHotspot> currentHotspots = new List<BiodiversityHotspot>();
    private Transform playerTransform;
    private float lastUpdateTime;
    
    // Shader property for global diversity saturation
    private static readonly int DiversitySaturationProperty = Shader.PropertyToID("_GlobalDiversitySaturation");
    
    public List<BiodiversityHotspot> GetBiodiversityHotspots()
    {
        return new List<BiodiversityHotspot>(currentHotspots);
    }
    
    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("[BiodiversityScoreManager] No Player tagged object found!");
            
        // Find and hook into the iNaturalist map controller
        INaturalistMapController mapController = FindObjectOfType<INaturalistMapController>();
        if (mapController != null)
        {
            Debug.Log("[BiodiversityScoreManager] Found iNaturalist Map Controller - will monitor for observations");
            StartCoroutine(MonitorObservationLoading());
        }
        else
        {
            Debug.LogWarning("[BiodiversityScoreManager] No INaturalistMapController found! Make sure it exists in the scene.");
        }
        
        // Initial calculation
        lastUpdateTime = Time.time;
        
        if (enableDebugLogging)
            Debug.Log($"[BiodiversityScoreManager] *** INITIALIZED ***\\n" +
                     $"Cell size: {cellSize}, Update interval: {updateInterval}s\\n" +
                     $"Saturation range: {minSaturation:F1} to {maxSaturation:F1}\\n" +
                     $"Spotlight effect: {useSpotlightEffect}, Intensity: {spotlightIntensity:F1}");
    }
    
    private IEnumerator MonitorObservationLoading()
    {
        int lastObservationCount = 0;
        
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second
            
            int currentObservationCount = FindObjectsOfType<ObservationDisplay>().Length;
            
            if (currentObservationCount != lastObservationCount)
            {
                Debug.Log($"[BiodiversityScoreManager] *** OBSERVATIONS CHANGED *** {lastObservationCount} → {currentObservationCount}");
                
                if (currentObservationCount > 0)
                {
                    // Wait a bit more for observations to fully initialize
                    yield return new WaitForSeconds(2f);
                    
                    Debug.Log("[BiodiversityScoreManager] *** TRIGGERING BIODIVERSITY CALCULATION ***");
                    UpdateBiodiversityScores();
                }
                
                lastObservationCount = currentObservationCount;
            }
        }
    }
    
    void Update()
    {
        // Update periodically or force constant updates for debugging
        if (forceConstantUpdates || Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateBiodiversityScores();
            lastUpdateTime = Time.time;
        }
        
        // Debug key to force immediate update
        if (Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log("[BiodiversityScoreManager] Manual update triggered!");
            UpdateBiodiversityScores();
        }
    }
    
    public void UpdateBiodiversityScores()
    {
        // Find all observations in the scene
        FindAllObservations();
        
        if (allObservations.Count == 0)
        {
            if (enableDebugLogging)
                Debug.Log("No observations found for biodiversity calculation");
            return;
        }
        
        // Clear previous data
        biodiversityGrid.Clear();
        speciesCountGrid.Clear();
        totalObservationsGrid.Clear();
        
        // Calculate Simpson's index for each grid cell
        CalculateSimpsonsIndex();
        
        // Generate biodiversity hotspots for post-processing
        GenerateBiodiversityHotspots();
        
        // Apply to terrain materials
        ApplyBiodiversityToTerrain();
        
        if (enableDebugLogging)
        {
            float avgDiversity = biodiversityGrid.Count > 0 ? biodiversityGrid.Values.Average() : 0f;
            averageDiversityIndex = avgDiversity;
            
            // Count total species across all cells
            HashSet<string> allSpecies = new HashSet<string>();
            int totalObs = 0;
            foreach (var speciesDict in speciesCountGrid.Values)
            {
                foreach (var species in speciesDict.Keys)
                {
                    allSpecies.Add(species);
                }
                totalObs += speciesDict.Values.Sum();
            }
            
            totalObservationsFound = totalObs;
            totalSpeciesFound = allSpecies.Count;
            
            Debug.Log($"[BiodiversityScoreManager] Updated Simpson's diversity index:\n" +
                     $"  - Cells calculated: {biodiversityGrid.Count}\n" +
                     $"  - Total observations: {totalObs}\n" +
                     $"  - Total species: {allSpecies.Count}\n" +
                     $"  - Average diversity: {avgDiversity:F3}\n" +
                     $"  - Global saturation: {currentGlobalSaturation:F2}\n" +
                     $"  - Hotspots (>0.7): {biodiversityGrid.Values.Count(d => d > 0.7)}");
        }
    }
    
    private void FindAllObservations()
    {
        allObservations.Clear();
        
        // Look for objects with ObservationDisplay component
        ObservationDisplay[] displays = FindObjectsOfType<ObservationDisplay>();
        
        Debug.Log($"[BiodiversityScoreManager] *** SCANNING FOR OBSERVATIONS ***");
        Debug.Log($"[BiodiversityScoreManager] Found {displays.Length} ObservationDisplay components");
        
        int validObservations = 0;
        int observationsWithData = 0;
        int observationsWithTaxon = 0;
        
        foreach (var display in displays)
        {
            if (display.gameObject.activeInHierarchy)
            {
                validObservations++;
                
                var data = display.GetData();
                if (data != null)
                {
                    observationsWithData++;
                    
                    if (data.taxon != null)
                    {
                        observationsWithTaxon++;
                        allObservations.Add(display);
                        
                        if (enableDebugLogging && allObservations.Count <= 5) // Log first 5 for debugging
                        {
                            string speciesId = GetSpeciesIdentifier(display);
                            Debug.Log($"[BiodiversityScoreManager] Obs #{allObservations.Count}: {speciesId} at {display.transform.position}");
                        }
                    }
                    else if (enableDebugLogging)
                    {
                        Debug.Log($"[BiodiversityScoreManager] WARNING: Observation {display.name} has data but no taxon info");
                    }
                }
                else if (enableDebugLogging)
                {
                    Debug.Log($"[BiodiversityScoreManager] WARNING: Observation {display.name} has no data");
                }
            }
        }
        
        Debug.Log($"[BiodiversityScoreManager] *** OBSERVATION SCAN RESULTS ***\\n" +
                 $"  Total ObservationDisplay components: {displays.Length}\\n" +
                 $"  Active observations: {validObservations}\\n" +
                 $"  With data: {observationsWithData}\\n" +
                 $"  With taxon info: {observationsWithTaxon}\\n" +
                 $"  Valid for biodiversity calc: {allObservations.Count}");
                 
        if (allObservations.Count == 0)
        {
            Debug.LogWarning("[BiodiversityScoreManager] ⚠️  NO VALID OBSERVATIONS FOUND! ⚠️\\n" +
                           "Make sure observations have been loaded and have taxon data.");
        }
    }
    
    private void CalculateSimpsonsIndex()
    {
        // Focus calculation around player position if available
        Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        
        // First pass: Count species in each grid cell
        foreach (var observation in allObservations)
        {
            Vector3 pos = observation.transform.position;
            
            // Skip observations too far from center
            if (Vector3.Distance(pos, centerPos) > maxCalculationDistance)
                continue;
            
            Vector2Int gridPos = WorldToGridPosition(pos);
            
            // Get species identifier (use taxon_id or species name)
            string speciesId = GetSpeciesIdentifier(observation);
            if (string.IsNullOrEmpty(speciesId))
                continue;
            
            // Initialize grid cell data if needed
            if (!speciesCountGrid.ContainsKey(gridPos))
            {
                speciesCountGrid[gridPos] = new Dictionary<string, int>();
                totalObservationsGrid[gridPos] = 0;
            }
            
            // Count this species observation
            if (!speciesCountGrid[gridPos].ContainsKey(speciesId))
                speciesCountGrid[gridPos][speciesId] = 0;
            
            speciesCountGrid[gridPos][speciesId]++;
            totalObservationsGrid[gridPos]++;
        }
        
        // Second pass: Calculate Simpson's index for each cell
        foreach (var kvp in speciesCountGrid)
        {
            Vector2Int gridPos = kvp.Key;
            Dictionary<string, int> speciesCounts = kvp.Value;
            int totalObservations = totalObservationsGrid[gridPos];
            
            if (totalObservations < 2)
            {
                // Cannot calculate Simpson's index with fewer than 2 observations
                biodiversityGrid[gridPos] = 0f;
                continue;
            }
            
            // Calculate Simpson's dominance index: D = Σ[n_i × (n_i-1)] / [N × (N-1)]
            float dominanceSum = 0f;
            foreach (var speciesCount in speciesCounts.Values)
            {
                dominanceSum += speciesCount * (speciesCount - 1);
            }
            
            float dominanceIndex = dominanceSum / (totalObservations * (totalObservations - 1));
            
            // Simpson's diversity index: Diversity = 1 - D
            float diversityIndex = 1f - dominanceIndex;
            
            // Apply smoothing by considering neighboring cells
            float smoothedDiversity = CalculateSmoothedDiversity(gridPos);
            
            // Combine calculated and smoothed diversity
            float finalDiversity = Mathf.Lerp(diversityIndex, smoothedDiversity, 0.3f);
            
            biodiversityGrid[gridPos] = Mathf.Clamp01(finalDiversity);
        }
    }
    
    private string GetSpeciesIdentifier(ObservationDisplay observation)
    {
        var data = observation.GetData();
        if (data == null)
            return null;
        
        // Priority order: taxon_id > scientific_name > common_name > iconic_taxon_name
        if (data.taxon != null)
        {
            if (data.taxon.id != 0)
                return data.taxon.id.ToString();
            
            if (!string.IsNullOrEmpty(data.taxon.name))
                return data.taxon.name;
                
            if (!string.IsNullOrEmpty(data.taxon.preferred_common_name))
                return data.taxon.preferred_common_name;
                
            if (!string.IsNullOrEmpty(data.taxon.iconic_taxon_name))
                return data.taxon.iconic_taxon_name;
        }
        
        // Fallback to a generic identifier if no species info available
        return "unknown_species";
    }
    
    private float CalculateSmoothedDiversity(Vector2Int centerGrid)
    {
        float totalDiversity = 0f;
        int cellCount = 0;
        
        // Check 3x3 neighborhood
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector2Int neighborGrid = centerGrid + new Vector2Int(x, z);
                if (speciesCountGrid.ContainsKey(neighborGrid) && totalObservationsGrid.ContainsKey(neighborGrid))
                {
                    int totalObs = totalObservationsGrid[neighborGrid];
                    if (totalObs >= 2)
                    {
                        // Recalculate Simpson's for this neighbor
                        float dominanceSum = 0f;
                        foreach (var speciesCount in speciesCountGrid[neighborGrid].Values)
                        {
                            dominanceSum += speciesCount * (speciesCount - 1);
                        }
                        
                        float dominanceIndex = dominanceSum / (totalObs * (totalObs - 1));
                        float diversity = 1f - dominanceIndex;
                        
                        totalDiversity += diversity;
                        cellCount++;
                    }
                }
            }
        }
        
        return cellCount > 0 ? totalDiversity / cellCount : 0f;
    }
    
    private void GenerateBiodiversityHotspots()
    {
        currentHotspots.Clear();
        
        Vector3 centerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        
        foreach (var kvp in biodiversityGrid)
        {
            Vector2Int gridPos = kvp.Key;
            float simpsonsIndex = kvp.Value;
            
            // Only create hotspots for areas with significant biodiversity
            if (simpsonsIndex > 0.1f) // Threshold for meaningful biodiversity
            {
                Vector3 worldPos = GridToWorldPosition(gridPos);
                
                // Skip if too far from player (for performance)
                if (Vector3.Distance(worldPos, centerPos) > maxCalculationDistance * 1.5f)
                    continue;
                
                BiodiversityHotspot hotspot = new BiodiversityHotspot
                {
                    position = worldPos,
                    simpsonsIndex = simpsonsIndex,
                    radius = cellSize * diversitySmoothness, // Radius based on cell size and smoothness
                    speciesCount = speciesCountGrid.ContainsKey(gridPos) ? speciesCountGrid[gridPos].Count : 0,
                    totalObservations = totalObservationsGrid.ContainsKey(gridPos) ? totalObservationsGrid[gridPos] : 0
                };
                
                currentHotspots.Add(hotspot);
            }
        }
        
        // Sort by Simpson's index (highest first) and limit to top 20 for performance
        currentHotspots = currentHotspots.OrderByDescending(h => h.simpsonsIndex).Take(20).ToList();
        
        if (enableDebugLogging)
        {
            int significantHotspots = currentHotspots.Count(h => h.simpsonsIndex > 0.5f);
            Debug.Log($"[BiodiversityScoreManager] Generated {currentHotspots.Count} biodiversity hotspots " +
                     $"({significantHotspots} with Simpson's Index > 0.5)");
        }
    }

    private void ApplyBiodiversityToTerrain()
    {
        // Calculate average diversity for global effects
        float averageDiversity = 0f;
        if (biodiversityGrid.Count > 0)
        {
            averageDiversity = biodiversityGrid.Values.Average();
        }
        
        // Map diversity to saturation (0-1 diversity maps to minSaturation-maxSaturation)
        float globalSaturation = Mathf.Lerp(minSaturation, maxSaturation, averageDiversity);
        
        // Apply spotlight effect for dramatic hotspots
        if (useSpotlightEffect && averageDiversity > 0.5f)
        {
            globalSaturation *= spotlightIntensity;
        }
        
        currentGlobalSaturation = globalSaturation;
        
        // Set global shader properties
        Shader.SetGlobalFloat(DiversitySaturationProperty, globalSaturation);
        Shader.SetGlobalFloat("_SpotlightIntensity", spotlightIntensity);
        Shader.SetGlobalFloat("_UseSpotlightEffect", useSpotlightEffect ? 1f : 0f);
        
        // Apply regional diversity effects
        ApplyRegionalDiversityEffects();
        
        if (enableDebugLogging)
        {
            Debug.Log($"Applied diversity saturation - Average diversity: {averageDiversity:F3}, " +
                     $"Global saturation: {globalSaturation:F2}");
        }
    }
    
    private void ApplyRegionalDiversityEffects()
    {
        List<Renderer> renderersToProcess = new List<Renderer>();
        
        // Add manually assigned test renderers first
        if (testRenderers != null && testRenderers.Length > 0)
        {
            renderersToProcess.AddRange(testRenderers.Where(r => r != null));
            Debug.Log($"[BiodiversityScoreManager] Added {testRenderers.Count(r => r != null)} manual test renderers");
        }
        
        // Find terrain renderers automatically
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in allRenderers)
        {
            if (renderer != null && !renderersToProcess.Contains(renderer))
            {
                Material material = renderer.sharedMaterial;
                if (material != null && IsTerrainMaterial(material))
                {
                    renderersToProcess.Add(renderer);
                }
            }
        }
        
        Debug.Log($"[BiodiversityScoreManager] *** APPLYING TO {renderersToProcess.Count} RENDERERS ***");
        
        int successCount = 0;
        foreach (var renderer in renderersToProcess)
        {
            // Calculate local diversity score for this renderer
            Vector3 rendererPos = renderer.transform.position;
            Vector2Int gridPos = WorldToGridPosition(rendererPos);
            
            float localDiversity = 0f;
            if (biodiversityGrid.ContainsKey(gridPos))
                localDiversity = biodiversityGrid[gridPos];
            
            // Calculate local diversity saturation with dramatic effect
            float localSaturation = Mathf.Lerp(minSaturation, maxSaturation, localDiversity);
            
            // Apply spotlight effect for high diversity areas
            if (useSpotlightEffect && localDiversity > 0.6f)
            {
                float spotlightMultiplier = Mathf.Lerp(1f, spotlightIntensity, (localDiversity - 0.6f) / 0.4f);
                localSaturation *= spotlightMultiplier;
            }
            
            // Apply to material if it supports diversity properties
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propertyBlock);
            
            propertyBlock.SetFloat("_LocalDiversitySaturation", localSaturation);
            propertyBlock.SetFloat("_SimpsonsIndex", localDiversity);
            propertyBlock.SetInt("_SpeciesCount", GetSpeciesCountAtPosition(rendererPos));
            
            // Debug info for first few renderers
            if (enableDebugLogging && successCount < 3)
            {
                Debug.Log($"[BiodiversityScoreManager] 🎨 Applied to '{renderer.name}': " +
                         $"Diversity={localDiversity:F3}, Saturation={localSaturation:F2}, " +
                         $"Material='{renderer.sharedMaterial?.name}', Shader='{renderer.sharedMaterial?.shader?.name}'");
            }
            
            renderer.SetPropertyBlock(propertyBlock);
            successCount++;
        }
        
        Debug.Log($"[BiodiversityScoreManager] ✅ Applied biodiversity effects to {successCount} renderers");
        
        // Also apply to manually assigned materials
        if (testMaterials != null && testMaterials.Length > 0)
        {
            foreach (var material in testMaterials)
            {
                if (material != null)
                {
                    // Set average diversity on material directly (for testing)
                    float avgDiversity = biodiversityGrid.Count > 0 ? biodiversityGrid.Values.Average() : 0f;
                    float avgSaturation = Mathf.Lerp(minSaturation, maxSaturation, avgDiversity);
                    
                    material.SetFloat("_LocalDiversitySaturation", avgSaturation);
                    material.SetFloat("_SimpsonsIndex", avgDiversity);
                    
                    Debug.Log($"[BiodiversityScoreManager] 🧪 Test material '{material.name}': saturation={avgSaturation:F2}");
                }
            }
        }
    }
    
    private bool IsTerrainMaterial(Material material)
    {
        // Check shader name or material name patterns
        string shaderName = material.shader.name.ToLower();
        string materialName = material.name.ToLower();
        
        return shaderName.Contains("terrain") || 
               shaderName.Contains("mapbox") || 
               shaderName.Contains("heightbased") ||
               materialName.Contains("terrain") ||
               materialName.Contains("ground");
    }
    
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }
    
    private Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float x = (gridPos.x + 0.5f) * cellSize;
        float z = (gridPos.y + 0.5f) * cellSize;
        return new Vector3(x, 0f, z);
    }
    
    public float GetBiodiversityAtPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return biodiversityGrid.ContainsKey(gridPos) ? biodiversityGrid[gridPos] : 0f;
    }
    
    public int GetObservationCountAtPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return totalObservationsGrid.ContainsKey(gridPos) ? totalObservationsGrid[gridPos] : 0;
    }
    
    public int GetSpeciesCountAtPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return speciesCountGrid.ContainsKey(gridPos) ? speciesCountGrid[gridPos].Count : 0;
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || biodiversityGrid == null)
            return;
        
            // Draw biodiversity grid with Simpson's index colors
            foreach (var kvp in biodiversityGrid)
            {
                Vector2Int gridPos = kvp.Key;
                float diversity = kvp.Value;
                
                Vector3 worldPos = GridToWorldPosition(gridPos);
                
                // Color based on Simpson's diversity index with dramatic effect
                float saturation = Mathf.Lerp(minSaturation, maxSaturation, diversity);
                if (useSpotlightEffect && diversity > 0.6f)
                {
                    saturation *= spotlightIntensity;
                }
                
                Color baseColor = diversity > 0.7f ? Color.yellow : Color.green;
                Color gizmoColor = Color.Lerp(Color.gray, baseColor, Mathf.Clamp01(saturation));
                gizmoColor.a = diversity > 0.5f ? 0.8f : 0.4f; // More visible for hotspots
                
                Gizmos.color = gizmoColor;
                
                // Different sizes based on diversity
                float cubeSize = cellSize * (diversity > 0.7f ? 1.2f : 0.9f);
                Gizmos.DrawCube(worldPos + Vector3.up * 5f, Vector3.one * cubeSize);
                
                // Draw wireframe for hotspots
                if (diversity > 0.7f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(worldPos + Vector3.up * 8f, Vector3.one * cubeSize * 1.5f);
                }
                
                // Draw diversity info
                Gizmos.color = Color.white;
                Vector3 textPos = worldPos + Vector3.up * 10f;
                
                #if UNITY_EDITOR
                int totalObs = GetObservationCountAtPosition(worldPos);
                int speciesCount = GetSpeciesCountAtPosition(worldPos);
                string hotspotText = diversity > 0.7f ? " ★HOTSPOT★" : "";
                UnityEditor.Handles.Label(textPos, $"Diversity: {diversity:F3}{hotspotText}\nObs: {totalObs}\nSpecies: {speciesCount}\nSat: {saturation:F1}");
                #endif
            }
    }
}