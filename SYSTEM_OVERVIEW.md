# Unity Biodiversity Network Visualization - System Overview

## Project Identity
- **Project Name:** mth-map (Biodiversity Network Visualization)
- **Company:** pimika
- **Unity Version:** 2022.3.x
- **Rendering:** Universal Render Pipeline (URP) 14.0.11
- **Primary Platform:** WebGL (with PWA support)
- **Version:** 2.4

---

## Table of Contents
1. [Project Structure](#1-project-structure)
2. [Core Systems](#2-core-systems)
3. [C# Scripts by Functionality](#3-c-scripts-by-functionality)
4. [Data Flow Architecture](#4-data-flow-architecture)
5. [External API Integrations](#5-external-api-integrations)
6. [UI System](#6-ui-system)
7. [Game Flow & Scene Management](#7-game-flow--scene-management)
8. [Asset Pipeline](#8-asset-pipeline)
9. [WebGL & Networking](#9-webgl--networking)
10. [Build Configuration](#10-build-configuration)

---

## 1. Project Structure

### Directory Organization
```
Unity_MScProject-main/
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenu.unity          # Entry point menu
│   │   └── MapScene.unity          # Main game scene
│   ├── Scripts/                    # 91 C# scripts
│   │   ├── Core/                   # Main controllers
│   │   ├── UI/                     # UI management
│   │   ├── Network/                # Network visualization
│   │   ├── Biodiversity/           # Calculation & visualization
│   │   ├── Player/                 # Character control
│   │   └── _Obsolete/              # Deprecated code
│   ├── Prefabs/
│   │   ├── Player/                 # Character prefabs
│   │   ├── Observations/           # Species markers (plants, animals, birds, etc.)
│   │   └── UI/                     # UI panels
│   ├── Materials/
│   │   ├── Terrain/                # Ground materials with biodiversity shaders
│   │   ├── Water/                  # Stylised water URP materials
│   │   └── PostProcessing/         # Biodiversity visual filters
│   ├── Mapbox/                     # Mapbox SDK integration
│   ├── Packages/                   # Third-party assets
│   │   ├── LMHPOLY/                # Low poly nature bundle
│   │   ├── AcornBringer/           # Animal models
│   │   └── BitgemWater/            # Water shader
│   ├── TextMesh Pro/               # Text rendering system
│   └── WebGLTemplates/             # PWA build template
├── ProjectSettings/                # Unity configuration
└── Packages/                       # Package Manager dependencies
```

### Key Asset Packages
- **LMHPOLY Low Poly Nature Bundle** - Terrain, trees, rocks, environment
- **Acorn Bringer Animals** - Animated low poly animal models
- **Bitgem Stylised Water (URP)** - Water rendering
- **Kinematic Character Controller** - Player movement
- **Mapbox Unity SDK** - Map rendering and geospatial data

---

## 2. Core Systems

### System Architecture Overview
The project consists of 5 major interconnected systems:

```
┌─────────────────────────────────────────────────────────────────┐
│                      MAPBOX 3D TERRAIN                          │
│                    (Geospatial Foundation)                      │
└────────────────────┬────────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌───────────┐              ┌───────────┐
│  Player   │              │iNaturalist│
│  System   │              │   Data    │
│  (KCC)    │              │  Fetcher  │
└─────┬─────┘              └─────┬─────┘
      │                          │
      │                          ▼
      │                    ┌───────────┐
      │                    │Observation│
      │                    │ Displays  │
      │                    │  (N=200)  │
      │                    └─────┬─────┘
      │                          │
      ▼                          ▼
┌──────────────────────────────────────────┐
│         Biodiversity Calculator          │
│       (Simpson's Diversity Index)        │
└────────────┬─────────────────────────────┘
             │
    ┌────────┴────────┐
    ▼                 ▼
┌─────────┐    ┌────────────┐
│ Network │    │Biodiversity│
│ Connects│    │   Visual   │
│ (3D+2D) │    │  Filters   │
└─────────┘    └────────────┘
```

### System Descriptions

#### 1. **Mapbox Terrain System**
- Renders real-world 3D terrain using Mapbox SDK
- Provides coordinate conversion (GPS ↔ Unity World)
- Tile-based streaming as player moves
- Height sampling for ground detection

#### 2. **iNaturalist Data System**
- Fetches real biodiversity observations from iNaturalist API
- Spawns 3D prefabs at observation locations (plants, animals, birds, fungi, insects)
- Updates when player moves >300m
- Displays species info, photos, observer data

#### 3. **Biodiversity Calculation System**
- Calculates Simpson's Diversity Index on 50m grid cells
- Formula: `D = Σ[n_i × (n_i-1)] / [N × (N-1)]`
- Generates biodiversity hotspots
- Updates terrain materials and post-processing effects

#### 4. **Network Visualization System**
- **3D Networks:** LineRenderer connections between nearby observations
- **2D Minimap:** Shows observation positions on static map
- Species-based filtering
- Player-proximity triggered

#### 5. **Player System**
- Kinematic Character Controller for physics-based movement
- Integrated with Mapbox terrain height
- Mouse-look camera
- WASD movement + sprint + jump

---

## 3. C# Scripts by Functionality

### 3.1 Core Controllers (Main Data Integration)

#### iNaturalist Integration
| Script | Lines | Purpose |
|--------|-------|---------|
| [INaturalistMapController.cs](Assets/Scripts/INaturalistMapController.cs) | 1565 | Primary API integration; fetches observations; spawns prefabs; ground detection |
| [ObservationDisplay.cs](Assets/Scripts/ObservationDisplay.cs) | ~200 | Individual observation UI card; species name, photo, date |
| [ObservationTriggerInteraction.cs](Assets/Scripts/ObservationTriggerInteraction.cs) | ~100 | Collision detection; triggers network connections |

#### Mapbox Integration
| Script | Lines | Purpose |
|--------|-------|---------|
| [MapboxKCCAdapter.cs](Assets/Scripts/MapboxKCCAdapter.cs) | 305 | Connects KCC to Mapbox terrain; height sampling; ground detection |

### 3.2 Biodiversity System

#### Core Calculations
| Script | Lines | Purpose |
|--------|-------|---------|
| [BiodiversityScoreManager.cs](Assets/Scripts/Biodiversity/BiodiversityScoreManager.cs) | 750 | Simpson's Index calculator; grid-based analysis; hotspot generation |
| [BiodiversityPrefabSpawner.cs](Assets/Scripts/Biodiversity/BiodiversityPrefabSpawner.cs) | 350 | Spawns vegetation based on diversity scores |

#### Visual Effects
| Script | Purpose |
|--------|---------|
| [BiodiversityFullScreenFeature.cs](Assets/Scripts/Biodiversity/BiodiversityFullScreenFeature.cs) | URP Render Feature for post-processing |
| [BiodiversityVolumeComponent.cs](Assets/Scripts/Biodiversity/BiodiversityVolumeComponent.cs) | Volume-based biodiversity effects |
| [BiodiversityCameraFilter.cs](Assets/Scripts/Biodiversity/BiodiversityCameraFilter.cs) | Camera shader integration |
| [BiodiversityMaterialController.cs](Assets/Scripts/Biodiversity/BiodiversityMaterialController.cs) | Material saturation control |

### 3.3 Network Visualization

#### 3D Network System
| Script | Lines | Purpose |
|--------|-------|---------|
| [ObservationNetworkManager.cs](Assets/Scripts/Network/ObservationNetworkManager.cs) | 943 | Creates LineRenderer connections; species filtering; proximity processing |
| [NetworkConnection.cs](Assets/Scripts/Network/NetworkConnection.cs) | ~100 | Individual connection line component |
| [ObservationNetworkUI.cs](Assets/Scripts/UI/ObservationNetworkUI.cs) | ~150 | Network control panel; filter toggles |

#### 2D Minimap System
| Script | Lines | Purpose |
|--------|-------|---------|
| [StaticMapMinimap.cs](Assets/Scripts/StaticMapMinimap.cs) | 390 | Mapbox Static Images API; 1024x1024 map; auto-regeneration |
| [MinimapImageMarker.cs](Assets/Scripts/MinimapImageMarker.cs) | ~50 | Observation markers on minimap |
| [MinimapImageMarkerManager.cs](Assets/Scripts/MinimapImageMarkerManager.cs) | ~100 | Marker lifecycle management |

### 3.4 Player & Camera

| Script | Purpose |
|--------|---------|
| ExampleCharacterController | WASD movement, jumping, sprint (from KCC package) |
| ExampleCharacterCamera | Mouse-look camera (from KCC package) |
| KinematicCharacterMotor | Physics-based movement (from KCC package) |

### 3.5 UI Controllers

| Script | Lines | Purpose |
|--------|-------|---------|
| [MainMenuManager.cs](Assets/Scripts/UI/MainMenuManager.cs) | 178 | Main menu system; start game; about/credits/controls |
| [InGameUIController.cs](Assets/Scripts/UI/InGameUIController.cs) | 209 | In-game interface; controls help (H); pause menu (ESC) |
| [UIManager.cs](Assets/Scripts/UI/UIManager.cs) | ~100 | Additional UI coordination |

### 3.6 Terrain & Vegetation

| Script | Purpose |
|--------|---------|
| [OptimizedGrassPatchSpawner.cs](Assets/Scripts/OptimizedGrassPatchSpawner.cs) | Object pooling grass spawner; 80m radius; incremental |
| [ElevationBasedMaterial.cs](Assets/Scripts/ElevationBasedMaterial.cs) | Material changes by altitude |
| [GrassExclusionMarker.cs](Assets/Scripts/GrassExclusionMarker.cs) | Prevents grass near observations |

### 3.7 Water System

| Script | Purpose |
|--------|---------|
| WaterSurfaceConformModifier | Water vertices conform to terrain |
| WaterHeightOffsetModifier | Uniform Y-axis water offset |
| WaterConformToTerrainModifier | Alternative conforming approach |
| WaterRenderQueueModifier | Transparency render queue |

### 3.8 WebGL Support

| Script | Purpose |
|--------|---------|
| [WebGLNetworkBridge.cs](Assets/Scripts/WebGL/WebGLNetworkBridge.cs) | JavaScript bridge for CORS bypass; Fetch API wrapper |
| [WebGLCorsHelper.cs](Assets/Scripts/WebGL/WebGLCorsHelper.cs) | CORS configuration helpers |

### 3.9 Debug & Tools

| Script | Purpose |
|--------|---------|
| DebugCoordinateOverlay | Coordinate info overlay |
| CanvasDebugHelper | Canvas debugging utilities |
| BiodiversitySetupGuide | Setup documentation script |

---

## 4. Data Flow Architecture

### Primary Data Flow

```
┌──────────────────────────────────────────────────────────────┐
│ 1. APPLICATION START                                         │
└────────────┬─────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 2. MAPBOX INITIALIZATION                                     │
│    - Load terrain tiles                                      │
│    - Initialize coordinate system                            │
└────────────┬─────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 3. PLAYER SPAWN                                              │
│    - Position player on terrain (MapboxKCCAdapter)           │
│    - Get GPS coordinates from Unity position                 │
└────────────┬─────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 4. iNATURALIST DATA FETCH (INaturalistMapController)         │
│    - Calculate bounding box (300m radius from player)        │
│    - API Request: GET /v1/observations?swlng=...&nelat=...   │
│    - Receive JSON array of observations (max 200)            │
└────────────┬─────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 5. OBSERVATION SPAWNING                                      │
│    - For each observation:                                   │
│      • Convert GPS → Unity world position                    │
│      • Raycast to find ground height                         │
│      • Select prefab based on taxon (plant/animal/bird...)   │
│      • Instantiate ObservationDisplay prefab                 │
│      • Populate UI canvas with species data                  │
└────────────┬─────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 6. BIODIVERSITY CALCULATION (BiodiversityScoreManager)       │
│    - Create 50m × 50m grid cells                             │
│    - Count species per cell                                  │
│    - Calculate Simpson's Index per cell                      │
│    - Generate hotspot data                                   │
│    - Update terrain material properties                      │
└────────────┬─────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 7. PLAYER MOVEMENT                                           │
│    - WASD input → KinematicCharacterMotor                    │
│    - Update terrain height via MapboxKCCAdapter              │
│    - Check distance from last fetch position                 │
└────────────┬─────────────────────────────────────────────────┘
             │
       ┌─────┴─────┐
       │ >300m?    │
       └───┬───┬───┘
           │   │
          NO  YES
           │   │
           │   └──► Go to Step 4 (re-fetch observations)
           │
           ▼
┌──────────────────────────────────────────────────────────────┐
│ 8. PROXIMITY TRIGGERS                                        │
│    - Player near observation → ObservationTriggerInteraction │
│    - Trigger NetworkManager to create connection             │
│    - Show observation UI canvas                              │
└──────────────────────────────────────────────────────────────┘
```

### Secondary Data Flows

#### Minimap System Flow
```
Player Movement → StaticMapMinimap
                      ↓
            Check movement > 200m threshold?
                      ↓
            YES → Build Mapbox Static Images URL
                      ↓
            Fetch 1024×1024 PNG (WebGL bridge or UnityWebRequest)
                      ↓
            Convert to Texture2D
                      ↓
            Update RawImage on UI Canvas
                      ↓
            Convert observations GPS → pixel coordinates
                      ↓
            Update marker positions
```

#### Network Visualization Flow
```
Player presses 'N' → ObservationNetworkManager
                      ↓
            Get all ObservationDisplay objects
                      ↓
            For each pair within max distance:
              - Check species filter
              - Create LineRenderer connection
              - Add to connection pool
                      ↓
            Display connection count in UI
                      ↓
            Update every frame (distance check)
```

---

## 5. External API Integrations

### 5.1 iNaturalist API (Primary Data Source)

**Endpoint:** `https://api.inaturalist.org/v1/observations`
**Authentication:** Public (no API key required)
**Rate Limits:** Unknown (public tier)

#### Request Parameters
| Parameter | Value | Purpose |
|-----------|-------|---------|
| `swlng`, `swlat` | Float | Southwest corner of bounding box |
| `nelng`, `nelat` | Float | Northeast corner of bounding box |
| `per_page` | 200 | Max observations per request |
| `quality_grade` | research, needs_id, casual | Quality filter |
| `captive` | any | Include/exclude captive species |
| `has[]` | photos | Require photos |
| `order_by` | distance | Sort by proximity |

#### Response Structure
```json
{
  "total_results": 150,
  "results": [
    {
      "id": 12345,
      "observed_on": "2024-03-15",
      "location": "51.5074,-0.1278",
      "taxon": {
        "id": 789,
        "name": "Quercus robur",
        "preferred_common_name": "English Oak",
        "iconic_taxon_name": "Plantae",
        "rank": "species"
      },
      "photos": [
        {
          "url": "https://static.inaturalist.org/photos/...",
          "medium_url": "...",
          "small_url": "..."
        }
      ],
      "user": {
        "login": "naturalist123",
        "name": "Jane Doe"
      },
      "quality_grade": "research",
      "captive": false
    }
  ]
}
```

#### Data Processing
1. Parse JSON response
2. Filter by quality grade (configurable)
3. Convert GPS coordinates to Unity world positions
4. Categorize by taxon (Plantae → plant prefab, Aves → bird prefab, etc.)
5. Download observation photos via additional UnityWebRequest
6. Instantiate prefabs with data

### 5.2 Mapbox APIs

#### A. Mapbox Maps SDK
**Type:** Unity SDK Integration
**Authentication:** Access token (stored in project settings)
**Usage:** 3D terrain tile streaming

**Features:**
- Real-time terrain mesh generation
- Height sampling API
- Coordinate conversion (GPS ↔ Unity)
- Tile caching

#### B. Mapbox Static Images API
**Endpoint:** `https://api.mapbox.com/styles/v1/mapbox/{style}/static/{lon},{lat},{zoom}/{width}x{height}`
**Authentication:** Access token in URL
**Rate Limits:** Monthly quota (free tier: 50,000 requests)

**Styles Available:**
- `streets-v11` - Street map
- `outdoors-v11` - Terrain with trails
- `satellite-streets-v11` - Satellite imagery

**Example Request:**
```
https://api.mapbox.com/styles/v1/mapbox/outdoors-v11/static/
-0.1278,51.5074,13/1024x1024?access_token=pk.ey...
```

**Usage:** Generated for minimap every 200m player movement

### 5.3 Xeno-Canto API (Bird Sounds)

**Endpoint:** `https://xeno-canto.org/api/2/recordings`
**Authentication:** API key (hardcoded in BirdAudioController)
**Status:** Currently returning 404 errors (mock mode active)

**Intended Usage:**
- Fetch bird call recordings by species name
- Play audio when player approaches bird observations
- Not currently functional

### 5.4 API Error Handling

#### WebGL CORS Strategy
- **Problem:** Browser CORS policies block external requests
- **Solution:** JavaScript bridge (WebGLNetworkBridge.cs)
  - Uses native Fetch API with proper headers
  - Bypasses Unity's UnityWebRequest CORS limitations

#### Fallback Logic
```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    // Use JavaScript bridge
    WebGLNetworkBridge.Instance.FetchJSON(url, OnSuccess, OnError);
#else
    // Use UnityWebRequest
    UnityWebRequest request = WebGLCorsHelper.CreateCorsRequest(url);
    yield return request.SendWebRequest();
#endif
```

#### Error Callbacks
- API timeout → Show error message to user
- Invalid JSON → Log error, skip observation
- Missing photos → Use placeholder texture
- Network unreachable → Retry after delay

---

## 6. UI System

### 6.1 Canvas Hierarchy

```
MainCanvas (Screen Space - Overlay)
├── MainMenuPanel
│   ├── TitleText ("Biodiversity Network Visualization")
│   ├── ButtonPanel
│   │   ├── StartButton → MainMenuManager.StartGame()
│   │   ├── AboutButton → Show AboutPanel
│   │   ├── CreditsButton → Show CreditsPanel
│   │   ├── ControlsButton → Show ControlsPanel
│   │   └── ExitButton → Application.Quit()
│   ├── AboutPanel (hidden)
│   │   └── AboutText + CloseButton
│   ├── CreditsPanel (hidden)
│   │   └── CreditsText + CloseButton
│   └── ControlsPanel (hidden)
│       └── ControlsText + CloseButton
│
├── InGameUI (disabled until game starts)
│   ├── TopLeftPanel
│   │   ├── InstructionsText ("Press H for controls")
│   │   └── NetworkStatusText ("Network: OFF")
│   ├── MinimapPanel (BottomRight)
│   │   ├── MinimapRawImage (1024×1024)
│   │   ├── PlayerMarker (arrow sprite)
│   │   └── ObservationMarkers (dynamically added)
│   ├── ControlsHelpPanel (hidden, toggle with H)
│   │   ├── ControlsText (WASD, Mouse, H, N, L, B, etc.)
│   │   └── CloseButton
│   └── PauseMenuPanel (hidden, toggle with ESC)
│       ├── PauseTitleText
│       ├── ResumeButton → InGameUIController.ResumeGame()
│       ├── MainMenuButton → Load MainMenu scene
│       └── ExitButton → Application.Quit()
│
├── NetworkUIPanel (BottomLeft)
│   ├── EnableNetworkToggle → ObservationNetworkUI.ToggleNetwork()
│   ├── SpeciesFilterPanel
│   │   ├── PlantsToggle
│   │   ├── AnimalsToggle
│   │   ├── BirdsToggle
│   │   ├── FungiToggle
│   │   └── InsectsToggle
│   └── ConnectionCountText ("Connections: 42")
│
└── BiodiversityUIPanel (TopRight)
    ├── EnableBiodiversityToggle
    ├── IntensitySlider
    └── RecalculateButton → BiodiversityScoreManager.RecalculateAll()
```

### 6.2 World Space UI

Each ObservationDisplay prefab contains:
```
ObservationPrefab
├── Model (3D mesh - plant/animal/bird/etc)
├── Collider (SphereCollider for interaction)
└── ObservationCanvas (World Space)
    ├── BackgroundPanel
    ├── SpeciesNameText (TextMeshPro)
    ├── CommonNameText (TextMeshPro)
    ├── ObserverText (TextMeshPro)
    ├── DateText (TextMeshPro)
    ├── PhotoImage (RawImage)
    └── AudioIndicator (for birds)
```

**Visibility Logic:**
- Canvas faces camera (Billboard behavior)
- Shows only when player within proximity threshold (~10m)
- Scales with distance

### 6.3 Input Mapping

| Key | Action | Controller |
|-----|--------|------------|
| **WASD** | Move player | ExampleCharacterController |
| **Mouse** | Rotate camera | ExampleCharacterCamera |
| **Shift** | Sprint | ExampleCharacterController |
| **Space** | Jump | ExampleCharacterController |
| **H** | Toggle controls help | InGameUIController |
| **N** | Toggle 3D network | ObservationNetworkManager |
| **L** | Toggle minimap connections | StaticMapMinimap |
| **B** | Toggle biodiversity visualization | BiodiversityMaterialController |
| **O** | Force reload observations | INaturalistMapController |
| **U** | Force biodiversity recalculation | BiodiversityScoreManager |
| **ESC** | Pause menu | InGameUIController |

### 6.4 UI Controllers

#### MainMenuManager.cs
```csharp
public void StartGame() {
    // Hide menu panels
    mainMenuPanel.SetActive(false);
    aboutPanel.SetActive(false);
    creditsPanel.SetActive(false);
    controlsPanel.SetActive(false);

    // Enable game components
    playerController.enabled = true;
    cameraController.enabled = true;
    mapController.enabled = true;

    // Show in-game UI
    inGameUI.SetActive(true);

    // Lock cursor
    Cursor.lockState = CursorLockMode.Locked;
}
```

#### InGameUIController.cs
```csharp
void Update() {
    if (Input.GetKeyDown(KeyCode.H)) {
        ToggleControlsHelp();
    }

    if (Input.GetKeyDown(KeyCode.Escape)) {
        if (isPaused) ResumeGame();
        else PauseGame();
    }
}

public void PauseGame() {
    Time.timeScale = 0f;
    pauseMenuPanel.SetActive(true);
    Cursor.lockState = CursorLockMode.None;
    isPaused = true;
}

public void ResumeGame() {
    Time.timeScale = 1f;
    pauseMenuPanel.SetActive(false);
    Cursor.lockState = CursorLockMode.Locked;
    isPaused = false;
}
```

---

## 7. Game Flow & Scene Management

### 7.1 Scene Structure

#### Scenes in Build
1. **MapScene.unity** (Index 0) - Only scene in build
   - Path: `Assets/Scenes/MapScene.unity`
   - Contains both main menu and game (single-scene architecture)

### 7.2 Application Flow

```
┌─────────────────────────────────────────────────────────────┐
│ APPLICATION START                                           │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ MapScene.unity LOADS                                        │
│ ├── Mapbox terrain (disabled)                              │
│ ├── Player (disabled)                                      │
│ ├── UI Canvas                                              │
│ │   ├── MainMenuPanel (active)                            │
│ │   └── InGameUI (disabled)                               │
│ └── Controllers (disabled)                                 │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ MAIN MENU STATE                                             │
│ - Show title, buttons                                      │
│ - User can view About/Credits/Controls                     │
│ - Time.timeScale = 1, but no movement                      │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ User clicks "Start"
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ GAME INITIALIZATION (MainMenuManager.StartGame())          │
│ 1. Hide main menu panels                                   │
│ 2. Enable player controller + camera                       │
│ 3. Enable map controller                                   │
│ 4. Show in-game UI                                         │
│ 5. Lock cursor                                             │
│ 6. Start 2-second delay timer                              │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ MAPBOX INITIALIZATION                                       │
│ - Load terrain tiles around player spawn                   │
│ - Initialize coordinate system                             │
│ - OnInitialized event → Trigger iNaturalist fetch          │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ FIRST DATA FETCH                                            │
│ - INaturalistMapController fetches observations            │
│ - Spawn ObservationDisplay prefabs                         │
│ - BiodiversityScoreManager calculates Simpson's Index      │
│ - StaticMapMinimap generates first minimap                 │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ GAME LOOP (Active Gameplay)                                │
│                                                             │
│ Every Frame:                                                │
│ ├── Process player input (WASD, Mouse)                     │
│ ├── Update player position (KinematicCharacterMotor)       │
│ ├── Sample terrain height (MapboxKCCAdapter)               │
│ ├── Check observation proximity (ObservationDisplay)       │
│ ├── Update network connections (ObservationNetworkManager) │
│ └── Update minimap marker rotation (StaticMapMinimap)      │
│                                                             │
│ Every 3 Seconds:                                            │
│ └── Recalculate biodiversity (BiodiversityScoreManager)    │
│                                                             │
│ When Player Moves >300m:                                    │
│ ├── Re-fetch iNaturalist observations                      │
│ ├── Clear old prefabs                                      │
│ └── Spawn new observations                                 │
│                                                             │
│ When Player Moves >200m:                                    │
│ └── Regenerate minimap (StaticMapMinimap)                  │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        │ User presses ESC
                        ▼
┌─────────────────────────────────────────────────────────────┐
│ PAUSE STATE                                                 │
│ - Time.timeScale = 0 (freeze game)                         │
│ - Show pause menu                                          │
│ - Unlock cursor                                            │
│ - Options: Resume / Main Menu / Exit                       │
└───────────────────────┬─────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
    Resume        Main Menu          Exit
        │               │               │
        ▼               ▼               ▼
   Back to Game    Reload Scene   Application.Quit()
```

### 7.3 State Management

#### Game States
1. **Main Menu** - Menu UI active, game disabled
2. **Playing** - Game active, Time.timeScale = 1
3. **Paused** - Game frozen, Time.timeScale = 0

#### State Transitions
```csharp
// Main Menu → Playing
MainMenuManager.StartGame() {
    DisableMenuPanels();
    EnableGameComponents();
    Cursor.lockState = CursorLockMode.Locked;
    gameState = GameState.Playing;
}

// Playing ↔ Paused
InGameUIController.PauseGame() {
    Time.timeScale = 0f;
    ShowPauseMenu();
    Cursor.lockState = CursorLockMode.None;
    gameState = GameState.Paused;
}

InGameUIController.ResumeGame() {
    Time.timeScale = 1f;
    HidePauseMenu();
    Cursor.lockState = CursorLockMode.Locked;
    gameState = GameState.Playing;
}
```

### 7.4 Performance Optimizations

#### Update Throttling
- Biodiversity calculations: Every 3 seconds (not every frame)
- Minimap regeneration: Only on 200m movement threshold
- Network connections: Process max 25 observations per frame

#### Distance-Based Culling
- Observations beyond 300m: Cleared and re-fetched
- Grass patches beyond 80m: Destroyed
- Network connections: Max distance configurable (default 100m)

#### Object Pooling
- NetworkConnection LineRenderers: Pooled and reused
- Grass patches: Incremental spawning, reuse destroyed patches

---

## 8. Asset Pipeline

### 8.1 Asset Categories

#### 3D Models
**Source:** Third-party asset packages
- **LMHPOLY Nature Bundle** - Trees, rocks, terrain props (optimized to lower res)
- **Acorn Bringer Animals** - Low poly animated animals
- Custom observation markers (simple geometric shapes)

**Usage:**
- Instantiated as prefabs via scripts
- Taxon-specific selection (e.g., bird observations → bird model)

#### Materials
**Terrain Materials:**
- Base material with albedo texture
- Biodiversity shader properties:
  - `_BiodiversityScore` (float)
  - `_Saturation` (float)
  - `_ColorGradient` (texture ramp)

**Water Materials:**
- Bitgem Stylised Water shader (URP)
- Modifiers: WaterSurfaceConformModifier, WaterHeightOffsetModifier

**Post-Processing Materials:**
- BiodiversityFullScreenFeature shader
- Applied via URP Render Feature

#### Textures
**Static Textures:**
- UI sprites (buttons, icons)
- Terrain albedo maps
- Water normal maps

**Dynamic Textures:**
- iNaturalist observation photos (downloaded at runtime)
- Minimap images (fetched from Mapbox API)

#### Prefabs
**Player:**
- ExamplePlayer prefab (KCC + Camera)

**Observations:**
- PlantObservation prefab
- AnimalObservation prefab
- BirdObservation prefab
- FungiObservation prefab
- InsectObservation prefab

**UI:**
- ObservationCanvas prefab (world space)
- NetworkPanel prefab
- MinimapPanel prefab

### 8.2 Loading Strategies

#### Static Loading (Build Time)
- All prefabs compiled into asset bundles
- Materials baked into build
- Textures compressed (ASTC/DXT)

#### Dynamic Loading (Runtime)
```csharp
// iNaturalist photos
UnityWebRequest request = UnityWebRequestTexture.GetTexture(photoUrl);
yield return request.SendWebRequest();
Texture2D photo = DownloadHandlerTexture.GetContent(request);
observationImage.texture = photo;

// Minimap images
#if UNITY_WEBGL
    WebGLNetworkBridge.Instance.FetchTexture(minimapUrl, OnSuccess, OnError);
#else
    UnityWebRequest request = UnityWebRequestTexture.GetTexture(minimapUrl);
    yield return request.SendWebRequest();
    Texture2D minimap = DownloadHandlerTexture.GetContent(request);
#endif

// Prefab instantiation
GameObject prefab = GetPrefabForTaxon(taxonName);
GameObject instance = Instantiate(prefab, position, rotation);
```

#### Resource Management
- **Observation photos:** Cached in memory, cleared on re-fetch
- **Minimap textures:** Replaced every 200m, previous destroyed
- **Prefabs:** Destroyed when player moves >300m, re-instantiated

### 8.3 Memory Optimization

#### Texture Compression
- LMHPOLY textures: Optimized to lower resolution
- UI sprites: ASTC/DXT5 compression
- Photos: Downloaded as medium size (not full resolution)

#### Mesh Optimization
- Low poly models (< 5000 tris)
- No LOD system (single-level)
- Static batching enabled

#### Asset Bundles
- Not explicitly used
- Unity handles bundling for WebGL build

---

## 9. WebGL & Networking

### 9.1 WebGL Architecture

#### CORS Problem
**Issue:** Browser security prevents direct API calls from WebGL builds

**Manifestation:**
```
Access to fetch at 'https://api.inaturalist.org/...' from origin 'https://yoursite.com'
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present
on the requested resource.
```

#### Solution: JavaScript Bridge

**WebGLNetworkBridge.cs** (C# → JavaScript)
```csharp
[DllImport("__Internal")]
private static extern void FetchJSON(string url, string objectName, string callbackMethod, string errorMethod);

public void FetchJSON(string url, Action<string> onSuccess, Action<string> onError) {
    #if UNITY_WEBGL && !UNITY_EDITOR
        FetchJSON(url, gameObject.name, "OnJSONSuccess", "OnJSONError");
    #endif
}
```

**Plugins/WebGL/NetworkBridge.jslib** (JavaScript implementation)
```javascript
mergeInto(LibraryManager.library, {
    FetchJSON: function(url, objectName, successCallback, errorCallback) {
        var urlStr = UTF8ToString(url);

        fetch(urlStr, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        })
        .then(response => response.json())
        .then(data => {
            SendMessage(UTF8ToString(objectName), UTF8ToString(successCallback), JSON.stringify(data));
        })
        .catch(error => {
            SendMessage(UTF8ToString(objectName), UTF8ToString(errorCallback), error.toString());
        });
    }
});
```

#### Texture Handling
```javascript
FetchTexture: function(url, objectName, successCallback, errorCallback) {
    var urlStr = UTF8ToString(url);

    fetch(urlStr)
    .then(response => response.blob())
    .then(blob => {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    })
    .then(base64 => {
        // Strip "data:image/png;base64," prefix
        const base64Data = base64.split(',')[1];
        SendMessage(UTF8ToString(objectName), UTF8ToString(successCallback), base64Data);
    })
    .catch(error => {
        SendMessage(UTF8ToString(objectName), UTF8ToString(errorCallback), error.toString());
    });
}
```

**C# Decoding:**
```csharp
void OnTextureSuccess(string base64Data) {
    byte[] imageBytes = System.Convert.FromBase64String(base64Data);
    Texture2D texture = new Texture2D(2, 2);
    texture.LoadImage(imageBytes);
    minimapImage.texture = texture;
}
```

### 9.2 Network Error Handling

#### Retry Logic
```csharp
private int retryCount = 0;
private const int MAX_RETRIES = 3;

void FetchObservations() {
    WebGLNetworkBridge.Instance.FetchJSON(url, OnSuccess, OnError);
}

void OnError(string error) {
    if (retryCount < MAX_RETRIES) {
        retryCount++;
        Debug.LogWarning($"Fetch failed, retrying ({retryCount}/{MAX_RETRIES})...");
        Invoke(nameof(FetchObservations), 2f); // Retry after 2 seconds
    } else {
        Debug.LogError($"Fetch failed after {MAX_RETRIES} attempts: {error}");
        ShowErrorMessage("Unable to load observations. Please check your connection.");
    }
}
```

#### Timeout Handling
- UnityWebRequest timeout: 30 seconds (default)
- JavaScript fetch timeout: Not explicitly set (browser default ~60s)

#### Fallback Strategies
- **No observations:** Show message "No observations found in this area"
- **No minimap:** Show placeholder texture
- **No photos:** Show placeholder "No image" texture

### 9.3 WebGL Build Settings

#### Memory Configuration
```
Initial Memory: 512MB
Maximum Memory: 2048MB
Growth Step: 0.2 (geometric growth)
Growth Cap: 96MB per step
```

#### Optimization Settings
- **Compression:** Enabled (format: Gzip)
- **Exception Support:** Full (for debugging)
- **Data Caching:** Enabled
- **Name Files As Hashes:** Disabled
- **Decompression Fallback:** Enabled
- **Linker Target:** WebAssembly (WASM)

#### PWA Configuration
**Template:** APPLICATION:PWA

**Features:**
- Offline caching (service worker)
- Installable on mobile/desktop
- Fullscreen mode support
- Manifest.json for app metadata

**Manifest Structure:**
```json
{
  "name": "Biodiversity Network Visualization",
  "short_name": "mth-map",
  "start_url": "./index.html",
  "display": "fullscreen",
  "background_color": "#87CEEB",
  "theme_color": "#87CEEB",
  "icons": [
    {
      "src": "icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

---

## 10. Build Configuration

### 10.1 Platform Settings

#### WebGL (Primary)
**Settings:**
- Template: `APPLICATION:PWA`
- Exception Support: Full
- Compression: Enabled (Gzip)
- Memory: 512MB initial / 2048MB max
- WebGL Threads: Disabled
- Data Caching: Enabled

**Output:**
- `Build/`
  - `index.html`
  - `Build.data` (asset data)
  - `Build.wasm` (compiled code)
  - `Build.framework.js` (Unity runtime)
  - `Build.loader.js` (initialization)
  - `manifest.json` (PWA metadata)
  - `service-worker.js` (offline caching)

#### Standalone (Secondary)
**Platforms:**
- Windows (x64)
- macOS (Universal)
- Linux (x64)

**Settings:**
- Fullscreen Mode: Windowed
- Default Resolution: 1920×1080
- Resizable Window: Yes
- Static Batching: Enabled
- Dynamic Batching: Disabled

### 10.2 Quality Settings

**URP Asset:** `UniversalRP-HighQuality.asset`

**Settings:**
- **Anti-Aliasing:** MSAA (fallback: 0)
- **Shadows:** Enabled, soft shadows
- **Render Scale:** 1.0
- **Post-Processing:** Enabled
- **HDR:** Enabled
- **MSAA:** 4x (desktop), 2x (mobile)
- **Depth Texture:** Enabled
- **Opaque Texture:** Enabled

### 10.3 Graphics Pipeline

#### Universal Render Pipeline Features
1. **Forward Renderer**
   - Opaque pass
   - Transparent pass
   - Post-processing pass

2. **Custom Render Features:**
   - BiodiversityFullScreenFeature (post-processing)
   - Water transparency handling

3. **Shader Graph:**
   - Terrain biodiversity shaders
   - Water surface shaders

#### Render Order
```
1. Skybox
2. Opaque geometry (terrain, buildings)
3. Biodiversity post-processing pass
4. Transparent geometry (water)
5. UI overlay
```

### 10.4 Package Dependencies

**Package Manifest (`Packages/manifest.json`):**
```json
{
  "dependencies": {
    "com.unity.render-pipelines.universal": "14.0.11",
    "com.unity.textmeshpro": "3.0.7",
    "com.unity.timeline": "1.7.6",
    "com.unity.visualscripting": "1.9.4",
    "com.unity.collab-proxy": "2.10.1",
    "com.unity.feature.development": "1.0.1"
  }
}
```

**Third-Party Assets:**
- Mapbox Unity SDK (embedded)
- Kinematic Character Controller (embedded)
- LMHPOLY Nature Bundle (imported)
- Acorn Bringer Animals (imported)
- Bitgem Stylised Water (imported)

### 10.5 Build Optimization

#### Code Stripping
- **Managed Stripping Level:** High (WebGL)
- **Strip Engine Code:** Enabled
- **Unused Code Stripping:** IL2CPP (iOS/Android)

#### Asset Compression
- **Textures:** ASTC (Android), PVRTC (iOS), DXT (desktop)
- **Audio:** Vorbis compression
- **Meshes:** Vertex compression enabled (mask: 4054)

#### Size Optimization
**Estimated Build Sizes:**
- WebGL: ~150MB (compressed)
- Windows Standalone: ~200MB
- macOS Standalone: ~220MB

**Size Breakdown:**
- Code (WASM): ~30MB
- Assets (textures, models): ~100MB
- Shaders: ~10MB
- Audio: ~5MB
- UI: ~5MB

---

## Summary

This Unity project is a **real-world biodiversity data visualization tool** that:

1. **Fetches live ecological data** from iNaturalist API
2. **Visualizes biodiversity** using Simpson's Diversity Index on 3D terrain
3. **Creates network connections** between species observations
4. **Provides interactive exploration** via KCC player movement
5. **Displays data on minimap** using Mapbox Static Images API
6. **Runs in web browsers** as a PWA with CORS bypass via JavaScript bridge

### Key Technologies
- Unity 2022.3.x + URP 14.0.11
- Mapbox Unity SDK for geospatial data
- iNaturalist API for biodiversity observations
- Kinematic Character Controller for player movement
- TextMesh Pro for UI
- JavaScript interop for WebGL CORS handling

### Performance Characteristics
- Handles 200 observations simultaneously
- Updates biodiversity every 3 seconds
- Re-fetches data every 300m player movement
- Supports up to 200 network connections
- Runs in browsers at 30-60 FPS (WebGL)

### Architecture Highlights
- Single-scene architecture (MapScene.unity)
- Component-based design (Unity ECS pattern)
- Object pooling for performance
- Distance-based culling
- API wrapper pattern for external services
- JavaScript bridge for WebGL CORS bypass
