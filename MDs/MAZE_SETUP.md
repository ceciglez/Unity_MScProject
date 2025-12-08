# Maze User Testing Integration for Unity WebGL

## Overview
This document explains how to integrate the Maze user testing snippet with your Unity WebGL build.

## The Problem
Unity WebGL builds generate the `index.html` file from templates during the build process. Any manual edits to the built `index.html` file will be **overwritten** on the next build.

## The Solution
We've created a custom WebGL template that includes the Maze snippet pre-integrated.

## Setup Instructions

### 1. Custom Template Location
The custom template is located at:
```
Assets/WebGLTemplates/PWA-Maze/
```

This template is based on Unity's PWA (Progressive Web App) template with the Maze snippet added.

### 2. Configure Unity to Use the Custom Template

1. Open your Unity project
2. Go to **File > Build Settings**
3. Select **WebGL** as the platform
4. Click **Player Settings**
5. In the Player Settings panel, find the **Resolution and Presentation** section
6. Look for **WebGL Template**
7. Select **PWA-Maze** from the dropdown menu

### 3. Build Your Project
Now when you build your WebGL project:
- The Maze snippet will automatically be included in the `<head>` section of the generated `index.html`
- All builds will have Maze tracking enabled

### 4. Verify Integration
After building, you can verify the integration by:
1. Opening the generated `index.html` file in your build folder
2. Looking for the Maze snippet in the `<head>` section
3. Opening the build in a browser and checking the browser console for any Maze-related messages

## What Was Added

The following script was added to the `<head>` section of `index.html`:

```javascript
<script>
  (function (m, a, z, e) {
    var s, t, u, v;
    try {
      t = m.sessionStorage.getItem('maze-us');
    } catch (err) {}

    if (!t) {
      t = new Date().getTime();
      try {
        m.sessionStorage.setItem('maze-us', t);
      } catch (err) {}
    }

    u = document.currentScript || (function () {
      var w = document.getElementsByTagName('script');
      return w[w.length - 1];
    })();
    v = u && u.nonce;

    s = a.createElement('script');
    s.src = z + '?apiKey=' + e;
    s.async = true;
    if (v) s.setAttribute('nonce', v);
    a.getElementsByTagName('head')[0].appendChild(s);
    m.mazeUniversalSnippetApiKey = e;
  })(window, document, 'https://snippet.maze.co/maze-universal-loader.js', '120ab19c-6c73-44a5-99d8-759c9d978ce2');
</script>
```

## Potential Conflicts

### Content Security Policy (CSP)
If you have a strict Content Security Policy, you may need to allow:
- `https://snippet.maze.co` for script loading
- `unsafe-inline` for the inline script (or use CSP nonces)

### Service Worker Caching
The PWA template includes a Service Worker. Make sure your Service Worker configuration allows external scripts from Maze domains:
- `https://snippet.maze.co`
- Any other Maze CDN domains

### Unity WebGL Loader Timing
The Maze snippet loads asynchronously in the `<head>`, so it should not interfere with Unity's loading process. The snippet:
- Uses `async` loading
- Doesn't block page rendering
- Loads independently from Unity's loader

## Troubleshooting

### Maze Not Loading
1. Check browser console for errors
2. Verify the API key is correct: `120ab19c-6c73-44a5-99d8-759c9d978ce2`
3. Check if any ad blockers or privacy tools are blocking Maze
4. Verify network requests to `snippet.maze.co` are successful

### Template Not Appearing in Unity
1. Make sure the template folder is inside `Assets/WebGLTemplates/`
2. Restart Unity Editor to refresh the template list
3. Check that the folder structure matches Unity's requirements

### Build Errors
If you encounter build errors after selecting the custom template:
1. Make sure all files were copied correctly from the PWA template
2. Check that the `thumbnail.png` file exists in the template folder
3. Verify the `index.html` syntax is correct

## Updating the Maze API Key

If you need to change the Maze API key in the future:

1. Open `Assets/WebGLTemplates/PWA-Maze/index.html`
2. Find the Maze snippet in the `<head>` section
3. Replace the API key in the last parameter of the IIFE:
   ```javascript
   '120ab19c-6c73-44a5-99d8-759c9d978ce2'  // Replace this
   ```
4. Rebuild your WebGL project

## Related Files
- [Assets/WebGLTemplates/PWA-Maze/index.html](../Assets/WebGLTemplates/PWA-Maze/index.html) - Main template file with Maze integration
- [ProjectSettings/ProjectSettings.asset](../ProjectSettings/ProjectSettings.asset) - Unity player settings (line 835 shows template selection)
