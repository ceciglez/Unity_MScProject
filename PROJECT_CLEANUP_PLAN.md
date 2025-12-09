# Project Cleanup Plan - Reduce Build Size

## 🔍 Analysis Results

I scanned your project and found **HUGE** space wasters:

### Large Packages:
```
LMHPOLY: 726 MB  🚨🚨🚨 BIGGEST CULPRIT!
Handpainted_Grass_and_Ground_Textures: 170 MB  🚨
KinematicCharacterController: 37 MB
Mapbox Examples: 22 MB (probably unused)
Darkbringer Shader: 16 MB
```

### Large Files Found:
```
9.3 MB - KinematicCharacterController Walkthrough images
4.6 MB - KinematicCharacterController docs
4.2 MB - Darkbringer textures
2.6 MB - Fantasy map textures
5.2 MB - Walkthrough FBX models
```

**Total unnecessary assets: ~900+ MB!**

---

## ✅ Step-by-Step Cleanup

### STEP 1: Remove Mapbox Examples (22 MB)

**Safe to delete** - These are just demo scenes, not needed for your build.

**In Unity:**
1. Right-click `Assets/Mapbox/Examples` folder
2. Delete
3. When prompted "Delete folder?", click **Yes**

**Or via terminal:**
```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"
rm -rf "Assets/Mapbox/Examples"
rm -f "Assets/Mapbox/Examples.meta"
```

**Savings: 22 MB**

---

### STEP 2: Clean Up LMHPOLY Package (726 MB!) 🚨

This is your BIGGEST problem! Options:

#### Option A: Keep Only What You Use

1. **Open your main scene** in Unity
2. **Check which LMHPOLY assets are actually used**:
   - Window → Analysis → Frame Debugger
   - Look for LMHPOLY materials/meshes in use

3. **If you're only using a few assets:**
   - Create new folder: `Assets/UsedAssets/LMHPOLY_Used`
   - Move ONLY the assets you use there
   - Delete the entire `Assets/Packages/LMHPOLY` folder
   - Keep only what you moved

#### Option B: Delete Unused Textures

Many asset packs include **massive uncompressed textures**:

```bash
# Find all large textures in LMHPOLY
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"
find Assets/Packages/LMHPOLY -name "*.png" -size +2M
```

**In Unity:**
1. Navigate to each large texture
2. Select it
3. Inspector → Max Size: **1024** (instead of 2048/4096)
4. Compression: **Normal Quality**
5. Click **Apply**

#### Option C: Remove Entirely (if not essential)

**If you're not using LMHPOLY assets in your current build:**

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"
rm -rf "Assets/Packages/LMHPOLY"
rm -f "Assets/Packages/LMHPOLY.meta"
```

**Savings: Up to 726 MB!**

---

### STEP 3: Optimize Handpainted Grass Textures (170 MB)

This package has MANY large grass/snow textures you probably don't need.

#### Check What's Used:

1. **Open Unity**
2. **Edit → Project Settings → Editor**
3. **Asset Database → Caching → Clear Cache** (to rebuild)
4. **Window → Analysis → Project Auditor** (if you have it)

#### Reduce Texture Sizes:

**For all grass textures:**

1. Navigate to `Assets/Packages/Handpainted_Grass_and_Ground_Textures/Textures`
2. **Select all PNG files**
3. Inspector → **Platform: WebGL**
4. **Max Size: 512** (instead of 2048)
5. **Compression: Normal Quality**
6. **Apply**

**Or delete unused variants:**

```bash
# Example: If you don't need snow textures
rm -rf "Assets/Packages/Handpainted_Grass_and_Ground_Textures/Textures/Snow"
```

**Savings: 50-100 MB**

---

### STEP 4: Remove Documentation & Examples

These are NOT included in builds, but they clutter your project:

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"

# Remove KinematicCharacterController walkthrough
rm -rf "Assets/Packages/KinematicCharacterController/Walkthrough"

# Remove any README/docs folders
find Assets/Packages -name "README*" -delete
find Assets/Packages -name "*.pdf" -delete
find Assets/Packages -name "Documentation" -type d -exec rm -rf {} + 2>/dev/null
```

**Savings: 10-15 MB in project size (not build, but cleaner)**

---

### STEP 5: Optimize Mapbox Style Textures

The Fantasy and Realistic map styles have large textures:

```bash
# These are 2.6 MB each:
Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Fantasy/Assets/Textures/FantasyNormal.png
Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Realistic/Assets/Textures/RealisticTopNormal.png
```

**If you're not using these map styles:**

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"
rm -rf "Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Fantasy"
rm -rf "Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Realistic"
```

**Savings: 5-10 MB**

---

### STEP 6: Remove Unused Packages Entirely

**Check which packages you're actually using in your scene:**

Probably NOT using:
- ❌ Darkbringer Shader (16 MB) - unless you have dark/night effects
- ❌ Acorn Bringer Assets (4.8 MB) - what is this?
- ❌ Bitgem (1.5 MB) - unless you know you need it

**Safe to delete if not used:**

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"

# Only run these if you're NOT using these assets!
rm -rf "Assets/Packages/Darkbringer Shader"
rm -rf "Assets/Packages/Acorn Bringer Assets"
rm -rf "Assets/Packages/Bitgem"
```

**Savings: 22 MB**

---

## 🎯 Recommended Quick Cleanup (Safe & Effective)

**Do these right now (5 minutes):**

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"

# 1. Remove Mapbox examples
rm -rf "Assets/Mapbox/Examples"
rm -f "Assets/Mapbox/Examples.meta"

# 2. Remove KinematicCharacterController walkthrough
rm -rf "Assets/Packages/KinematicCharacterController/Walkthrough"

# 3. Remove unused Mapbox sample styles (if not using)
rm -rf "Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Fantasy"
rm -rf "Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Realistic"
```

**Expected savings: 30-40 MB immediately**

---

## 🚨 LMHPOLY Decision (MOST IMPORTANT)

**You need to decide:**

### Are you using LMHPOLY assets in your current scene?

**To check:**
1. Open Unity
2. Open your main scene
3. Window → Frame Debugger → Enable
4. Play the scene
5. Look for "LMHPOLY" in the hierarchy or materials

**If YES (using LMHPOLY):**
- Keep the package
- Optimize textures (reduce max size to 1024)
- Delete unused variants

**If NO (not using LMHPOLY):**
```bash
# DELETE IT - saves 726 MB!
rm -rf "Assets/Packages/LMHPOLY"
rm -f "Assets/Packages/LMHPOLY.meta"
```

---

## 📊 Expected Results

### Conservative Cleanup (keeping most assets):
- Remove Mapbox Examples: -22 MB
- Remove walkthroughs/docs: -15 MB
- Optimize textures: -50 MB
- **Total savings: ~87 MB**
- **Build size: 27 MB → ~20 MB** (still might be over 25 MB)

### Aggressive Cleanup (removing LMHPOLY if unused):
- All of the above: -87 MB
- Remove LMHPOLY: -726 MB
- **Total savings: ~813 MB**
- **Build size: 27 MB → ~10-15 MB** ✅ Under 25 MB!

---

## ✅ After Cleanup Checklist

```
[ ] Removed Mapbox/Examples folder
[ ] Removed KinematicCharacterController/Walkthrough
[ ] Decided on LMHPOLY (keep/remove/optimize)
[ ] Optimized texture sizes (Max Size: 1024 or 512)
[ ] Removed unused packages
[ ] In Unity: Assets → Reimport All (to rebuild)
[ ] File → Build Settings → Build (rebuild WebGL)
[ ] Check new build size in Build/Build folder
```

---

## 🔧 Unity Settings to Verify After Cleanup

Before rebuilding, verify these settings:

**Edit → Project Settings → Player → WebGL:**
```
Publishing Settings:
  ✅ Compression Format: Gzip
  ✅ Enable Exceptions: None
  ✅ C++ Compiler Configuration: Master
  ✅ Code Optimization: Size

Other Settings:
  ✅ Managed Stripping Level: Minimal
  ✅ Api Compatibility Level: .NET Standard 2.1
```

**File → Build Settings:**
```
✅ Development Build: UNCHECKED
✅ Only your main scene in "Scenes In Build"
```

---

## 🎯 My Recommendation

**Run this cleanup script (paste in terminal):**

```bash
#!/bin/bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"

echo "Starting cleanup..."

# Remove Mapbox examples
echo "Removing Mapbox examples..."
rm -rf "Assets/Mapbox/Examples"
rm -f "Assets/Mapbox/Examples.meta"

# Remove walkthroughs
echo "Removing walkthroughs..."
rm -rf "Assets/Packages/KinematicCharacterController/Walkthrough"

# Remove sample map styles
echo "Removing sample map styles..."
rm -rf "Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Fantasy"
rm -rf "Assets/Mapbox/Resources/MapboxStyles/Styles/MapboxSampleStyles/Realistic"

echo "Cleanup complete!"
echo "Savings: ~30-40 MB"
echo ""
echo "Next steps:"
echo "1. Open Unity and check if build references LMHPOLY assets"
echo "2. If NOT using LMHPOLY, delete it (saves 726 MB!)"
echo "3. Rebuild: File → Build Settings → Build"
```

---

## Summary

**Quick wins (do now):**
- ✅ Delete Mapbox/Examples (22 MB)
- ✅ Delete walkthroughs/docs (15 MB)
- ✅ Delete unused map styles (10 MB)

**Big decision:**
- 🤔 **LMHPOLY (726 MB)** - Check if you're using it!
  - If NO → Delete it, instant 726 MB savings
  - If YES → Optimize textures (Max Size: 1024)

**After cleanup:**
- Rebuild in Unity
- Check build size
- Should be well under 25 MB! 🎉
