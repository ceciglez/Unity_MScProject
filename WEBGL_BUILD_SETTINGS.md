# WebGL Build Settings - Critical Configuration

## 🎯 Essential Build Settings for WebGL

### Player Settings (Edit → Project Settings → Player → WebGL)

#### 1. **Resolution and Presentation**
```
✅ Default Canvas Width: 960 (or your preference)
✅ Default Canvas Height: 600 (or your preference)
✅ Run In Background: ✓ (CHECKED)
```

#### 2. **Other Settings → Rendering**
```
✅ Color Space: Linear (recommended) or Gamma
✅ Auto Graphics API: ✓ (CHECKED)
✅ Static Batching: ✓ (CHECKED)
✅ Dynamic Batching: ✓ (CHECKED)
```

#### 3. **Other Settings → Configuration**

**CRITICAL SETTINGS:**

```
✅ Scripting Backend: IL2CPP
   ⚠️ MUST be IL2CPP for WebGL
   ⚠️ Mono is NOT supported in WebGL

✅ API Compatibility Level: .NET Standard 2.1
   ⚠️ Use .NET Standard 2.1 (NOT .NET Framework)
   ⚠️ .NET 4.x may cause issues in WebGL

✅ Managed Stripping Level: Minimal
   ⚠️ CRITICAL: Use Minimal, NOT Medium or High
   ⚠️ High stripping can break JavaScript bridge
   ⚠️ May strip required code for DllImport

✅ Vertex Compression: Mixed (default is fine)

✅ Optimize Mesh Data: ✓ (optional, for performance)
```

#### 4. **Publishing Settings**

**VERY IMPORTANT:**

```
✅ Compression Format: Disabled OR Gzip
   ⚠️ AVOID Brotli - not all servers support it
   ⚠️ Gzip is best for compatibility
   ⚠️ Disabled works but larger file size

✅ Enable Exceptions: None (for performance)
   OR Explicitly Thrown Exceptions Only (for debugging)
   ⚠️ AVOID "Full" - huge file size increase

✅ Data Caching: ✓ (CHECKED - speeds up reload)

✅ Debug Symbols: Only for debugging builds
   ⚠️ Uncheck for production (adds ~50MB+)

✅ Decompression Fallback: ✓ (CHECKED - safety net)

✅ Initial Memory Size: 512 MB (minimum)
   ⚠️ Increase if you see "Out of Memory" errors
   ⚠️ 1024 MB recommended for larger projects

✅ Maximum Memory Size: 2048 MB (default)
```

#### 5. **Code Optimization**

```
✅ C++ Compiler Configuration: Master (Release)
   - For development: Use "Debug" or "Release"
   - For production: Use "Master"

✅ Code Optimization:
   - Size: Smaller build, slower performance
   - Speed: Larger build, better performance
   - Runtime Speed: Best performance (recommended)
```

---

## ⚠️ Settings That Can BREAK the JavaScript Bridge

### DON'T DO THESE:

❌ **Managed Stripping Level: HIGH**
   - Will strip the `[DllImport]` code
   - JavaScript bridge won't work
   - Stick to "Minimal"

❌ **Compression Format: Brotli**
   - Not all web servers support it
   - May fail to load in some browsers
   - Use Gzip instead

❌ **Enable Exceptions: Full**
   - Adds 50-100MB to build size
   - Significantly slower load times
   - Use "None" or "Explicitly Thrown"

❌ **API Compatibility: .NET Framework 4.x**
   - Can cause compatibility issues in WebGL
   - Use .NET Standard 2.1

❌ **Initial Memory < 256 MB**
   - May run out of memory
   - Use at least 512 MB

---

## 🔧 Build Settings Window (File → Build Settings)

### Platform: WebGL

```
✅ Switch Platform: WebGL (if not already)

✅ Scenes In Build: Add your scenes
   - Make sure your main scene is at index 0
   - Remove any unused scenes

✅ Development Build:
   - ✓ CHECKED for testing (enables console logs)
   - ✗ UNCHECKED for production

✅ Autoconnect Profiler:
   - Only if using Unity Profiler
   - Usually unchecked

✅ Deep Profiling:
   - Only for performance debugging
   - Usually unchecked

✅ Script Debugging:
   - Only for debugging C# in browser
   - Usually unchecked
```

---

## 🚀 Recommended Settings for This Project

### For Development/Testing:

```
Compression Format: Gzip
Enable Exceptions: Explicitly Thrown Exceptions Only
Managed Stripping Level: Minimal
Development Build: ✓ CHECKED
Code Optimization: Runtime Speed
Initial Memory: 512 MB
Debug Symbols: ✓ CHECKED (for better error messages)
```

### For Production/Deployment:

```
Compression Format: Gzip
Enable Exceptions: None
Managed Stripping Level: Minimal
Development Build: ✗ UNCHECKED
Code Optimization: Runtime Speed
Initial Memory: 512 MB
Debug Symbols: ✗ UNCHECKED (smaller file size)
```

---

## 📋 Pre-Build Checklist

Before clicking "Build", verify:

- [ ] **Scripting Backend**: IL2CPP ✅
- [ ] **API Compatibility**: .NET Standard 2.1 ✅
- [ ] **Managed Stripping**: Minimal ✅
- [ ] **Compression**: Gzip or Disabled ✅
- [ ] **Initial Memory**: 512 MB minimum ✅
- [ ] **JavaScript Plugin Exists**: `Assets/Plugins/WebGL/WebGLNetworking.jslib` ✅
- [ ] **C# Bridge Exists**: `Assets/Scripts/WebGLNetworkBridge.cs` ✅

---

## 🐛 Common Build Errors and Fixes

### Error: "Building for WebGL requires IL2CPP"
**Fix**: Set Scripting Backend to IL2CPP

### Error: "Out of memory"
**Fix**: Increase Initial Memory Size (try 1024 MB)

### Error: "Failed to decompress data"
**Fix**:
1. Change Compression Format to Gzip
2. Check "Decompression Fallback"

### Error: "WebGLNetworking.jslib not found"
**Fix**: Verify the file exists in `Assets/Plugins/WebGL/`

### Build takes forever (30+ minutes)
**Solutions**:
- First build is always slow (15-30 min)
- Subsequent builds are faster (5-10 min)
- Use "Development Build" during testing
- Switch to "Master" only for final production build

### Build succeeds but JavaScript bridge doesn't work
**Causes**:
1. Managed Stripping Level too high → Set to Minimal
2. Old browser cache → Clear cache or use Incognito
3. Not using local server → Must use http://localhost

---

## 🔍 How to Verify JavaScript Plugin is Included

After building, check your build folder:

### Method 1: Check Build Logs
Look in the Unity Console during build:
```
Building Library/Bee/artifacts/WebGLBuild/build/Build.framework.js.gz
Including Plugin: WebGLNetworking.jslib
```

### Method 2: Check Build Files
Your build folder should contain:
```
Build/
├── Build/
│   ├── Build.data.gz
│   ├── Build.framework.js.gz  ← JavaScript plugins merged here
│   ├── Build.loader.js
│   └── Build.wasm.gz
├── TemplateData/
└── index.html
```

The `.jslib` file is **compiled into** `Build.framework.js.gz` - you won't see it as a separate file.

### Method 3: Test in Browser Console
After loading the WebGL build, open browser console (F12) and run:
```javascript
// Check if the functions exist
typeof WebGLFetchJSON !== 'undefined'  // Should return true
typeof WebGLFetchTexture !== 'undefined'  // Should return true
```

---

## 💡 Performance Optimization Tips

### Reduce Build Size:
1. **Compression**: Use Gzip
2. **Exceptions**: Set to "None"
3. **Debug Symbols**: Uncheck for production
4. **Code Optimization**: Use "Size" if build is too large

### Improve Load Time:
1. **Data Caching**: Check this option
2. **Streaming Assets**: Move large files here
3. **Asset Bundles**: For very large projects

### Improve Runtime Performance:
1. **Code Optimization**: Use "Runtime Speed"
2. **Memory Size**: Allocate enough (512-1024 MB)
3. **Graphics API**: Leave as Auto Graphics API

---

## 📝 Quick Reference Card

**Copy this to keep handy:**

```
CRITICAL WEBGL SETTINGS:
✅ Scripting Backend: IL2CPP
✅ API Compatibility: .NET Standard 2.1
✅ Managed Stripping: Minimal (NOT High)
✅ Compression: Gzip
✅ Initial Memory: 512 MB minimum
✅ Enable Exceptions: None or Explicitly Thrown

MUST HAVE FILES:
✅ Assets/Plugins/WebGL/WebGLNetworking.jslib
✅ Assets/Scripts/WebGLNetworkBridge.cs

AFTER BUILD:
✅ Use local server (python3 -m http.server 8000)
✅ Open http://localhost:8000 (NOT file://)
✅ Check browser console for [WebGLNetworking] logs
✅ Clear cache if updating existing build
```

---

## 🎯 Summary

The **most critical settings** for the JavaScript bridge to work:

1. **Managed Stripping Level: Minimal** - High will break the bridge
2. **Compression Format: Gzip** - Best compatibility
3. **Scripting Backend: IL2CPP** - Required for WebGL
4. **Initial Memory: 512+ MB** - Prevent out-of-memory errors

If you set these correctly, the JavaScript bridge **will work**! 🎉
