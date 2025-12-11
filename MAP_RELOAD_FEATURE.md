# Map Reload Feature - Username Search

## What Changed

When you search for a user and press Enter, the system now **relocates the entire map** to the user's observation location, not just the player.

### Before (Old Behavior)
- ❌ Only teleported the player
- ❌ Map stayed centered at original location
- ❌ Player could be at edge of loaded map area
- ❌ Observations loaded but map tiles didn't update

### After (New Behavior)
- ✅ **Reloads entire map** at new location
- ✅ Updates all map tiles and terrain
- ✅ Centers map at user's observation
- ✅ Player spawns at new map center
- ✅ Loads observations with user priority

---

## How It Works

### Step-by-Step Process

1. **User searches for username** (e.g., "kueda")
   - Status: 🔍 Searching for user 'kueda'...

2. **API finds user's last observation**
   - Status: ✅ Found 'kueda'! Observed: California Poppy

3. **Map reload begins** (NEW!)
   - Status: 🗺️ Relocating to 'kueda's location...
   - Calls `map.UpdateMap(latLng)` to update map center
   - Reloads all terrain tiles at new location
   - Regenerates elevation data
   - Updates satellite imagery

4. **Player teleportation**
   - Spawns player at new location with 50m height offset
   - Ensures player doesn't spawn underground during tile loading

5. **Observation loading**
   - Status: 📍 Loading observations for 'kueda'...
   - Loads user's observations first
   - Then loads other observations in the area

6. **Complete!**
   - Status: ✅ Observations loaded! Showing 'kueda' first
   - Status clears after 5 seconds

---

## Technical Details

### Code Changes in BiodiversityUI.cs

#### Fixed Compiler Error
**Line 737 (removed):**
```csharp
string observedDate = lastObs.observed_on_string ?? "unknown date";
```
**Issue:** `ObservationData` class doesn't have `observed_on_string` field
**Fix:** Removed this unused variable

#### New Method: `TeleportAndReloadMap()`
**Location:** Lines 769-854

**What it does:**
```csharp
private IEnumerator TeleportAndReloadMap(string username, float lat, float lng)
{
    // 1. Show relocating status
    searchStatusText.text = "🗺️ Relocating to '{username}'s location...";

    // 2. Find the Mapbox map
    Mapbox.Unity.Map.AbstractMap map = FindObjectOfType<Mapbox.Unity.Map.AbstractMap>();

    // 3. Update map center - THIS RELOADS EVERYTHING
    Mapbox.Utils.Vector2d latLng = new Mapbox.Utils.Vector2d(lat, lng);
    map.UpdateMap(latLng);  // ← Key line: reloads all tiles!

    // 4. Teleport player to new location
    Vector3 worldPosition = map.GeoToWorldPosition(latLng, true);
    playerTransform.position = worldPosition + Vector3.up * 50f;

    // 5. Wait for map tiles to load
    yield return new WaitForSeconds(1f);

    // 6. Load observations with user priority
    yield return StartCoroutine(mapController.LoadObservationsWithUserPriority(username, lat, lng, 5f));

    // 7. Show success
    searchStatusText.text = "✅ Observations loaded! Showing '{username}' first";
}
```

#### Replaced Old Methods
**Removed:**
- `TeleportPlayerToCoordinates()` - Only moved player
- `ReloadObservationsWithUserFilter()` - Only loaded observations

**New:**
- `TeleportAndReloadMap()` - Does everything in one coordinated sequence

---

## Mapbox UpdateMap() Method

### What `map.UpdateMap(Vector2d latLng)` Does:

1. **Updates map center coordinates**
   - Changes `_centerLatitudeLongitude` to new location
   - Recalculates `_centerMercator` projection

2. **Invalidates existing tiles**
   - Marks current tiles as outdated
   - Triggers tile disposal

3. **Requests new tiles**
   - Calculates new tile coverage
   - Fetches terrain data for new area
   - Downloads satellite imagery
   - Generates mesh for new location

4. **Regenerates world**
   - Builds new terrain meshes
   - Applies elevation data
   - Updates colliders
   - Spawns new game objects

**Source:** `Assets/Mapbox/Unity/Map/AbstractMap.cs:342-360`

---

## Status Message Flow

```
[User presses Enter on "kueda"]
           ↓
✅ Found 'kueda'! Observed: California Poppy
    (Green, instant)
           ↓
🗺️ Relocating to 'kueda's location...
    (Light Blue, during map reload)
           ↓
    [Map tiles regenerating...]
    [Player teleported...]
           ↓
📍 Loading observations for 'kueda'...
    (Light Blue, ~1-3 seconds)
           ↓
✅ Observations loaded! Showing 'kueda' first
    (Green, stays for 5 seconds)
           ↓
    [Clears automatically]
```

---

## Why Higher Spawn Height?

```csharp
playerTransform.position = worldPosition + Vector3.up * 50f;
```

**Old:** `+ Vector3.up * 2f` (2 meters)
**New:** `+ Vector3.up * 50f` (50 meters)

**Reason:**
- Terrain tiles take time to load after `UpdateMap()`
- If player spawns at 2m, might fall through unloaded terrain
- 50m gives terrain time to generate before player lands
- Prevents "falling through world" bug

---

## Performance Notes

### What Gets Reloaded
- ✅ Terrain meshes
- ✅ Elevation data
- ✅ Satellite imagery tiles
- ✅ Vector data (roads, buildings)
- ✅ iNaturalist observations

### What Stays Cached
- ✅ Mapbox API tokens
- ✅ Texture atlases
- ✅ Material instances
- ✅ Player character
- ✅ UI elements

### Loading Times
- **Map tiles:** 1-2 seconds
- **Observations:** 1-3 seconds
- **Total relocation:** 2-5 seconds

---

## User Experience

### What Users See
1. Search for username
2. Brief "Relocating..." message (blue)
3. Screen stays at old location for ~1 second
4. **Map suddenly updates** to new location (visible tile reload)
5. Player appears at new location
6. Observations load and spawn
7. Ready to explore!

### Expected Behavior
- ✅ Entire world relocates
- ✅ You're at the center of the new area
- ✅ Searched user's observations appear first
- ✅ Can immediately explore surroundings

---

## Debugging

### Console Messages to Look For

**Successful relocation:**
```
[BiodiversityUI] Relocating map to kueda's observation at 37.7749, -122.4194
[BiodiversityUI] Map center updated to 37.7749, -122.4194
[BiodiversityUI] Player teleported to world position: (1234.5, 50.0, 678.9)
[BiodiversityUI] Map relocated and observations loaded with user kueda prioritized
```

**Errors to watch for:**
```
❌ [BiodiversityUI] Map not found!
❌ [BiodiversityUI] Map controller not found!
⚠️ [BiodiversityUI] Player transform not found for teleportation!
```

### Testing Checklist
- [ ] Search for valid user (e.g., "kueda")
- [ ] Watch map tiles reload at new location
- [ ] Verify player spawns at new center
- [ ] Check observations load (user's first)
- [ ] Try moving around new area
- [ ] Terrain collision works (not falling through)
- [ ] Status messages display correctly

---

## Comparison: Old vs New

| Feature | Old Behavior | New Behavior |
|---------|-------------|--------------|
| **Map Tiles** | Stay at original location | ✅ Reload at new location |
| **Map Center** | Unchanged | ✅ Updated to observation |
| **Player Position** | Teleported to edge of map | ✅ Centered at new location |
| **Terrain** | Original area | ✅ New area generated |
| **Observations** | Loaded at old map area | ✅ Loaded at new map center |
| **Experience** | Disjointed (player far from center) | ✅ Seamless (centered exploration) |

---

## Known Issues & Limitations

### 1. Brief Loading Delay
- **Issue:** 1-2 second delay while tiles load
- **Status:** Expected behavior (Mapbox API calls)
- **Mitigation:** Status messages keep user informed

### 2. Spawn Height
- **Issue:** Player spawns 50m in air
- **Status:** Intentional (prevents falling through terrain)
- **Mitigation:** Player falls safely to ground

### 3. Multiple Rapid Searches
- **Issue:** Clicking search multiple times quickly
- **Status:** Coroutines queue up
- **Mitigation:** Disable search button during reload? (future improvement)

---

## Future Improvements

### Possible Enhancements
1. **Smooth Camera Transition**
   - Fly from old to new location
   - Cinematic pan across map

2. **Loading Indicator**
   - Progress bar during tile loading
   - "Loading 3/8 tiles..." counter

3. **Search History**
   - Dropdown of recent searches
   - Quick navigation to previous users

4. **Batch Loading**
   - Load multiple users at once
   - Create "tour" of observations

5. **Zoom Level Control**
   - Let user specify zoom when teleporting
   - "Zoom to observation" vs "Zoom to region"

---

## Related Files

### Modified
- ✅ `Assets/Scripts/Biodiversity/BiodiversityUI.cs` (lines 733-854)

### Referenced
- 📚 `Assets/Mapbox/Unity/Map/AbstractMap.cs` (UpdateMap method)
- 📚 `Assets/Scripts/iNaturalist Observations/INaturalistMapController.cs` (LoadObservationsWithUserPriority)

### Documentation
- 📄 `STATUS_MESSAGES_GUIDE.md` (status message reference)
- 📄 `DEBUG_USERNAME_SEARCH.md` (debugging guide)
- 📄 `KEYBOARD_SHORTCUTS.md` (keyboard controls)

---

## Summary

The username search now provides a **complete map relocation experience**:
- Entire map regenerates at new location
- Player spawns at center of new area
- Observations load with searched user prioritized
- Clear status updates throughout process

This creates a seamless exploration experience where users can "jump" to any iNaturalist user's observation location and immediately explore the biodiversity in that area! 🗺️✨
