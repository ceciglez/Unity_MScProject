# CRITICAL: Missing Component Assignments

## The Problem

The **iNaturalist Controller** GameObject has script components with alerts because required references are not assigned in the Inspector. Without these assignments, observations will NOT spawn.

## REQUIRED ASSIGNMENTS in Unity Inspector

### 1. Select "iNaturalist Controller" in Hierarchy

### 2. Look at the Inspector - INaturalistMapController component

You need to assign these fields (they currently show "None"):

#### **Map References:**
- **Map**: Drag the "Map Container" GameObject from your Hierarchy into this field
  - This should have an AbstractMap or MapboxMap component on it

#### **Observation Prefab:**
- **Observation Prefab**: You need to create a prefab for observations
  - **Quick Option 1**: Drag any simple 3D object from Hierarchy (like a Sphere or Cube)
  - **Quick Option 2**: Create a prefab:
    1. In Hierarchy: Create → 3D Object → Sphere
    2. Name it "ObservationIndicator"
    3. Scale it to (0.5, 0.5, 0.5) so it's small
    4. Drag it from Hierarchy into your Assets/Prefabs folder
    5. Delete the sphere from the Hierarchy (now it's saved as a prefab)
    6. Drag the prefab from Assets/Prefabs into the "Observation Prefab" field

- **Observation Container**: Leave as "None" - the script will auto-create this

#### **API Settings:**
- Max Observations: **100** (default is fine)
- Update Delay: **2** (default is fine)
- Auto Update: **✓ Checked** (default is fine)

#### **Visual Settings:**
- Prefab Scale: **1** (default is fine)
- Recent Observation Pulse Days: **7** (not used anymore, ignore)
- Show Debug Info: **✓ Checked** (shows console logs)
- Show Debug Overlay: **✓ Checked** (shows coordinate overlay)

## Quick Setup Steps:

### Step 1: Create an Observation Prefab (1 minute)
```
1. Hierarchy → Right-click → Create → 3D Object → Sphere
2. Rename it: "ObservationIndicator"
3. Set Scale: (0.5, 0.5, 0.5)
4. Optional: Change color
   - Add Component → Rendering → Material
   - Or create a new Material in Assets and assign it
5. Drag from Hierarchy → Assets/Prefabs folder (creates prefab)
6. Delete the sphere from Hierarchy (keep the prefab in Assets)
```

### Step 2: Assign References to INaturalistMapController
```
1. Select "iNaturalist Controller" in Hierarchy
2. In Inspector, find "INaturalist Map Controller (Script)" component
3. Map field: Drag "Map Container" from Hierarchy
4. Observation Prefab field: Drag the prefab you created from Assets/Prefabs
```

### Step 3: Verify Map Container
```
1. Select "Map Container" in Hierarchy
2. Check it has one of these components:
   - AbstractMap
   - MapboxMap
   - Or similar Mapbox component
3. If it doesn't, you need to add it (Add Component → Mapbox)
```

## How to Tell It's Working

Once you've assigned the references:

1. **Alerts should disappear** on the iNaturalist Controller component
2. **Press Play**
3. **Check Console** for:
   - "Debug coordinate overlay created"
   - "Loading iNaturalist observations..."
   - "Loaded X valid observations"
   - "Spawned X observation prefabs"
4. **Look in Hierarchy** - should see "ObservationContainer" appear with child objects
5. **Debug overlay** should appear in top-left with text
6. **Small spheres** should appear on the map (observations)

## If Still Not Working

### Check Console for these specific errors:

- **"Map reference is not set!"** 
  → You didn't assign the Map field

- **"Observation prefab is not set!"**
  → You didn't assign the Observation Prefab field

- **"No AbstractMap component found"**
  → Your Map Container doesn't have the right Mapbox component

- **No spawn messages at all**
  → API might be failing, check for network errors in Console

### Debug the Map:

1. Select "Map Container"
2. Check its position is at or near (0, 0, 0)
3. Check it has proper Mapbox API token set
4. Look for the RangeAroundTransformTileProvider component
5. Make sure the "Target" field points to your player

## Player Assignment

Your player appears to be "ExampleCharacter" (from KCC asset). The debug overlay will auto-find it, but verify:

1. ExampleCharacter has a KinematicCharacterMotor component
2. It has a Collider (capsule or similar)
3. It has a Rigidbody (should be Kinematic)

Without a proper player setup, the map might not center correctly.

## Current State Based on Your Hierarchy

You have:
- ✓ Map Container (good)
- ✓ ExampleCharacter (good - this is your player)
- ✗ No ObservationContainer (means observations haven't spawned yet)
- ✗ INaturalistController has errors (missing assignments)

**Priority: Assign the Map and Observation Prefab fields RIGHT NOW, then press Play.**
