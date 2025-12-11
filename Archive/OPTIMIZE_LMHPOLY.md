# Optimize LMHPOLY Package (Keep & Reduce Size)

## ✅ LMHPOLY Restored!

The package has been restored from git. Now let's optimize it to reduce build size **without deleting your prefabs**.

## 🎯 Strategy: Optimize Textures, Not Models

The 726 MB LMHPOLY package is mostly **textures** (images), not the actual 3D models. We can:
1. Keep ALL your prefabs/models
2. Reduce texture sizes (won't affect visual quality much)
3. Enable texture compression

This can reduce the package from **726 MB → ~100-200 MB** while keeping everything functional!

---

## 🔧 Method 1: Batch Optimize LMHPOLY Textures in Unity

### Step 1: Select All LMHPOLY Textures

1. **Open Unity**
2. Navigate to **Project window**
3. Go to `Assets/Packages/LMHPOLY`
4. In search box (top right), type: `t:texture`
5. This will show ALL textures in LMHPOLY
6. **Select all** (Ctrl+A / Cmd+A)

### Step 2: Batch Change Texture Settings

With all textures selected:

1. **Inspector** (right panel)
2. **Platform: WebGL** (click the WebGL tab)
3. **Override for WebGL**: ✓ CHECK this box
4. **Max Size**: Change to **1024** (or even 512 for more savings)
5. **Compression**: Normal Quality
6. **Click "Apply"**

### Savings: 400-500 MB! (726 MB → ~200-300 MB)

---

## 🔧 Method 2: Use Texture Compression

Even more aggressive optimization:

### For All LMHPOLY Textures:

1. Select all textures (as above)
2. **Max Size: 512** (instead of 1024)
3. **Compression: High Quality Compressed**
4. **Generate Mip Maps: ✓** (CHECK - better performance)
5. Apply

### Savings: 500-600 MB! (726 MB → ~100-200 MB)

---

## 🔧 Method 3: Identify Largest Textures

Let me find the biggest texture files:

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"
find Assets/Packages/LMHPOLY -name "*.png" -o -name "*.tga" -o -name "*.jpg" | \
  xargs ls -lh | sort -k5 -hr | head -20
```

Then manually optimize just the largest ones.

---

## 🔧 Method 4: Remove Texture Variants You Don't Use

LMHPOLY often includes multiple versions of textures:
- Different seasons (spring, summer, fall, winter)
- Different times of day
- Different resolutions

### Check What You're Using:

1. Open your scene in Unity
2. Window → Rendering → Lighting
3. Check which materials are actually used
4. Delete unused texture variants

### Common Unused Variants:
```
/Winter/      (if you're not using winter)
/Autumn/      (if you're not using autumn)
/Night/       (if you're not using night mode)
```

---

## 📊 Expected Results

### Before Optimization:
- LMHPOLY: 726 MB
- Your build: 27 MB (with compression)

### After Texture Optimization (Max Size: 1024):
- LMHPOLY: ~200-300 MB
- **Your build: ~15-20 MB** ✅ Under 25 MB!

### After Aggressive Optimization (Max Size: 512):
- LMHPOLY: ~100-200 MB
- **Your build: ~10-15 MB** ✅ Well under 25 MB!

---

## ⚠️ Important Notes

**Will this break my prefabs?**
- ❌ **NO!** The prefabs/models stay the same
- ✅ Only the texture **resolution** changes
- ✅ Your game will look almost identical
- ✅ Might even run faster (smaller textures = better performance)

**What's the difference between 2048 and 1024?**
- 2048 = Very high resolution (often overkill for WebGL)
- 1024 = High resolution (perfect for most WebGL games)
- 512 = Medium resolution (still looks good, very performant)

**Can I undo this?**
- ✅ YES! Just select textures again and change Max Size back
- Or use git: `git checkout Assets/Packages/LMHPOLY`

---

## 🎯 Recommended Quick Fix (5 minutes)

**Do this in Unity:**

1. **Project window** → `Assets/Packages/LMHPOLY`
2. **Search**: `t:texture`
3. **Select all** (Ctrl+A)
4. **Inspector** → Max Size: **1024**
5. **Compression**: Normal Quality
6. **Click "Apply"**
7. **Wait for Unity to reimport** (5-10 minutes)

**Result:**
- LMHPOLY: 726 MB → ~200 MB
- Build size: 27 MB → **~15 MB** ✅

---

## 🚀 Alternative: Use Asset Bundles

If your build is STILL too large after texture optimization:

### Move LMHPOLY to Asset Bundle

1. Select LMHPOLY prefabs
2. Inspector → Bottom → Asset Bundle → New
3. Name: `lmhpoly`
4. Build asset bundle separately
5. Load at runtime

**Benefits:**
- Main build: Much smaller
- Asset bundle: Loads on-demand
- Can be hosted separately (no 25 MB limit)

**Downside:**
- More complex setup
- Prefabs load asynchronously

---

## 📋 Step-by-Step: Optimize LMHPOLY Now

**Let's do the texture optimization together:**

### Step 1: Open Unity

Wait for it to reimport assets after restoring LMHPOLY.

### Step 2: Find All LMHPOLY Textures

```
1. Project window
2. Click "Assets/Packages/LMHPOLY"
3. Search box (top right): t:texture
4. You'll see hundreds of textures
```

### Step 3: Select and Optimize

```
1. Select all textures (Ctrl+A / Cmd+A)
2. Inspector (right panel)
3. Find "Default" or "WebGL" platform tab
4. Max Size: 1024 (change from 2048/4096)
5. Compression: Normal Quality
6. Click "Apply" button at bottom
```

### Step 4: Wait for Reimport

Unity will reimport all textures (5-15 minutes). You'll see progress bar.

### Step 5: Rebuild

```
File → Build Settings → WebGL → Build
```

Check new build size - should be ~15 MB now! ✅

---

## Summary

✅ **LMHPOLY restored** - Your prefabs are safe!
✅ **Solution**: Optimize textures, not models
✅ **Method**: Reduce Max Size to 1024 or 512
✅ **Expected savings**: 400-600 MB
✅ **Build size**: Should drop to ~10-15 MB
✅ **Visual quality**: Almost no difference
✅ **Your prefabs**: 100% functional

**Next step:** Follow "Step-by-Step" above to optimize textures in Unity! 🎯
