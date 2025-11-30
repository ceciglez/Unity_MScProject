# Point Grass Renderer Setup Guide

## IMPORTANT: Don't attach components directly to MapContainer!

The MapContainer (AbstractMap) can be reset by Mapbox, losing your component settings.

## Correct Setup:

### 1. Create a Separate GameObject
- Right-click in Hierarchy → Create Empty
- Name it `GrassManager`
- Position it at (0, 0, 0)

### 2. Add Components to GrassManager:
- Add Component → `PointGrassRenderer`
- Add Component → `DynamicGrassManager`

### 3. Configure PointGrassRenderer:
- **Distribution Source:** `Scene Filters`
- **Scene Filters:** Leave empty (auto-populated)
- **Point Count:** `5000` (start lower for safety)
- **Multiply By Area:** ✓ Checked
- **Blade Type:** `Flat`
- **Material:** Assign grass material with `PointGrass_SHAD` shader
- **Overwrite Normal Direction:** ✓ Checked
- **Forced Normal:** `(0, 1, 0)`

### 4. Configure DynamicGrassManager:
- **Map:** Drag MapContainer here
- **Grass Renderer:** Auto-assigns
- **Player:** Auto-finds or drag your player
- **Point Count:** `5000` (match above)
- **Multiply By Area:** ✓ Checked
- **Grass Render Distance:** `100` meters (conservative)
- **Update Interval:** `2` seconds
- **Min Vertex Count:** `100`
- **Debug Mode:** ✓ Checked

### 5. Configure MapContainer:
- **Tile Material:** Assign `TerrainBase_URP` (NOT the grass shader!)
  - This material should use `Universal Render Pipeline/Lit` shader
  - Makes the terrain visible

## How It Works:
- **MapContainer** renders visible terrain tiles with TerrainBase_URP material
- **GrassManager** has PointGrassRenderer that adds grass on top
- **DynamicGrassManager** updates which tiles get grass (only near player)
- Grass follows player as they move, only rendering within 100m

## Performance Tips:
- Start with Point Count: 5000
- Keep Grass Render Distance: 100m or less
- If it crashes, reduce Point Count to 3000
- Max 9 tiles will have grass at once (safety limit)

## Troubleshooting:
- No grass visible: Check Console for warnings
- Crashes: Reduce Point Count and Grass Render Distance
- Compute Buffer error: Wait for tiles to load, system will auto-rebuild
- Terrain transparent: Make sure Tile Material uses URP/Lit, not PointGrass shader
