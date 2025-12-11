# More-Than-Human Urbanism: Biodiversity Explorer

**MSc Thesis Project - University of the Arts London (UAL)**
**Student:** Ceci
**Technology:** Unity 2022 LTS, Universal Render Pipeline
**Completion:** December 2025

---

## 📖 Overview

An interactive Unity application exploring more-than-human perspectives of urban environments through real-world biodiversity data integration. Navigate actual London locations while viewing live iNaturalist species observations overlaid on Mapbox terrain. The application makes invisible urban biodiversity visible and quantifiable through real-time metrics and visual systems.

---

## 🎯 Concept

This project applies **more-than-human urbanism** theory to create a technological mediation tool that:
- Centers non-human species presence in urban spaces
- Uses community-science data (iNaturalist) to ground observations in reality
- Visualizes biodiversity metrics using Simpson's Diversity Index
- Creates network connections between species observations
- Provides interactive exploration of real-world ecological data

**Research Question:** How can real-time biodiversity data visualization shift human perception of urban environments toward more-than-human awareness?

---

## 🌍 Core Systems

### 1. Real-World Mapping
- **Mapbox Unity SDK v2.1.1** for dynamic tile loading
- London area (51.5073° N, -0.1277° W) as primary location
- Real-time terrain generation with elevation data
- Player navigation using Kinematic Character Controller (KCC)

### 2. Biodiversity Data Integration
- **iNaturalist API v1** live integration
- Community-science observations from real users
- Species classification (Plants, Animals, Birds, Fungi, Insects)
- Filter by quality grade (Research, Needs ID, Casual)
- Distance-based observation sorting

### 3. Visual Biodiversity Metrics
- **Simpson's Diversity Index** calculation
- Real-time biodiversity scoring at player position
- Color-coded diversity labels (Very Low → Very High)
- Grid-based spatial analysis (50m cells)
- Post-processing visual effects reflecting biodiversity

### 4. Network Visualization
- Species connection mapping
- Distance-based connection algorithm
- Interactive species filtering UI
- Same-species vs different-species relationships
- Dynamic network updates as player moves

### 5. Minimap Navigation
- Mapbox Static Images API for 2D representation
- Smooth panning viewport system
- Real-time player position tracking
- 200m update threshold for API efficiency

---

## 🛠️ Technical Stack

### Core Technologies
- **Unity 2022 LTS** - Game engine
- **Universal Render Pipeline (URP) 14.0.11** - Modern rendering
- **Mapbox Unity SDK v2.1.1** - Real-world mapping
- **iNaturalist API v1** - Biodiversity observations
- **Kinematic Character Controller** - Player movement

### Third-Party Assets
- **LMHPOLY Asset Pack** - Low-poly nature assets and textures
- **Bitgem Stylized Water (URP)** - Water rendering system
- **Acorn Bringer Animated Animals** - Wildlife prefabs
- **Plus Jakarta Sans Font** - UI typography

### APIs & Data Sources
- **iNaturalist API:** https://api.inaturalist.org/v1/docs/
- **iNaturalist Platform:** https://www.inaturalist.org
- **Mapbox:** https://docs.mapbox.com/unity/maps/overview/

---

## ✨ Key Features

### Implemented Systems
1. **Real-Time Observation Loading**
   - Auto-updates when player moves >500m
   - Spawns taxon-specific 3D prefabs
   - Raycasts for accurate terrain placement
   - Canvas-based interaction prompts

2. **Biodiversity Metrics Display**
   - Simpson's Diversity Index (0.0-1.0 range)
   - Observation count per grid cell
   - Species count per grid cell
   - Adjustable diversity intensity (0-2x multiplier)

3. **Network Connections**
   - Visual lines connecting nearby observations
   - Distance filtering (10m-1km range)
   - Species-based filtering UI
   - Connection limits for performance

4. **Post-Processing Effects**
   - Biodiversity-based saturation adjustment
   - Volumetric color grading
   - Distance-based intensity falloff
   - URP Volume system integration

5. **Interactive UI**
   - Toggle UI visibility (B key)
   - Species filter controls
   - Real-time metric updates
   - Minimap with player tracking

---

## 📊 Project Statistics

**Development Time:** 26+ hours (documented)
**Code Files:** 45 C# scripts (~2500+ lines)
**Documentation:** 15+ markdown guides
**API Integrations:** 2 (Mapbox, iNaturalist)
**Third-Party Assets:** 4 packages

### Code Distribution by System
- **Biodiversity:** 14 scripts (visualization, metrics, spawning)
- **iNaturalist Integration:** 6 scripts (API, observations, network)
- **Mapbox Modifiers:** 3 scripts (custom terrain/water behaviors)
- **UI & Minimap:** 6 scripts (interface, controls, navigation)
- **Utilities:** 16 scripts (terrain, water, WebGL, debugging)

---

## 🎓 Academic Framework

### Theoretical Foundations
- **More-Than-Human Urbanism** - Centering non-human perspectives
- **Community Science** - iNaturalist citizen observations
- **Biodiversity Metrics** - Simpson's Diversity Index
- **Technological Mediation** - Making invisible data visible

### Research Methodology
- Real-world data integration (not simulated)
- London as specific cultural/geographic context
- User interaction design for ecological awareness
- Visual communication of scientific metrics

### Key Insights
1. Real-world data grounds project in actual biodiversity
2. Pedestrian-scale exploration (100m grass render, zoom 16 minimap)
3. Technical implementation supports conceptual goals
4. Community-science data democratizes ecological knowledge

---

## 🤖 AI Contribution Documentation

AI assistance (Claude Sonnet 4.5) was used throughout development for:

### High AI Contribution (~65-80%)
- API integration and JSON parsing logic
- Coordinate conversion mathematics
- Performance optimization patterns
- Code structure and architecture

### Moderate AI Contribution (~40-60%)
- UI update logic and color coding systems
- Network connection algorithms
- Post-processing shader integration
- Debug logging and error handling

### Low AI Contribution (~20-40%)
- Prefab assignment and Unity-specific setup
- Visual design and UX decisions
- Research framework integration
- Documentation writing and organization

**All AI contributions are documented per-file in script headers** following this template:
```csharp
/// AI CONTRIBUTION: [X]% - [What AI helped with]
/// HUMAN CONTRIBUTION: [Y]% - [What you designed/implemented]
```

---

## 📚 References & Attribution

### APIs & Data
- iNaturalist API v1: https://api.inaturalist.org/v1/docs/
- iNaturalist Community Data: https://www.inaturalist.org
- Mapbox Unity SDK: https://docs.mapbox.com/unity/maps/overview/

### Unity Technologies
- Universal Render Pipeline: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/
- Unity 2022 LTS Documentation: https://docs.unity3d.com/2022.3/Documentation/Manual/

### Asset Creators
- LMHPOLY (Low Poly Nature): Unity Asset Store
- Bitgem (Stylized Water URP): Unity Asset Store
- Acorn Bringer (Animated Animals): Unity Asset Store

### Academic Concepts
- Simpson's Diversity Index for biodiversity measurement
- More-than-human urbanism theoretical framework
- Community/citizen science data practices

---

## 🚀 Build & Deployment

### WebGL Build
- Deployed to GitHub Pages
- CORS handling for API requests
- WebGL-specific network bridge
- Optimized for browser performance

### Standalone Build
- Windows/Mac/Linux support
- No network restrictions
- Full performance capabilities
- VR-ready architecture

### Build Settings
- Unity 2022.3 LTS
- URP 14.0.11
- .NET Standard 2.1
- IL2CPP scripting backend (WebGL)

---

## 📁 Project Structure

```
Assets/
├── Scenes/
│   └── MapScene.unity          # Main scene
├── Scripts/
│   ├── Biodiversity/           # Metrics, visualization, spawning
│   ├── iNaturalist Observations/ # API integration, observations
│   ├── UI and Minimap/         # Interface controllers
│   ├── Mapbox Custom Scripts/  # Terrain/water modifiers
│   ├── Network/                # Network visualization
│   ├── Terrain/                # Material systems
│   ├── Water/                  # Water conforming
│   ├── WebGL/                  # CORS fixes
│   └── Debugging/              # Utility tools
├── Mapbox/                     # Mapbox SDK resources
├── Packages/                   # Third-party assets
└── UI/                         # Fonts, sprites

MDs/
├── PROJECT_LOG.md              # Development timeline
├── TUTORIALS/                  # System-specific guides
├── PROJECT_AUDIT.md            # Code inventory
└── OBSERVATION_NETWORK_SETUP.md # Network system docs
```

---

## 🎯 Usage

### Controls
- **WASD** - Move player
- **Mouse** - Look around
- **B** - Toggle biodiversity UI
- **E** - Interact with observation markers
- **Esc** - Pause menu

### Getting Started
1. Open `MapScene.unity`
2. Press Play in Unity Editor
3. Navigate London area to discover observations
4. Approach markers to see species information
5. Toggle UI to view biodiversity metrics

---

## 📝 License & Academic Use

**Academic Project - UAL MSc Thesis 2025**

This project was developed as part of an MSc thesis at the University of the Arts London.

**Code:** Educational use only
**Data:** iNaturalist data used under Creative Commons licenses
**Assets:** Third-party assets retain original licenses

---

## 🙏 Acknowledgments

### Technology Providers
- **Mapbox** - Excellent Unity SDK and documentation
- **iNaturalist** - Community-science biodiversity data
- **Unity Technologies** - Universal Render Pipeline
- **Asset Store Creators** - LMHPOLY, Bitgem, Acorn Bringer

### AI Assistance
- **Claude Sonnet 4.5 (Anthropic)** - Code structure, debugging, documentation (contribution documented per-file)

### Community Resources
- Unity community forums for troubleshooting
- Stack Overflow for technical solutions
- GitHub for version control and deployment

---

## 📧 Contact

**Project:** More-Than-Human Urbanism: Biodiversity Explorer
**Institution:** University of the Arts London
**Year:** 2025
**GitHub:** https://github.com/ceciglez/Unity_MScProject

---

**Making Urban Biodiversity Visible Through Technology** 🌱🦋🐦
