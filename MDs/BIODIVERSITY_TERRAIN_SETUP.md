# Biodiversity-Responsive Terrain Setup Guide

This guide explains TWO complementary systems that make terrain visually respond to biodiversity:

1. **Color/Saturation Changes** (Post-Processing Volumes) - Makes high biodiversity areas vibrant
2. **Density-Based Object Spawning** (NEW Prefab Spawner) - Spawns more objects in biodiverse areas

## Overview

### System 1: Post-Processing Saturation (BiodiversityVolumeSpawner)
Already working! See [BIODIVERSITY_VOLUME_SETUP.md](BIODIVERSITY_VOLUME_SETUP.md)
- High biodiversity → Vibrant, saturated colors
- Low biodiversity → Desaturated, gray colors

### System 2: Biodiversity-Driven Prefab Spawning (NEW BiodiversityPrefabSpawner)
This document focuses on the NEW prefab spawner:
- High biodiversity → MANY trees, plants, decorations
- Low biodiversity → FEW rocks, sparse vegetation

Both systems work together for maximum visual impact!

---

## 🚀 NEW System Setup: Biodiversity Prefab Spawner

### Step 1: Create the Modifier Asset

1. **Right-click in Project** → **Create → Mapbox → Modifiers → Biodiversity Prefab Spawner**
2. Name it: `BiodiversityPrefabSpawner`

### Step 2: Prepare Your Prefabs

Gather or create prefabs for different biodiversity levels:

**High Biodiversity** (lush areas):
- Trees, flowering plants, dense bushes, ferns

**Medium Biodiversity** (moderate areas):
- Small bushes, grass clumps, wildflowers

**Low Biodiversity** (barren areas):
- Rocks, dead trees, dry grass

### Step 3: Configure the Spawner

Select `BiodiversityPrefabSpawner` and set:

```
Prefab Categories:
├─ High Biodiversity Prefabs: [Your lush vegetation]
├─ Medium Biodiversity Prefabs: [Your moderate decoration]
├─ Low Biodiversity Prefabs: [Your sparse objects]
└─ Universal Prefabs: [Optional - works everywhere]

Spawn Density:
├─ Base Density: 0.05 (5 objects per 100m²)
├─ Max Density: 0.3 (high biodiversity)
└─ Min Density: 0.01 (low biodiversity)

Placement:
├─ Snap To Terrain: ✓ (raycast to surface)
├─ Random Rotation: ✓
├─ Scale Variation: 0.2 (±20%)
└─ Min/Max Scale: 0.8 - 1.5

Thresholds:
├─ High Biodiversity: 0.6 (Simpson's Index ≥ 0.6)
└─ Medium Biodiversity: 0.3

Performance:
├─ Max Objects Per Tile: 100
└─ Min Biodiversity To Spawn: 0.05
```

### Step 4: Add to Mapbox Terrain

1. Find your **AbstractMap** GameObject in Hierarchy
2. Navigate to **Terrain → Factories → Terrain Factory**
3. Find **Game Object Modifiers** section
4. Click **[+]** and add `BiodiversityPrefabSpawner`

### Step 5: Test!

1. Enter **Play Mode**
2. Wait for terrain generation
3. Check **Console** for logs:
   ```
   [BiodiversityPrefabSpawner] Tile biodiversity: 0.734
   [BiodiversityPrefabSpawner] Completed: 42/42 objects spawned
   ```
4. **Look around** - high biodiversity areas should have more objects!

---

## 🌍 Combined Effect (Both Systems Together)

When both systems work together:

### High Biodiversity Area (Simpson's Index 0.8)
- ✅ **Vibrant, saturated colors** (Post-processing volume)
- ✅ **Dense vegetation spawning** (Prefab spawner)
- **Result:** Lush, colorful, alive-looking terrain

### Low Biodiversity Area (Simpson's Index 0.2)
- ✅ **Desaturated, gray colors** (Post-processing volume)
- ✅ **Sparse rock/debris spawning** (Prefab spawner)
- **Result:** Barren, lifeless, desolate terrain

### Visual Progression
```
Player walks from low → high biodiversity:

Low Bio Area          Medium Bio Area       High Bio Area
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│  Gray/dull  │  →   │ Some color  │  →   │   Vibrant   │
│  Few rocks  │      │ Some bushes │      │ Many trees  │
│   Sparse    │      │  Moderate   │      │    Dense    │
└─────────────┘      └─────────────┘      └─────────────┘
Simpson: 0.2         Simpson: 0.5         Simpson: 0.9
```

---

## ⚙️ Tuning Tips

### Too Many Objects?
- Lower `baseDensity` (try 0.02)
- Lower `maxDensity` (try 0.15)
- Increase `minimumBiodiversityToSpawn` (try 0.1)

### Too Few Objects?
- Increase `baseDensity` (try 0.1)
- Increase `maxDensity` (try 0.5)
- Adjust `densityCurve` for steeper multiplier

### Objects Not Snapping to Terrain?
- Enable `snapToTerrain`
- Set `terrainLayerMask` correctly
- Ensure terrain has colliders

### Performance Issues?
- Lower `maxObjectsPerTile`
- Set `spawnRadius` to limit area
- Use simpler prefab models

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