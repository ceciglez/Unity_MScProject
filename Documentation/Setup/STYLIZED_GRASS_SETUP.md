# Stylized Grass Shader Setup for Mapbox Tiles

## Complete Setup Guide

### Step 1: Choose a Grass Material

The Stylized Grass Shader package includes several pre-made materials in:
`Assets/Stylized Grass Shader/Materials/`

**Recommended materials to try:**
- `StylizedGrass.mat` - Classic stylized grass (good starting point)
- `GrassThin.mat` - Sparse, thin grass
- `GrassThick.mat` - Dense, thick grass
- `GrassTall.mat` - Taller grass blades

**Pick one and duplicate it:**
1. Select the material you like
2. Press Ctrl/Cmd+D to duplicate
3. Rename to `MapboxGrass`
4. Customize colors/settings as desired

### Step 2: Create Base Terrain Material

Create a simple material for distant tiles (no grass):
1. Right-click in Materials folder → Create → Material
2. Name it `TerrainNoGrass`
3. Set shader to `Universal Render Pipeline/Lit`
4. Assign a terrain texture (like LMHPOLY grass texture)
5. Set color to brownish-green
6. Adjust smoothness to ~0.3

### Step 3: Setup StylizedGrassRenderer (Required!)

**Important:** The Stylized Grass Shader requires a `StylizedGrassRenderer` component in your scene.

1. Create an empty GameObject: Hierarchy → Create Empty
2. Name it `StylizedGrassSystem`
3. Add Component → `Stylized Grass Renderer` (from the package)
4. Configure:
   - **Listen To Wind Zone:** Check if you have a WindZone
   - **Wind Zone:** Drag your WindZone here (optional)
   - **Color Map:** Leave empty for now

### Step 4: Setup StylizedGrassManager

1. Select your existing `GrassManager` GameObject (or create new one)
2. **Remove** old components (PointGrassRenderer, DynamicGrassManager)
3. **Add Component** → `Stylized Grass Manager` (the script I created)
4. **Configure:**
   - **Map:** Drag `MapContainer` (Abstract Map)
   - **Player:** Should auto-find, or drag your player GameObject
   - **Grass Material:** Drag your `MapboxGrass` material
   - **Base Material:** Drag `TerrainNoGrass` material
   - **Grass Render Distance:** Start with `100` meters
   - **Update Interval:** `1` second
   - **Debug Mode:** ✓ Check to see console logs

### Step 5: Configure MapContainer

**On your MapContainer (Abstract Map):**
- **General → Others → Tile Material:** Assign `TerrainNoGrass`
  (This is the default - nearby tiles will be switched to grass by the script)

### Step 6: Optional - Add Wind

For animated grass:
1. GameObject → 3D Object → Wind Zone
2. Configure Wind Zone:
   - **Mode:** Directional
   - **Main:** 0.3 - 0.5 (ambient wind strength)
   - **Turbulence:** 0.5 - 1.0 (gust strength)
   - **Pulse Magnitude:** 0.2 - 0.5
3. On `StylizedGrassSystem` → Stylized Grass Renderer:
   - Check **Listen To Wind Zone**
   - Drag your WindZone into the field

### Step 7: Test

1. **Enter Play mode**
2. **Check Console** - should see:
   - `[StylizedGrassManager] Tiles with grass: X, without: Y`
3. **Walk around** - grass should appear on nearby tiles
4. **Select GrassManager** - green sphere shows grass render distance

### Troubleshooting

**No grass visible:**
- Check that grass material uses the Stylized Grass Shader (not URP/Lit)
- Verify `StylizedGrassRenderer` component is in the scene
- Check Console for errors
- Make sure grass material has a texture assigned

**Grass too dense/sparse:**
- Select grass material
- Adjust `Density` parameter in Inspector
- Typical range: 10-50

**Grass wrong height:**
- Select grass material  
- Adjust `Height` and `Height Variation` parameters
- Typical range: 0.5-2.0

**Performance issues:**
- Reduce `Grass Render Distance` to 75-100m
- Increase `Update Interval` to 2-3 seconds
- Use simpler grass material (GrassThin instead of GrassThick)
- Reduce grass `Density` in material

**Grass doesn't move with wind:**
- Make sure WindZone exists
- Check `Listen To Wind Zone` on StylizedGrassRenderer
- Verify WindZone is assigned
- Adjust wind multipliers if needed

### Key Concepts

**How it works:**
1. Distant tiles use `TerrainNoGrass` (simple texture, no grass blades)
2. Tiles within 100m of player automatically switch to `MapboxGrass` (Stylized Grass Shader)
3. As player moves, tiles dynamically change materials
4. Grass only renders nearby = better performance

**Material switching:**
- Grass appears on tiles near player
- Grass disappears on distant tiles
- Smooth transition as player moves
- Max ~9 tiles with grass at once

**Performance:**
- Grass only on nearby tiles (not entire map)
- Dynamic LOD via distance
- Customizable render distance
- Minimal overhead from material swaps

### Customization

**Grass appearance:**
- Open grass material
- Adjust colors, height, density, wind response
- See Stylized Grass Shader documentation for all parameters

**Render distance:**
- 100m = balanced (default)
- 75m = better performance
- 150m = more grass visible (may impact FPS)

**Update frequency:**
- 1s = responsive (default)
- 2s = less overhead
- 0.5s = instant response (more CPU)

### Resources

- [Stylized Grass Shader Documentation](https://staggart.xyz/unity/stylized-grass-shader/sgs-docs/)
- [Placing Grass Guide](https://staggart.xyz/unity/stylized-grass-shader/sgs-docs/?section=placing-grass)
- Material parameters explained in package documentation
