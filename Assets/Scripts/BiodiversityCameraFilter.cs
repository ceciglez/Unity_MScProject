using UnityEngine;

/// <summary>
/// Camera Filter for Biodiversity Visualization
/// Add this to your Main Camera to apply biodiversity saturation effects
/// Works like Instagram filters but based on real biodiversity data!
/// </summary>
public class BiodiversityCameraFilter : MonoBehaviour
{
    [Header("🌿 Biodiversity Camera Filter")]
    [Tooltip("Material with the biodiversity shader - drag your material here")]
    public Material biodiversityMaterial;
    
    [Header("🎨 Visual Settings")]
    [Range(0f, 3f)]
    [Tooltip("How strong the biodiversity effect is (0 = no effect, 2+ = dramatic)")]
    public float effectStrength = 1f;
    
    [Range(0.5f, 5f)]
    [Tooltip("How sharply the effect fades with distance from hotspots")]
    public float edgeSharpness = 2f;
    
    [Header("🐛 Debug")]
    [Tooltip("Show pink overlay where biodiversity is detected")]
    public bool showDebugOverlay = false;
    
    [Header("📊 Runtime Info")]
    [SerializeField] private int hotspotsDetected = 0;
    [SerializeField] private bool biodiversityManagerFound = false;
    
    private BiodiversityScoreManager biodiversityManager;
    
    void Start()
    {
        // Find the biodiversity calculation system
        biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
        biodiversityManagerFound = biodiversityManager != null;
        
        // Validation and helpful messages
        if (biodiversityMaterial == null)
        {
            Debug.LogError("🚨 BiodiversityCameraFilter: No material assigned!\n" +
                          "Create a material with shader 'Custom/BiodiversityDebugTest' and drag it here.");
        }
        else
        {
            Debug.Log($"✅ Biodiversity camera filter ready! Material: {biodiversityMaterial.name}");
            Debug.Log($"   Shader: {biodiversityMaterial.shader.name}");
            
            // Test material assignment immediately
            biodiversityMaterial.SetFloat("_GlobalSaturation", 2.0f);
            biodiversityMaterial.SetInt("_ShowDebug", 1);
            Debug.Log("🧪 Set test values: Saturation=2.0, Debug=ON");
        }
        
        if (!biodiversityManagerFound)
        {
            Debug.LogWarning("⚠️ BiodiversityScoreManager not found! " +
                           "The filter will work with basic saturation only.");
        }
        else
        {
            Debug.Log("✅ Connected to biodiversity calculation system");
        }
    }
    
    /// <summary>
    /// This is the magic! Unity calls this after the camera renders the scene
    /// We intercept the image and apply our biodiversity effects
    /// </summary>
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // If something is wrong, just pass through without effects
        if (biodiversityMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Update the material with current biodiversity data
        UpdateBiodiversityData();
        
        // Apply the filter! This transforms the camera image
        Graphics.Blit(source, destination, biodiversityMaterial);
    }
    
    private void UpdateBiodiversityData()
    {
        // Basic shader properties that any shader can use
        biodiversityMaterial.SetFloat("_GlobalSaturation", effectStrength);
        biodiversityMaterial.SetInt("_ShowDebug", showDebugOverlay ? 1 : 0);
        
        // If we have the biodiversity system, get more complex data
        if (biodiversityManager != null)
        {
            try
            {
                var hotspots = biodiversityManager.GetBiodiversityHotspots();
                hotspotsDetected = hotspots.Count;
                
                // Only set advanced properties if the shader supports them
                if (biodiversityMaterial.HasProperty("_HotspotCount"))
                {
                    // Prepare data for advanced shaders
                    Vector4[] hotspotPositions = new Vector4[20];
                    float[] hotspotRadii = new float[20];
                    
                    int count = Mathf.Min(hotspots.Count, 20);
                    for (int i = 0; i < count; i++)
                    {
                        var hotspot = hotspots[i];
                        hotspotPositions[i] = new Vector4(
                            hotspot.position.x, 
                            hotspot.position.y, 
                            hotspot.position.z, 
                            hotspot.simpsonsIndex
                        );
                        hotspotRadii[i] = hotspot.radius;
                    }
                    
                    // Send data to advanced shaders
                    biodiversityMaterial.SetVectorArray("_HotspotPositions", hotspotPositions);
                    biodiversityMaterial.SetFloatArray("_HotspotRadii", hotspotRadii);
                    biodiversityMaterial.SetInt("_HotspotCount", count);
                    biodiversityMaterial.SetFloat("_FalloffPower", edgeSharpness);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"BiodiversityCameraFilter: Error getting hotspot data: {e.Message}");
                hotspotsDetected = 0;
            }
        }
    }
    
    void Update()
    {
        // Real-time updates for the inspector sliders
        if (biodiversityMaterial != null)
        {
            biodiversityMaterial.SetFloat("_GlobalSaturation", effectStrength);
            biodiversityMaterial.SetInt("_ShowDebug", showDebugOverlay ? 1 : 0);
            
            // Debug logging for troubleshooting
            if (Time.frameCount % 60 == 0) // Every 60 frames
            {
                Debug.Log($"🎛️ Filter Update: Saturation={effectStrength}, Debug={showDebugOverlay}, Hotspots={hotspotsDetected}");
            }
            
            if (biodiversityMaterial.HasProperty("_FalloffPower"))
            {
                biodiversityMaterial.SetFloat("_FalloffPower", edgeSharpness);
            }
        }
    }
}