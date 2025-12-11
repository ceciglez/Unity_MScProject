# Quick Setup Instructions

## Latest Changes (Canvas Fix)

Fixed canvas sizing and visibility issues:
- Canvas now uses **Awake()** to ensure it's hidden before anything else runs
- Canvas scale set to **0.005** (small 1.5m x 2m billboard in world space)
- Canvas RectTransform sizeDelta: **300 x 400 pixels**
- Canvas positioned **2m above observation** (canvasOffset)
- Debug overlay now has **rich text support** enabled

## Canvas Settings Breakdown

If you need to manually check or adjust canvas settings in Unity:

### World-Space Canvas Settings:
1. **Canvas Component:**
   - Render Mode: **World Space**
   - Event Camera: (leave None for world-space)
   - Sorting Layer: Default
   
2. **Rect Transform:**
   - Width: **300**
   - Height: **400**
   - Pos X, Y, Z: Should be **(0, 2, 0)** relative to observation (2m above)
   - Scale: **0.005, 0.005, 0.005** ← This makes it small in world space
   
3. **Canvas Scaler:**
   - Dynamic Pixels Per Unit: **10**

### Why canvases appeared everywhere:
- Old scale was 0.01 (too big)
- SetupCanvas() was being called twice, overriding settings
- Canvas wasn't hidden in Awake(), only in Start() which was too late

### Why debug text was missing:
- Rich text wasn't enabled
- Text overflow wasn't set to allow expansion
- Panel might have been too small

## What Changed

The scripts now **automatically create** the UI canvas and debug overlay! You don't need to manually configure anything in Unity.

## Changes Made:

1. **ObservationDisplay.cs** - Now automatically creates a world-space canvas with:
   - Photo display (RawImage)
   - Common name text (bold, white)
   - Scientific name text (italic, gray)
   - Dark semi-transparent background panel

2. **INaturalistMapController.cs** - Now automatically creates a debug overlay on Start()

3. **Debug logging added** to track:
   - Canvas creation and initialization
   - When player enters/exits triggers
   - When canvases show/hide
   - UI component assignment

## What to Check in Unity:

### 1. Make sure your observation prefab has a collider:
   - If your prefab is a simple sphere/cube primitive, it already has one
   - If it's an empty GameObject, the script will add a SphereCollider automatically
   - The collider should be set to "Is Trigger" (the script does this automatically)

### 2. Make sure your player has a Rigidbody:
   - Select your player character in the hierarchy
   - If it doesn't have a Rigidbody, add one: Add Component → Physics → Rigidbody
   - Check "Is Kinematic" to prevent physics from affecting movement
   - The KCC controller should handle movement, not physics

### 3. Check the Console for debug messages:
   - Look for messages like:
     - "ObservationDisplay: Automatically created canvas UI on..."
     - "Debug coordinate overlay created"
     - "Player detected! Showing canvas on..."

### 4. Look for the debug overlay:
   - It should appear in the **top-left corner** of the screen
   - Shows player coordinates and closest 3 observations
   - If you don't see it, check the Console for errors

## Troubleshooting:

### Canvas is always visible:
- Check Console - should see "Ensured canvas starts hidden"
- Make sure observation prefabs are being instantiated (not pre-placed in scene)

### Canvas shows but no information:
- Check Console for "Set common name:" and "Set scientific name:" messages
- Verify iNaturalist API is returning data

### Player doesn't trigger canvas:
- Make sure player has a Rigidbody (kinematic is fine)
- Check Console when walking near observations - should see "TriggerEnter from..."
- If you see "Not recognized as player", your player might need the "Player" tag

### No debug overlay:
- Check that `showDebugOverlay` is checked on the INaturalistMapController component
- Look in Console for "Debug coordinate overlay created"
- Check for any errors about missing fonts

### Observations seem far away or at wrong scale:
- The debug overlay will show distances in meters
- If distances are in thousands, there's a coordinate conversion issue
- Check that observations are being parented to `map.transform`

## Quick Test:

1. Enter Play mode
2. Check Console for startup messages
3. Look for debug overlay in top-left corner
4. Walk toward an observation indicator
5. Canvas should appear when you get within 3 meters
6. Canvas should hide when you move more than 10 meters away

## Debug Overlay Format:

```
=== COORDINATE DEBUG INFO ===

PLAYER:
  World Pos: (123.45, 67.89, 234.56)
  Lat/Lng: (51.507351, -0.127758)

OBSERVATIONS: 15 found
  #1: American Robin
    World: (125.30, 68.00, 230.15)
    LatLng: (51.507400, -0.127800)
    Distance: 5.23m
    Canvas Visible: True
```

This tells you exactly where everything is and whether it's working!
