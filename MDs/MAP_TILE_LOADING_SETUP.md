# Map Tile Loading Setup

## Problem
Map tiles don't load as the player moves to new areas - only the initial tiles are visible.

## Solution
Configure the Mapbox map to use a **RangeAroundTransformTileProvider** that follows the player.

## Setup Steps in Unity:

### 1. Select "Map Container" in Hierarchy

### 2. Find the AbstractMap (or similar Mapbox component) in Inspector

### 3. Look for "Tile Provider" Settings

You should see a section about how tiles are loaded. You need to configure it to:

#### **Tile Provider Type**: 
- Change from "Range Tile Provider" to **"Range Around Transform Tile Provider"**

#### **Target Transform**:
- Drag your **"ExampleCharacter"** (or player GameObject) into this field
- This tells the map to load tiles around the player's position

#### **Range Settings**:
- **North**: 3-5 (tiles to load north of player)
- **South**: 3-5 (tiles to load south of player)  
- **East**: 3-5 (tiles to load east of player)
- **West**: 3-5 (tiles to load west of player)

Higher numbers = more tiles loaded = bigger visible area, but slower performance.

### 4. Other Important Settings:

#### **Initialize On Start**: ✓ Checked
- Map starts loading immediately

#### **Load Unity Tiles**: ✓ Checked  
- Actually loads the tile meshes

#### **Snap Map To Zero**: ✓ Checked (usually)
- Keeps map at origin, moves objects instead

## Alternative: If You Can't Find These Settings

The map might be using a different tile provider system. Here's what to check:

### Option A: Check for "Tile Provider" Component
1. Look on the "Map Container" GameObject
2. Find any component with "TileProvider" in the name
3. Make sure it's set to follow the player transform

### Option B: Check Map Initialization Code
If the map is set up via code, you might need to modify the initialization. Look for scripts on "Map Container" that might be setting up the tile provider.

### Option C: Use Built-in Range Around Transform
The Mapbox Unity SDK should have `RangeAroundTransformTileProvider` built-in. If it's not showing up:

1. Check your Mapbox SDK version (look in Package Manager)
2. Update to latest version if needed
3. Reimport Mapbox package if necessary

## Quick Test:

1. **Start the game**
2. **Press F key** (or however you move) to walk in one direction continuously
3. **Watch the terrain** - new tiles should appear ahead as you approach the edge
4. **Check Console** for Mapbox messages about tile loading

## Debug Info:

If tiles still don't load, check Console for:
- Mapbox API errors (invalid token?)
- Tile download errors (network issues?)
- "No tiles loaded" or similar warnings

## Expected Behavior:

✅ **Should happen:**
- Tiles load around player position
- New tiles appear as you walk toward edges
- Old tiles unload behind you (optional, saves memory)
- Smooth continuous terrain as you explore

❌ **Should NOT happen:**
- Static map that doesn't update
- Falling off edge of loaded area
- "Hole" or "void" in terrain where tiles should be

## Manual Workaround (If Settings Don't Exist):

If you absolutely can't find the tile provider settings, you might need to:

1. **Create a new Map** using Mapbox's map wizard
2. Select **"Range Around Transform"** during setup
3. Point it at your player
4. Copy your API token and location settings
5. Replace the old map with the new one

## Check Your Setup:

Run this checklist:

- [ ] Map Container has AbstractMap component
- [ ] Tile provider is set to "Range Around Transform" 
- [ ] Target Transform points to player (ExampleCharacter)
- [ ] Range is set to at least 3 in all directions
- [ ] Mapbox API token is valid (check Mapbox dashboard)
- [ ] Internet connection is working (tiles load from web)

Once configured correctly, tiles should automatically load as you navigate!
