using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


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