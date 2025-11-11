# GameObject Hierarchy Reference

## Observation Canvas Hierarchy

When an observation spawns, this is the GameObject structure created:

```
ObservationPrefab (your prefab instance)
├── ObservationDisplay (component)
├── ObservationPositionTracker (component)
├── ObservationTriggerInteraction (component)
├── SphereCollider (component - trigger)
└── InfoCanvas (GameObject, Canvas component)
    ├── Canvas (component) - RenderMode: WorldSpace, Scale: 0.005
    ├── CanvasScaler (component)
    ├── GraphicRaycaster (component)
    └── Panel (GameObject, Image component - dark background)
        ├── Photo (GameObject, RawImage component)
        │   └── RectTransform: 250x200, position (0, 60)
        ├── CommonName (GameObject, Text component)
        │   ├── Font: Arial (built-in)
        │   ├── Size: 24, Bold
        │   ├── Color: White (1, 1, 1, 1)
        │   └── RectTransform: 280x40, position (0, -70)
        └── ScientificName (GameObject, Text component)
            ├── Font: Arial (built-in)
            ├── Size: 18, Italic
            ├── Color: White (1, 1, 1, 1)
            └── RectTransform: 280x35, position (0, -100)
```

## Debug Overlay Hierarchy

Created automatically by INaturalistMapController:

```
DebugCoordinateOverlay (GameObject)
├── DebugCoordinateOverlay (component)
└── DebugCoordinateCanvas (GameObject)
    ├── Canvas (component) - RenderMode: ScreenSpaceOverlay, SortingOrder: 1000
    ├── CanvasScaler (component)
    ├── GraphicRaycaster (component)
    └── DebugPanel (GameObject, Image component - dark background)
        └── DebugText (GameObject, Text component)
            ├── Font: Arial (built-in)
            ├── Size: 14
            ├── Color: White (1, 1, 1, 1)
            ├── Alignment: Upper Left
            └── RectTransform: Fills panel with 10px padding
```

## How to Check in Unity Inspector

### For Observation Canvas (when one spawns):

1. **Find an observation in Hierarchy** - look for your prefab instances (they'll be children of the Map or ObservationContainer)
2. **Expand it** - you should see "InfoCanvas" as a child GameObject
3. **Select InfoCanvas** and check:
   - Canvas component: Render Mode = "World Space"
   - RectTransform: Width = 300, Height = 400
   - Transform: Scale = (0.005, 0.005, 0.005)
4. **Expand InfoCanvas → Panel** and check:
   - Should see "Photo", "CommonName", "ScientificName" as children
5. **Select CommonName** and check:
   - Text component: Text field should have the species name
   - Color should be white (255, 255, 255, 255)
   - Font should be "Arial"
   - Material should be "UI-Default (Material)"

### For Debug Overlay:

1. **Find "DebugCoordinateOverlay" in Hierarchy** (at root level)
2. **Expand it** - should see "DebugCoordinateCanvas"
3. **Expand DebugCoordinateCanvas → DebugPanel → DebugText**
4. **Select DebugText** and check:
   - Text component: Should have content in the Text field
   - Color: White
   - Font: Arial
   - Material: UI-Default

## Common Issues and Checks

### Text not visible:

1. **Check Material**: Text component → Material should be "UI-Default (Material)"
2. **Check Font**: Should show "Arial" (not "None")
3. **Check Color**: Should be white (R:255 G:255 B:255 A:255)
4. **Check Text field**: Should have actual text content (not empty)
5. **Check Enabled checkbox**: Should be checked ✓
6. **Check Canvas**: Parent Canvas should be active (checkbox next to name)

### Canvas always visible:

1. **Check InfoCanvas active state**: Should be unchecked when not triggered
2. **Check Console** for "Ensured canvas starts hidden" message
3. **Verify trigger collider**: Should have "Is Trigger" checked

### Debug overlay empty:

1. **Check Console** for "DebugCoordinateOverlay: UI created" message
2. **Verify Text component** has "Support Rich Text" enabled
3. **Check Update loop** - Console should NOT show repeated warnings about null components

## What the Console Should Show on Play:

```
ObservationDisplay.Awake: Creating canvas on Observation_01
ObservationDisplay.Awake: Canvas created and hidden on Observation_01
ObservationDisplay: Canvas UI created on Observation_01
Debug coordinate overlay created
DebugCoordinateOverlay: UI created - Text font: True, Material: True, Color: RGBA(1.000, 1.000, 1.000, 1.000)
ObservationDisplay.Initialize called on Observation_01
  Data: Valid
  Taxon: American Robin
  Set common name: 'American Robin' - Text enabled: True, Color: RGBA(1.000, 1.000, 1.000, 1.000), Font: True
  Set scientific name: 'Turdus migratorius' - Text enabled: True
  Loading photo: https://...
```

If you don't see these messages, there's a problem with the script execution order or component setup.
