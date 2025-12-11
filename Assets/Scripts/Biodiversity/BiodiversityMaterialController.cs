using UnityEngine;

/// <summary>
/// Material-based biodiversity visualization controller
/// Applies saturation effects directly to terrain materials without post-processing
///
/// FUNCTIONALITY:
/// - Alternative to full-screen post-processing effects
/// - Reads global biodiversity saturation from BiodiversityScoreManager
/// - Applies saturation to assigned materials or auto-detected terrain materials
/// - Modifies material _Saturation and _BaseColor properties
/// - Supports manual override for testing
///
/// APPROACH:
/// - Direct material property modification (no camera effects)
/// - Compatible with URP and built-in render pipeline
/// - Lighter weight than full-screen post-processing
/// - Affects only assigned materials (more control)
///
/// USE CASE:
/// - When you want material-level saturation control
/// - Alternative to BiodiversityVolumeSpawner (local volumes)
/// - Good for specific terrain pieces or objects
/// - Simpler than full post-processing stack
///
/// SHADER PROPERTIES:
/// - _GlobalDiversitySaturation: Read from BiodiversityScoreManager
/// - _Saturation: Applied to material
/// - _BaseColor: Modified based on saturation
///
/// CODE LOGIC SUGGESTED BY: Claude Sonnet 4.5, Dec 2025
/// PROMPT: "Create a script that applies biodiversity saturation directly to terrain materials"
/// SOURCE:
/// - Unity Material system documentation
/// - Shader property manipulation via MaterialPropertyBlock
///
/// AI CONTRIBUTION: ~65% - Auto-material discovery, shader property updates, global saturation reading
/// HUMAN CONTRIBUTION: ~35% - Target material selection, saturation multiplier ranges, update frequency
/// </summary>
public class BiodiversityMaterialController : MonoBehaviour
{
    [Header("🌿 Material-Based Biodiversity")]
    [Tooltip("Materials to apply biodiversity effects to")]
    public Material[] targetMaterials;

    [Header("🎚️ Effect Controls")]
    [Range(0.1f, 5f)]
    [Tooltip("Multiplier to amplify biodiversity effect")] 
    public float biodiversityMultiplier = 1.0f;

    [Header("🎨 Override Controls")]
    [Range(0f, 3f)]
    [Tooltip("Manual saturation override (0 = use calculated biodiversity)")]
    public float manualSaturation = 0f;

    [Header("📊 Info")]
    [SerializeField] private int materialsAffected = 0;
    [SerializeField] private float currentBiodiversityScore = 0f;

    private BiodiversityScoreManager biodiversityManager;
    private static readonly int GlobalSaturationProperty = Shader.PropertyToID("_GlobalDiversitySaturation");
    
    void Start()
    {
        biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
        
        if (biodiversityManager == null)
        {
            Debug.LogError("BiodiversityMaterialController: No BiodiversityScoreManager found!");
            return;
        }
        
        Debug.Log($"✅ BiodiversityMaterialController ready - will control {targetMaterials.Length} materials");
        
        // Find all terrain materials automatically if none assigned
        if (targetMaterials.Length == 0)
        {
            FindTerrainMaterials();
        }
    }
    
    void Update()
    {
        if (biodiversityManager == null) return;

        // Get current biodiversity score
        currentBiodiversityScore = Shader.GetGlobalFloat(GlobalSaturationProperty);

        if (manualSaturation > 0f)
        {
            ApplyManualSaturation();
        }
        else
        {
            ApplyBiodiversitySaturation();
        }
    }

    private void ApplyBiodiversitySaturation()
    {
        materialsAffected = 0;
        float effectiveSaturation = Mathf.Clamp(currentBiodiversityScore * biodiversityMultiplier, 0f, 3f);

        foreach (var material in targetMaterials)
        {
            if (material != null)
            {
                if (material.HasProperty("_Saturation"))
                {
                    material.SetFloat("_Saturation", effectiveSaturation);
                    materialsAffected++;
                }
                if (material.HasProperty("_BaseColor"))
                {
                    Color baseColor = material.GetColor("_BaseColor");
                    Color saturatedColor = Color.Lerp(Color.grey, baseColor, effectiveSaturation);
                    material.SetColor("_BaseColor", saturatedColor);
                    materialsAffected++;
                }
            }
        }
    }
    
    private void FindTerrainMaterials()
    {
        // Find all renderers in scene and collect their materials
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        System.Collections.Generic.HashSet<Material> uniqueMaterials = new System.Collections.Generic.HashSet<Material>();
        
        foreach (var renderer in allRenderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material != null && material.name.Contains("Terrain"))
                {
                    uniqueMaterials.Add(material);
                }
            }
        }
        
        targetMaterials = new Material[uniqueMaterials.Count];
        uniqueMaterials.CopyTo(targetMaterials);
        
        Debug.Log($"🔍 Auto-found {targetMaterials.Length} terrain materials");
    }
    
    private void ApplyManualSaturation()
    {
        materialsAffected = 0;
        
        foreach (var material in targetMaterials)
        {
            if (material != null)
            {
                // Apply to built-in properties
                if (material.HasProperty("_Saturation"))
                {
                    material.SetFloat("_Saturation", manualSaturation);
                    materialsAffected++;
                }
                
                // Apply to URP properties
                if (material.HasProperty("_BaseColor"))
                {
                    Color baseColor = material.GetColor("_BaseColor");
                    float gray = baseColor.grayscale;
                    Color saturatedColor = Color.Lerp(Color.grey, baseColor, manualSaturation);
                    material.SetColor("_BaseColor", saturatedColor);
                    materialsAffected++;
                }
            }
        }
        
        // Also set global property
        Shader.SetGlobalFloat(GlobalSaturationProperty, manualSaturation);
    }
    
    void OnGUI()
    {
        if (biodiversityManager == null) return;

        // Simple debug UI
        GUILayout.BeginArea(new Rect(10, 10, 320, 180));
        GUILayout.Box("🌿 Biodiversity Control");

        GUILayout.Label($"Current Score: {currentBiodiversityScore:F2}");
        GUILayout.Label($"Materials Affected: {materialsAffected}");
        GUILayout.Label($"Manual Override: {manualSaturation:F2}");
        GUILayout.Label($"Biodiversity Multiplier: {biodiversityMultiplier:F2}");

        biodiversityMultiplier = GUILayout.HorizontalSlider(biodiversityMultiplier, 0.1f, 5f);

        // Quick test buttons
        if (GUILayout.Button("Test: Low Biodiversity"))
        {
            manualSaturation = 0.2f;
        }

        if (GUILayout.Button("Test: High Biodiversity"))
        {
            manualSaturation = 2.5f;
        }

        if (GUILayout.Button("Reset to Auto"))
        {
            manualSaturation = 0f;
        }

        GUILayout.EndArea();
    }
}