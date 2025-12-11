# Height-Based Terrain Textures Setup

## What This Does

Automatically applies different textures to your Mapbox terrain based on elevation:
- **Low elevation** (valleys, water level): Grass, sand, dirt
- **Mid elevation** (hills, plains): Rocky ground, mixed terrain  
- **High elevation** (mountains, peaks): Rock, snow, ice

Works with LMHPOLY terrain textures or any other texture pack!

## Setup Steps

### 1. Create the Material

1. In Unity, **Create → Material** (name it "TerrainHeightMaterial")
2. Set **Shader** to "Custom/HeightBasedTerrain"
3. You should now see three texture slots

### 2. Assign LMHPOLY Textures

In the Material Inspector:

**Low Elevation:**
- Drag your grass/valley texture (e.g., LMHPOLY_Ground_Grass_01)
- Adjust "Low Tint" color if needed

**Mid Elevation:**
- Drag your dirt/hill texture (e.g., LMHPOLY_Ground_Dirt_01)
- Adjust "Mid Tint" color if needed

**High Elevation:**
- Drag your rock/snow texture (e.g., LMHPOLY_Ground_Rock_01 or Snow)
- Adjust "High Tint" color if needed

**Settings:**
- **Texture Scale**: Start with 0.01 (adjust to make textures bigger/smaller)
- **Smoothness**: 0-0.3 for rough terrain, higher for wet/icy
- **Metallic**: Keep at 0 for natural terrain

### 3. Create the Modifier Asset

1. **Right-click in Assets** → Create → Mapbox → Modifiers → Elevation Based Material
2. **Name it**: "TerrainHeightModifier"
3. **Configure thresholds**:

**Example for Moderate Terrain:**
```
Low Threshold: 0 (sea level)
Mid Threshold: 50 (hills start)
High Threshold: 150 (mountains start)
Smooth Blending: ✓ Checked
Blend Distance: 20 (smooth transitions)
```

**Example for Exaggerated Terrain:**
```
Low Threshold: 0
Mid Threshold: 100
High Threshold: 300
Blend Distance: 30
```

Adjust these based on your map's actual height range!

### 4. Apply to Mapbox Terrain

1. **Select Map Container** in Hierarchy
2. **Find Terrain/Elevation Layer** settings
3. **Look for "Mesh Modifiers"** section
4. **Add your TerrainHeightModifier** asset
5. **In "Material Options"**, assign your "TerrainHeightMaterial"

### 5. Test and Adjust

Press Play and check:
- ✓ Low areas show grass texture
- ✓ Hills show dirt texture  
- ✓ Mountains show rock texture
- ✓ Smooth blending between zones

**If textures are too small/large:**
- Adjust "Texture Scale" in the material (lower = bigger textures)

**If transitions are too harsh:**
- Increase "Blend Distance" in the modifier

**If wrong heights are colored:**
- Adjust the thresholds to match your terrain's actual height values
- Check Console for height debug info (optional: add logging)

## Alternative: Simple Single Material Approach

If the height-based system is too complex, you can also:

### Quick Method:

1. Create a **simple material** with your LMHPOLY ground texture
2. In **Map Container → Terrain/Elevation → Material Options**
3. Assign your material directly
4. Adjust **UV Tiling** (try 10-50) to scale texture properly

This gives uniform terrain but is simpler to set up.

## Troubleshooting

**Textures not showing:**
- Check shader compiled without errors
- Make sure textures are assigned in material
- Verify material is assigned to terrain layer

**All one color:**
- Modifier might not be running
- Check it's in "Mesh Modifiers" not "Game Object Modifiers"
- Verify thresholds match your terrain heights

**Textures stretched/wrong size:**
- Adjust "Texture Scale" in material
- Try values between 0.001 and 1.0
- Lower numbers = bigger texture tiles

**Sharp transitions:**
- Increase "Blend Distance"
- Enable "Smooth Blending"

**Wrong elevation zones:**
- Select an observation and check its Y position
- Use that to calibrate your thresholds
- Example: If mountains are at Y=250, set high threshold to ~200

## Advanced: Slope-Based Textures

Want rock on steep slopes regardless of height? Let me know and I can add slope detection to the shader!

## Performance Note

This shader is efficient but uses 3 texture samples. If performance is an issue:
- Reduce terrain detail/resolution in Mapbox settings
- Use smaller texture sizes (1024x1024 instead of 2048x2048)
- Disable smooth blending for hard transitions (faster)
