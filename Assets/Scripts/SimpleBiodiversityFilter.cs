using UnityEngine;

/// <summary>
/// Ultra-simple biodiversity camera filter with minimal dependencies
/// Just basic saturation adjustment - guaranteed to compile!
/// </summary>
public class SimpleBiodiversityFilter : MonoBehaviour
{
    [Header("🌿 Basic Biodiversity Filter")]
    public Material filterMaterial;
    
    [Range(0f, 3f)]
    public float saturation = 1f;
    
    public bool showDebug = false;
    
    void Start()
    {
        if (filterMaterial == null)
        {
            Debug.LogError("No material assigned! Use shader 'Custom/BiodiversityBasic'");
        }
        else
        {
            Debug.Log("✅ Simple biodiversity filter ready!");
        }
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (filterMaterial != null)
        {
            filterMaterial.SetFloat("_GlobalSaturation", saturation);
            filterMaterial.SetInt("_ShowDebug", showDebug ? 1 : 0);
            Graphics.Blit(source, destination, filterMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}