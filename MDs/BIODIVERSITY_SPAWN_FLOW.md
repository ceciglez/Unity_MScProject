# Biodiversity Spawn System - Complete Flow Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          GAME STARTS                                     │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  AbstractMap (Mapbox)                                                    │
│  • Loads map tiles                                                       │
│  • Has BIO_SpawnInsideModifier in GameObject Modifiers                   │
│  • Has BiodiversitySpawnController component attached                    │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Tile Generated                                                          │
│  → BIO_SpawnInsideModifier.Run() called                                  │
│  → Caches tile data (VectorEntity, UnityTile)                           │
│  → Starts MonitorObservationsAndSpawn() coroutine                       │
└────────────────────────────────┬────────────────────────────────────────┘
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                    ▼                         ▼
    ┌───────────────────────────┐   ┌──────────────────────────┐
    │ AUTOMATIC PATH            │   │ MANUAL PATH              │
    │ (Default)                 │   │ (Testing/Override)       │
    └───────────┬───────────────┘   └────────┬─────────────────┘
                │                            │
                ▼                            │
┌─────────────────────────────────┐          │
│ MonitorObservationsAndSpawn()   │          │
│ Coroutine Running               │          │
│ • Checks every 1 second         │          │
│ • Looks for ObservationDisplay  │          │
└────────────┬────────────────────┘          │
             │                               │
             ▼                               │
┌─────────────────────────────────┐          │
│ INaturalistMapController        │          │
│ • Fetches observations from API │          │
│ • Creates ObservationDisplay    │          │
│   GameObjects                   │          │
│ • Positions them on map         │          │
└────────────┬────────────────────┘          │
             │                               │
             ▼                               │
┌─────────────────────────────────┐          │
│ Observations Loaded             │          │
│ • ObservationDisplay count      │          │
│   stops changing                │          │
│ • Coroutine detects stability   │          │
└────────────┬────────────────────┘          │
             │                               │
             ▼                               │
┌─────────────────────────────────┐          │
│ Wait 2 more seconds             │          │
│ • Allow biodiversity            │          │
│   calculations to complete      │          │
└────────────┬────────────────────┘          │
             │                               │
             ▼                               │
┌─────────────────────────────────┐          │
│ BiodiversityScoreManager        │          │
│ • Finds all ObservationDisplay  │          │
│ • Calculates Simpson's Index    │          │
│ • Creates biodiversity grid     │          │
└────────────┬────────────────────┘          │
             │                               │
             │◄──────────────────────────────┘
             │                User presses B key
             │                or clicks Inspector button
             │                or calls ForceSpawn()
             │
             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  BIO_SpawnInsideModifier.RespawnPrefabs()                                │
│                                                                           │
│  1. Get biodiversity score from BiodiversityScoreManager                 │
│     → float score = manager.GetSimpsonsIndexAtPosition(tileCenter)      │
│                                                                           │
│  2. Normalize score (0-1 range)                                          │
│     → float t = InverseLerp(minScore, maxScore, score)                  │
│                                                                           │
│  3. For each spawn position:                                             │
│     • Raycast to find ground                                             │
│     • Check minimum distance from other spawns                           │
│     • Choose prefab based on score:                                      │
│       - If Random.value < t → High biodiversity prefab                   │
│       - Else → Low biodiversity prefab                                   │
│     • Instantiate prefab                                                 │
│                                                                           │
│  4. Log results:                                                          │
│     "Spawned 87 total: 65 high-bio (74.7%), 22 low-bio (25.3%)"         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Manual Control Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     MANUAL CONTROL OPTIONS                               │
└───────────┬──────────────────────┬──────────────────────┬───────────────┘
            │                      │                      │
            ▼                      ▼                      ▼
┌──────────────────────┐  ┌──────────────────┐  ┌─────────────────────┐
│  Keyboard Shortcut   │  │ Inspector Button │  │  On-Screen GUI      │
│  Press 'B' key       │  │ Click button     │  │  Click debug button │
│  (in Play Mode)      │  │ (in Play Mode)   │  │  (in Play Mode)     │
└──────────┬───────────┘  └────────┬─────────┘  └──────────┬──────────┘
           │                       │                       │
           └───────────────────────┼───────────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │ BiodiversitySpawnController  │
                    │ .ForceSpawnBiodiversity()    │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │ BIO_SpawnInsideModifier      │
                    │ .ForceSpawn()                │
                    └──────────────┬───────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────────┐
                    │ Clears previous spawns       │
                    │ Respawns with current        │
                    │ biodiversity data            │
                    └──────────────────────────────┘
```

## Component Relationships

```
┌────────────────────────────────────────────────────────────────┐
│  Scene Hierarchy                                                │
│                                                                  │
│  AbstractMap (GameObject)                                       │
│  ├── BiodiversitySpawnController (MonoBehaviour) ◄─── YOU ADD  │
│  │   ├── map: AbstractMap reference                            │
│  │   ├── forceSpawnKey: KeyCode.B                              │
│  │   └── showDebugInfo: true/false                             │
│  │                                                               │
│  ├── VectorTile_001 (Generated at runtime)                     │
│  │   └── LandUsePoly (Has spawned prefabs as children)         │
│  │                                                               │
│  └── [Other map components]                                     │
│                                                                  │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│  Project Assets                                                 │
│                                                                  │
│  Mapbox/User/Modifiers/CUSTOM MODIFIERS/                       │
│  └── BIO_SPAWNGREEN.asset (ScriptableObject)                   │
│      Type: BIO_SpawnInsideModifier                              │
│      ├── lowBiodiversityPrefabs[] ◄─── ASSIGN PREFABS          │
│      ├── highBiodiversityPrefabs[] ◄─── ASSIGN PREFABS         │
│      ├── minBiodiversityScore: 0.0                             │
│      └── maxBiodiversityScore: 1.0                             │
│                                                                  │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│  Other Scene Objects                                            │
│                                                                  │
│  BiodiversityScoreManager (MonoBehaviour)                      │
│  ├── Monitors ObservationDisplay objects                       │
│  ├── Calculates Simpson's Index                                │
│  └── Provides GetSimpsonsIndexAtPosition()                     │
│                                                                  │
│  INaturalistMapController (MonoBehaviour)                      │
│  ├── Fetches observations from iNaturalist API                 │
│  └── Creates ObservationDisplay prefabs                        │
│                                                                  │
│  ObservationDisplay (Prefabs in scene)                         │
│  ├── Created by INaturalistMapController                       │
│  └── Used by BiodiversityScoreManager                          │
│                                                                  │
└────────────────────────────────────────────────────────────────┘
```

## Biodiversity Score → Prefab Selection Logic

```
Biodiversity Score Range: 0.0 (low) ────────────► 1.0 (high)

┌─────────────────────────────────────────────────────────────────┐
│ Score: 0.0 - 0.3 (Low Biodiversity)                             │
│ Result: ~90% low-biodiversity prefabs, ~10% high-biodiversity   │
│ Visual: Mostly dead trees, sparse vegetation                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Score: 0.3 - 0.7 (Medium Biodiversity)                          │
│ Result: Mixed prefabs, ~50/50 ratio                             │
│ Visual: Mix of healthy and sparse vegetation                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ Score: 0.7 - 1.0 (High Biodiversity)                            │
│ Result: ~90% high-biodiversity prefabs, ~10% low-biodiversity   │
│ Visual: Lush vegetation, healthy trees, flowers                 │
└─────────────────────────────────────────────────────────────────┘

Formula:
  normalized_score = (score - minScore) / (maxScore - minScore)
  if Random.value < normalized_score:
      spawn high-biodiversity prefab
  else:
      spawn low-biodiversity prefab
```

## Timeline Example

```
T = 0s    Game starts, map loads
          ↓
T = 2s    First tile generated
          → BIO_SpawnInsideModifier.Run() called
          → Monitoring coroutine starts
          ↓
T = 3s    INaturalistMapController fetches API data
          ↓
T = 5s    First observations loaded
          → MonitorCoroutine detects 0 → 10 observations
          ↓
T = 6s    More observations loaded
          → MonitorCoroutine detects 10 → 25 observations
          ↓
T = 7s    All observations loaded
          → MonitorCoroutine detects 25 → 25 observations (stable!)
          ↓
T = 9s    Wait period complete (2 seconds after stabilization)
          → BiodiversityScoreManager calculates scores
          → BIO_SpawnInsideModifier.RespawnPrefabs() called
          → Biodiversity prefabs spawn!
          ↓
T = 15s   [User presses 'B' key]
          → BiodiversitySpawnController.ForceSpawnBiodiversity()
          → Clears old prefabs
          → Respawns with updated biodiversity data
```

## Quick Reference

### Setup Checklist:
- ✓ BIO_SpawnInsideModifier asset created and configured
- ✓ Assigned to AbstractMap → Vector Layer → GameObject Modifiers
- ✓ lowBiodiversityPrefabs[] array has prefabs
- ✓ highBiodiversityPrefabs[] array has prefabs
- ✓ BiodiversitySpawnController added to AbstractMap GameObject
- ✓ BiodiversityScoreManager in scene
- ✓ INaturalistMapController in scene

### Debug Keys:
- **B** - Force spawn/respawn biodiversity prefabs
- **U** - BiodiversityScoreManager manual update (if enabled)
- **N** - ObservationNetworkManager manual trigger (if enabled)

### Console Log Pattern:
```
[BIO_SpawnInsideModifier] Monitoring for observations to load...
[iNaturalist] Loading observations...
[BiodiversityScoreManager] Observations loaded and stabilized! Count: 25
[BiodiversityScoreManager] Updated Simpson's diversity index
[BIO_SpawnInsideModifier] Spawning biodiversity prefabs...
[BIO_SpawnInsideModifier] Spawning 100 prefabs. Biodiversity Score: 0.756
[BIO_SpawnInsideModifier] ✓ Spawned 87 total: 65 high-bio, 22 low-bio
```
