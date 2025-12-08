using UnityEngine;

/// <summary>
/// Setup guide for full-screen biodiversity effects in URP
/// This will affect EVERYTHING: sky, terrain, objects, UI - the whole screen!
/// </summary>
public class BiodiversityFullScreenSetup : MonoBehaviour
{
    [TextArea(15, 30)]
    public string setupInstructions = @"
🌍 FULL-SCREEN BIODIVERSITY VISUALIZATION FOR URP 🌍

This affects EVERYTHING on screen - sky, terrain, objects, UI, particles!
Creates a global environmental mood based on real biodiversity data.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 URP RENDERER FEATURE SETUP:

1. FIND YOUR URP RENDERER ASSET:
   • Look in Settings/ folder for 'UniversalRP-HighQuality_Renderer'
   • OR Window → Rendering → URP Settings

2. ADD RENDERER FEATURE:
   • Click 'Add Renderer Feature'
   • Select 'Biodiversity Full Screen Feature'
   • Set Render Pass Event to 'Before Rendering Post Processing'

3. CREATE MATERIAL:
   • Right-click → Create → Material
   • Name: 'BiodiversityFullScreenMaterial'
   • Shader: 'Custom/BiodiversityFullScreen'

4. ASSIGN MATERIAL:
   • Drag material to Renderer Feature's 'Material' slot

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🎨 VISUAL EFFECTS:

LOW BIODIVERSITY (Simpson's Index 0-0.3):
   → Heavy desaturation (gray/brown world)
   → Slightly darker atmosphere
   → Post-apocalyptic mood

MEDIUM BIODIVERSITY (0.3-0.7):
   → Normal colors with slight desaturation
   → Neutral environmental mood

HIGH BIODIVERSITY (0.7-1.0):
   → Enhanced saturation and brightness
   → Vibrant, lush atmosphere
   → Living world feeling

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚡ FEATURES:

✅ GLOBAL SCREEN EFFECT
   • Affects sky, clouds, terrain, objects, particles
   • Creates environmental atmosphere
   • Works with all URP features

✅ REAL-TIME BIODIVERSITY
   • Uses Simpson's Index from observation data
   • Updates automatically as you move
   • Reflects actual species diversity

✅ DRAMATIC MOOD CHANGES
   • Low biodiversity = desolate, gray world
   • High biodiversity = vibrant, colorful environment
   • Smooth transitions between areas

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔍 TROUBLESHOOTING:

❌ NO RENDERER FEATURE OPTION:
   1. Make sure scripts compiled (check Console)
   2. Try restarting Unity
   3. Check you're looking in URP Renderer Asset, not Pipeline Asset

❌ NO VISUAL EFFECT:
   1. Verify material assigned to Renderer Feature
   2. Check BiodiversityScoreManager is in scene
   3. Look for console messages about hotspots
   4. Try moving to different areas

❌ TOO SUBTLE:
   • Effects might be subtle with current data
   • Check BiodiversityScoreManager settings
   • Look for 'Global saturation' value in console

This creates a powerful environmental storytelling effect where the visual
mood of your entire world reflects the biodiversity of the current area!
";

    [Header("Quick Info")]
    [SerializeField] private bool rendererFeatureFound = false;
    [SerializeField] private float currentGlobalSaturation = 1f;
    
    void Start()
    {
        Debug.Log("=== FULL-SCREEN BIODIVERSITY SETUP LOADED ===");
        Debug.Log("This will affect EVERYTHING on screen once set up!");
        
        CheckCurrentState();
    }

    void Update()
    {
        // Track current biodiversity effect
        currentGlobalSaturation = Shader.GetGlobalFloat("_GlobalDiversitySaturation");
    }

    void CheckCurrentState()
    {
        var biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
        
        if (biodiversityManager == null)
        {
            Debug.LogWarning("⚠️ BiodiversityScoreManager not found in scene!");
            return;
        }

        Debug.Log("✅ BiodiversityScoreManager found");
        Debug.Log($"📊 Current global saturation: {currentGlobalSaturation:F2}");
        
        if (currentGlobalSaturation == 1f)
        {
            Debug.Log("💡 Saturation = 1.0 suggests no biodiversity effects yet");
            Debug.Log("   Wait for observations to load or move to different area");
        }
        else if (currentGlobalSaturation < 0.7f)
        {
            Debug.Log("🏜️ Low biodiversity detected - world should appear desaturated");
        }
        else
        {
            Debug.Log("🌿 High biodiversity detected - world should appear vibrant");
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 200, 300, 100));
        GUILayout.Box("🌍 Full-Screen Biodiversity");
        
        GUILayout.Label($"Global Saturation: {currentGlobalSaturation:F2}");
        
        if (currentGlobalSaturation < 0.5f)
            GUILayout.Label("Status: Desolate World");
        else if (currentGlobalSaturation < 0.8f)
            GUILayout.Label("Status: Normal World");
        else
            GUILayout.Label("Status: Vibrant World");
        
        GUILayout.EndArea();
    }
}