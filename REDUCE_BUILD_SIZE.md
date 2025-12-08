# Reducing WebGL Build Size

## Problem

Your build file `mth_map_web_FS_v2.4.data.unityweb` is **28.4 MB**, but GitHub Pages has a **25 MB file limit**.

## ✅ SOLUTION 1: Enable Gzip Compression (BEST FIX)

This is the **most effective** way to reduce size by 60-80%!

### Steps:

1. **Open Unity**
2. **Edit → Project Settings → Player → WebGL (tab)**
3. **Scroll to "Publishing Settings"**
4. **Compression Format**: Change to **Gzip**
5. **Rebuild**

### Result:
- Your 28.4 MB file → **5-10 MB** (compressed)
- Files will be `.gz` instead of `.unityweb`
- GitHub Pages and browsers support Gzip natively

**This alone should fix your problem!** ✅

---

## ✅ SOLUTION 2: Disable Development Build

Development builds include debug symbols that add 5-10 MB.

### Steps:

1. **File → Build Settings**
2. **Uncheck "Development Build"**
3. **Rebuild**

### Savings: 5-10 MB

---

## ✅ SOLUTION 3: Code Optimization

Change compiler settings for smaller code size.

### Steps:

1. **Edit → Project Settings → Player → WebGL**
2. **Publishing Settings → C++ Compiler Configuration**: Set to **Master**
3. **Code Optimization**: Change to **Size** (instead of Speed)
4. **Rebuild**

### Savings: 2-5 MB

⚠️ **Note**: This may slightly reduce performance, but makes build smaller.

---

## ✅ SOLUTION 4: Remove Unused Assets

Unity includes assets you're not using in the build.

### Find Large Assets:

1. **Window → Analysis → Build Report** (after building)
2. Look for large textures, audio, or models
3. Check if you're actually using them
4. Delete unused assets

### Common Culprits:

- **Large textures** (>1MB each)
  - Reduce max size: Select texture → Inspector → Max Size: 1024 or 512
  - Enable compression: Compression: Normal Quality or High Quality

- **Audio files** (uncompressed WAV files)
  - Select audio → Inspector
  - Load Type: **Streaming**
  - Compression Format: **Vorbis**
  - Quality: **50-70%**

- **Unused models or prefabs**
  - Delete or move to a folder outside Assets/

### Savings: 5-20 MB depending on assets

---

## ✅ SOLUTION 5: Texture Compression Settings

Compress all textures more aggressively.

### Steps:

1. **Edit → Project Settings → Player → WebGL**
2. **Other Settings → Texture Compression**: Use **ETC2** (or ASTC)

### For Individual Textures:

1. Select texture in Project window
2. Inspector → **Platform: WebGL**
3. **Max Size**: 1024 or 512 (instead of 2048/4096)
4. **Compression**: Normal Quality or High Quality
5. Click **Apply**

### Savings: 3-10 MB

---

## ✅ SOLUTION 6: Exclude Unnecessary Scenes

Only include scenes you need in the build.

### Steps:

1. **File → Build Settings**
2. **Scenes In Build**: Uncheck any scenes you don't need
3. Only include your main scene(s)

### Savings: 1-5 MB per unused scene

---

## ✅ SOLUTION 7: Streaming Assets

Move large assets to StreamingAssets folder to load on-demand.

### Steps:

1. Create `Assets/StreamingAssets` folder
2. Move large files there (audio, videos, large textures)
3. Load them at runtime when needed

### Savings: Varies (files load separately, not in main data file)

---

## ❌ DON'T Change These Settings

These will break the JavaScript bridge fix:

- ❌ **Managed Stripping Level**: Keep at **Minimal** (don't change to High)
- ❌ **Enable Exceptions**: Keep at current setting
- ❌ **Scripting Backend**: Keep at IL2CPP/Default

---

## 📊 Build Size Breakdown

After applying compression and optimizations, typical WebGL build:

```
Before Optimizations:
├── .data.unityweb: 28.4 MB  ← Your current file
├── .wasm.unityweb: ~15 MB
├── .framework.js.unityweb: ~5 MB
└── Total: ~50 MB

After Gzip Compression:
├── .data.gz: ~5-8 MB  ✅ Under 25 MB limit!
├── .wasm.gz: ~3-5 MB
├── .framework.js.gz: ~1-2 MB
└── Total: ~10-15 MB  ✅ Much better!
```

---

## 🎯 Recommended Quick Fix

**Do these 3 things right now:**

1. ✅ **Enable Gzip compression** (Publishing Settings)
2. ✅ **Disable Development Build** (Build Settings)
3. ✅ **Code Optimization: Size** (Publishing Settings)

**Then rebuild and check the file sizes.**

This should get you **well under 25 MB** without much effort!

---

## 📋 Step-by-Step Optimization Checklist

```
[ ] Edit → Project Settings → Player → WebGL
    [ ] Publishing Settings → Compression Format: Gzip
    [ ] Publishing Settings → C++ Compiler Configuration: Master
    [ ] Publishing Settings → Code Optimization: Size
    [ ] Publishing Settings → Enable Exceptions: None

[ ] File → Build Settings
    [ ] Uncheck "Development Build"
    [ ] Remove unused scenes from "Scenes In Build"

[ ] Review Assets
    [ ] Check large textures (>1MB) and reduce Max Size
    [ ] Check audio files and enable Vorbis compression
    [ ] Delete unused assets

[ ] Rebuild
    [ ] File → Build Settings → Build
    [ ] Check new file sizes in build folder
```

---

## 🔍 After Building - Check File Sizes

After rebuilding, check your build folder:

```bash
# Navigate to your build folder
cd "/path/to/your/build/Build"

# List files with sizes
ls -lh

# You should see files like:
# mth_map.data.gz      (should be ~5-10 MB now!)
# mth_map.wasm.gz      (~3-5 MB)
# mth_map.framework.js.gz  (~1-2 MB)
```

All files should now be under 25 MB! ✅

---

## 🚀 Expected Results

**Before optimization:**
- Total build: ~50-60 MB
- Largest file: 28.4 MB

**After Gzip + optimizations:**
- Total build: ~10-20 MB
- Largest file: ~8-10 MB

**All files under 25 MB limit!** 🎉

---

## Alternative: Use Git LFS (If Still Too Large)

If individual files are still >25 MB after compression:

### Install Git LFS:

**Mac:**
```bash
brew install git-lfs
```

**Windows/Linux:** Download from https://git-lfs.github.com/

### Set Up for Your Build:

```bash
cd your-build-folder

# Initialize Git LFS
git lfs install

# Track large files
git lfs track "*.gz"
git lfs track "*.wasm.gz"
git lfs track "*.data.gz"

# Add .gitattributes
git add .gitattributes

# Now add and commit as normal
git add .
git commit -m "WebGL build with Git LFS"
git push
```

**Note**: Git LFS has free tier limits (1 GB storage, 1 GB bandwidth/month).

---

## Summary

**Quick Fix (5 minutes):**
1. Enable Gzip compression
2. Disable Development Build
3. Set Code Optimization to Size
4. Rebuild

**Expected result:** 28.4 MB → **~5-10 MB** ✅

**If you need more reduction:**
- Optimize textures (reduce max size)
- Compress audio files (Vorbis format)
- Remove unused assets
- Use Streaming Assets for large files

**This should easily get you under the 25 MB GitHub limit!** 🚀
