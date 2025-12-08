# BIO_SpawnInsideModifier - Updated for Observation-Based Spawning

## Overview
The `BIO_SpawnInsideModifier` has been updated to spawn biodiversity prefabs **AFTER** observations are loaded and positioned in the map. This ensures that the biodiversity score calculations are accurate when determining which prefabs to spawn.

## Key Changes

### 1. Automatic Monitoring System
The modifier now monitors for observations to be loaded instead of spawning immediately when tiles are created.

**New Fields:**
- `_hasSpawned` - Prevents duplicate spawning
- `_cachedVectorEntity` - Stores the tile entity for later spawning
- `_cachedTile` - Stores the tile reference for later spawning
- `_observationsReady` - Tracks when observations are ready
- `_monitorCoroutine` - Handles the monitoring coroutine

### 2. MonitorObservationsAndSpawn() Coroutine
This new coroutine:
1. Checks every second for `ObservationDisplay` objects in the scene
2. Waits for the observation count to stabilize (same count for 2 consecutive checks)
3. Waits an additional 2 seconds for biodiversity calculations to complete
4. Spawns prefabs with accurate biodiversity scores
5. Has a 20-second timeout as a fallback

**Debug Logging:**
```
[BIO_SpawnInsideModifier] Monitoring for observations to load...
[BIO_SpawnInsideModifier] Found X observations, waiting for stabilization...
[BIO_SpawnInsideModifier] Observations loaded and stabilized! Count: X
[BIO_SpawnInsideModifier] Spawning biodiversity prefabs...
```

### 3. Enhanced Run() Method
The `Run()` method now:
- Caches the `VectorEntity` and `UnityTile` for later use
- Starts the monitoring coroutine
- Does NOT spawn immediately

### 4. ForceSpawn() Method
New public method for manual control:
```csharp
public void ForceSpawn()
```

**Use cases:**
- Testing the spawn system
- Re-spawning when observations update
- Manual control via UI or debug commands

### 5. Improved Logging in RespawnPrefabs()
Added detailed logging to track spawning:
```
[BIO_SpawnInsideModifier] Spawning 100 prefabs at (0, 0, 0). Biodiversity Score: 0.756
[BIO_SpawnInsideModifier] ✓ Spawned 87 total prefabs: 65 high-biodiversity (74.7%), 22 low-biodiversity (25.3%)
```

## How It Works

### Timeline:
1. **Map Tile Created** → `Run()` called → Caches tile data, starts monitoring
2. **Observations Loading** → Coroutine checks every second
3. **Observations Stabilized** → Waits 2 more seconds for biodiversity calculations
4. **Biodiversity Score Calculated** → `BiodiversityScoreManager` computes Simpson's Index
5. **Prefabs Spawned** → Uses biodiversity score to determine high/low prefab ratio

### Integration with BiodiversityScoreManager:
```csharp
// Get biodiversity score for tile location
float biodiversityScore = _biodiversityManager.GetSimpsonsIndexAtPosition(center);

// Normalize score to determine prefab mix
float t = Mathf.InverseLerp(minBiodiversityScore, maxBiodiversityScore, biodiversityScore);

// Spawn prefabs based on score
// Higher score = more high-biodiversity prefabs (happy trees, flowers, etc.)
// Lower score = more low-biodiversity prefabs (dead trees, sparse vegetation, etc.)
```

## Configuration

The modifier still uses the existing inspector settings:

### Spawn Settings:
- `_spawnRateInSquareMeters` - Density of spawned objects
- `_maxSpawn` - Maximum objects to spawn
- `_densityMultiplier` - Adjust overall density
- `_minDistanceBetweenObjects` - Minimum spacing

### Biodiversity Settings:
- `lowBiodiversityPrefabs[]` - Prefabs for low biodiversity areas
- `highBiodiversityPrefabs[]` - Prefabs for high biodiversity areas
- `minBiodiversityScore` - Minimum score threshold
- `maxBiodiversityScore` - Maximum score threshold

## Testing

### Manual Testing:
1. Run the game
2. Wait for observations to load (check console)
3. Watch for spawn logs
4. Verify prefabs match biodiversity scores

### Force Spawn (for debugging):
You can manually trigger spawning by calling `ForceSpawn()` from another script or inspector button.

## Troubleshooting

### No prefabs spawning:
- Check console for `[BIO_SpawnInsideModifier]` logs
- Verify observations are loading (`ObservationDisplay` objects exist)
- Ensure `BiodiversityScoreManager` is in the scene
- Check that prefab arrays are assigned in inspector

### Wrong prefab ratios:
- Verify `minBiodiversityScore` and `maxBiodiversityScore` range
- Check biodiversity score in logs
- Ensure both `lowBiodiversityPrefabs` and `highBiodiversityPrefabs` have prefabs assigned

### Timeout warnings:
- Increase `maxChecks` in `MonitorObservationsAndSpawn()` if observations take longer to load
- Check that `INaturalistMapController` is working properly

## Future Improvements

Potential enhancements:
- Add event system for more direct communication
- Support multiple tiles with different biodiversity scores
- Add option to respawn when player moves to different areas
- Add visualization of spawn areas in editor
