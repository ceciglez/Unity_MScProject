# PROJECT AUDIT & SIMPLIFICATION PLAN
Generated: November 11, 2025

## CURRENT SCRIPT INVENTORY

### ✅ KEEP - Core Essential Scripts

#### 1. **MapboxKCCAdapter.cs** - Player Character Controller
- **Location**: Assets/Scripts/
- **Purpose**: Handles Kinematic Character Controller integration with Mapbox terrain
- **Status**: KEEP - Currently in use with KCC
- **Functions**: 
  - Spawns player at map center
  - Positions player at terrain height + 10m
  - Gravity-based falling
  - Safety net for falling through terrain

#### 2. **INaturalistMapController.cs** - Core iNaturalist System
- **Location**: Assets/Scripts/
- **Purpose**: Main controller for fetching and displaying iNaturalist observations
- **Status**: KEEP & SIMPLIFY
- **Functions**:
  - Fetches observations from iNaturalist API
  - Spawns observation prefabs on map
  - Manages observation data
  - Handles map bounds queries
- **Data Classes**: ObservationData, PhotoData, TaxonData, UserData, INaturalistResponse

#### 3. **ObservationDisplay.cs** - Individual Observation Display
- **Location**: Assets/Scripts/
- **Purpose**: Manages individual observation display with photo loading
- **Status**: KEEP & SIMPLIFY
- **Functions**:
  - Loads and displays observation data
  - Downloads photos from iNaturalist
  - Canvas show/hide (currently broken)

#### 4. **ObservationTriggerInteraction.cs** - Collision Detection
- **Location**: Assets/Scripts/
- **Purpose**: Detects when player collides with observation
- **Status**: KEEP & SIMPLIFY - Make this the ONLY interaction method
- **Functions**:
  - 3m radius sphere trigger
  - Detects both Unity CharacterController and KCC
  - Shows UI on collision

#### 5. **ObservationPositionTracker.cs** - Map Synchronization
- **Location**: Assets/Scripts/
- **Purpose**: Updates observation positions when map updates
- **Status**: KEEP
- **Functions**:
  - Tracks observation lat/lng
  - Updates world position on map updates

---

### ❌ DELETE - Redundant/Unused Scripts

#### 6. **FirstPersonController.cs** - OLD CHARACTER CONTROLLER
- **Status**: DELETE - You're using KCC now, not this
- **Reason**: Replaced by Kinematic Character Controller asset

#### 7. **MapboxTerrainAdapter.cs** - OLD TERRAIN ADAPTER
- **Status**: DELETE - You're using MapboxKCCAdapter now
- **Reason**: Duplicate functionality, replaced by KCC version

#### 8. **DynamicMapLoader.cs** - Unused Dynamic Loading
- **Status**: DELETE - You decided not to use this
- **Reason**: Using Mapbox's built-in RangeAroundTransformTileProvider instead

#### 9. **MapUserController.cs** - Top-down Map Controller
- **Status**: DELETE - Not for FPS navigation
- **Reason**: Designed for WASD map panning (top-down), not FPS walking

#### 10. **ObservationInteractionManager.cs** - Mouse Hover System
- **Status**: DELETE - Using trigger collision instead
- **Reason**: Raycast mouse hover is not needed, using collision triggers

#### 11. **ProximityInteractionManager.cs** - Proximity System
- **Status**: DELETE - Using trigger collision instead
- **Reason**: Redundant with ObservationTriggerInteraction

#### 12. **ObservationTooltip.cs** - 2D Tooltip System
- **Status**: DELETE - Using canvas display instead
- **Reason**: Simpler to use canvas directly

#### 13. **ObservationScreenUI.cs** - Screen Overlay UI (Broken)
- **Status**: DELETE - Not working, simplify to direct canvas
- **Reason**: Overcomplicated singleton approach, simpler to use world-space canvas

#### 14. **INaturalistFilterManager.cs** - Advanced Filtering
- **Status**: DELETE - Not needed for MVP
- **Reason**: Can add filtering later, keep it simple

#### 15. **NewBehaviourScript.cs** - Empty Template
- **Status**: DELETE - Unused template

---

### 📝 EDITOR SCRIPTS

#### 16. **INaturalistMapControllerEditor.cs**
- **Status**: KEEP - Useful for testing

#### 17. **NaturePolygonModifierEditor.cs**
- **Status**: DELETE - NaturePolygonModifier is causing errors

#### 18. **ObservationUISetup.cs**
- **Status**: DELETE - Not using screen overlay UI

---

### 🌲 MAPBOX MODIFIERS

#### 19. **NaturePolygonModifier.cs** - Tree/Rock Spawning
- **Status**: DELETE FOR NOW - Causing runtime errors
- **Reason**: Can add back later once core functionality works

---

## SIMPLIFIED ARCHITECTURE

### Core System (3 Scripts):

```
┌─────────────────────────────────────────┐
│     MapboxKCCAdapter.cs                 │
│  (Player movement & terrain spawning)  │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│   INaturalistMapController.cs           │
│  (Fetch & spawn observations)           │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│     Observation Prefab                  │
│  ┌───────────────────────────────────┐  │
│  │ ObservationDisplay.cs             │  │
│  │ (Data, photo, canvas)             │  │
│  │                                   │  │
│  │ ObservationTriggerInteraction.cs │  │
│  │ (Collision detection)             │  │
│  │                                   │  │
│  │ ObservationPositionTracker.cs    │  │
│  │ (Position sync)                   │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## REFACTORING PLAN

### Phase 1: Delete Unused Scripts ✂️

**Delete these files:**
```
Assets/Scripts/FirstPersonController.cs
Assets/Scripts/MapboxTerrainAdapter.cs
Assets/Scripts/DynamicMapLoader.cs
Assets/Scripts/MapUserController.cs
Assets/Scripts/ObservationInteractionManager.cs
Assets/Scripts/ProximityInteractionManager.cs
Assets/Scripts/ObservationTooltip.cs
Assets/Scripts/ObservationScreenUI.cs
Assets/Scripts/INaturalistFilterManager.cs
Assets/Scripts/NewBehaviourScript.cs
Assets/Scripts/NaturePolygonModifier.cs
Assets/Scripts/Editor/NaturePolygonModifierEditor.cs
Assets/Scripts/Editor/ObservationUISetup.cs
```

### Phase 2: Simplify ObservationDisplay.cs 🎨

**Changes:**
- Remove ObservationScreenUI logic
- Keep only canvas display (world space, not screen overlay)
- Make canvas small and centered on observation
- Distance-based hiding built-in

### Phase 3: Simplify ObservationTriggerInteraction.cs 🎯

**Changes:**
- Remove screen UI option
- Remove tooltip option
- Keep ONLY canvas display
- Simpler, single-purpose: collision = show canvas

### Phase 4: Clean INaturalistMapController.cs 🗺️

**Changes:**
- Remove unnecessary editor references
- Simplify spawn logic
- Ensure ObservationTriggerInteraction is added to spawned prefabs

---

## FINAL SIMPLIFIED STRUCTURE

```
Assets/
├── Scripts/
│   ├── MapboxKCCAdapter.cs              ← Player controller
│   ├── INaturalistMapController.cs      ← Observation fetcher/spawner
│   ├── ObservationDisplay.cs            ← Display data & photo
│   ├── ObservationTriggerInteraction.cs ← Collision detection
│   ├── ObservationPositionTracker.cs    ← Position updates
│   └── Editor/
│       └── INaturalistMapControllerEditor.cs ← Testing tool
```

**Total: 5 runtime scripts + 1 editor script = 6 files**

---

## INTERACTION FLOW (Simplified)

1. **Player walks around** → MapboxKCCAdapter + KCC handles movement
2. **Mapbox loads tiles** → RangeAroundTransformTileProvider (built-in)
3. **iNaturalist observations spawn** → INaturalistMapController
4. **Player gets near observation** → ObservationTriggerInteraction detects collision
5. **Canvas appears** → ObservationDisplay shows world-space canvas with photo/info
6. **Player walks away** → Canvas hides (distance check in Update)

---

## NEXT STEPS

Ready to proceed with:
1. ✂️ **Delete unused scripts**
2. ✏️ **Simplify remaining 5 core scripts**
3. 🎨 **Fix canvas to be small, centered, world-space**
4. ✅ **Test and verify**

Should I proceed with the deletions and refactoring?
