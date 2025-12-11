# Quick Setup Guide - Biodiversity Spawn Manual Controls

## TL;DR - Fast Setup (2 minutes)

1. **Add controller to your map:**
   - Select your `AbstractMap` GameObject in the scene
   - Add Component → `BiodiversitySpawnController`
   - Done! The map reference will auto-populate

2. **Play and test:**
   - Press Play
   - Press **B** key to force spawn
   - Check console for results

## What You Get

### Automatic Spawning (Default)
- Waits for observations to load from iNaturalist API
- Waits for biodiversity calculations to complete
- Spawns prefabs automatically with correct biodiversity distribution
- **No manual intervention needed** ✓

### Manual Controls (When Needed)
- **Press B** to force respawn at any time
- **Inspector button** for one-click spawning
- **On-screen debug panel** showing status
- **Code API** for custom triggers

## Files You Need

All files are already created:

1. **Scripts/BiodiversitySpawnController.cs** - Main controller
2. **Scripts/Editor/BiodiversitySpawnControllerEditor.cs** - Inspector UI
3. **Mapbox/.../BIO_SpawnInsideModifier.cs** - Modified spawner (already updated)

## Usage Examples

### Testing Different Biodiversity Levels

```
1. Enter Play Mode
2. Wait for observations to load (automatic)
3. See initial spawn based on real biodiversity data
4. [Optional] Modify biodiversity settings in BiodiversityScoreManager
5. Press 'B' to respawn with new settings
6. Compare the difference!
```

### Debugging Spawn Issues

```
1. Check if prefabs are assigned:
   Project → Mapbox/User/Modifiers/BIO_SPAWNGREEN.asset
   → lowBiodiversityPrefabs[] should have prefabs
   → highBiodiversityPrefabs[] should have prefabs

2. Check console logs:
   [BIO_SpawnInsideModifier] Spawning... ← Should see this
   [BiodiversityScoreManager] Updated... ← Should see this

3. Force respawn manually:
   Press 'B' or use Inspector button

4. Check spawn results:
   Look for "✓ Spawned X total prefabs" in console
```

### Integrating with Your Game Logic

```csharp
// Example: Respawn when player enters new area
public class AreaTrigger : MonoBehaviour
{
    private BiodiversitySpawnController spawnController;

    void Start()
    {
        spawnController = FindObjectOfType<BiodiversitySpawnController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Respawn biodiversity for this new area
            spawnController?.ForceSpawnBiodiversity();
        }
    }
}
```

## Inspector Overview

When you select the AbstractMap with BiodiversitySpawnController:

```
┌─────────────────────────────────────────────────┐
│ Biodiversity Spawn Controller (Script)          │
├─────────────────────────────────────────────────┤
│ References                                       │
│   Map: AbstractMap (auto-assigned) ✓            │
│                                                  │
│ Manual Controls                                  │
│   Force Spawn Key: B                            │
│                                                  │
│ Debug                                            │
│   Show Debug Info: ☑                            │
│                                                  │
│ ┌─────────────────────────────────────────────┐ │
│ │  🌳 Force Spawn Biodiversity Prefabs        │ │
│ └─────────────────────────────────────────────┘ │
│                                                  │
│ ✓ BIO_SpawnInsideModifier found and ready!     │
└─────────────────────────────────────────────────┘
```

## Keyboard Shortcuts Reference

| Key | Action | When |
|-----|--------|------|
| **B** | Force spawn/respawn biodiversity prefabs | Play Mode |
| **U** | Force biodiversity score update | Play Mode (if enabled) |
| **N** | Force observation network update | Play Mode (if enabled) |

## Common Scenarios

### Scenario 1: "I want to test quickly without waiting"
```
Solution: Use manual controls
1. Enter Play Mode
2. Press 'B' immediately
3. System will spawn with default biodiversity (fallback)
```

### Scenario 2: "I want accurate biodiversity-based spawning"
```
Solution: Use automatic mode (default)
1. Enter Play Mode
2. Wait ~10 seconds for observations to load
3. System spawns automatically with real data
```

### Scenario 3: "I changed biodiversity settings and want to see the effect"
```
Solution: Force respawn
1. Modify BiodiversityScoreManager settings in Inspector
2. Press 'B' to respawn
3. See immediate visual feedback
```

### Scenario 4: "I want to spawn only in specific areas"
```
Solution: Use proximity-based spawning
1. Keep automatic spawning
2. Create trigger zones
3. Call ForceSpawnBiodiversity() when player enters
```

## Performance Notes

- **Spawn Count**: Configurable via `_spawnRateInSquareMeters` and `_densityMultiplier`
- **Distance Check**: Uses `_minDistanceBetweenObjects` to prevent overlap
- **Monitoring**: Checks every 1 second (low overhead)
- **Web Optimization**: System designed to work on WebGL builds

## Next Steps

1. ✅ Add BiodiversitySpawnController to AbstractMap
2. ✅ Test with 'B' key in Play Mode
3. ✅ Verify prefabs spawn correctly
4. ✅ Check biodiversity distribution in console
5. ✅ Customize settings as needed

## Support Files

- **Full Documentation**: [BIODIVERSITY_SPAWN_MANUAL_CONTROLS.md](./BIODIVERSITY_SPAWN_MANUAL_CONTROLS.md)
- **System Flow**: [BIODIVERSITY_SPAWN_FLOW.md](./BIODIVERSITY_SPAWN_FLOW.md)
- **Technical Details**: [BIO_SPAWN_MODIFIER_UPDATE.md](./BIO_SPAWN_MODIFIER_UPDATE.md)

## Troubleshooting Quick Fixes

| Problem | Quick Fix |
|---------|-----------|
| Nothing spawns | Check prefab arrays are assigned |
| Wrong prefab ratio | Check biodiversity score in console |
| "Modifier not found" | Verify modifier is in map's GameObject modifiers |
| Button grayed out | Must be in Play Mode |
| No debug panel | Enable `showDebugInfo` in controller |

---

**That's it!** You now have full manual control over biodiversity spawning while still keeping the automatic, observation-based system working. 🌳
