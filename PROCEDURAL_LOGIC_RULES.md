# Procedural Logic & Rules - Biodiversity Network Visualization

## Overview
This document abstracts the procedural logic, algorithms, and rules used in the biodiversity visualization system, making them applicable for documentation, replication, or adaptation to other platforms.

---

## 1. Data Fetching & Initialization Rules

### Rule 1.1: Geospatial Bounding Box Query
**Purpose:** Fetch observations within player's vicinity
**Algorithm:**
```
WHEN player position changes OR initial load
THEN:
  1. Get player GPS coordinates (lat, lon)
  2. Calculate bounding box with radius R (default: 300m)
     - Southwest corner: (lat - R, lon - R)
     - Northeast corner: (lat + R, lon + R)
  3. Construct API query with bbox parameters
  4. Filter by quality_grade, captive status, photo requirement
  5. Limit results to N (default: 200)
  6. Sort by distance from player
```

**Parameters:**
- Fetch radius: 300m
- Max results: 200 observations
- Quality grades: research, needs_id, casual
- Required: photos=true

### Rule 1.2: Delayed Initialization
**Purpose:** Prevent race conditions and ensure terrain is ready
**Algorithm:**
```
WHEN game starts
THEN:
  1. Wait for terrain system initialization
  2. Add delay timer D (default: 2 seconds)
  3. AFTER delay, trigger first data fetch
```

**Parameters:**
- Initialization delay: 2 seconds

### Rule 1.3: Movement-Based Re-fetch
**Purpose:** Keep data relevant to current location
**Algorithm:**
```
EVERY frame:
  1. Calculate distance D between current position and last fetch position
  2. IF D > threshold (default: 300m) THEN:
     - Clear all existing observation objects
     - Trigger new data fetch at current position
     - Store current position as last fetch position
```

**Parameters:**
- Re-fetch threshold: 300m
- Prefab cleanup: destroy all previous observations

---

## 2. Spatial Positioning Rules

### Rule 2.1: GPS to World Coordinate Conversion
**Purpose:** Place observations in 3D world space
**Algorithm:**
```
FOR each observation with GPS coordinates (lat, lon):
  1. Convert GPS → 3D world position using terrain coordinate system
  2. Set initial Y position to high value (e.g., 1000m)
  3. Apply ground detection (Rule 2.2)
```

### Rule 2.2: Ground Detection (Multi-Strategy Raycast)
**Purpose:** Place observations on terrain surface
**Algorithm:**
```
GIVEN world position (x, y, z):
  Strategy 1: Downward raycast
    1. Start from (x, 1000, z)
    2. Raycast downward with max distance M (e.g., 2000m)
    3. IF hit terrain THEN use hit.point.y as ground height

  Strategy 2: Upward raycast (fallback)
    1. Start from (x, -100, z)
    2. Raycast upward
    3. IF hit terrain THEN use hit.point.y

  Strategy 3: Terrain height sampling (fallback)
    1. Query terrain system for height at (x, z)
    2. Use sampled height

  Final:
    4. Add vertical offset V (default: 0.5m) to prevent ground clipping
    5. Position observation at (x, ground_height + V, z)
```

**Parameters:**
- Raycast start height: 1000m
- Max raycast distance: 2000m
- Vertical offset: 0.5m

### Rule 2.3: Player Terrain Following
**Purpose:** Keep player on terrain surface
**Algorithm:**
```
EVERY frame:
  1. Get player XZ position
  2. Sample terrain height at XZ
  3. Apply height to player Y position (via character controller)
  4. Add player height offset (based on character capsule)
```

---

## 3. Biodiversity Calculation Rules (Simpson's Diversity Index)

### Rule 3.1: Grid-Based Spatial Analysis
**Purpose:** Calculate biodiversity scores per geographic region
**Algorithm:**
```
1. Define grid parameters:
   - Cell size: S × S meters (default: 50m × 50m)
   - Grid origin: player position or map center

2. FOR each observation O:
   - Calculate grid cell coordinates:
     cellX = floor(O.x / S)
     cellZ = floor(O.z / S)
   - Add observation to cell(cellX, cellZ)

3. Create spatial hash map: cell_coordinates → [observations]
```

**Parameters:**
- Grid cell size: 50m × 50m

### Rule 3.2: Simpson's Diversity Index Calculation
**Purpose:** Quantify biodiversity in each cell
**Algorithm:**
```
FOR each grid cell C with observations:
  1. Count total observations N in cell
  2. Group observations by species/taxon ID
  3. FOR each species i:
     - Count individuals n_i of species i

  4. Calculate Simpson's Index:
     numerator = Σ[n_i × (n_i - 1)]  for all species
     denominator = N × (N - 1)

     IF denominator > 0:
       D = 1 - (numerator / denominator)
     ELSE:
       D = 0

  5. Store diversity score for cell C
  6. Map D to visual representation (0.0 = low, 1.0 = high)
```

**Formula:**
```
D = 1 - [Σ(n_i × (n_i - 1)) / (N × (N - 1))]

Where:
- D = Simpson's Diversity Index (0-1 range)
- n_i = number of individuals of species i
- N = total number of individuals
```

**Interpretation:**
- D ≈ 0: Low diversity (few species dominate)
- D ≈ 1: High diversity (many species, evenly distributed)

### Rule 3.3: Neighborhood Smoothing
**Purpose:** Create gradual transitions between cells
**Algorithm:**
```
FOR each grid cell C:
  1. Get diversity score D_c
  2. Get scores from adjacent 8 cells: D_neighbors[]
  3. Calculate smoothed score:
     D_smoothed = (D_c × W_center) + Σ(D_neighbor × W_neighbor) / (W_center + Σ W_neighbor)

  4. Apply smoothed score to visual effects
```

**Parameters:**
- Center weight: 0.6
- Neighbor weights: 0.4 / 8 = 0.05 each

### Rule 3.4: Update Frequency Throttling
**Purpose:** Balance accuracy with performance
**Algorithm:**
```
1. Calculate biodiversity on initial load
2. Schedule recalculation every T seconds (default: 3s)
3. On recalculation:
   - Only process cells within radius R of player (default: 500m)
   - Skip cells with no observation changes
```

**Parameters:**
- Update interval: 3 seconds
- Active calculation radius: 500m

---

## 4. Network Visualization Rules

### Rule 4.1: Connection Eligibility
**Purpose:** Determine which observations should connect
**Algorithm:**
```
FOR each pair of observations (A, B):
  1. Calculate Euclidean distance D:
     D = sqrt((A.x - B.x)² + (A.y - B.y)² + (A.z - B.z)²)

  2. Check eligibility:
     - IF D > max_distance (default: 100m) THEN skip
     - IF species filter enabled AND (A.taxon ≠ B.taxon) THEN skip
     - IF same observation (A.id == B.id) THEN skip

  3. IF eligible THEN create connection
```

**Parameters:**
- Max connection distance: 100m (configurable)
- Species filtering: optional, per taxon category

### Rule 4.2: Player-Centric Processing
**Purpose:** Prioritize nearby connections, manage performance
**Algorithm:**
```
1. Get all observations
2. Sort by distance from player
3. Process only first N observations (default: 25 per frame)
4. FOR each processed observation:
   - Check connections to all other observations
   - Apply Rule 4.1 for eligibility
5. Pool connections (reuse LineRenderer objects)
6. Display total connection count in UI
```

**Parameters:**
- Max observations processed per frame: 25
- Max total connections: 200

### Rule 4.3: Manual Network Toggle
**Purpose:** User control over network display
**Algorithm:**
```
WHEN user presses key 'N' OR toggles UI button:
  1. IF network OFF:
     - Set network_enabled = true
     - Create all eligible connections (Rule 4.1)
     - Show connection count
  2. IF network ON:
     - Set network_enabled = false
     - Destroy/hide all connections
     - Clear connection pool
```

### Rule 4.4: 2D Minimap Network Projection
**Purpose:** Show network on minimap
**Algorithm:**
```
FOR each connection between observations (A, B):
  1. Convert A and B GPS coordinates → minimap pixel coordinates:
     pixelX = (lon - map_min_lon) / (map_max_lon - map_min_lon) × map_width
     pixelY = (lat - map_min_lat) / (map_max_lat - map_min_lat) × map_height

  2. Draw line on 2D texture from A_pixel to B_pixel
  3. Apply color based on taxon or distance
```

---

## 5. Minimap Generation Rules

### Rule 5.1: Static Map Request
**Purpose:** Generate minimap from map tiles
**Algorithm:**
```
1. Define minimap parameters:
   - Resolution: W × H pixels (default: 1024×1024)
   - Style: street, satellite, or outdoor
   - Zoom level: Z (default: 13-15)

2. Build API URL:
   url = "https://api.mapbox.com/styles/v1/mapbox/{style}/static/"
   url += "{lon},{lat},{zoom}/{width}x{height}"
   url += "?access_token={token}"

3. Fetch image as texture
4. Display on UI canvas
```

**Parameters:**
- Map size: 1024×1024 pixels
- Default zoom: 13
- Default style: outdoors-v11

### Rule 5.2: Dynamic Minimap Regeneration
**Purpose:** Keep minimap centered on player
**Algorithm:**
```
EVERY frame:
  1. Calculate distance D from player to last minimap center
  2. IF D > threshold (default: 200m) THEN:
     - Check cooldown timer (min 2 seconds since last regeneration)
     - IF cooldown elapsed THEN:
       * Build new API request at current player position
       * Fetch new minimap texture
       * Replace old texture
       * Update last minimap center position
       * Reset cooldown timer
```

**Parameters:**
- Regeneration threshold: 200m
- Cooldown period: 2 seconds

### Rule 5.3: Marker Positioning on Minimap
**Purpose:** Show observation locations on 2D map
**Algorithm:**
```
FOR each observation O:
  1. Get GPS coordinates (lat, lon)
  2. Get minimap bounds (min_lat, max_lat, min_lon, max_lon)
  3. Convert to normalized coordinates (0-1):
     norm_x = (lon - min_lon) / (max_lon - min_lon)
     norm_y = (lat - min_lat) / (max_lat - min_lat)

  4. Convert to pixel coordinates:
     pixel_x = norm_x × map_width
     pixel_y = (1 - norm_y) × map_height  // Flip Y axis

  5. Place marker sprite at (pixel_x, pixel_y)
  6. Apply color/icon based on taxon
```

### Rule 5.4: Player Marker Rotation
**Purpose:** Show player orientation on minimap
**Algorithm:**
```
EVERY frame:
  1. Get player camera Y rotation (yaw angle)
  2. Convert to marker rotation:
     marker_rotation = player_yaw + 180°  // Adjust for sprite orientation
  3. Apply rotation to player marker sprite
```

---

## 6. Prefab Selection & Spawning Rules

### Rule 6.1: Taxon-Based Prefab Mapping
**Purpose:** Visual distinction between species types
**Algorithm:**
```
GIVEN observation with taxon.iconic_taxon_name:
  MATCH taxon_name:
    CASE "Plantae":
      prefab = plant_prefab
      scale = random(0.8, 1.2)

    CASE "Animalia":
      prefab = animal_prefab
      scale = random(0.9, 1.1)
      apply_random_rotation()

    CASE "Aves":
      prefab = bird_prefab
      scale = random(0.7, 1.0)
      enable_audio_indicator()

    CASE "Fungi":
      prefab = fungi_prefab
      scale = random(0.6, 1.0)

    CASE "Insecta":
      prefab = insect_prefab
      scale = random(0.5, 0.8)

    DEFAULT:
      prefab = default_observation_prefab
      scale = 1.0

  INSTANTIATE prefab at world position
  APPLY scale
  ATTACH ObservationDisplay component with observation data
```

### Rule 6.2: Vegetation Density Spawning
**Purpose:** Visual feedback for biodiversity levels
**Algorithm:**
```
FOR each grid cell C with biodiversity score D:
  1. Evaluate density curve:
     IF D < 0.3 (low biodiversity):
       density = low_density_value (e.g., 5 prefabs)
       prefab_set = low_biodiversity_prefabs

     ELSE IF D < 0.7 (medium biodiversity):
       density = medium_density_value (e.g., 15 prefabs)
       prefab_set = medium_biodiversity_prefabs

     ELSE (high biodiversity):
       density = high_density_value (e.g., 30 prefabs)
       prefab_set = high_biodiversity_prefabs

  2. FOR i = 1 to density:
     - Generate random position within cell bounds
     - Select random prefab from prefab_set
     - Apply random rotation and scale variation
     - Instantiate prefab
```

**Parameters:**
- Low density: 5 prefabs per cell
- Medium density: 15 prefabs per cell
- High density: 30 prefabs per cell

---

## 7. UI Proximity & Visibility Rules

### Rule 7.1: Distance-Based UI Display
**Purpose:** Show observation info when player is near
**Algorithm:**
```
FOR each observation O:
  EVERY frame:
    1. Calculate distance D from player to observation
    2. IF D < show_threshold (default: 10m):
       - Enable observation UI canvas
       - Face canvas toward camera (billboard)
       - Scale canvas based on distance (closer = larger)
    3. ELSE:
       - Disable observation UI canvas
```

**Parameters:**
- Show threshold: 10m
- Scale range: 0.5 (far) to 1.0 (near)

### Rule 7.2: Collision-Based Interaction
**Purpose:** Trigger network connections when player approaches
**Algorithm:**
```
WHEN player collider enters observation trigger collider:
  1. Get observation ID
  2. Trigger ObservationNetworkManager
  3. Create connections from this observation to nearby observations
  4. Highlight observation (optional visual feedback)

WHEN player collider exits observation trigger collider:
  1. Remove highlight
  2. Optionally prune connections (if not persistent)
```

**Parameters:**
- Trigger collider radius: 5m (SphereCollider)

---

## 8. Performance Optimization Rules

### Rule 8.1: Object Pooling
**Purpose:** Reduce instantiation overhead
**Algorithm:**
```
Network Connections:
  1. Create pool of N LineRenderer objects on start (default: 200)
  2. WHEN connection needed:
     - Get inactive LineRenderer from pool
     - Configure endpoints and visual properties
     - Activate
  3. WHEN connection removed:
     - Deactivate LineRenderer
     - Return to pool

Grass Patches:
  1. Create pool of M grass patch objects
  2. Spawn incrementally (X patches per frame)
  3. Reuse destroyed patches for new positions
```

**Parameters:**
- Connection pool size: 200
- Grass patches per frame: 5

### Rule 8.2: Distance-Based Culling
**Purpose:** Remove distant objects to save resources
**Algorithm:**
```
EVERY update cycle:
  FOR each spawned object:
    1. Calculate distance D from player
    2. IF D > cull_threshold:
       - Destroy object
       - Remove from active list
    3. IF D > deactivate_threshold:
       - Deactivate object (keep in memory)
```

**Thresholds by Object Type:**
- Observations: 300m (destroy and re-fetch)
- Grass patches: 80m (destroy)
- Network connections: 100m (hide/remove)

### Rule 8.3: Incremental Processing
**Purpose:** Spread heavy operations over multiple frames
**Algorithm:**
```
FOR expensive operations (grass spawning, network creation):
  1. Divide task into N chunks
  2. Process X chunks per frame (default: 1-5)
  3. Use coroutine or frame counter to track progress
  4. Display progress indicator if needed
```

**Parameters:**
- Chunks per frame: 1-5 (depends on operation)

### Rule 8.4: Update Frequency Throttling
**Purpose:** Reduce unnecessary calculations
**Algorithm:**
```
Biodiversity Calculation:
  - Update every 3 seconds (not every frame)

Minimap Regeneration:
  - Check every frame, regenerate only if threshold met
  - Enforce 2-second cooldown between regenerations

Network Connections:
  - Update every frame BUT process max 25 observations
  - Early exit if no changes
```

---

## 9. WebGL & CORS Handling Rules

### Rule 9.1: Platform-Specific API Calls
**Purpose:** Bypass browser CORS restrictions
**Algorithm:**
```
WHEN making API request:
  IF platform == WebGL AND NOT editor:
    1. Use JavaScript bridge (Fetch API)
    2. Build request in JS with proper headers
    3. Return data via callback to C#

  ELSE:
    1. Use UnityWebRequest
    2. Add CORS headers if needed
    3. Handle response directly in C#
```

### Rule 9.2: Texture Loading via Base64
**Purpose:** Load images in WebGL
**Algorithm:**
```
WebGL Path:
  1. Fetch image as Blob via JavaScript
  2. Convert Blob → Base64 string
  3. Pass Base64 string to C# via callback
  4. Decode Base64 → byte array
  5. Load byte array into Texture2D

Native Path:
  1. Fetch image via UnityWebRequestTexture
  2. Extract texture directly from response
```

### Rule 9.3: Error Handling & Retry Logic
**Purpose:** Handle unreliable network conditions
**Algorithm:**
```
ON API request failure:
  1. IF retry_count < MAX_RETRIES (default: 3):
     - Increment retry_count
     - Wait delay D (default: 2 seconds)
     - Retry request

  2. IF retry_count >= MAX_RETRIES:
     - Show error message to user
     - Use fallback data/placeholder
     - Log error for debugging
```

**Parameters:**
- Max retries: 3
- Retry delay: 2 seconds

---

## 10. State Management Rules

### Rule 10.1: Game State Transitions
**Purpose:** Control game flow
**States:**
```
1. MainMenu
   - Time.timeScale = 1
   - Player disabled
   - Map disabled
   - Menu UI visible

2. Playing
   - Time.timeScale = 1
   - Player enabled
   - Map enabled
   - Game UI visible
   - Cursor locked

3. Paused
   - Time.timeScale = 0
   - Player input disabled
   - Pause UI visible
   - Cursor unlocked
```

**Transitions:**
```
MainMenu → Playing:
  - On "Start" button click
  - Enable game components
  - Lock cursor
  - Initialize data fetch

Playing ↔ Paused:
  - On ESC key press
  - Toggle Time.timeScale
  - Toggle UI panels
  - Toggle cursor lock
```

### Rule 10.2: Component Lifecycle Management
**Purpose:** Enable/disable systems based on state
**Algorithm:**
```
ON game start:
  1. Keep controllers attached but disabled
  2. Enable only when game state = Playing

ON pause:
  1. Don't disable components
  2. Use Time.timeScale = 0 to freeze updates

ON cleanup (player moves far):
  1. Destroy observation GameObjects
  2. Clear data structures
  3. Release textures from memory
  4. Pool reusable objects
```

---

## Summary of Key Parameters

| Parameter | Default Value | Purpose |
|-----------|---------------|---------|
| **Data Fetching** | | |
| Fetch radius | 300m | Observation query range |
| Max observations | 200 | API result limit |
| Re-fetch threshold | 300m | Player movement trigger |
| Initialization delay | 2s | Wait for terrain ready |
| **Biodiversity** | | |
| Grid cell size | 50m × 50m | Spatial analysis resolution |
| Update interval | 3s | Calculation frequency |
| Active radius | 500m | Processing zone |
| **Network** | | |
| Max connection distance | 100m | Line creation range |
| Max connections | 200 | Total limit |
| Process per frame | 25 | Performance throttle |
| **Minimap** | | |
| Map resolution | 1024×1024px | Image quality |
| Regeneration threshold | 200m | Update trigger |
| Cooldown period | 2s | Rate limiting |
| Default zoom | 13 | Detail level |
| **UI & Interaction** | | |
| Proximity threshold | 10m | UI visibility |
| Trigger radius | 5m | Collision detection |
| **Performance** | | |
| Observation cull distance | 300m | Memory management |
| Grass cull distance | 80m | Vegetation cleanup |
| Connection pool size | 200 | Object reuse |
| **Error Handling** | | |
| Max retries | 3 | API failure recovery |
| Retry delay | 2s | Backoff period |
| Request timeout | 30s | Connection limit |

---

## Design Patterns Used

1. **Observer Pattern**: Map events (OnInitialized) trigger data fetches
2. **Object Pooling**: LineRenderers, grass patches reused
3. **Spatial Hashing**: Grid-based biodiversity calculation
4. **Distance-Based LOD**: Culling, UI visibility, processing priority
5. **Throttling/Debouncing**: Update frequencies, regeneration cooldowns
6. **Strategy Pattern**: Multiple ground detection methods (fallback chain)
7. **State Machine**: Game states (Menu, Playing, Paused)
8. **Factory Pattern**: Prefab selection based on taxon type
9. **Singleton**: WebGL bridge for centralized API calls

---

## Application Beyond Unity

These procedural rules can be adapted to:
- **Web applications** (Three.js, Babylon.js)
- **Mobile apps** (React Native, Flutter)
- **Data visualization tools** (D3.js, Processing)
- **Scientific simulations** (Python, R)
- **VR/AR experiences** (Unreal, custom engines)

The core logic (biodiversity calculations, spatial queries, network algorithms) is platform-agnostic and can be implemented in any language that supports:
- HTTP requests (API calls)
- 3D coordinate systems (spatial positioning)
- Grid-based spatial data structures
- Distance calculations
- Visual rendering (connections, UI)
