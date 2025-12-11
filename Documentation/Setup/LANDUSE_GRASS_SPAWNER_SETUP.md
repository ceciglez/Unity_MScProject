# Landuse-Aware Grass Spawner System

## Overview
The `LanduseAwareGrassPatchSpawner` is an advanced grass spawning system that:

- **Spawns grass patches based on Mapbox landuse data** (parks, forests, residential areas, etc.)
- **Integrates with the Stylized Grass Shader colormap system** for terrain-based color variations
- **Uses object pooling and chunk-based spawning** for optimal performance
- **Supports multiple grass prefab variations** per landuse type
- **Provides real-time player-tracking** with distance-based updates

## Key Features

### ✅ Landuse Integration
- Automatically detects Mapbox landuse features (parks, forests, residential, etc.)
- Different grass densities, scales, and prefabs per landuse type
- Configurable rules for each landuse category

### ✅ Colormap Support
- Integrates with `GrassColorMapRenderer` from Stylized Grass Shader
- Samples terrain colors and applies tinting to grass patches
- Adjustable colormap strength for natural color blending

### ✅ Performance Optimized
- **Chunk-based spawning**: Divides world into manageable chunks
- **Object pooling**: Reuses grass patch instances for memory efficiency
- **Gradual spawning**: Spawns limited patches per frame to prevent hitches
- **Distance culling**: Only spawns grass within player radius

### ✅ Spawner-Based Architecture
- Works with existing grass patch prefabs and spawning systems
- Compatible with your current `OptimizedGrassPatchSpawner` approach
- Maintains deterministic placement and object pooling benefits

## Setup Instructions

### 1. Basic Setup
1. Add `LanduseAwareGrassPatchSpawner` component to a GameObject in your scene
2. Add `GrassSpawnerSetupHelper` component to the same GameObject
3. Click "Setup Grass Spawner" button in the helper component
4. The setup helper will automatically find references and create default rules

### 2. Manual Configuration

#### Required References
- **Map**: Assign your `AbstractMap` component
- **Player**: Assign your player Transform (character controller)
- **Default Grass Patch Prefab**: Your main grass patch prefab

#### Optional References
- **ColorMap Renderer**: `GrassColorMapRenderer` component for colormap integration
- **Landuse-Specific Prefabs**: Different grass prefabs for parks, forests, etc.

#### Grass Rules Configuration
Each `GrassPatchLanduseRule` defines:
- **Rule Name**: Identifier for the rule
- **Landuse Types**: Array of landuse keywords to match (e.g., "park", "forest")
- **Grass Patch Prefabs**: Array of prefabs to randomly choose from
- **Grass Density**: Patches per 100 square units
- **Scale Range**: Random scale variation (min, max)
- **Custom Material**: Optional material override
- **Random Rotation**: Enable/disable random Y rotation

### 3. Performance Settings

#### Recommended Settings
```csharp
spawnRadius = 60f;           // Distance around player to spawn grass
chunkSize = 25f;             // Size of each spawn chunk
patchesPerFrame = 15;        // Patches spawned per frame (prevents hitches)
initialPoolSize = 300;       // Starting object pool size
updateInterval = 0.4f;       // Seconds between update checks
```

#### For Lower-End Hardware
```csharp
spawnRadius = 40f;
chunkSize = 30f;
patchesPerFrame = 8;
initialPoolSize = 200;
updateInterval = 0.6f;
```

#### For Higher-End Hardware
```csharp
spawnRadius = 80f;
chunkSize = 20f;
patchesPerFrame = 25;
initialPoolSize = 500;
updateInterval = 0.2f;
```

## Landuse Types

### Common Mapbox Landuse Classes
- **Parks**: "park", "recreation", "playground", "pitch", "grass"
- **Forests**: "forest", "wood", "tree", "natural"
- **Residential**: "residential", "suburb", "garden"
- **Urban**: "urban", "commercial", "industrial"
- **Water Features**: "water", "river", "lake"

### Creating Custom Rules
```csharp
var customRule = new GrassPatchLanduseRule
{
    ruleName = "Golf Course",
    enabled = true,
    landuseTypes = new string[] { "golf", "course", "sport" },
    grassPatchPrefabs = new GameObject[] { golfGrassPrefab },
    grassDensity = 4f,
    scaleRange = new Vector2(0.9f, 1.1f),
    randomRotation = true,
    customMaterial = golfGrassMaterial
};
```

## Colormap Integration

### Requirements
1. `GrassColorMapRenderer` component in scene
2. Valid colormap texture assigned to the renderer
3. `useColorMap = true` in spawner settings

### How It Works
1. When spawning a grass patch, the system samples the colormap at the world position
2. The sampled color is blended with the grass material's base color
3. Blend strength is controlled by `colorMapStrength` (0-1)

### Troubleshooting Colormap
- Ensure colormap bounds cover your spawn area
- Check that colormap texture is readable
- Verify colormap UV coordinates are correctly set

## Debug Features

### Enable Debug Mode
```csharp
debugMode = true;           // Enable console logging
showChunkGizmos = true;     // Show chunk boundaries in Scene view
```

### Debug Information
- Chunk spawning/removal logs
- Landuse detection results
- Performance metrics
- Object pool statistics

## Migration from Component-Based System

If you were previously using the component-based approach (`AdvancedGrassLanduseModifier`):

1. **Replace the component**: Remove the old modifier and add the spawner
2. **Update prefab references**: Use grass patch prefabs instead of grass meshes
3. **Adjust densities**: Spawner density values may need tweaking
4. **Performance benefits**: Enjoy better performance with object pooling

## Performance Tips

### Optimization Strategies
1. **Reduce spawn radius** if experiencing performance issues
2. **Increase chunk size** to reduce chunk count (but larger chunks take longer to spawn)
3. **Decrease patches per frame** if getting frame drops during spawning
4. **Use simpler grass prefabs** for distant chunks
5. **Implement LOD system** for grass patches at different distances

### Memory Management
- Object pools automatically expand as needed
- Inactive patches are returned to pool when player moves away
- Chunk landuse data is cached for performance

## Troubleshooting

### Common Issues

#### "No grass spawning"
- Check that player reference is assigned
- Verify map has landuse data
- Enable debug mode to see landuse detection results
- Ensure default grass prefab is assigned

#### "Grass not following terrain"
- Enable `alignToTerrain` setting
- Check that terrain has proper colliders
- Adjust `heightOffset` value

#### "Poor performance"
- Reduce `spawnRadius` and `patchesPerFrame`
- Increase `updateInterval`
- Optimize grass prefab complexity

#### "Colormap not working"
- Verify `GrassColorMapRenderer` is assigned and active
- Check that colormap texture exists and covers spawn area
- Ensure `useColorMap` is enabled

### Debug Commands
```csharp
// In debug mode, use these console commands to inspect the system:
// Check active chunks: Look for "[LanduseGrassSpawner] Spawned chunk" messages
// Check landuse detection: Look for landuse area counts
// Monitor performance: Check patches spawned per frame
```

## API Reference

### Public Properties
- `map`: AbstractMap reference
- `player`: Player transform for tracking
- `grassRules`: List of landuse-based grass rules
- `spawnRadius`: Distance around player to spawn grass
- `useColorMap`: Enable colormap integration
- `colorMapRenderer`: GrassColorMapRenderer reference

### Public Methods
- No public methods needed for normal operation
- All spawning is handled automatically based on player movement

### Events
- System uses internal coroutines for spawning
- No custom events exposed currently

---

## Quick Start Checklist

- [ ] Add `LanduseAwareGrassPatchSpawner` component
- [ ] Add `GrassSpawnerSetupHelper` component
- [ ] Assign grass patch prefabs
- [ ] Click "Setup Grass Spawner" button
- [ ] Test in Play mode and adjust settings
- [ ] Enable colormap integration if desired
- [ ] Optimize performance settings for your target hardware

**Ready to enjoy dynamic, landuse-aware grass spawning! 🌱**