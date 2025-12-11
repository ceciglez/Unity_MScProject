# Observation Network System Setup Guide

## Overview
The Observation Network System creates visual connections between iNaturalist observations based on species relationships and distance criteria. It includes a customizable UI for filtering species and controlling network appearance.

## Components

### 1. ObservationNetworkManager
**Main controller that manages the entire network system.**

**Key Features:**
- Connects observations with LineRenderer components
- Species-based filtering (same species, different species, or both)
- Distance-based connection limits
- Object pooling for performance
- Real-time updates as observations change

**Inspector Settings:**
- `Max Connection Distance`: How far apart observations can be to connect
- `Min Connection Distance`: Minimum distance to avoid cluttered connections
- `Max Connections Per Observation`: Prevents any single observation from having too many lines
- `Connect Same Species Only`: Enable connections between identical species
- `Connect Different Species`: Enable connections between different species
- `Line Width`: Thickness of connection lines
- `Same/Different Species Colors`: Color coding for connection types
- `Use Distance Gradient`: Color connections based on distance (close = bright, far = dim)

### 2. NetworkConnection
**Individual connection line between two observations.**

**Features:**
- Curved lines for long distances (more natural appearance)
- Animated alpha for pulsing effects
- Configurable colors and widths
- Height offset to prevent z-fighting with terrain

### 3. ObservationNetworkUI
**Canvas-based UI for user interaction.**

**Features:**
- Species filter toggles (show/hide individual species)
- Enable/Disable All buttons
- Network type toggles (same species vs different species)
- Connection count display
- Collapsible panel

## Setup Instructions

### Step 1: Basic Setup
1. Add `ObservationNetworkManager` component to any GameObject in your scene
2. Assign the `NetworkConnectionMaterial` to the Connection Material field
3. Optionally create a custom UI prefab and assign it to Network UI Prefab

### Step 2: Material Setup
The included `NetworkConnectionMaterial` uses Unity's Standard Shader in Transparent mode:
- **Rendering Mode**: Transparent
- **Alpha**: 0.5 (adjust for desired transparency)
- **Color**: White (will be tinted by script)

You can customize this material or create your own with these requirements:
- Must support transparency/alpha blending
- Should work well with LineRenderer component

### Step 3: Custom UI Prefab (Optional)
To create a custom UI prefab:

1. Create a Canvas in your scene
2. Design your UI layout with these components:
   - Main panel (RectTransform)
   - Header text
   - Species scroll view with content area
   - Control buttons (Enable All, Disable All, Refresh)
   - Network control toggles
   - Info display texts

3. Add the `ObservationNetworkUI` component to the root UI GameObject
4. Assign all UI elements to the corresponding fields
5. Save as prefab and assign to Network Manager

### Step 4: Integration with Existing Systems
The system automatically finds:
- `INaturalistMapController`: For accessing observation data
- `ObservationDisplay` components: For getting observation positions and species info

Ensure these systems are already working in your project.

## Configuration Examples

### Dense Urban Network
```
Max Connection Distance: 100m
Min Connection Distance: 10m
Max Connections Per Observation: 5
Connect Same Species Only: true
Connect Different Species: false
```

### Sparse Ecological Network
```
Max Connection Distance: 500m
Min Connection Distance: 20m
Max Connections Per Observation: 2
Connect Same Species Only: false
Connect Different Species: true
Use Distance Gradient: true
```

### Performance Optimized
```
Max Total Connections: 50
Update Interval: 5 seconds
Max Connections Per Observation: 2
Max Connection Distance: 200m
```

## Customization Options

### Visual Appearance
- **Line Materials**: Create custom materials with different shaders (glowing, dashed, animated)
- **Color Schemes**: Modify species colors, gradient colors, and connection types
- **Line Effects**: Enable animation, adjust pulsing speed, add particle effects

### UI Customization
- **Panel Styling**: Modify colors, transparency, borders, shadows
- **Layout**: Adjust positioning, sizing, anchor points
- **Controls**: Add sliders for real-time distance adjustment, color pickers, etc.
- **Species Icons**: Add icons next to species names in filter list

### Advanced Features
- **Cluster Analysis**: Group connected observations visually
- **Time-based Connections**: Connect observations from similar time periods
- **Elevation-based Filtering**: Connect only observations within similar elevations
- **Migration Paths**: Special connection types for animal movement patterns

## Performance Tips

1. **Limit Total Connections**: Set reasonable maximums to prevent performance issues
2. **Update Intervals**: Use longer intervals (2-5 seconds) unless real-time updates are critical
3. **Connection Pooling**: The system includes object pooling - avoid creating/destroying connections frequently
4. **Distance Culling**: Use reasonable max distances based on your use case
5. **LOD System**: Consider hiding distant connections when player is zoomed out

## Debugging

Enable `Show Debug Info` in the ObservationNetworkManager to see:
- Connection creation/removal logs
- Species filtering changes
- Performance metrics
- UI updates

Use `Show Connection Gizmos` to visualize connections in the Scene view.

## Troubleshooting

**No connections appear:**
- Check that observations exist and have valid species data
- Verify max connection distance is appropriate for observation spread
- Ensure at least one connection type is enabled (same/different species)

**Performance issues:**
- Reduce max total connections
- Increase update interval
- Decrease max connection distance
- Limit connections per observation

**UI not working:**
- Verify ObservationNetworkUI component is attached
- Check that all UI references are assigned
- Ensure Canvas is in Screen Space overlay mode

**Lines appear incorrectly:**
- Verify LineRenderer material supports transparency
- Check that connection material is assigned
- Ensure height offset prevents z-fighting with terrain