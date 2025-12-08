# Deploying WebGL Build to GitHub Pages

## Problem

- GitHub has a **100 MB file size limit** for individual files
- WebGL builds often contain files >100 MB (.wasm, .data files)
- You cannot push large build files to GitHub normally

## ✅ RECOMMENDED Solution: Separate Build Repository

The **best practice** is to keep your source code and build separate:

### Step 1: Create a New Repository for the Build

1. Go to GitHub: https://github.com/new
2. **Repository name**: `mth-map-webgl` (or your project name + `-webgl`)
3. **Public** or Private (your choice)
4. **Do NOT initialize** with README, .gitignore, or license
5. Click **Create repository**

### Step 2: Build Your WebGL Project

1. **Open Unity**
2. **File → Build Settings**
3. **Select WebGL** platform
4. Click **"Build"** (NOT "Build and Run")
5. **Choose output folder**: Create a new folder outside your Unity project
   - Example: `~/Desktop/WebGLBuild`
6. **Wait for build to complete** (10-30 minutes)

### Step 3: Push Build to the New Repository

Open terminal and navigate to your build folder:

```bash
# Navigate to your build folder
cd ~/Desktop/WebGLBuild  # Or wherever you built it

# Initialize git
git init

# Add all files
git add .

# Commit
git commit -m "Initial WebGL build with CORS fix"

# Rename branch to main
git branch -M main

# Add remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/mth-map-webgl.git

# Push
git push -u origin main
```

### Step 4: Enable GitHub Pages

1. Go to your **new repository** on GitHub
2. Click **Settings** (top right)
3. Click **Pages** (left sidebar)
4. Under **Source**:
   - Branch: `main`
   - Folder: `/ (root)`
5. Click **Save**
6. Wait 1-2 minutes for deployment

### Step 5: Access Your Build

Your WebGL build will be live at:
```
https://YOUR_USERNAME.github.io/mth-map-webgl/
```

Example:
```
https://pimika.github.io/mth-map-webgl/
```

---

## Alternative: Use Docs Folder (Keep Everything in One Repo)

If you prefer to keep source and build together:

### Step 1: Update .gitignore

Edit `.gitignore` and add at the bottom:

```gitignore
# Allow docs folder for GitHub Pages deployment
![Dd]ocs/
!docs/**/*
```

### Step 2: Build to Docs Folder

1. In Unity: **File → Build Settings → WebGL**
2. Click **"Build"**
3. **Choose folder**: Your Unity project's `docs` folder
   - If it doesn't exist, create it: `YourProject/docs`
4. Build and wait

### Step 3: Commit and Push

```bash
cd "/Users/ceci/UAL MSC THESIS/6Dec/Unity_MScProject-main"

# Add docs folder
git add docs/

# Commit
git commit -m "Add WebGL build for GitHub Pages"

# Push
git push
```

### Step 4: Enable GitHub Pages

1. Go to repository **Settings → Pages**
2. Source:
   - Branch: `main`
   - Folder: `/docs`
3. Save

Your build will be at:
```
https://YOUR_USERNAME.github.io/Unity_MScProject-main/
```

---

## If You Get "File Too Large" Error

If individual files are >100 MB, use **Git Large File Storage (LFS)**:

### Install Git LFS

**Mac:**
```bash
brew install git-lfs
```

**Windows:**
Download from: https://git-lfs.github.com/

**Linux:**
```bash
sudo apt-get install git-lfs
```

### Set Up Git LFS

```bash
cd YourBuildFolder

# Initialize Git LFS
git lfs install

# Track large files
git lfs track "*.wasm"
git lfs track "*.data"
git lfs track "*.wasm.gz"
git lfs track "*.data.gz"
git lfs track "*.unityweb"

# Add .gitattributes
git add .gitattributes

# Add and commit
git add .
git commit -m "Add WebGL build with Git LFS"
git push
```

**Note**: GitHub LFS has free tier limits:
- 1 GB storage
- 1 GB bandwidth per month
- Additional storage/bandwidth costs money

---

## Recommended Approach Summary

**For Best Results:**

1. ✅ **Create separate repo** for WebGL build (`mth-map-webgl`)
2. ✅ **Build outside Unity project** folder
3. ✅ **Push only build files** to new repo
4. ✅ **Enable GitHub Pages** on build repo
5. ✅ **Keep source code repo clean** (no builds)

**Your Repositories:**
- `Unity_MScProject-main` → Source code only
- `mth-map-webgl` → WebGL build only (for GitHub Pages)

---

## Updating Your Build

When you make changes and rebuild:

```bash
# Build in Unity to same folder

# Navigate to build folder
cd ~/Desktop/WebGLBuild

# Add changes
git add .

# Commit
git commit -m "Update WebGL build with latest changes"

# Push
git push

# GitHub Pages automatically updates in 1-2 minutes
```

---

## Testing Before Deployment

**ALWAYS test locally before pushing to GitHub Pages:**

```bash
cd YourBuildFolder
python3 -m http.server 8000

# Open browser: http://localhost:8000
# Test that minimap and observations work
# Check browser console for errors
```

Only push to GitHub Pages after confirming it works locally!

---

## GitHub Pages Limitations

✅ **Works Great For:**
- Static WebGL builds
- Free hosting
- Fast CDN delivery
- Custom domains

⚠️ **Limitations:**
- 1 GB repository size limit (total)
- 100 MB individual file limit (use Git LFS if needed)
- Static content only (no server-side code)
- Bandwidth: Reasonable for small projects

---

## Custom Domain (Optional)

If you want a custom domain like `mth-map.yourdomain.com`:

1. Buy domain (Namecheap, Google Domains, etc.)
2. Add DNS CNAME record:
   - Name: `mth-map`
   - Value: `YOUR_USERNAME.github.io`
3. In GitHub Pages settings:
   - Custom domain: `mth-map.yourdomain.com`
   - Check "Enforce HTTPS"

---

## Troubleshooting

### Build works locally but not on GitHub Pages

**Check browser console for errors:**
- Mixed content errors (HTTP/HTTPS)
- 404 errors for missing files
- CORS errors (our fix should prevent this!)

**Common fixes:**
1. Ensure all paths are relative (not absolute)
2. Check file names are correct (case-sensitive on GitHub)
3. Wait a few minutes for GitHub Pages to update
4. Clear browser cache and hard reload

### Files too large for GitHub

**Solutions:**
1. Use Git LFS (see above)
2. Or use separate build repo (recommended)
3. Or use Itch.io, Netlify, or Vercel instead

### GitHub Pages not updating

**Try:**
1. Make a small change and push again
2. Check Settings → Pages is still enabled
3. Wait 5-10 minutes (sometimes takes time)
4. Check GitHub Actions tab for deployment status

---

## Alternative Hosting Options

If GitHub Pages doesn't work for your needs:

### Itch.io (FREE)
- **Best for:** Game distribution
- Upload: ZIP file of build
- URL: `yourusername.itch.io/mth-map`
- Pros: Designed for games, easy upload
- Cons: Less professional URL

### Netlify (FREE)
- **Best for:** Modern web apps
- Drag & drop build folder
- URL: `mth-map.netlify.app`
- Pros: Fast, automatic HTTPS, custom domains
- Cons: 100 GB bandwidth/month limit

### Vercel (FREE)
- **Best for:** Web applications
- Similar to Netlify
- URL: `mth-map.vercel.app`
- Pros: Fast, excellent performance
- Cons: 100 GB bandwidth/month

### GitHub Pages (FREE)
- **Best for:** Project demos, portfolios
- No upload needed (git push)
- URL: `username.github.io/mth-map-webgl`
- Pros: Integrated with GitHub, version control
- Cons: 1 GB repo limit, static only

---

## Summary

**Recommended Setup:**

1. **Source Code Repo** (`Unity_MScProject-main`):
   - Contains: Unity project, C# scripts, assets
   - **Does NOT contain**: Build files
   - **Purpose**: Development and version control

2. **Build Repo** (`mth-map-webgl`):
   - Contains: Only the WebGL build files
   - **Does NOT contain**: Source code
   - **Purpose**: GitHub Pages hosting

3. **GitHub Pages**:
   - Serves: `mth-map-webgl` repository
   - URL: `https://YOUR_USERNAME.github.io/mth-map-webgl/`
   - **Purpose**: Public web hosting

This separation keeps everything clean, fast, and within GitHub's limits! 🚀
