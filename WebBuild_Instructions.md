# Unity Web Build Instructions

## Step 1: Optimize for Web Performance

### In NetworkConnection.cs Inspector:
- ✅ Set `useSimpleLines = true`
- ✅ Set `enableTerrainFollowing = false`
- ✅ Set `curveResolution = 5` (lower for better performance)

### In ObservationNetworkManager.cs Inspector:
- ✅ Set `optimizeForWeb = true`
- ✅ Set `playerProximityRange = 500` (lower for better performance)
- ✅ Set `maxObservationsToProcess = 15` (lower for better performance)
- ✅ Set `maxTotalConnections = 100` (lower for better performance)

## Step 2: Build Settings

1. **File → Build Settings**
2. **Switch Platform to WebGL**
3. **Player Settings:**
   - Company Name: Your university/name
   - Product Name: iNaturalist Network Visualization
   - WebGL Template: Default or Minimal
   - Compression Format: Gzip (smaller file size)
   - Code Optimization: Master (faster performance)

## Step 3: Build Process

1. **Create build folder**: `WebGL_Build/`
2. **Click Build**
3. **Wait for build** (can take 30+ minutes)
4. **Result**: Folder with `index.html` and supporting files

## Step 4: Testing Locally

```bash
# Navigate to build folder
cd WebGL_Build

# Start local web server (Python 3)
python3 -m http.server 8000

# Or Python 2
python -m SimpleHTTPServer 8000

# Open browser to: http://localhost:8000
```

## Step 5: Hosting Options

### Option A: GitHub Pages (Free)
1. Upload build folder to GitHub repository
2. Enable GitHub Pages in repo settings
3. Share the GitHub Pages URL

### Option B: Unity Cloud Build (Free)
1. Upload to Unity Cloud
2. Share the Unity Play URL

### Option C: Local Network
```bash
# Find your IP address
ipconfig getifaddr en0  # macOS
ipconfig                # Windows

# Share URL: http://YOUR_IP:8000
```

## Configuration Files to Check

### Mapbox Configuration:
- `Assets/Mapbox/MapboxConfiguration.asset`
- Should contain your Mapbox access token
- If missing, target computer needs to configure Mapbox

### API Endpoints:
- iNaturalist: `https://api.inaturalist.org/v1/observations`
- No authentication required - should work anywhere

## Performance Notes

- Web builds are slower than native builds
- Network connections render differently in WebGL
- Test on target computer's browser for compatibility

## Troubleshooting

### If Mapbox doesn't work:
1. Check browser console for CORS errors
2. Verify Mapbox token is embedded in build
3. Ensure target has internet access

### If performance is poor:
1. Lower connection limits in ObservationNetworkManager
2. Disable terrain following in NetworkConnection
3. Reduce observation processing count