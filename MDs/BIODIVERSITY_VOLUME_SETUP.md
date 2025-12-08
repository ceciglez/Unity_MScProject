# Biodiversity Volume Post-Processing Setup Guide

## Overview

This guide explains how to set up the **BiodiversityVolumeSpawner** system that creates visual feedback for biodiversity through Unity's Post-Processing Stack. Areas with high biodiversity (high Simpson's Index) appear vibrant and saturated, while low-diversity areas appear desaturated.

**System:** Grid-based local volumes + global baseline
**Effect:** Color saturation based on biodiversity score
**Performance:** Optimized with max volume limits and spawn radius

---

## Architecture

```
Global Volume (Priority: 0)
├─ Low saturation baseline (-0.5)
└─ Applied everywhere

Local Volumes (Priority: 5)
├─ Spawned per biodiversity grid cell
├─ High priority overrides global
└─ Saturation = f(Simpson's Index)
    ├─ Simpson's Index 0.0 → Saturation -0.8 (very desaturated)
    ├─ Simpson's Index 0.5 → Saturation -0.15 (medium)
    └─ Simpson's Index 1.0 → Saturation +0.5 (vibrant)

Player enters high biodiversity area
→ Local volume takes effect
→ Colors become vibrant
→ Visual feedback: "This area is biodiverse!"
```

---

## Part 1: Unity Post-Processing Setup

### Step 1: Install URP Post-Processing (if not already)

1. **Window → Package Manager**
2. Search for **"Universal RP"**
3. Ensure version 10.0+ is installed

### Step 2: Create Volume Profile Asset

1. **Right-click in Assets** → **Create → Volume Profile**
2. Name it: `BiodiversityVolumeProfile`
3. Select the profile
4. Click **"Add Override"** → **Post-processing** → **Color Adjustments**
5. Enable the **Saturation** parameter (checkmark)
6. Leave saturation at 0 (script will override per-volume)

### Step 3: Create Volume Prefab

1. **Right-click in Hierarchy** → **Create Empty**
2. Name it: `BiodiversityVolumePrefab`
3. **Add Component** → **Volume** (from Rendering)
4. Configure Volume component:
   - **Is Global:** ☐ (unchecked)
   - **Profile:** Assign `BiodiversityVolumeProfile`
   - **Priority:** 5
   - **Weight:** 1
   - **Blend Distance:** 25
5. **Add Component** → **Box Collider**
   - **Is Trigger:** ☑ (checked)
   - Size: Will be set by script
6. Drag to **Assets** folder to create prefab
7. Delete from Hierarchy

### Step 4: Create Global Baseline Volume

1. **Right-click in Hierarchy** → **Volume → Global Volume**
2. Name it: `GlobalBaselineVolume`
3. **Create new Volume Profile** for it: `GlobalBaselineProfile`
4. Add **Color Adjustments** override
5. Set **Saturation: -0.5** (desaturated baseline)
6. Configure:
   - **Is Global:** ☑ (checked)
   - **Priority:** 0 (low, so local volumes override)
   - **Weight:** 1

---

## Part 2: BiodiversityVolumeSpawner Setup

### Step 1: Add Component to Scene

1. Select your **BiodiversityScoreManager** GameObject (or create new empty)
2. **Add Component** → **BiodiversityVolumeSpawner**
3. The script will auto-find BiodiversityScoreManager if on same GameObject

### Step 2: Configure Inspector Settings

#### References
- **Biodiversity Manager:** Auto-found or manually assign
- **Volume Prefab:** Assign `BiodiversityVolumePrefab`

#### Saturation Settings
```
Low Biodiversity Saturation: -0.8
  (Very desaturated, gray - for monoculture areas)

High Biodiversity Saturation: +0.5
  (Vibrant, colorful - for diverse areas)

Global Baseline Saturation: -0.5
  (Matches global volume baseline)
```

**Effect Curve:**
- Simpson's Index 0.0 → Saturation -0.8 (very gray)
- Simpson's Index 0.3 → Saturation -0.5 (baseline)
- Simpson's Index 0.6 → Saturation -0.05 (slight color)
- Simpson's Index 1.0 → Saturation +0.5 (vibrant)

#### Volume Settings
```
Volume Priority: 5
  (Higher than global, lower than UI/critical effects)

Blend Distance: 25m
  (Smooth transition between volumes)

Volume Height: 100m
  (Tall enough to cover terrain and player)
```

#### Update Settings
```
Auto Update: ☑
  (Automatically refresh when biodiversity recalculates)

Update Interval: 5 seconds
  (0 = only update on biodiversity changes)
```

#### Performance
```
Max Volumes: 100
  (Limits total volumes for performance)

Spawn Radius: 0m (unlimited)
  (Or 200m to only spawn near player)
```

#### Debugging
```
Enable Debug Logging: ☑
  (Shows spawn info in Console)

Show Volume Gizmos: ☑
  (Visualize volumes in Scene view)

Manual Update Key: V
  (Press V in Play Mode to force update)
```

---

## Part 3: Integration with BiodiversityScoreManager

The system automatically integrates with BiodiversityScoreManager:

1. **Same Grid Alignment:**
   - Uses `biodiversityManager.cellSize` (default 50m)
   - Spawns one volume per grid cell
   - Perfect alignment with biodiversity calculations

2. **Data Source:**
   - Calls `biodiversityManager.GetBiodiversityHotspots()`
   - Gets Simpson's Index per grid cell
   - Maps to saturation value

3. **Update Synchronization:**
   - Auto-updates when biodiversity recalculates (updateInterval)
   - Manual trigger with `V` key
   - Can be called from other scripts: `volumeSpawner.SpawnBiodiversityVolumes()`

---

## Part 4: Testing & Validation

### Test 1: Verify Global Baseline
```
Play Mode:
1. No observations loaded yet
2. All terrain should be desaturated (gray-ish)
3. Global volume at priority 0 is active
→ Expected: Low saturation everywhere ✓
```

### Test 2: Load Observations
```
Play Mode:
1. iNaturalist observations spawn
2. BiodiversityScoreManager calculates Simpson's Index
3. BiodiversityVolumeSpawner spawns local volumes
4. Console shows: "✅ Spawned N volumes"
→ Expected: Volume count > 0 ✓
```

### Test 3: Enter High Biodiversity Area
```
Play Mode:
1. Move player toward green cubes in Scene view (gizmos)
2. As you enter, colors should become more vibrant
3. Check Console for Simpson's Index values
→ Expected: High index = high saturation ✓
```

### Test 4: Enter Low Biodiversity Area
```
Play Mode:
1. Move player to area with few observations
2. Colors should remain desaturated (or become more gray)
3. May fall back to global baseline
→ Expected: Low index = low saturation ✓
```

### Test 5: Gizmo Visualization
```
Scene View (Play Mode):
1. Enable Gizmos
2. Green cubes = high biodiversity volumes
3. Red cubes = low biodiversity volumes
4. Yellow wireframes = volume boundaries
→ Expected: Visual alignment with observations ✓
```

### Test 6: Manual Update
```
Play Mode:
1. Press 'V' key
2. Console shows: "Manual update triggered!"
3. Volumes respawn/update
→ Expected: Fresh spawn based on current data ✓
```

---

## Part 5: Common Issues & Solutions

### Issue 1: No Volumes Spawning
**Symptoms:** Console says "No biodiversity hotspots available yet"

**Solutions:**
1. Check BiodiversityScoreManager has observations
2. Press `U` to force biodiversity update
3. Wait for observations to load (2-3 seconds after spawn)
4. Check `biodiversityManager.cellSize` matches expected area

### Issue 2: No Visual Effect
**Symptoms:** Volumes spawn but colors don't change

**Solutions:**
1. Verify Global Volume exists with low saturation baseline
2. Check Volume Prefab has **VolumeProfile** assigned
3. Ensure Color Adjustments override is enabled in profile
4. Check Volume Priority (local should be > global)
5. Verify player is actually entering volume colliders

### Issue 3: Performance Issues
**Symptoms:** Frame rate drops, lag when spawning volumes

**Solutions:**
1. Lower `maxVolumes` (try 50)
2. Set `spawnRadius` to 200m (only spawn near player)
3. Increase `updateInterval` to 10 seconds
4. Disable `autoUpdate` and trigger manually

### Issue 4: Harsh Transitions
**Symptoms:** Sudden color changes when entering/leaving volumes

**Solutions:**
1. Increase `blendDistance` (try 50m)
2. Adjust `volumeHeight` to ensure overlap
3. Lower saturation range contrast (try -0.5 to 0.2 instead of -0.8 to 0.5)

### Issue 5: Wrong Grid Alignment
**Symptoms:** Volumes don't match biodiversity gizmos

**Solutions:**
1. Verify same `cellSize` in both scripts
2. Check `WorldToGridPosition` calculation
3. Enable `showDebugGizmos` on both scripts to compare
4. Ensure both using same world origin

---

## Part 6: Advanced Configuration

### Per-Taxon Saturation (Future Enhancement)
```csharp
// Instead of linear biodiversity → saturation:
if (hotspot.speciesCount > 10) {
    saturation *= 1.5f; // Boost for high species count
}
if (hotspot.totalObservations < 5) {
    saturation *= 0.5f; // Reduce for sparse data
}
```

### Dynamic Update Based on Player Movement
```csharp
// Only respawn volumes when player moves significant distance
Vector3 lastSpawnPosition;
float respawnDistance = 50f;

void Update() {
    if (Vector3.Distance(playerTransform.position, lastSpawnPosition) > respawnDistance) {
        SpawnBiodiversityVolumes();
        lastSpawnPosition = playerTransform.position;
    }
}
```

### Additional Post-Processing Effects
Besides saturation, you can add to the VolumeProfile:
- **Bloom:** Higher bloom in high-diversity areas (glowing effect)
- **Color Curves:** Shift hues (green = high, brown = low)
- **Vignette:** Darken edges in low-diversity areas
- **Chromatic Aberration:** Subtle effect for "hotspot" feeling

---

## Part 7: Saturation Curve Examples

### Conservative (Subtle Effect)
```
Low Biodiversity: -0.3
High Biodiversity: +0.2
Global Baseline: -0.2
→ Gentle desaturation, suitable for realistic look
```

### Dramatic (High Contrast)
```
Low Biodiversity: -1.0 (grayscale)
High Biodiversity: +1.0 (oversaturated)
Global Baseline: -0.7
→ Strong visual feedback, stylized look
```

### Recommended (Current Settings)
```
Low Biodiversity: -0.8
High Biodiversity: +0.5
Global Baseline: -0.5
→ Clear feedback without being overwhelming
```

---

## Part 8: Inspector Quick Reference

### BiodiversityVolumeSpawner Component
```
📦 References
  ├─ Biodiversity Manager: [Auto or Manual]
  └─ Volume Prefab: BiodiversityVolumePrefab

🎨 Saturation Settings
  ├─ Low Biodiversity: -0.8
  ├─ High Biodiversity: +0.5
  └─ Global Baseline: -0.5

🔊 Volume Settings
  ├─ Priority: 5
  ├─ Blend Distance: 25m
  └─ Height: 100m

⏱️ Update Settings
  ├─ Auto Update: ☑
  └─ Interval: 5s

⚡ Performance
  ├─ Max Volumes: 100
  └─ Spawn Radius: 0m

🐛 Debugging
  ├─ Logging: ☑
  ├─ Gizmos: ☑
  └─ Manual Key: V
```

---

## Part 9: Expected Console Output

### Successful Setup
```
[BiodiversityVolumeSpawner] Initialized
  Saturation range: -0.80 to 0.50
  Global baseline: -0.50
  Volume priority: 5, Blend distance: 25m
  Max volumes: 100, Spawn radius: 0m

[BiodiversityVolumeSpawner] Volume #1: Grid (0, 0), Simpson's Index 0.654, Saturation 0.03
[BiodiversityVolumeSpawner] Volume #2: Grid (1, 0), Simpson's Index 0.812, Saturation 0.26
...

[BiodiversityVolumeSpawner] ✅ Spawned 47 volumes
  Skipped (distance): 0
  Skipped (limit): 0
  Total hotspots: 47
```

### Common Warnings
```
[BiodiversityVolumeSpawner] No biodiversity hotspots available yet
→ Wait for observations to load

[BiodiversityVolumeSpawner] Volume prefab has no VolumeProfile! Cannot set saturation.
→ Assign profile to prefab

[BiodiversityVolumeSpawner] No BiodiversityScoreManager found in scene!
→ Add BiodiversityScoreManager component
```

---

## Part 10: Source Attribution

### Techniques & Sources

1. **Unity Post-Processing Stack**
   - Source: Unity Technologies URP Documentation
   - URL: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest
   - Used: Volume component, ColorAdjustments override, priority system

2. **Grid-Based Spatial System**
   - Source: BiodiversityScoreManager (existing system)
   - Used: cellSize, WorldToGridPosition, grid alignment

3. **Volume Blending & Priority**
   - Source: Unity Volume Documentation
   - URL: https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest
   - Used: Priority-based blending, blend distance, local vs global

4. **Simpson's Biodiversity Index**
   - Source: Ecological Science (via BiodiversityScoreManager)
   - Used: Diversity metric (0-1 scale) for visual mapping

5. **Performance Optimization**
   - Source: Unity Best Practices
   - Used: Max limits, spawn radius, object pooling concepts

### AI Contribution Breakdown
- **System Design (Grid-based approach):** 80% AI
- **Volume Management:** 90% AI
- **Inspector Configuration:** 60% AI, 40% Human (parameter tuning)
- **Integration with BiodiversityScoreManager:** 70% AI
- **Documentation:** 85% AI

### Human Validation Required
- Test saturation ranges in actual gameplay
- Adjust parameters for visual feel
- Verify performance on target hardware
- Tune blend distances for smooth transitions

---

## Troubleshooting Checklist

- [ ] URP Post-Processing installed
- [ ] Volume Profile created with Color Adjustments
- [ ] Volume Prefab configured (not global, has collider)
- [ ] Global Volume exists with low saturation baseline
- [ ] BiodiversityVolumeSpawner component added to scene
- [ ] Volume Prefab assigned in Inspector
- [ ] BiodiversityScoreManager reference set
- [ ] Observations loaded in scene
- [ ] Biodiversity calculations complete (press U)
- [ ] Volumes spawning (check Console)
- [ ] Player tagged correctly for radius checks
- [ ] Scene view gizmos enabled to visualize volumes

---

**Ready to test!** Press Play, press `U` to force biodiversity update, then press `V` to spawn volumes. Walk around and watch the saturation change based on biodiversity. 🌿✨
