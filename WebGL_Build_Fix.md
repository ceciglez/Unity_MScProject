# WebGL Build Fix Instructions

## The Assembly-CSharp-Editor Error Solution

This error is caused by Unity's Burst compiler trying to reference editor assemblies in WebGL builds. Here are the steps to fix it:

### Step 1: Clean Unity Cache (Already Done)
✅ Deleted Library/ScriptAssemblies
✅ Deleted Library/Bee  
✅ Deleted Temp folder

### Step 2: In Unity - Disable Burst for WebGL

1. **Open Unity**
2. **Go to**: Window → Package Manager
3. **Search for**: Burst
4. **Click**: Burst package
5. **Click**: Remove (if possible) OR
6. **Go to**: Edit → Project Settings → XR Plug-in Management → Burst AOT Settings
7. **Uncheck**: Enable Burst Compilation for WebGL

### Step 3: Alternative - Project Settings Fix

1. **Edit → Project Settings**
2. **Player → WebGL Settings**
3. **Configuration**: Master or Release
4. **Scripting Backend**: IL2CPP
5. **Api Compatibility Level**: .NET Standard 2.1
6. **Managed Stripping Level**: Minimal (not High)

### Step 4: If Above Doesn't Work - Create Assembly Definition

1. **Right-click in Assets/Scripts**
2. **Create → Assembly Definition**
3. **Name it**: "RuntimeScripts"
4. **Settings**:
   - Platforms: Everything EXCEPT Editor
   - References: Leave empty initially
5. **Move all runtime scripts** to this assembly

### Step 5: Last Resort - Exclude Problematic Scripts

1. **Find any scripts using [BurstCompile] attribute**
2. **Temporarily disable or remove** these attributes
3. **Try building again**

### Step 6: Build Settings Optimization

1. **File → Build Settings**
2. **Player Settings → Publishing Settings**
3. **Compression Format**: Gzip
4. **Memory Size**: 512 MB
5. **Enable Exceptions**: None
6. **Code Optimization**: Size

## Try These Steps in Order:

1. ✅ **Restart Unity** after cleaning cache
2. **Try Step 2** (Disable Burst)  
3. **Try Step 3** (Project Settings)
4. **Try Step 4** (Assembly Definition) if needed
5. **Try Step 5** (Exclude scripts) as last resort

The most likely fix is Step 2 - disabling Burst compilation for WebGL builds.