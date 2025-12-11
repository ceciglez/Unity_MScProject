# UI Canvas Independent Scaling - Feature Documentation

## Problem
When scaling observation prefabs (plants, animals, etc.) using the taxon-specific scale multipliers in `INaturalistMapController`, the UI canvas was also being scaled because it's a child of the observation GameObject. This made the UI either too large or too small depending on the prefab scale.

## Solution
Added independent UI canvas scaling controls that apply the UI scale directly to the canvas transform's `localScale`, which overrides the parent's scale inheritance.

## New Inspector Controls

### INaturalistMapController

Added new section: **"UI Canvas Scaling"**

```
UI Canvas Scaling
├─ Override UI Scale: ☑ (Enable independent UI scaling)
├─ UI Canvas Scale: 0.005 (Default size, range: 0.001 - 0.02)
└─ UI Canvas Y Offset: 2.0 (Height above observation, range: 0 - 10)
```

### Control Details:

#### **Override UI Scale** (bool)
- **Default:** `true` (enabled)
- **Purpose:** Enable/disable independent UI scaling
- **When disabled:** UI will scale with the prefab (old behavior)
- **When enabled:** UI will use the fixed `uiCanvasScale` value

#### **UI Canvas Scale** (float, 0.001 - 0.02)
- **Default:** `0.005`
- **Purpose:** Fixed scale for the UI canvas
- **Effect:**
  - `0.005` = Normal size (1.5m wide x 2m tall)
  - `0.001` = Very small
  - `0.01` = Double size
  - `0.02` = Very large

#### **UI Canvas Y Offset** (float, 0 - 10)
- **Default:** `2.0`
- **Purpose:** How high above the observation the UI appears
- **Effect:**
  - `0` = At observation center
  - `2.0` = 2 meters above
  - `5.0` = 5 meters above (useful for tall prefabs)

## How It Works

### Before (Problem):
```
Observation GameObject (scale: 0.4x for insects)
  ├─ Insect Prefab Mesh (scale: 0.4x) ✓ Correct
  └─ UI Canvas (scale: 0.4x) ✗ Too small!
```

### After (Solution):
```
Observation GameObject (scale: 0.4x for insects)
  ├─ Insect Prefab Mesh (inherits scale: 0.4x) ✓ Correct
  └─ UI Canvas (localScale set to 0.005) ✓ Normal size!
```

The canvas uses `transform.localScale = Vector3.one * uiCanvasScale` which directly overrides the inherited scale from the parent.

## Code Changes

### ObservationDisplay.cs
Added new method:
```csharp
public void SetUICanvasScale(float scale, float yOffset)
{
    if (infoCanvas != null)
    {
        // Set scale independently
        infoCanvas.transform.localScale = Vector3.one * scale;

        // Update Y offset
        canvasOffset.y = yOffset;
        infoCanvas.transform.localPosition = canvasOffset;
    }
}
```

### INaturalistMapController.cs
Added in `SpawnObservationPrefabs()` after `display.Initialize()`:

```csharp
// Set UI canvas scale independently from prefab scale
if (overrideUIScale)
{
    display.SetUICanvasScale(uiCanvasScale, uiCanvasYOffset);
}
```

## Usage Examples

### Example 1: Small Insects with Normal UI
```
Taxon Settings:
├─ Insect Scale: 0.4

UI Canvas Scaling:
├─ Override UI Scale: ☑
├─ UI Canvas Scale: 0.005 (normal)
└─ UI Canvas Y Offset: 2.0

Result: Tiny insect with normal-sized readable UI
```

### Example 2: Large Plants with Larger UI
```
Taxon Settings:
├─ Plant Scale: 2.0

UI Canvas Scaling:
├─ Override UI Scale: ☑
├─ UI Canvas Scale: 0.008 (larger)
└─ UI Canvas Y Offset: 4.0 (higher for tall plants)

Result: Large plant with proportionally larger UI positioned higher
```

### Example 3: Disable Independent Scaling (Old Behavior)
```
UI Canvas Scaling:
├─ Override UI Scale: ☐ (disabled)

Result: UI scales with prefab (not recommended)
```

## Benefits

✅ **Consistency** - All observation UIs are the same size regardless of prefab scale
✅ **Readability** - Text remains legible even for very small or large prefabs
✅ **Flexibility** - Can adjust UI size and position independently
✅ **Backwards Compatible** - Can disable to get old behavior if needed

## Tips

### Finding the Right UI Scale:
1. Set a medium value like `0.005` to start
2. Play the game and look at various observations
3. If text is too small → increase to `0.007` or `0.01`
4. If UI feels too intrusive → decrease to `0.003` or `0.004`

### Finding the Right Y Offset:
1. Consider your tallest prefab
2. Set Y offset high enough that UI doesn't overlap the top of the tallest observation
3. For mixed sizes: Use average height (e.g., `2.0` - `3.0`)
4. For very tall prefabs (like trees): Use `4.0` - `6.0`

### Per-Taxon Offsets (Future Enhancement):
Currently, one Y offset is used for all observations. If you want different offsets for different taxa, you could extend the system:

```csharp
// Future enhancement idea
[SerializeField] private float plantUIYOffset = 4f;
[SerializeField] private float insectUIYOffset = 1.5f;
// etc.
```

## Troubleshooting

### UI still scales with prefab
- **Check:** Is "Override UI Scale" enabled?
- **Check:** Did you reload/respawn observations after changing settings?

### UI is in the wrong position
- **Adjust:** "UI Canvas Y Offset" value
- **Note:** Position is relative to the prefab's center, not ground

### UI is too small/large
- **Adjust:** "UI Canvas Scale" slider
- **Range:** 0.001 (tiny) to 0.02 (huge)
- **Default:** 0.005 works for most cases

### UI appears before canvas is created
- The UI canvas is created when `Initialize()` is called
- The scale override happens immediately after initialization
- If you see old scale, reload observations

## Testing Checklist

- ✅ Test with very small prefabs (insects, scale 0.4)
- ✅ Test with normal prefabs (animals, scale 0.8-1.0)
- ✅ Test with large prefabs (plants, scale 1.5-2.0)
- ✅ Verify UI text is readable in all cases
- ✅ Verify UI position is appropriate for all prefab heights
- ✅ Test disabling override to ensure old behavior works
- ✅ Test adjusting scale slider during Play mode

## Performance Notes

- Setting `localScale` has minimal performance impact
- Operation happens once per observation at spawn time
- No per-frame updates needed
- Works well even with hundreds of observations

## Related Settings

This feature works alongside existing observation settings:

| Setting | Purpose | Related To |
|---------|---------|-----------|
| Taxon Scale Multipliers | Scale the 3D prefab | Independent from UI |
| UI Display Distance | When to show UI | Visibility |
| Always Show UI | Show all UIs always | Visibility |
| Enable Observation UI | Master UI toggle | Visibility |
| **Override UI Scale** | **Fix UI size** | **This feature** |
| **UI Canvas Scale** | **UI size value** | **This feature** |
| **UI Canvas Y Offset** | **UI height** | **This feature** |

## Files Modified

- ✅ [INaturalistMapController.cs](Assets/Scripts/INaturalistMapController.cs) - Added inspector controls and method calls
- ✅ [ObservationDisplay.cs](Assets/Scripts/ObservationDisplay.cs) - Added `SetUICanvasScale()` method
