# Obsolete Scripts

These scripts are **no longer used** but preserved for reference and documentation purposes.

## Grass Rendering Attempts (Nov 29 - Dec 1, 2024)

### Failed Implementations:

**DynamicGrassManager.cs**
- Purpose: Auto-refresh Point Grass Renderer Scene Filters as Mapbox tiles load
- Status: FAILED - Compute buffer errors, crashes
- Issue: Point Grass Renderer BuildPoints() never initialized buffers with dynamic tiles
- Attempts: Event-based updates, reflection calls, coroutine toggles - all failed
- Time spent: ~8 hours

**PointGrassTileModifier.cs**
- Purpose: Modifier to setup Point Grass on individual tiles
- Status: FAILED - Same compute buffer issues as DynamicGrassManager
- Issue: GPU instancing incompatible with Mapbox's dynamic mesh generation

**GrassSpawnerModifier.cs**
- Purpose: Early grass spawning attempt
- Status: ABANDONED - Empty placeholder file

### Superseded Implementations:

**GrassPatchSpawner.cs** 
- Purpose: Spawn grass patch prefabs around player
- Status: SUPERSEDED by OptimizedGrassPatchSpawner.cs
- Issue: Caused frame freezes (50-100ms) when spawning all patches at once
- Replaced: Dec 1, 2024 with optimized version using object pooling + incremental spawning

**StylizedGrassManager.cs**
- Purpose: Apply Stylized Grass Shader material to entire tiles based on player proximity
- Status: SUPERSEDED - Wrong visual approach
- Issue: Applied grass shader to entire tile mesh creating "striped moving" effect
- User feedback: "Not exactly the effect I want" - expected discrete grass blades
- Replaced: With grass patch prefab spawning approach

**StylizedGrassLanduseModifier.cs**
- Purpose: Spawn grass meshes only on landuse features (parks, gardens, etc.)
- Status: NOT USED - Alternative approach explored but not implemented
- Reason: User wanted grass everywhere near player, not limited to specific landuse types

## Current Active Solution:

**OptimizedGrassPatchSpawner.cs** (Dec 1, 2024)
- Object pooling (200 pre-instantiated patches)
- Grid-based chunk system (20m chunks)
- Incremental spawning (10 patches/frame)
- No frame drops, seamless updates
- **SUCCESS** ✓

## Lessons Learned:

1. Point Grass Renderer incompatible with Mapbox dynamic tiles (compute buffer initialization issues)
2. Surface shaders (Stylized Grass on full mesh) create wrong visual aesthetic for discrete grass
3. Instant spawning causes frame freezes - need object pooling + incremental spawning
4. Grid-based chunk systems provide predictable, performant streaming
5. Always test asset compatibility with dynamic systems before committing time

## Time Investment Summary:

- Point Grass attempts: 8+ hours (failed)
- Stylized Grass material approach: 2 hours (wrong approach)
- Grass patch spawning v1: 1 hour (frame drops)
- Grass patch spawning v2 (optimized): 2 hours (SUCCESS)
- **Total: ~13 hours to working grass system**
