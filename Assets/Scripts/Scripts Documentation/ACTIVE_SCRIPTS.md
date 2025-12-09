# Active Scripts Overview

## Core Systems (Currently Used)

### Map & Movement
- **MapboxKCCAdapter.cs** - Adapts Kinematic Character Controller to work with Mapbox tiles
- **StaticMapMinimap.cs** - Working minimap system (1024x1024, 0.5 scale multiplier, smooth panning)

### Grass Rendering
- **OptimizedGrassPatchSpawner.cs** ✓ ACTIVE - Object pooling + incremental spawning, grid chunks, 80m radius
- **SimpleGrassPlaneModifier.cs** - Backup option (simple grass planes on landuse features) - NOT CURRENTLY USED

### VFX & Weather
- **AreaVFXManager.cs** ✓ NEW - Zone-based VFX (Blizzard, Dust, etc.) with radius/altitude triggers
- **WeatherVFXController.cs** ✓ NEW - OpenWeatherMap API integration for real-world weather VFX

### Water Systems
- **WaterSurfaceConformModifier.cs** - Water vertices conform to terrain contours (vertex-based)
- **WaterHeightOffsetModifier.cs** - Simple uniform Y-axis water offset (superseded by above)
- **WaterConformToTerrainModifier.cs** - Alternative water conforming approach
- **WaterRenderQueueModifier.cs** - Sets water render queue for proper transparency

### iNaturalist Integration
- **INaturalistMapController.cs** - Main iNaturalist API integration
- **INaturalistMapControllerEditor.cs** - Custom editor for iNaturalist controller
- **ObservationDisplay.cs** - Displays observation data in UI
- **ObservationPositionTracker.cs** - Tracks observation positions
- **ObservationTriggerInteraction.cs** - Interaction triggers for observations

### Vegetation & Environment
- **CustomPrefabSpawner.cs** - ScriptableObject for spawning trees/vegetation on landuse features
- **LanduseAnalyzer.cs** - Analyzes and color-codes landuse types
- **ElevationBasedMaterial.cs** - Changes materials based on elevation

### Debug Tools
- **DebugCoordinateOverlay.cs** - Shows coordinate information overlay
- **TerrainMaterialFixer.cs** - Diagnostic tool for terrain material issues

## Recommended Actions

### Keep as-is:
- All iNaturalist scripts (core feature)
- StaticMapMinimap.cs (working perfectly)
- OptimizedGrassPatchSpawner.cs (current grass solution)
- New VFX scripts (just added)
- MapboxKCCAdapter.cs (essential)

### Consider Testing/Implementing:
- **WaterSurfaceConformModifier.cs** - Should test this for water features
- **CustomPrefabSpawner.cs** - Could spawn trees/vegetation in parks
- **LanduseAnalyzer.cs** - Could color-code different landuse areas

### Potentially Archive:
- **SimpleGrassPlaneModifier.cs** - Backup grass option, only if you want alternative
- **WaterHeightOffsetModifier.cs** - Superseded by WaterSurfaceConformModifier
- **WaterConformToTerrainModifier.cs** - Duplicate water solution
- **TerrainMaterialFixer.cs** - Diagnostic tool, not needed in production

### Clean Up:
- Delete orphaned .meta files in Scripts folder:
  - DynamicGrassManager.cs.meta
  - GrassPatchSpawner.cs.meta
  - GrassSpawnerModifier.cs.meta
  - PointGrassTileModifier.cs.meta
  - StylizedGrassLanduseModifier.cs.meta
  - StylizedGrassManager.cs.meta

## Next Steps

1. **Delete orphaned .meta files** (for moved scripts)
2. **Test water conforming** on water features
3. **Test vegetation spawning** with CustomPrefabSpawner
4. **Implement weather VFX** with OpenWeatherMap
5. **Consider archiving** backup/diagnostic scripts you won't use
