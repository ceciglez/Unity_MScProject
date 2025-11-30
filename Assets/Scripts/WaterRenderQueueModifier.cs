using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Water Render Queue Modifier")]
public class WaterRenderQueueModifier : GameObjectModifier
{
    [Header("Rendering")]
    [Tooltip("Render queue value (2000=Geometry, 3000=Transparent, higher=renders later)")]
    [Range(2000, 4000)]
    public int renderQueue = 3000;
    
    [Tooltip("Disable depth write for transparent water")]
    public bool disableDepthWrite = true;
    
    [Header("Debug")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve.GameObject == null)
            return;

        MeshRenderer renderer = ve.GameObject.GetComponent<MeshRenderer>();
        if (renderer == null || renderer.material == null)
        {
            if (debugMode)
                Debug.LogWarning($"[WaterRenderQueue] No renderer/material on {ve.GameObject.name}");
            return;
        }

        // Modify render queue to ensure water renders after terrain
        Material waterMaterial = renderer.material;
        waterMaterial.renderQueue = renderQueue;

        if (disableDepthWrite && waterMaterial.HasProperty("_ZWrite"))
        {
            waterMaterial.SetInt("_ZWrite", 0);
        }

        if (debugMode)
        {
            Debug.Log($"[WaterRenderQueue] Set render queue to {renderQueue} for {ve.GameObject.name}");
        }
    }
}
