using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;

/// <summary>
/// Applies vertex colors to terrain based on elevation for shader blending
/// Works with HeightBasedTerrain shader to blend three textures by height
/// </summary>
[CreateAssetMenu(menuName = "Mapbox/Modifiers/Elevation Based Material")]
public class ElevationBasedMaterial : GameObjectModifier
{
    [Header("Height Thresholds")]
    [Tooltip("Height below this gets low texture (R channel) - valleys, water level")]
    [Range(-100f, 500f)]
    public float lowThreshold = 0f;
    
    [Tooltip("Height between low and this gets mid texture (G channel) - hills, plains")]
    [Range(-100f, 500f)]
    public float midThreshold = 50f;
    
    [Tooltip("Height above this gets high texture (B channel) - mountains, peaks")]
    [Range(-100f, 500f)]
    public float highThreshold = 150f;
    
    [Header("Blend Settings")]
    [Tooltip("Smooth transition between materials")]
    public bool smoothBlending = true;
    [Tooltip("Blend distance for smooth transitions - larger = smoother gradient")]
    [Range(1f, 100f)]
    public float blendDistance = 10f;
    
    public override void Run(VectorEntity ve, UnityTile tile)
    {
        // Get the mesh filter
        MeshFilter meshFilter = ve.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;
        
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        
        // Create vertex colors array
        Color[] colors = new Color[vertices.Length];
        
        // Calculate colors based on height
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = vertices[i].y;
            colors[i] = CalculateHeightColor(height);
        }
        
        // Apply colors to mesh
        mesh.colors = colors;
    }
    
    private Color CalculateHeightColor(float height)
    {
        if (!smoothBlending)
        {
            // Hard transitions
            if (height < lowThreshold)
                return new Color(1, 0, 0); // Red = low
            else if (height < midThreshold)
                return new Color(0, 1, 0); // Green = mid
            else
                return new Color(0, 0, 1); // Blue = high
        }
        else
        {
            // Smooth blending using RGB channels
            float lowWeight = Mathf.Clamp01(1 - (height - lowThreshold) / blendDistance);
            float midWeight = Mathf.Clamp01(1 - Mathf.Abs(height - midThreshold) / blendDistance);
            float highWeight = Mathf.Clamp01((height - highThreshold) / blendDistance);
            
            // Normalize weights
            float totalWeight = lowWeight + midWeight + highWeight;
            if (totalWeight > 0)
            {
                lowWeight /= totalWeight;
                midWeight /= totalWeight;
                highWeight /= totalWeight;
            }
            
            return new Color(lowWeight, midWeight, highWeight);
        }
    }
}
