# WebGL Build Quick Start Guide

## 🚀 Quick Steps to Fix and Test

### 1. Rebuild in Unity (REQUIRED!)
```
File → Build Settings → WebGL → Build
```
⚠️ **The fix won't work in old builds - you MUST rebuild!**

### 2. Run Local Server
```bash
cd YourBuildFolder
python3 -m http.server 8000
```

### 3. Open in Browser
```
http://localhost:8000
```

### 4. Check Browser Console (F12)
Look for these SUCCESS messages:
```
[WebGLNetworking] Fetching texture from: https://api.mapbox.com/...
[WebGLNetworking] Texture response status: 200
[StaticMapMinimap] Map loaded successfully via WebGL bridge!

[WebGLNetworking] Fetching JSON from: https://api.inaturalist.org/...
[WebGLNetworking] Response status: 200
[iNaturalist] API fetch successful
```

---

## ❌ Common Issues

### "Still not working after rebuild"
- Clear browser cache (Ctrl+Shift+Delete)
- Use Incognito/Private mode
- Hard reload (Ctrl+Shift+R)

### "File not found / 404 error"
- Make sure you're using a web server
- Do NOT open `file:///path/to/index.html`
- Must use `http://localhost:8000`

### "CORS errors in console"
- Check if you rebuilt after adding the fix
- Verify `Assets/Plugins/WebGL/WebGLNetworking.jslib` exists
- Check if ad blocker is enabled (disable it)

### "Minimap is blank"
- Check Mapbox access token in Unity
- Look for "[StaticMapMinimap]" errors in console
- Verify you're in the correct map location

### "No observations spawning"
- Check player position (must be in area with observations)
- Press 'O' key to manually reload observations
- Look for "[iNaturalist]" errors in console

---

## 📁 Files Added/Modified

**New Files:**
- `Assets/Plugins/WebGL/WebGLNetworking.jslib` - JavaScript plugin
- `Assets/Scripts/WebGLNetworkBridge.cs` - C# bridge

**Modified Files:**
- `Assets/Scripts/WebGLCorsHelper.cs` - Enhanced error logging
- `Assets/Scripts/StaticMapMinimap.cs` - Uses JavaScript bridge in WebGL
- `Assets/Scripts/INaturalistMapController.cs` - Uses JavaScript bridge in WebGL

---

## 🔍 How to Debug

### 1. Open Browser Console (F12)

### 2. Filter by keywords:
- `WebGLNetworking` - JavaScript plugin messages
- `StaticMapMinimap` - Minimap loading
- `iNaturalist` - Observations loading

### 3. Look for RED errors
- CORS errors = Check if using local server
- 404 errors = Check API URLs and tokens
- Timeout errors = Check internet connection

### 4. Check Unity logs
- In WebGL: Browser console shows Unity Debug.Log()
- Look for detailed troubleshooting messages

---

## ✅ What Should Happen

1. **Minimap**: Small map appears in corner of screen, updates as you move
2. **Observations**: 3D models spawn in world (trees, animals, etc.)
3. **Console**: Green/blue success messages from `[WebGLNetworking]`
4. **No CORS errors**: No red errors about "Cross-Origin"

---

## 🆘 Still Having Issues?

1. **Read full documentation**: [CHROME_WEBGL_FIX.md](CHROME_WEBGL_FIX.md)
2. **Check Unity version**: Unity 2021.3+ recommended for WebGL
3. **Try different browser**: Test in Chrome, Firefox, and Safari
4. **Disable extensions**: Ad blockers and privacy extensions can block API calls
5. **Check API limits**: Mapbox and iNaturalist have rate limits

---

## 🎯 Success Checklist

- ✅ Rebuilt WebGL build after adding fix files
- ✅ Using local web server (not file://)
- ✅ Cleared browser cache / using incognito
- ✅ Browser console open (F12) to see logs
- ✅ See `[WebGLNetworking]` success messages
- ✅ No CORS errors in red
- ✅ Minimap visible and updating
- ✅ Observations spawning in world

**If all boxes checked = Fix is working! 🎉**
