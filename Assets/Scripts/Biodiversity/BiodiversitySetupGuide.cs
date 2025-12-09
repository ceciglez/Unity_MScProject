using UnityEngine;

/// <summary>
/// Simple setup guide for the Biodiversity System
/// Add this to an empty GameObject to see setup instructions in the inspector
/// </summary>
public class BiodiversitySetupGuide : MonoBehaviour
{
    [Header("🌱 BIODIVERSITY SYSTEM SETUP GUIDE 🌱")]
    [Space(10)]
    
    [TextArea(20, 30)]
    [SerializeField] private string setupInstructions = 
        "1. ADD BIODIVERSITY MANAGER:\n" +
        "   • Create empty GameObject\n" +
        "   • Add 'BiodiversityScoreManager' component\n" +
        "   • Enable 'Show Debug Gizmos' and 'Enable Debug Logging'\n" +
        "   • Set 'Force Constant Updates' to true for testing\n\n" +
        
        "2. ASSIGN MATERIALS (IMPORTANT!):\n" +
        "   • Drag your terrain/ground materials to 'Test Materials' array\n" +
        "   • OR drag terrain renderers to 'Test Renderers' array\n" +
        "   • These will receive the biodiversity effects\n\n" +
        
        "3. UPDATE MATERIAL SHADERS:\n" +
        "   • Change material shader to:\n" +
        "     - 'Custom/BiodiversityTerrain' (new dedicated shader)\n" +
        "     - OR 'Custom/HeightBasedTerrain' (enhanced existing)\n" +
        "     - OR 'Mapbox/MapboxStyles' (enhanced existing)\n" +
        "   • Enable 'Use Simpson's Index' on materials\n" +
        "   • Set 'Diversity Effect Intensity' to 2.0+\n\n" +
        
        "4. ENSURE OBSERVATIONS ARE LOADED:\n" +
        "   • Make sure INaturalistMapController is in scene\n" +
        "   • Load some iNaturalist observations\n" +
        "   • Wait for them to appear as GameObjects\n\n" +
        
        "5. TEST THE SYSTEM:\n" +
        "   • Press 'U' key to force biodiversity update\n" +
        "   • Check Console for detailed debug logs\n" +
        "   • Enable Scene Gizmos to see biodiversity grid\n" +
        "   • Look for yellow hotspot markers\n\n" +
        
        "6. DEBUGGING:\n" +
        "   • Console shows observation counts and species diversity\n" +
        "   • Scene gizmos show biodiversity grid with colors\n" +
        "   • Yellow wireframes = biodiversity hotspots\n" +
        "   • Check '_GlobalDiversitySaturation' in shader globals\n\n" +
        
        "7. TROUBLESHOOTING:\n" +
        "   • No effect? Check materials have correct shaders\n" +
        "   • No observations? Wait for iNaturalist to load\n" +
        "   • Check Console for error messages\n" +
        "   • Verify materials are assigned to Test Materials/Renderers";
    
    [Header("Quick Test")]
    [Tooltip("Click this to force an immediate biodiversity update")]
    public bool triggerUpdate = false;
    
    void Update()
    {
        if (triggerUpdate)
        {
            triggerUpdate = false;
            
            BiodiversityScoreManager manager = FindObjectOfType<BiodiversityScoreManager>();
            if (manager != null)
            {
                Debug.Log("[BiodiversitySetupGuide] 🔄 TRIGGERING MANUAL UPDATE");
                manager.UpdateBiodiversityScores();
            }
            else
            {
                Debug.LogError("[BiodiversitySetupGuide] ❌ No BiodiversityScoreManager found in scene!");
            }
        }
    }
    
    void OnDrawGizmos()
    {
        // Draw a helpful indicator in Scene view
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 5f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 7f, "🌱 Biodiversity Setup Guide");
        #endif
    }
}