using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP ScriptableRendererFeature for full-screen biodiversity post-processing effects
/// Applies saturation and color grading based on biodiversity hotspots
///
/// FUNCTIONALITY:
/// - Custom URP render pass that executes before post-processing
/// - Passes biodiversity hotspot data to shader (up to 20 hotspots)
/// - Applies single full-screen blit with biodiversity material
/// - Modulates saturation based on distance to hotspots
/// - Creates "spotlight" effect around biodiverse areas
///
/// SHADER DATA PASSED:
/// - _HotspotPositions[]: Array of hotspot world positions (xyz) + Simpson's Index (w)
/// - _HotspotRadii[]: Radius of effect for each hotspot
/// - _HotspotCount: Number of active hotspots
/// - _GlobalSaturation: Overall biodiversity saturation value
/// - _FalloffPower: Edge sharpness control (default 2.0)
///
/// INTEGRATION:
/// - Add to URP Renderer asset (Forward Renderer)
/// - Assign biodiversity shader material in settings
/// - Automatically queries BiodiversityScoreManager each frame
/// - Works with BiodiversityVolumeSpawner for local effects
///
/// TECHNICAL APPROACH:
/// - Inherits from ScriptableRendererFeature (URP architecture)
/// - Creates BiodiversityFullScreenPass render pass
/// - Executes at RenderPassEvent.BeforeRenderingPostProcessing
/// - Uses RTHandle for temporary render targets
/// - Single blit operation (performance optimized)
///
/// SOURCE:
/// - Unity URP ScriptableRendererFeature documentation
/// - Reference: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/renderer-features/intro-to-scriptable-renderer-features.html
/// - Custom shader integration for biodiversity data
///
/// AI CONTRIBUTION: ~75% - URP integration, render pass setup, shader data passing, hotspot system
/// HUMAN CONTRIBUTION: ~25% - Material assignment, render event timing, performance tuning
/// </summary>
public class BiodiversityFullScreenFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material material;
    }

    public Settings settings = new Settings();
    private BiodiversityFullScreenPass fullScreenPass;

    public override void Create()
    {
        fullScreenPass = new BiodiversityFullScreenPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material != null && fullScreenPass != null)
        {
            fullScreenPass.Setup();
            renderer.EnqueuePass(fullScreenPass);
        }
    }
}

public class BiodiversityFullScreenPass : ScriptableRenderPass
{
    private BiodiversityFullScreenFeature.Settings settings;
    private Material material;
    private RTHandle tempColorTarget;
    
    public BiodiversityFullScreenPass(BiodiversityFullScreenFeature.Settings settings)
    {
        this.settings = settings;
        this.material = settings.material;
        renderPassEvent = settings.renderPassEvent;
    }

    public void Setup()
    {
        // Update material with biodiversity data
        UpdateBiodiversityProperties();
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        var descriptor = cameraTextureDescriptor;
        descriptor.depthBufferBits = 0;
        RenderingUtils.ReAllocateIfNeeded(ref tempColorTarget, descriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null) return;

        var cmd = CommandBufferPool.Get("BiodiversityFullScreen");
        
        var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        
        // Blit with biodiversity effect
        Blit(cmd, cameraTarget, tempColorTarget, material, 0);
        Blit(cmd, tempColorTarget, cameraTarget);
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private void UpdateBiodiversityProperties()
    {
        if (material == null) return;

        var biodiversityManager = Object.FindObjectOfType<BiodiversityScoreManager>();
        if (biodiversityManager != null)
        {
            try
            {
                var hotspots = biodiversityManager.GetBiodiversityHotspots();
                
                // Get global saturation from existing system
                float globalSaturation = Shader.GetGlobalFloat("_GlobalDiversitySaturation");
                
                // Basic properties
                material.SetFloat("_GlobalSaturation", globalSaturation);
                material.SetInt("_ShowDebug", 0);
                
                // Advanced hotspot data if shader supports it
                if (material.HasProperty("_HotspotCount"))
                {
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
                    
                    material.SetVectorArray("_HotspotPositions", hotspotPositions);
                    material.SetFloatArray("_HotspotRadii", hotspotRadii);
                    material.SetInt("_HotspotCount", count);
                    material.SetFloat("_FalloffPower", 2f);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"BiodiversityFullScreenPass: {e.Message}");
            }
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        tempColorTarget?.Release();
    }
}