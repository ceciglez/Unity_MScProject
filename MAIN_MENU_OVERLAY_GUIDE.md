# Main Menu Overlay - Setup Guide

## 🎯 What Is This?

An **overlay version** of the main menu that appears in the **same scene** as your game, instead of using a separate MainMenu scene.

---

## ⚡ Overlay vs Separate Scene

| Feature | Overlay (Same Scene) | Separate Scene |
|---------|---------------------|----------------|
| **Setup** | ✅ Very Simple | More complex |
| **Speed** | ✅ Instant (no loading) | Scene transition delay |
| **Scene Count** | ✅ One scene | Two scenes |
| **Background** | Can show map preview | Black/loading screen |
| **Complexity** | ✅ Lower | Higher |
| **Build Settings** | ✅ One scene | Two scenes |
| **Best For** | Quick setup, previews | Professional menu system |

**TL;DR**: Overlay is **much simpler** and **faster** to setup!

---

## 🚀 Quick Setup (3 Minutes!)

### Step 1: Create UI Overlay in Your Game Scene

**In your existing game scene:**

1. Find or create your UI Canvas
2. Add these panels as children of Canvas:

```
Canvas
├── MainMenuOverlayPanel (Panel)
│   ├── Title: "BIODIVERSITY EXPLORER"
│   ├── ExplorationModeButton: "Exploration Mode"
│   ├── SearchUserModeButton: "Search iNaturalist User"
│   └── AboutButton: "About"
│
├── UserSearchPanel (Panel, inactive)
│   ├── Title: "Search iNaturalist User"
│   ├── UsernameInput (InputField)
│   ├── SearchButton: "Search & Start"
│   ├── BackButton: "Back"
│   └── SearchStatusText: ""
│
└── AboutPanel (Panel, inactive)
    ├── Title: "About"
    ├── Description: Your project info
    └── AboutBackButton: "Back"
```

### Step 2: Style the Overlay

**Make it look like a real menu:**

1. **MainMenuOverlayPanel** settings:
   - Anchor: Stretch (fill screen)
   - Color: Semi-transparent black or solid color
   - Example: `Color(0, 0, 0, 0.9)` for dark overlay

2. **Buttons** should be centered and large
3. **UserSearchPanel** and **AboutPanel** should be inactive by default

### Step 3: Add MainMenuOverlay Script

1. Select **Canvas**
2. Add Component → **Main Menu Overlay**
3. Assign all references:

```
Main Menu Overlay:
├── UI Panels
│   ├── Main Menu Panel: [Drag MainMenuOverlayPanel]
│   ├── User Search Panel: [Drag UserSearchPanel]
│   └── About Panel: [Drag AboutPanel]
│
├── Main Menu Buttons
│   ├── Exploration Mode Button: [Drag button]
│   ├── Search User Mode Button: [Drag button]
│   └── About Button: [Drag button]
│
├── User Search Elements
│   ├── Username Input: [Drag InputField]
│   ├── Search Button: [Drag SearchButton]
│   ├── Back Button: [Drag BackButton]
│   └── Search Status Text: [Drag SearchStatusText]
│
├── About Panel Elements
│   └── About Back Button: [Drag AboutBackButton]
│
├── Map References
│   ├── Map: [Drag AbstractMap GameObject]
│   └── Map Controller: [Drag INaturalistMapController]
│
├── Biodiversity Effect References
│   ├── Biodiversity Volume Spawner: [Drag BiodiversityVolumeSpawner]
│   └── Global Volume Object: [Drag "Global Volume" GameObject]
│
├── Default Location
│   ├── Default Latitude: 37.7749
│   └── Default Longitude: -122.4194
│
└── Settings
    ├── Hide After Selection: ✓ Checked
    └── Block Player Movement: ✓ Checked
```

### Step 4: Test!

1. Press Play
2. Menu overlay appears on top of scene
3. Click "Exploration Mode" or search for `kueda`
4. Overlay fades away, game starts!

---

## 🎨 Styling Tips

### Make It Look Professional:

**1. Dark Semi-Transparent Background:**
```csharp
// MainMenuOverlayPanel settings:
Color: (0, 0, 0, 230) // Dark with alpha
```

**2. Blur Effect (Optional):**
- Use UI Blur shader on MainMenuOverlayPanel
- Makes background map blurred/frosted

**3. Fade In/Out Animation:**
- Add CanvasGroup to MainMenuOverlayPanel
- Animate Alpha from 0 to 1 on show
- Animate Alpha from 1 to 0 on hide

**4. Button Styling:**
```
Width: 400px
Height: 70px
Font Size: 28
Color: White text on colored background
Hover: Slight scale up or color change
```

---

## 🔧 How It Works

### On Scene Start:

```
1. MainMenuOverlay.Start() runs
   ↓
2. Shows main menu overlay
   ↓
3. Blocks player movement
   ↓
4. Unlocks cursor
   ↓
5. Waits for user selection...
```

### When User Selects Mode:

```
User clicks button
   ↓
Search API (if user mode)
   ↓
map.Initialize(lat, lng)
   ↓
Wait 2 seconds for map
   ↓
Load observations (if user searched)
   ↓
Hide overlay
   ↓
Enable player movement
   ↓
Game starts!
```

---

## ✨ Key Features

### Overlay-Specific Features:

1. **Player Movement Blocking**
   - Disables all MonoBehaviour scripts on Player
   - Re-enables after selection
   - Prevents movement during menu

2. **Map Visible in Background**
   - Can show loading/generating map tiles
   - Creates anticipation
   - Looks more polished

3. **No Scene Transitions**
   - Instant start
   - No loading screens
   - Smoother experience

4. **Simpler Architecture**
   - One scene to manage
   - No GameManager needed
   - No scene loading code

---

## 📋 Comparison: Features

### MainMenuController (Separate Scene):
- ✅ Professional separate menu
- ✅ Can have elaborate menu scenes
- ✅ Clear separation of concerns
- ❌ Requires scene loading
- ❌ More setup (GameManager, Build Settings)
- ❌ Two scenes to maintain

### MainMenuOverlay (Same Scene):
- ✅ Very simple setup
- ✅ Instant (no loading)
- ✅ Single scene
- ✅ Can show map preview
- ✅ Less code needed
- ❌ Always loads full game scene first
- ❌ Can't have elaborate menu backgrounds

---

## 🎯 Which Should You Use?

### Use **Overlay** if:
- ✅ You want simplest setup
- ✅ You want fastest loading
- ✅ You have one main scene
- ✅ You want to show map preview
- ✅ You're prototyping

### Use **Separate Scene** if:
- ✅ You want professional menu system
- ✅ You want elaborate menu visuals
- ✅ You want settings/options menus
- ✅ You want clean separation
- ✅ You're building final product

**For your use case**: Overlay is probably **perfect**! It's simple, fast, and works great for a research project.

---

## 🐛 Troubleshooting

### Issue: Overlay Doesn't Show

**Check:**
- Is MainMenuOverlay attached to Canvas?
- Is MainMenuPanel active?
- Is Canvas enabled?

**Fix:**
- Ensure MainMenuPanel.SetActive(true) in Start()
- Check Canvas Scaler settings

---

### Issue: Can Still Move During Menu

**Check:**
- Is "Block Player Movement" checked?
- Does player have "Player" tag?

**Fix:**
- Tag your player GameObject as "Player"
- Or manually assign player reference

---

### Issue: Map Not Initializing

**Check:**
- Is map reference assigned?
- Is map already initialized?

**Fix:**
- Drag AbstractMap into Map slot
- Ensure map doesn't auto-initialize on Start()

---

## 💡 Advanced: Remove Existing Map Initialization

If your map currently initializes on Start(), you need to delay it:

### Option 1: Disable AbstractMap Initially

1. Disable AbstractMap component
2. MainMenuOverlay will enable it when ready

### Option 2: Use InitializeOnStart = false

```csharp
// In AbstractMap Inspector:
Initialize On Start: Unchecked
```

Then MainMenuOverlay calls `map.Initialize()` when ready.

---

## 🎨 Example Overlay Style

### Full-Screen Dark Overlay:

```
MainMenuOverlayPanel:
├── Rect Transform
│   ├── Anchor: Stretch
│   └── Offset: 0, 0, 0, 0
├── Image
│   ├── Color: (0, 0, 0, 230) // Dark with transparency
│   └── Raycast Target: ✓ Checked
└── Canvas Group (optional)
    └── Alpha: 1 (animate 0→1 for fade in)
```

### Centered Buttons:

```
ExplorationModeButton:
├── Rect Transform
│   ├── Anchor: Center
│   ├── Position: (0, 50, 0)
│   └── Size: (400, 70)
├── Image (Button background)
│   └── Color: Your accent color
└── Text
    ├── Text: "Exploration Mode"
    ├── Font Size: 28
    ├── Color: White
    └── Alignment: Center
```

---

## 📝 Quick Checklist

Setup checklist:
- [ ] Created MainMenuOverlayPanel in Canvas
- [ ] Created UserSearchPanel (inactive)
- [ ] Created AboutPanel (inactive)
- [ ] Created all buttons
- [ ] Added MainMenuOverlay script to Canvas
- [ ] Assigned all UI references
- [ ] Assigned map reference
- [ ] Assigned map controller reference
- [ ] Set default location
- [ ] Checked "Block Player Movement"
- [ ] Tested exploration mode
- [ ] Tested user search with `kueda`
- [ ] Overlay hides after selection
- [ ] Player can move after selection

---

## 🚀 Summary

**Overlay Version:**
- **Setup Time**: 3 minutes
- **Scenes Needed**: 1 (your game scene)
- **Scripts Needed**: 1 (MainMenuOverlay.cs)
- **Scene Transitions**: 0
- **Loading Time**: Instant
- **Complexity**: Very Low ✅

**Perfect for:**
- Quick implementation
- Research projects
- Prototypes
- Single-scene applications
- When you want simplicity

---

## 📖 Files

| File | Purpose |
|------|---------|
| `MainMenuOverlay.cs` | Overlay controller script |
| `MAIN_MENU_OVERLAY_GUIDE.md` | This guide |
| `MainMenuController.cs` | Alternative: Separate scene version |
| `MAIN_MENU_SETUP_GUIDE.md` | Separate scene guide |

**Choose one approach** - Overlay OR Separate Scene!

---

## ✅ You're Done!

That's it! The overlay is **much simpler** than the separate scene approach:
- No GameManager needed
- No scene loading code
- No Build Settings configuration
- Just add UI to your existing scene!

**Try it and see which you prefer!** 🎉
