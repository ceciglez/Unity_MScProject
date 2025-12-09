using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.MeshGeneration.Data;

[CreateAssetMenu(menuName = "Mapbox/Modifiers/Water Height Offset Modifier")]
public class WaterHeightOffsetModifier : GameObjectModifier
{
    [Header("Height Offset")]
    [Tooltip("Height offset in meters to raise water above terrain (0.1-1.0m recommended)")]
    [Range(0.01f, 5f)]
    public float heightOffset = 0.2f;
    
    [Header("Debug")]
    public bool debugMode = false;

    public override void Run(VectorEntity ve, UnityTile tile)
    {
        if (ve.GameObject == null)
            return;

        // Offset the entire water feature upward
        Vector3 currentPosition = ve.GameObject.transform.localPosition;
        ve.GameObject.transform.localPosition = new Vector3(
            currentPosition.x, 
            currentPosition.y + heightOffset, 
            currentPosition.z
        );

        if (debugMode)
        {
            Debug.Log($"[WaterHeightOffset] Raised water '{ve.GameObject.name}' by {heightOffset}m");
        }
    }
}
