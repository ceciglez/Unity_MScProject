using UnityEngine;

/// <summary>
/// Simple marker component that grass spawners can check for to avoid spawning grass
/// </summary>
public class GrassExclusionMarker : MonoBehaviour
{
    [Header("Exclusion Settings")]
    public float exclusionRadius = 5f;
    
    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    
    private void OnDrawGizmosSelected()
    {
        if (showGizmos)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, exclusionRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, exclusionRadius);
        }
    }
    
    /// <summary>
    /// Check if a point is within this exclusion zone
    /// </summary>
    public bool IsPositionExcluded(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= exclusionRadius;
    }
    
    /// <summary>
    /// Check if a bounds overlaps with this exclusion zone
    /// </summary>
    public bool DoesBoundsOverlap(Bounds bounds)
    {
        float distance = Vector3.Distance(transform.position, bounds.center);
        float combinedRadius = exclusionRadius + bounds.extents.magnitude;
        return distance <= combinedRadius;
    }
}