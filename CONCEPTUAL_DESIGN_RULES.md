# Conceptual Design Rules - Biodiversity Network Visualization

## Overview
This document describes the high-level conceptual rules, spatial relationships, and design logic that govern how the biodiversity visualization system works. These are the "big picture" ideas behind the implementation.

---

## 1. Terrain & Environmental Rules

### 1.1 Base Terrain
**Concept:** Real-world 3D terrain from geographic data
- **Source:** Mapbox terrain tiles representing actual Earth topography
- **Properties:** Height data, landuse classification, geographic coordinates
- **Behavior:** Streams dynamically as player moves through the world

### 1.2 Landuse-Based Vegetation Distribution
**Concept:** Different land types have different vegetation densities

**Rule:** `IF landuse = type THEN spawn vegetation_set WITH density`

| Landuse Type | Vegetation Density | Prefab Set |
|--------------|-------------------|------------|
| **Forest/Woods** | High (30-50 trees) | Dense trees, bushes, undergrowth |
| **Grassland** | Medium (15-30 grass) | Grass patches, scattered shrubs |
| **Park** | Medium-High (20-40) | Mix of trees and grass |
| **Agriculture** | Low-Medium (5-15) | Crops, sparse vegetation |
| **Urban/Built** | Very Low (0-5) | Minimal vegetation |
| **Water** | None (0) | No vegetation spawning |

**Implementation:** Mapbox modifiers analyze landuse data and spawn appropriate prefabs

### 1.3 Green Space Identification
**Concept:** Automatically identify and emphasize biodiversity-rich areas

**Rule:**
```
FOR each terrain area:
  IF landuse IN [forest, park, grassland, wetland]:
    - Mark as "green space"
    - Increase vegetation spawn rate
    - Apply green color tinting to terrain material
    - Prioritize for biodiversity visualization
```

---

## 2. Observation Data Rules

### 2.1 Observation Placement
**Concept:** Real species observations appear at their actual GPS locations

**Rule:**
```
FOR each iNaturalist observation:
  1. Convert GPS coordinates → 3D world position
  2. Place on terrain surface (ground detection)
  3. Spawn visual prefab based on species type
  4. Attach data card with species info
```

**Visual Mapping:**
- **Plants** → Tree/flower models (rooted to ground)
- **Animals** → Animal models (ground level, randomized rotation)
- **Birds** → Bird models (slightly elevated, audio indicator)
- **Fungi** → Mushroom models (small scale, close to ground)
- **Insects** → Small creature models (very small scale)

### 2.2 Observation Exclusion Zones
**Concept:** Prevent visual clutter around observation points

**Rule:**
```
AROUND each observation (radius: 2-5m):
  - Suppress procedural grass spawning
  - Reduce tree density
  - Clear space for observation visibility
```

**Purpose:** Ensure observation prefabs are clearly visible and not obscured by vegetation

---

## 3. Biodiversity Visualization Rules

### 3.1 Grid-Based Biodiversity Mapping
**Concept:** Divide world into cells and calculate biodiversity per cell

**Spatial Structure:**
```
World divided into 50m × 50m grid cells

FOR each cell:
  - Count observations inside cell
  - Calculate species diversity (Simpson's Index)
  - Assign diversity score (0.0 = low, 1.0 = high)
```

**Visual Representation:**
```
Low Biodiversity (D < 0.3):
  - Desaturated terrain colors (browns, greys)
  - Sparse vegetation
  - Few observation markers

Medium Biodiversity (0.3 ≤ D < 0.7):
  - Mixed colors (greens with browns)
  - Moderate vegetation density
  - Scattered observation markers

High Biodiversity (D ≥ 0.7):
  - Vibrant colors (rich greens, blues)
  - Dense vegetation
  - Clustered observation markers
```

### 3.2 Biodiversity Hotspots
**Concept:** Visually highlight areas of exceptional biodiversity

**Rule:**
```
IF cell.diversity_score > threshold (e.g., 0.8):
  - Generate "hotspot" marker
  - Apply post-processing glow effect
  - Increase terrain color saturation
  - Spawn extra vegetation prefabs
  - Draw attention with visual pulsing
```

**Effect:** Players can easily identify biodiversity-rich zones

### 3.3 Material-Based Color Grading
**Concept:** Terrain colors reflect biodiversity levels

**Rule:**
```
Terrain Material Shader:
  color = lerp(low_biodiversity_color, high_biodiversity_color, diversity_score)

  low_biodiversity_color = desaturated brown/grey
  high_biodiversity_color = vibrant green
```

**Result:** Walking through the world shows a gradient from lifeless → thriving ecosystems

---

## 4. Network Connection Rules

### 4.1 Observation Networks (3D)
**Concept:** Species observations are connected by visible lines (LineRenderers) representing ecological relationships

**Basic Connection Rule:**
```
FOR each pair of observations (A, B):
  IF distance(A, B) < max_distance (100m):
    - Draw line from A to B
    - Line represents potential ecological interaction
```

**Visual Properties:**
- **Line thickness:** Based on proximity (closer = thicker)
- **Line color:** Based on species similarity or diversity
- **Line opacity:** Fades with distance

### 4.2 Species-Based Network Filtering
**Concept:** Show only connections between specific species types

**Rule:**
```
IF filter_enabled:
  SHOW connections WHERE:
    - Both observations match selected taxon (e.g., both are Plants)
    - OR both are within same ecosystem category
```

**Use Cases:**
- View only plant-to-plant networks (pollination, competition)
- View only animal networks (predator-prey, habitat sharing)
- View cross-species networks (plant-pollinator relationships)

### 4.3 Player-Proximity Network Activation
**Concept:** Networks appear dynamically as player explores

**Rule:**
```
WHEN player approaches observation (trigger distance: 5-10m):
  - Activate all connections from that observation
  - Highlight observation prefab
  - Show observation info card
  - Draw connections to nearby observations
```

**Effect:** Network "grows" as player explores, revealing ecosystem connections

### 4.4 Network Density & Complexity
**Concept:** Network complexity reflects ecosystem health

**Rule:**
```
Sparse Networks (few connections):
  - Low biodiversity area
  - Isolated species
  - Simple ecosystem

Dense Networks (many connections):
  - High biodiversity area
  - Interconnected species
  - Complex ecosystem
```

---

## 5. Minimap & 2D Visualization Rules

### 5.1 Overhead Minimap
**Concept:** 2D top-down map showing spatial relationships

**Components:**
- **Base layer:** Static satellite/street map from Mapbox
- **Observation markers:** Colored dots at GPS positions
- **Network connections:** 2D lines between markers
- **Player marker:** Arrow showing position and orientation

### 5.2 Minimap Network Projection
**Concept:** 3D networks projected onto 2D minimap

**Rule:**
```
FOR each 3D connection line:
  1. Get observation A and B GPS coordinates
  2. Convert GPS → minimap pixel coordinates
  3. Draw 2D line on minimap texture
  4. Color by species type or diversity
```

**Visual Effect:** See network patterns from bird's-eye view

### 5.3 Dynamic Minimap Centering
**Concept:** Minimap follows player movement

**Rule:**
```
WHEN player moves > threshold_distance (200m):
  - Regenerate minimap centered on new player position
  - Update all observation marker positions
  - Redraw network connections
  - Update map zoom level if needed
```

---

## 6. Spatial Interaction Rules

### 6.1 Proximity-Based UI Visibility
**Concept:** Information appears when player is nearby

**Rule:**
```
FOR each observation:
  distance = distance(player, observation)

  IF distance < show_threshold (10m):
    - Display observation info card (floating UI)
    - Show species name, photo, observer, date
    - Orient card to face player (billboard)
    - Scale card based on distance (closer = larger)
  ELSE:
    - Hide info card
```

### 6.2 Collision-Based Triggering
**Concept:** Physical proximity triggers network visualization

**Rule:**
```
WHEN player collider enters observation trigger sphere (radius: 5m):
  - Fire interaction event
  - Create network connections from this observation
  - Play sound effect (for birds)
  - Highlight observation visually
```

### 6.3 Distance-Based Culling
**Concept:** Remove distant objects to maintain performance

**Spatial Zones:**
```
Zone 1 (0-100m): Full Detail
  - All observations visible
  - All networks active
  - Full vegetation
  - Interactive UI

Zone 2 (100-300m): Reduced Detail
  - Observations visible
  - Networks culled
  - Reduced vegetation
  - No UI

Zone 3 (300m+): Not Loaded
  - Observations destroyed
  - No networks
  - Minimal vegetation
  - Re-fetch when player approaches
```

---

## 7. Temporal & Dynamic Rules

### 7.1 Real-Time Data Updates
**Concept:** Data refreshes as player moves through world

**Rule:**
```
WHEN player moves > fetch_threshold (300m):
  1. Clear old observations from previous location
  2. Fetch new observations for current location
  3. Recalculate biodiversity for new grid cells
  4. Regenerate networks
  5. Update terrain visualization
```

**Effect:** Seamless exploration of different ecosystems

### 7.2 Periodic Biodiversity Recalculation
**Concept:** Keep visualization accurate as player moves

**Rule:**
```
EVERY interval (3 seconds):
  1. Recalculate diversity scores for active grid cells
  2. Update terrain material properties
  3. Adjust vegetation density
  4. Refresh hotspot markers
```

**Purpose:** Smooth transitions between biodiversity zones

### 7.3 Throttled Processing
**Concept:** Spread heavy calculations over time

**Rule:**
```
Instead of processing everything at once:
  - Process max N observations per frame (e.g., 25)
  - Update subset of grid cells each cycle
  - Regenerate networks incrementally
```

**Effect:** Maintain smooth framerate during complex calculations

---

## 8. Visual Hierarchy Rules

### 8.1 Layer Priority
**Concept:** What players see first

**Visual Stack (front to back):**
```
1. UI Overlays (always visible)
   - Observation info cards
   - Minimap
   - Control hints

2. Network Connections (mid-layer)
   - LineRenderers between observations
   - Transparent, colorful

3. Observation Prefabs (focal points)
   - Species models (plants, animals, birds)
   - Scaled for visibility

4. Vegetation (environmental context)
   - Trees, grass, bushes
   - Density based on landuse & biodiversity

5. Terrain (base layer)
   - 3D ground mesh
   - Color-coded by biodiversity
```

### 8.2 Color Coding System
**Concept:** Consistent color language throughout visualization

**Color Mappings:**
```
Biodiversity Levels:
  - Brown/Grey → Low biodiversity
  - Yellow/Orange → Medium biodiversity
  - Green/Blue → High biodiversity

Species Categories (taxon):
  - Green → Plants (Plantae)
  - Brown/Orange → Animals (Animalia)
  - Blue/Cyan → Birds (Aves)
  - Purple → Fungi
  - Yellow → Insects (Insecta)

Network Connections:
  - Same color as species type
  - Or gradient based on diversity of connected observations
```

### 8.3 Scale Relationships
**Concept:** Size hierarchy for visual clarity

```
Player (reference scale: 1.0x)
  ├─ Large Trees: 3-10x player height
  ├─ Observation Prefabs: 0.5-1.5x player height
  ├─ Grass/Bushes: 0.1-0.5x player height
  └─ Small Details: 0.05-0.1x player height
```

---

## 9. Ecosystem Representation Rules

### 9.1 Biodiversity = Visual Abundance
**Concept:** More species → More visual richness

**Mapping:**
```
Real World                    Virtual World
─────────────────────────────────────────────────
High species count       →    Dense vegetation
Many observations       →    Many 3D markers
Complex food webs       →    Dense networks
Healthy ecosystem       →    Vibrant colors
```

### 9.2 Spatial Clustering
**Concept:** Species tend to cluster in favorable habitats

**Rule:**
```
WHERE terrain has favorable properties (landuse, elevation, water):
  - Increase observation spawn probability
  - Increase vegetation density
  - Create biodiversity hotspots
  - Form dense network clusters
```

**Effect:** Players discover "biodiversity oases" in the landscape

### 9.3 Empty Spaces Have Meaning
**Concept:** Lack of observations shows ecological gaps

**Rule:**
```
IF grid cell has zero observations:
  - Apply desaturated terrain color
  - Minimal vegetation
  - No networks
  - Visual indication of ecological desert
```

**Purpose:** Highlight areas that may need conservation attention

---

## 10. Player Experience Rules

### 10.1 Exploration Reward
**Concept:** Moving through world reveals hidden complexity

**Rule:**
```
AS player explores:
  - New observations load and appear
  - Networks grow and connect
  - Biodiversity patterns emerge
  - Understanding deepens
```

**Effect:** Encourages curiosity and movement

### 10.2 Information Progressive Disclosure
**Concept:** Details appear at appropriate distances

```
Far (300m+): General patterns
  - Terrain color gradients
  - Vegetation density changes

Medium (50-300m): Structures emerge
  - Individual observation markers visible
  - Network clusters apparent

Near (10-50m): Specific connections
  - Network lines clearly visible
  - Observation prefabs identifiable

Close (0-10m): Detailed information
  - Species names and data
  - Photos and observer info
  - Individual relationships
```

### 10.3 Player Agency
**Concept:** User controls what they see

**Toggles:**
```
Network Display:
  - ON: See ecological connections
  - OFF: Clean view of observations only

Species Filters:
  - ALL: Show all connections
  - PLANTS: Only plant networks
  - ANIMALS: Only animal networks
  - etc.

Biodiversity Visualization:
  - ON: Color-coded terrain
  - OFF: Natural terrain colors
```

---

## 11. Architectural Relationships

### 11.1 Data → Visualization Pipeline
```
Real World Biodiversity Data (iNaturalist API)
            ↓
GPS Coordinates + Species Info
            ↓
3D World Positions (Mapbox conversion)
            ↓
Observation Prefabs (taxon-based models)
            ↓
Biodiversity Calculation (Simpson's Index)
            ↓
Visual Representation (colors, networks, density)
            ↓
Player Experience (exploration, discovery)
```

### 11.2 Terrain → Vegetation → Observations
```
Base Terrain (Mapbox 3D mesh)
      ↓
Landuse Analysis (Mapbox modifiers)
      ↓
Vegetation Spawning (density rules)
      ↓
Observation Placement (GPS positioning)
      ↓
Network Creation (connection rules)
      ↓
Unified Ecosystem Visualization
```

### 11.3 Player Position Drives Everything
```
Player Position (GPS coordinates)
      ↓
   ┌─────┼─────┐
   ↓     ↓     ↓
Terrain  Data  Minimap
Loading  Fetch Regen
   ↓     ↓     ↓
Height  Obs.  Markers
Sample  Spawn Update
   ↓     ↓     ↓
Biodiv. Net.  Display
Calc.   Create
```

---

## 12. Key Design Principles

### 12.1 Spatial Authenticity
**Principle:** Everything appears at its real-world location
- Observations at actual GPS coordinates
- Terrain represents real topography
- Networks show real spatial relationships

### 12.2 Visual Encoding of Complexity
**Principle:** More biodiversity = More visual information
- High diversity → Dense networks, vibrant colors
- Low diversity → Sparse networks, muted colors

### 12.3 Scale Consistency
**Principle:** All spatial measurements use real-world units
- Distances in meters (not arbitrary units)
- Areas in square meters
- Network connections reflect actual proximity

### 12.4 Performance Through Proximity
**Principle:** Only process what's near the player
- Distant objects culled
- Updates focused on active zone
- Resources freed when player moves away

### 12.5 Layered Information Density
**Principle:** Simple at distance, complex up close
- Far: Overall patterns
- Medium: Structures
- Near: Details
- Close: Data

---

## Summary Table: Conceptual Relationships

| **Element** | **Governed By** | **Visual Result** |
|-------------|-----------------|-------------------|
| **Terrain** | Real-world topography | 3D ground mesh |
| **Vegetation** | Landuse type + Biodiversity | Trees/grass density |
| **Observations** | GPS coordinates + Species | 3D prefabs at locations |
| **Networks** | Distance + Species | LineRenderer connections |
| **Colors** | Biodiversity score | Terrain material gradient |
| **Density** | Species count | Prefab spawn frequency |
| **Hotspots** | High diversity areas | Glowing regions |
| **Minimap** | Overhead projection | 2D representation |
| **UI Cards** | Player proximity | Floating information |
| **Exclusion Zones** | Observation placement | Clear space around markers |
| **Grid Cells** | 50m spatial divisions | Calculation units |
| **Culling** | Distance thresholds | Performance optimization |

---

## Application Examples

### Example 1: Walking Through a Forest
```
1. Player enters forest area (Mapbox landuse: "forest")
2. Terrain spawns dense trees (30-50 trees per 50m cell)
3. iNaturalist observations load (many plant/bird species)
4. Biodiversity calculation shows HIGH diversity (D = 0.85)
5. Terrain turns vibrant green
6. Dense network connections appear between observations
7. Player approaches oak tree observation
8. Info card appears: "Quercus robur (English Oak)"
9. Networks from this tree to nearby species light up
10. Minimap shows clustered observations in this zone
```

### Example 2: Crossing from Park to Urban Area
```
1. Player leaves park (high biodiversity, green terrain)
2. Crosses boundary into urban area (landuse: "built")
3. Vegetation density drops (5 → 2 prefabs per cell)
4. Observations become sparse (20 → 3)
5. Biodiversity score drops (0.7 → 0.2)
6. Terrain color desaturates (green → grey/brown)
7. Network connections thin out (50 → 5 lines)
8. Minimap shows transition from dense → sparse markers
9. Visual contrast highlights ecological boundary
```

### Example 3: Discovering a Biodiversity Hotspot
```
1. Player walking through mixed landscape
2. Enters area with many species observations
3. Biodiversity calculation detects high diversity (D = 0.92)
4. Terrain begins glowing subtly
5. Vegetation becomes lush and varied
6. Network connections form dense web
7. Post-processing effect highlights hotspot boundary
8. Minimap shows concentrated cluster of markers
9. Player realizes: "This is an important ecological zone!"
```

---

## Conceptual Summary

This system translates **ecological data into spatial experience** through:

1. **Real terrain** → 3D exploration space
2. **Landuse data** → Vegetation distribution rules
3. **Species observations** → Visual markers in space
4. **Biodiversity calculations** → Color and density encoding
5. **Spatial proximity** → Network connections
6. **Player movement** → Progressive revelation
7. **Distance relationships** → Information hierarchy

The result is an **embodied data visualization** where walking through virtual space reveals patterns in real-world biodiversity.
