using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP-compatible biodiversity visualization using Volume system
/// This works with Universal Render Pipeline instead of Built-in
///
/// CODE LOGIC SUGGESTED BY: Claude Sonnet 4.5, Dec 2025
/// PROMPT: "Create a VolumeComponent for URP post-processing integration"
/// SOURCE: Unity URP VolumeComponent documentation
/// AI CONTRIBUTION: ~95% - VolumeComponent boilerplate structure
/// HUMAN CONTRIBUTION: ~5% - Parameter naming
/// </summary>
[System.Serializable, VolumeComponentMenu("Custom/Biodiversity Effect")]
public class BiodiversityVolumeComponent : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Effect intensity")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    
    [Tooltip("Show debug visualization")]
    public BoolParameter showDebug = new BoolParameter(false);
    
    public bool IsActive() => intensity.value > 0f;
    public bool IsTileCompatible() => false;
}