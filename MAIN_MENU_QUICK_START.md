# Main Menu System - Quick Start

## 🚀 What This Does

Instead of searching for users **after** the map loads, users now choose their exploration mode in a **main menu** before the map initializes. This is:
- ✅ Faster (map only generates once)
- ✅ Cleaner (no map relocation)
- ✅ More intuitive (choose before loading)

---

## ⚡ Quick Setup (5 Minutes)

### 1. Copy the Scripts ✅ (Already Done!)

These files have been created:
- ✅ `Assets/Scripts/UI and Minimap/MainMenuController.cs`
- ✅ `Assets/Scripts/GameManager.cs`
- ✅ `Assets/Scripts/MapInitializer.cs`

### 2. Create Main Menu Scene

**In Unity:**
1. File → New Scene
2. Save as `MainMenu.unity` in `Assets/Scenes/`
3. GameObject → UI → Canvas
4. GameObject → UI → Event System (if not auto-created)

### 3. Create UI Elements

**On Canvas, create these GameObjects:**

#### Main Menu Panel:
```
- Panel (name it "MainMenuPanel")
  - Text: "Biodiversity Explorer"
  - Button: "Exploration Mode" (name it "ExplorationModeButton")
  - Button: "Search iNaturalist User" (name it "SearchUserModeButton")
  - Button: "About" (name it "AboutButton")
  - Button: "Quit" (name it "QuitButton")
```

#### User Search Panel:
```
- Panel (name it "UserSearchPanel", set inactive)
  - Text: "Search iNaturalist User"
  - InputField: (name it "UsernameInput")
    - Placeholder: "Enter username..."
  - Button: "Search & Start" (name it "SearchButton")
  - Button: "Back" (name it "BackButton")
  - Text: (name it "SearchStatusText", leave empty)
```

#### About Panel:
```
- Panel (name it "AboutPanel", set inactive)
  - Text: "About Biodiversity Explorer"
  - Text: Your project description
  - Button: "Back" (name it "AboutBackButton")
```

**Quick Tip**: Right-click Canvas → UI → Panel/Button/InputField/Text to create these

### 4. Add MainMenuController

1. Select **Canvas**
2. Add Component → **Main Menu Controller**
3. Drag and drop all UI elements into the Inspector slots:
   - Main Menu Panel
   - User Search Panel
   - About Panel
   - All buttons
   - Username Input
   - Search Status Text
4. Set **Game Scene Name** to your main scene name (e.g., "MainScene")

### 5. Setup Game Scene

**In your main game scene:**

1. Create Empty GameObject: "GameManager"
   - Add Component → **Game Manager**

2. Find your map GameObject (the one with AbstractMap)
   - Add Component → **Map Initializer**

### 6. Configure Build Settings

1. File → Build Settings
2. Add **MainMenu** scene (drag from Project)
3. Add your **MainScene** (game scene)
4. **Ensure MainMenu is index 0** (top of list)

```
Scenes In Build:
 ✓ 0  MainMenu         ← Must be first!
 ✓ 1  MainScene        ← Your game scene
```

### 7. Test It!

1. Press Play in **MainMenu** scene
2. Click "Search iNaturalist User"
3. Type `kueda`
4. Press Enter
5. Should load game at kueda's location!

---

## 🎯 That's It!

Your main menu is ready! Users can now:
- Choose exploration mode (default location)
- Search for iNaturalist users
- Read about your project
- All before the map loads!

---

## 📖 Full Documentation

For detailed setup, customization, and troubleshooting, see:
- **MAIN_MENU_SETUP_GUIDE.md** - Complete setup guide
- **INATURALIST_USERNAME_GUIDE.md** - Valid username formats
- **STATUS_MESSAGES_GUIDE.md** - All status messages

---

## 🐛 Quick Troubleshooting

**Map loads at wrong location?**
- Check MapInitializer is attached to map GameObject
- Verify GameManager exists in game scene

**Scene won't load?**
- Check scene name in MainMenuController matches exactly
- Verify both scenes in Build Settings
- MainMenu must be index 0

**Search doesn't work?**
- Check all UI references assigned in MainMenuController
- Try a known user: `kueda`
- No underscores in usernames!

---

## ✅ Checklist

- [ ] MainMenu scene created
- [ ] UI elements created and named correctly
- [ ] MainMenuController added to Canvas with all references assigned
- [ ] GameManager added to game scene
- [ ] MapInitializer added to map GameObject
- [ ] Both scenes in Build Settings (MainMenu first!)
- [ ] Tested exploration mode
- [ ] Tested user search with `kueda`
- [ ] Everything works!

---

**Need help?** See MAIN_MENU_SETUP_GUIDE.md for detailed instructions!
