using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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