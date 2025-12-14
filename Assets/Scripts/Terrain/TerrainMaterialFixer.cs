using UnityEngine;
using Mapbox.Unity.Map;

public class TerrainMaterialFixer : MonoBehaviour
{
    [Header("Diagnostic Info")]
    [SerializeField] private bool runDiagnostics = true;
    
    [Header("Auto-Fix Settings")]
    [SerializeField] private bool autoFixMaterial = false;
    [SerializeField] private Material replacementMaterial = null;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private AbstractMap _map;
    
    void Start()
    {
        _map = GetComponent<AbstractMap>();
        
        if (_map == null)
        {
            Debug.LogError("TerrainMaterialFixer: No AbstractMap found on this GameObject!");
            return;
        }
        
        if (runDiagnostics)
        {
            RunDiagnostics();
        }
        
        if (autoFixMaterial && replacementMaterial != null)
        {
            ApplyFixedMaterial();
        }
    }
    
    void RunDiagnostics()
    {
        Debug.Log("=== TERRAIN MATERIAL DIAGNOSTICS ===");
        
        // Check tile material
        Material tileMat = _map.TileMaterial;
        if (tileMat == null)
        {
            Debug.LogWarning("⚠️ PROBLEM: Tile Material is NULL! Map will use fallback 'Standard' shader which doesn't work with URP.");
            Debug.Log("SOLUTION: Assign a URP-compatible material to MapContainer → General → Others → Tile Material");
        }
        else
        {
            Debug.Log($"✓ Tile Material assigned: {tileMat.name}");
            Debug.Log($"  Shader: {tileMat.shader.name}");
            
            // Check if shader is URP compatible
            if (tileMat.shader.name.Contains("Standard") || 
                tileMat.shader.name.Contains("Diffuse") || 
                tileMat.shader.name.Contains("Specular"))
            {
                Debug.LogWarning($"⚠️ PROBLEM: Shader '{tileMat.shader.name}' is Built-in RP, won't work with URP!");
                Debug.Log("SOLUTION: Change shader to 'Universal Render Pipeline/Lit' or use a URP ShaderGraph");
            }
            else if (tileMat.shader.name.Contains("Universal") || 
                     tileMat.shader.name.Contains("URP") ||
                     tileMat.shader.name.Contains("Shader Graphs"))
            {
                Debug.Log($"✓ Shader appears to be URP-compatible: {tileMat.shader.name}");
            }
        }
        
        // Check existing tiles
        Debug.Log("\n=== CHECKING EXISTING TILES ===");
        int tileCount = 0;
        foreach (Transform child in _map.transform)
        {
            var meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                tileCount++;
                if (debugMode)
                {
                    Debug.Log($"Tile '{child.name}':");
                    Debug.Log($"  MeshRenderer enabled: {meshRenderer.enabled}");
                    Debug.Log($"  Material: {(meshRenderer.sharedMaterial != null ? meshRenderer.sharedMaterial.name : "NULL")}");
                    if (meshRenderer.sharedMaterial != null)
                    {
                        Debug.Log($"  Shader: {meshRenderer.sharedMaterial.shader.name}");
                    }
                }
            }
        }
        Debug.Log($"Found {tileCount} tiles with MeshRenderer");
        
        Debug.Log("=== END DIAGNOSTICS ===\n");
    }
    
    void ApplyFixedMaterial()
    {
        if (replacementMaterial == null)
        {
            Debug.LogError("TerrainMaterialFixer: Replacement material not assigned!");
            return;
        }
        
        Debug.Log($"Applying fixed material '{replacementMaterial.name}' to map...");
        _map.SetTileMaterial(replacementMaterial);
        
        // Also update existing tiles
        int updated = 0;
        foreach (Transform child in _map.transform)
        {
            var meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = replacementMaterial;
                updated++;
            }
        }
        
        Debug.Log($"Updated {updated} existing tiles with new material");
    }
    
    // Can be called from Inspector button or manually
    [ContextMenu("Run Diagnostics Now")]
    public void RunDiagnosticsManual()
    {
        if (_map == null)
            _map = GetComponent<AbstractMap>();
        RunDiagnostics();
    }
    
    [ContextMenu("Fix All Tiles Now")]
    public void FixAllTilesManual()
    {
        if (_map == null)
            _map = GetComponent<AbstractMap>();
        if (replacementMaterial != null)
            ApplyFixedMaterial();
        else
            Debug.LogError("Assign a replacement material first!");
    }
}
