# Biodiversity-Based Terrain Color System Setup

This system creates dynamic terrain coloration based on the density of iNaturalist observations, making areas with higher biodiversity appear more vibrant and saturated.

## Overview

The system works by:
1. **BiodiversityScoreManager**: Calculates observation density in grid cells around the player
2. **Enhanced Shaders**: Terrain shaders that respond to biodiversity data via saturation changes
3. **BiodiversityUI**: Optional UI to show current biodiversity information and controls

## 🚀 Quick Setup Instructions

### Step 1: Add BiodiversityScoreManager to Scene

1. **Create Empty GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name it `BiodiversityManager`

2. **Add Component**:
   - Select the GameObject
   - Add Component → `BiodiversityScoreManager`

3. **Configure Settings**:
   ```
   Cell Size: 50 (size of calculation areas)
   Max Calculation Distance: 200 (how far to calculate around player)
   Update Interval: 3 (seconds between updates)
   Min Saturation: 0.4 (low biodiversity areas)
   Max Saturation: 1.8 (high biodiversity areas)
   ```

### Step 2: Update Terrain Materials

**Option A: Use Enhanced HeightBasedTerrain Shader**
1. Select your terrain materials
2. Change shader to `Custom/HeightBasedTerrain` (now includes biodiversity)
3. Enable "Use Biodiversity Saturation"
4. Set "Biodiversity Effect Intensity" to 0.5-1.0

**Option B: Use Enhanced Mapbox Shaders**
1. Your Mapbox materials now automatically support biodiversity
2. Enable "Use Biodiversity Saturation" on materials
3. Set "Biodiversity Effect Intensity" to 0.8

**Option C: Use New BiodiversityTerrain Shader**
1. Create new material
2. Set shader to `Custom/BiodiversityTerrain`
3. Assign base texture and configure colors

### Step 3: Setup UI (Optional)

1. **Add to Canvas**:
   - Find your existing game UI canvas
   - Create Empty GameObject as child
   - Name it `BiodiversityUI`

2. **Add Component**:
   - Add `BiodiversityUI` script to the GameObject

3. **Create UI Panel** (optional):
   - Create UI → Panel as child
   - Add Text components for biodiversity info
   - Add Slider for saturation control
   - Add Toggle for enable/disable
   - Assign references in BiodiversityUI component

## 🎮 Player Controls

- **B Key**: Toggle biodiversity UI display
- The system automatically updates as you move around
- Areas with more observations become more vibrant
- Effect is most noticeable in areas with varied observation density

## ⚙️ Configuration Options

### BiodiversityScoreManager Settings

**Performance**:
- `Cell Size`: Larger = less detailed but better performance
- `Update Interval`: Higher = less frequent updates, better performance
- `Max Calculation Distance`: Smaller = less area calculated

**Visual Effect**:
- `Min/Max Saturation`: Control the saturation range
- `Saturation Smoothness`: How gradually effects change between areas

**Debugging**:
- `Show Debug Gizmos`: Visualize calculation grid in Scene view
- `Enable Debug Logging`: See calculation details in Console

### Shader Settings

**BiodiversityTerrain Shader**:
- `Biodiversity Intensity`: How strong the effect is
- `High/Low Biodiversity Colors`: Tint colors for different areas
- `Pulse Speed/Intensity`: Animated effects for high-biodiversity areas

**Enhanced Terrain Shaders**:
- `Use Biodiversity Saturation`: Enable/disable the effect
- `Biodiversity Effect Intensity`: Strength of saturation changes

## 🐛 Troubleshooting

**No Effect Visible**:
- Check that materials have "Use Biodiversity Saturation" enabled
- Verify BiodiversityScoreManager is finding observations
- Enable debug logging to see if calculations are working

**Performance Issues**:
- Increase Cell Size (50 → 100)
- Increase Update Interval (3 → 5 seconds)
- Reduce Max Calculation Distance (200 → 150)

**Effect Too Subtle**:
- Increase Biodiversity Effect Intensity
- Adjust Min/Max Saturation values for more contrast
- Try the dedicated BiodiversityTerrain shader for stronger effects

**Effect Too Strong**:
- Reduce Biodiversity Effect Intensity
- Bring Min/Max Saturation closer together (0.7 to 1.3)

## 🔧 Advanced Customization

### Custom Shader Integration

To add biodiversity effects to your own shaders:

1. **Add Properties**:
```hlsl
[Toggle] _UseBiodiversitySaturation ("Use Biodiversity Saturation", Float) = 1
_BiodiversityIntensity ("Biodiversity Effect Intensity", Range(0, 2)) = 0.8
```

2. **Add Variables**:
```hlsl
float _UseBiodiversitySaturation;
float _BiodiversityIntensity;
float _GlobalSaturation; // Set by BiodiversityScoreManager
```

3. **Add HSV Functions** (copy from BiodiversityTerrain.shader)

4. **Apply in Surface Function**:
```hlsl
// After calculating your base color
if (_UseBiodiversitySaturation > 0.5 && _GlobalSaturation > 0)
{
    float3 hsv = rgb2hsv(finalColor.rgb);
    float saturationMultiplier = lerp(1.0, _GlobalSaturation, _BiodiversityIntensity);
    hsv.y *= saturationMultiplier;
    hsv.y = saturate(hsv.y);
    finalColor.rgb = hsv2rgb(hsv);
}
```

### Region-Specific Effects

For more advanced implementations, you can modify `ApplyRegionalBiodiversityEffects()` in BiodiversityScoreManager to:
- Apply different effects to different terrain types
- Use texture blending for smoother transitions
- Implement gradient maps for complex color schemes

## 📊 How It Works

1. **Grid-Based Calculation**: The world is divided into cells, each tracking observation density
2. **Smoothing**: Neighboring cells influence each other for gradual transitions
3. **Normalization**: Scores are normalized relative to the area with highest density
4. **Shader Communication**: Global shader properties pass saturation values to materials
5. **HSV Manipulation**: Colors are converted to HSV space for saturation adjustment

The system is designed to be lightweight and responsive, focusing calculations around the player's position and caching results for optimal performance.