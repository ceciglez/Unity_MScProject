# Biodiversity Spawn Manual Controls - Setup Guide

## Overview
Since `BIO_SpawnInsideModifier` is used as a ScriptableObject asset attached to your Mapbox AbstractMap, you need a controller script to access and control it at runtime. This guide shows you how to set up manual controls.

## Setup Instructions

### Step 1: Add BiodiversitySpawnController to Your Map

1. **Locate your AbstractMap GameObject** in the scene hierarchy
2. **Add the BiodiversitySpawnController component:**
   - Select your AbstractMap GameObject
   - Click "Add Component"
   - Search for "BiodiversitySpawnController"
   - Add it

3. **Configure the controller:**
   - The `map` field should auto-populate with your AbstractMap
   - Set `forceSpawnKey` to your preferred key (default: B)
   - Enable `showDebugInfo` to see on-screen controls

### Step 2: Verify BIO_SpawnInsideModifier is Configured

Make sure your BIO_SpawnInsideModifier is properly added to your map:

1. **In Project view**, locate your BIO_SpawnInsideModifier asset (likely in `Assets/Mapbox/User/Modifiers/CUSTOM MODIFIERS/`)
2. **In your AbstractMap Inspector**, find the Vector layer that should spawn vegetation
3. **Check the GameObject Modifiers** - BIO_SpawnInsideModifier should be listed
4. **Verify prefab arrays** are assigned:
   - `lowBiodiversityPrefabs` - dead trees, sparse vegetation
   - `highBiodiversityPrefabs` - healthy trees, flowers

## How to Use Manual Controls

### Method 1: Keyboard Shortcut (Runtime)

While playing:
1. **Enter Play Mode**
2. **Press the configured key** (default: `B`)
3. Watch the console for spawn logs:
   ```
   [BiodiversitySpawnController] Force spawning biodiversity prefabs...
   [BIO_SpawnInsideModifier] Spawning 100 prefabs at (0, 0, 0). Biodiversity Score: 0.756
   [BIO_SpawnInsideModifier] ✓ Spawned 87 total prefabs: 65 high-biodiversity, 22 low-biodiversity
   ```

### Method 2: Inspector Button (Runtime)

While playing:
1. **Enter Play Mode**
2. **Select your AbstractMap GameObject**
3. **In the BiodiversitySpawnController Inspector**, click the big green button:
   - **"🌳 Force Spawn Biodiversity Prefabs"**
4. Check console for results

### Method 3: On-Screen GUI Button (Runtime)

If `showDebugInfo` is enabled:
1. **Enter Play Mode**
2. **Look at the bottom-left corner** of the Game view
3. You'll see a debug panel with:
   - Current status of the modifier
   - Keyboard shortcut reminder
   - "Force Spawn Now" button
4. Click the button to trigger spawn

### Method 4: From Code

You can also trigger spawning from your own scripts:

```csharp
public class MyGameController : MonoBehaviour
{
    private BiodiversitySpawnController spawnController;

    void Start()
    {
        // Find the controller (assuming it's on the AbstractMap)
        spawnController = FindObjectOfType<BiodiversitySpawnController>();
    }

    void SomeMethod()
    {
        // Force spawn when needed
        if (spawnController != null)
        {
            spawnController.ForceSpawnBiodiversity();
        }
    }
}
```

Or access the modifier directly:

```csharp
public class MyGameController : MonoBehaviour
{
    private BiodiversitySpawnController spawnController;

    void Start()
    {
        spawnController = FindObjectOfType<BiodiversitySpawnController>();
    }

    void SomeMethod()
    {
        // Get the modifier directly
        var modifier = spawnController.GetSpawnModifier();
        if (modifier != null)
        {
            modifier.ForceSpawn();
        }
    }
}
```

## When to Use Manual Controls

### Use Cases:

1. **Testing** - Quickly test biodiversity spawning during development
2. **Debugging** - Force respawn to see updated biodiversity scores
3. **Dynamic Updates** - Respawn when observations change or update
4. **Player Actions** - Trigger spawning based on player proximity or interactions
5. **Time-Based** - Respawn vegetation based on in-game time or seasons

## Troubleshooting

### "BIO_SpawnInsideModifier NOT FOUND"

**Causes:**
- Modifier not added to map's GameObject modifiers
- Wrong layer selected
- Modifier is on a different map instance

**Solutions:**
1. Open your AbstractMap Inspector
2. Check Vector Data → Layers → [Your Layer] → GameObject Modifiers
3. Ensure BIO_SpawnInsideModifier is in the list
4. Try clicking "Find Spawn Modifier" in the controller

### "Cannot force spawn - no cached tile/entity data!"

**Causes:**
- No tiles have been generated yet
- Modifier hasn't run its `Run()` method
- Map hasn't initialized

**Solutions:**
1. Wait for the map to fully load
2. Move around to trigger tile generation
3. The automatic monitoring system will spawn when ready
4. Manual spawn only works after at least one tile is created

### Force spawn does nothing

**Causes:**
- Not in Play Mode
- BiodiversitySpawnController not attached to map
- No prefabs assigned to modifier

**Solutions:**
1. Make sure you're in Play Mode
2. Verify BiodiversitySpawnController is on your AbstractMap GameObject
3. Check that `lowBiodiversityPrefabs` and `highBiodiversityPrefabs` arrays have prefabs assigned

### Console shows "No prefabs available for observation"

**Causes:**
- Prefab arrays are empty
- Prefab references are broken

**Solutions:**
1. Select your BIO_SpawnInsideModifier asset in the Project view
2. Assign prefabs to both arrays:
   - `lowBiodiversityPrefabs` - at least 1 prefab
   - `highBiodiversityPrefabs` - at least 1 prefab
3. Save the asset

## Advanced: Multiple Tile Management

If you have multiple tiles and want to control spawning per-tile:

```csharp
// This is more advanced and requires access to the tile system
// The current implementation spawns on the cached tile
// For multiple tiles, you may need to extend the system
```

Currently, the `BIO_SpawnInsideModifier` caches the most recent tile. If you need per-tile control, consider:

1. Keeping a dictionary of tiles → spawned objects
2. Extending the modifier to support multiple cached tiles
3. Creating separate modifier instances for different regions

## Files Created

- **BiodiversitySpawnController.cs** - Main controller script
- **BiodiversitySpawnControllerEditor.cs** - Custom Inspector with buttons

## API Reference

### BiodiversitySpawnController

#### Public Methods:
- `ForceSpawnBiodiversity()` - Manually trigger spawn/respawn
- `GetSpawnModifier()` - Get reference to BIO_SpawnInsideModifier

#### Public Fields:
- `map` - Reference to AbstractMap
- `forceSpawnKey` - Keyboard shortcut (default: B)
- `showDebugInfo` - Show debug UI panel

### BIO_SpawnInsideModifier

#### Public Methods:
- `ForceSpawn()` - Force spawn/respawn with current biodiversity data
- `RespawnPrefabs(VectorEntity, UnityTile)` - Spawn for specific tile
- `TriggerProximitySpawn(VectorEntity, UnityTile, Transform, float)` - Proximity-based spawn

## Example Workflow

1. **Setup** (One time):
   - Add BiodiversitySpawnController to AbstractMap
   - Assign prefabs to BIO_SpawnInsideModifier
   - Configure biodiversity score range

2. **Normal Gameplay**:
   - Map loads automatically
   - Observations load from API
   - Modifier monitors for observations
   - Auto-spawns when observations stabilize

3. **Manual Control** (When needed):
   - Press B to force respawn
   - See updated biodiversity distribution
   - Test different scenarios

4. **Debugging**:
   - Check console logs for spawn details
   - Watch biodiversity scores in logs
   - Verify prefab distribution matches scores
