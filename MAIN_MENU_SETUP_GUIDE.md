# Main Menu System - Setup Guide

## 🎯 Overview

The new main menu system allows users to choose their exploration mode **before** the map loads:
- **Exploration Mode**: Start at default location
- **Search User Mode**: Search for an iNaturalist user and start at their last observation
- **About**: Information about the project

This is **much better** than the previous approach because:
- ✅ Map initializes directly at desired location (no relocation needed)
- ✅ Cleaner UX with clear mode selection
- ✅ Faster loading (no wasted map generation)
- ✅ Better separation of concerns

---

## 📦 What Was Created

### New Scripts

1. **MainMenuController.cs** - `Assets/Scripts/UI and Minimap/MainMenuController.cs`
   - Handles main menu UI
   - Username search functionality
   - Scene loading

2. **GameManager.cs** - `Assets/Scripts/GameManager.cs`
   - Singleton that persists between scenes
   - Stores selected coordinates
   - Passes data from menu to game

3. **MapInitializer.cs** - `Assets/Scripts/MapInitializer.cs`
   - Reads coordinates from GameManager
   - Initializes map at correct location
   - Auto-loads user observations if searched

---

## 🛠️ Setup Instructions

### Step 1: Create Main Menu Scene

1. **Create New Scene**:
   - File → New Scene
   - Name it `MainMenu`
   - Save in `Assets/Scenes/MainMenu.unity`

2. **Add UI Canvas**:
   - GameObject → UI → Canvas
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1920x1080

3. **Add EventSystem** (if not auto-created):
   - GameObject → UI → Event System

---

### Step 2: Create Main Menu UI

#### Panel 1: Main Menu Panel

Create a panel with three buttons:

```
Canvas
├── MainMenuPanel
│   ├── Title (Text): "Biodiversity Explorer"
│   ├── ExplorationModeButton (Button)
│   │   └── Text: "Exploration Mode"
│   ├── SearchUserModeButton (Button)
│   │   └── Text: "Search iNaturalist User"
│   ├── AboutButton (Button)
│   │   └── Text: "About"
│   └── QuitButton (Button)
│       └── Text: "Quit"
```

**Layout Example:**
```
┌─────────────────────────────────┐
│                                 │
│    Biodiversity Explorer        │
│                                 │
│   ┌─────────────────────────┐  │
│   │  Exploration Mode       │  │
│   └─────────────────────────┘  │
│                                 │
│   ┌─────────────────────────┐  │
│   │  Search iNaturalist User│  │
│   └─────────────────────────┘  │
│                                 │
│   ┌─────────────────────────┐  │
│   │  About                  │  │
│   └─────────────────────────┘  │
│                                 │
│   ┌─────────────────────────┐  │
│   │  Quit                   │  │
│   └─────────────────────────┘  │
│                                 │
└─────────────────────────────────┘
```

---

#### Panel 2: User Search Panel

Create a panel for username search:

```
Canvas
├── UserSearchPanel (initially inactive)
│   ├── Title (Text): "Search iNaturalist User"
│   ├── Instructions (Text): "Enter username (no underscores)"
│   ├── UsernameInput (InputField)
│   │   └── Placeholder: "Enter username..."
│   ├── SearchButton (Button)
│   │   └── Text: "Search & Start"
│   ├── BackButton (Button)
│   │   └── Text: "Back"
│   └── SearchStatusText (Text)
│       └── Initial text: ""
```

**Layout Example:**
```
┌─────────────────────────────────┐
│                                 │
│  Search iNaturalist User        │
│                                 │
│  Enter username (no underscores)│
│                                 │
│  ┌───────────────────────────┐ │
│  │ Enter username...         │ │
│  └───────────────────────────┘ │
│                                 │
│  ┌───────────────────────────┐ │
│  │ Search & Start            │ │
│  └───────────────────────────┘ │
│                                 │
│  ┌───────────────────────────┐ │
│  │ Back                      │ │
│  └───────────────────────────┘ │
│                                 │
│  [Status messages appear here]  │
│                                 │
└─────────────────────────────────┘
```

---

#### Panel 3: About Panel

Create an informational panel:

```
Canvas
├── AboutPanel (initially inactive)
│   ├── Title (Text): "About Biodiversity Explorer"
│   ├── Description (Text): Your project description
│   └── BackButton (Button)
│       └── Text: "Back"
```

---

### Step 3: Configure MainMenuController

1. **Add MainMenuController to Canvas**:
   - Select Canvas GameObject
   - Add Component → Main Menu Controller

2. **Assign References in Inspector**:

```
Main Menu Controller
├── UI Panels
│   ├── Main Menu Panel: [Drag MainMenuPanel]
│   ├── User Search Panel: [Drag UserSearchPanel]
│   └── About Panel: [Drag AboutPanel]
│
├── Main Menu Buttons
│   ├── Exploration Mode Button: [Drag ExplorationModeButton]
│   ├── Search User Mode Button: [Drag SearchUserModeButton]
│   ├── About Button: [Drag AboutButton]
│   └── Quit Button: [Drag QuitButton]
│
├── User Search Elements
│   ├── Username Input: [Drag UsernameInput]
│   ├── Search Button: [Drag SearchButton]
│   ├── Back Button: [Drag BackButton]
│   └── Search Status Text: [Drag SearchStatusText]
│
├── About Panel Elements
│   └── About Back Button: [Drag AboutBackButton]
│
├── Scene Settings
│   └── Game Scene Name: "MainScene" (or your scene name)
│
└── Default Location
    ├── Default Latitude: 37.7749 (San Francisco)
    └── Default Longitude: -122.4194
```

---

### Step 4: Setup Game Scene

1. **Add GameManager to Game Scene**:
   - Create Empty GameObject: "GameManager"
   - Add Component → Game Manager
   - It will persist automatically (DontDestroyOnLoad)

2. **Add MapInitializer to Map GameObject**:
   - Find your GameObject with AbstractMap component
   - Add Component → Map Initializer
   - Check "Show Debug Logs" for testing

**Inspector:**
```
Map Initializer
├── References
│   └── Map: [Auto-assigned or drag AbstractMap]
└── Debug
    └── Show Debug Logs: ✓ Checked
```

---

### Step 5: Configure Build Settings

1. **Add Scenes to Build Settings**:
   - File → Build Settings
   - Add MainMenu scene as index 0
   - Add MainScene (game) as index 1

**Build Settings:**
```
Scenes In Build:
 ✓ 0  MainMenu
 ✓ 1  MainScene
```

---

## 🎮 How It Works

### Flow Diagram

```
User Opens App
     ↓
[Main Menu Scene Loads]
     ↓
User Chooses Mode
     ↓
┌────┴────────────────────────────┐
│                                 │
Exploration Mode          Search User Mode
     ↓                            ↓
Set Default Location    Search iNaturalist API
     ↓                            ↓
Store in GameManager    Parse User's Last Observation
     ↓                            ↓
Load Game Scene         Store Coordinates in GameManager
     ↓                            ↓
     └─────────┬──────────────────┘
               ↓
    [Game Scene Loads]
               ↓
    MapInitializer.Awake()
               ↓
    Read Coordinates from GameManager
               ↓
    Update AbstractMap Options
               ↓
    Map Initializes at Correct Location
               ↓
    (If user was searched)
    Auto-load User's Observations
               ↓
    Ready to Explore!
```

---

## 🔧 Technical Details

### GameManager (Singleton Pattern)

**Purpose**: Pass data between scenes

**Key Methods**:
```csharp
// Set custom location
GameManager.Instance.SetStartLocation(lat, lng, username, species);

// Check if custom location was set
bool hasCustom = GameManager.Instance.HasCustomLocation();

// Get coordinates
double lat = GameManager.Instance.startLatitude;
double lng = GameManager.Instance.startLongitude;
```

**Persistence**:
- Uses `DontDestroyOnLoad()` to persist between scenes
- Singleton pattern ensures only one instance exists
- Automatically created if doesn't exist

---

### MapInitializer

**Purpose**: Initialize map with GameManager coordinates

**Execution Order**:
1. `Awake()` - Runs BEFORE map's Start()
2. Reads coordinates from GameManager
3. Updates `map.Options.locationOptions.latitudeLongitude`
4. Map's `Start()` initializes at new coordinates

**Auto-Loading User Observations**:
- Waits 2 seconds for map to initialize
- Checks if user was searched
- Calls `LoadObservationsWithUserPriority()`
- Shows status message

---

### MainMenuController

**Purpose**: Handle main menu UI and user search

**Key Features**:
- Button click handlers for all modes
- iNaturalist API integration
- Error handling (422, network errors, etc.)
- Scene loading with AsyncOperation
- Keyboard shortcuts (Enter, Escape)

---

## 🧪 Testing

### Test Exploration Mode:
1. Run MainMenu scene
2. Click "Exploration Mode"
3. Should load at default location (San Francisco)

### Test User Search Mode:
1. Run MainMenu scene
2. Click "Search iNaturalist User"
3. Type `kueda`
4. Press Enter or click "Search & Start"
5. Should show: "✅ Found 'kueda'! Starting at their observation..."
6. Game loads at kueda's last observation
7. Observations load with kueda's shown first

### Test Error Handling:
1. Try username with underscore: `user_name`
2. Should show: "❌ Invalid username format..."
3. Try non-existent user: `invaliduser12345`
4. Should show: "❌ No observations found..."

---

## 🎨 UI Customization

### Colors

```csharp
// Status message colors
Yellow (Warning):    new Color(1f, 0.7f, 0f)
Orange (Searching):  new Color(1f, 0.8f, 0.2f)
Green (Success):     new Color(0.2f, 0.8f, 0.2f)
Red (Error):         Color.red
Light Blue (Loading): new Color(0.5f, 0.7f, 1f)
Orange-Red (Not Found): new Color(1f, 0.4f, 0f)
```

### Button Styling

Recommended button settings:
- Font: Bold
- Font Size: 24-32
- Color: White text on colored background
- Transition: Color Tint
- Highlight: Slightly brighter
- Press: Slightly darker

---

## 📋 Checklist

Setup checklist:

- [ ] Created MainMenu scene
- [ ] Added Canvas and EventSystem
- [ ] Created all UI panels and buttons
- [ ] Added MainMenuController script to Canvas
- [ ] Assigned all UI references in Inspector
- [ ] Set correct game scene name
- [ ] Added GameManager to game scene
- [ ] Added MapInitializer to map GameObject
- [ ] Added both scenes to Build Settings
- [ ] MainMenu is index 0, Game is index 1
- [ ] Tested exploration mode
- [ ] Tested user search mode with valid username
- [ ] Tested error handling with invalid username
- [ ] Verified map initializes at correct location

---

## 🐛 Troubleshooting

### Issue: Map Still Loads at Default Location

**Check:**
1. Is MapInitializer attached to map GameObject?
2. Is GameManager in the game scene?
3. Check console: "Map center updated via Options"
4. Verify scene load order (MainMenu → Game)

**Debug:**
```csharp
// In MapInitializer.Awake(), check:
Debug.Log($"HasCustomLocation: {GameManager.Instance.HasCustomLocation()}");
Debug.Log($"Coordinates: {GameManager.Instance.startLatitude}, {GameManager.Instance.startLongitude}");
```

---

### Issue: User Search Returns Error

**Check:**
1. Username format (no underscores!)
2. Network connection
3. Console logs for API URL
4. Test URL in browser: `https://api.inaturalist.org/v1/observations?user_login=kueda&per_page=1`

---

### Issue: Observations Don't Load

**Check:**
1. Is INaturalistMapController in scene?
2. Check MapInitializer logs: "Loading observations for user..."
3. Verify `LoadObservationsWithUserPriority()` exists in INaturalistMapController
4. Check radius (default 5km)

---

### Issue: Scene Won't Load

**Check:**
1. Scene name matches exactly in MainMenuController
2. Both scenes in Build Settings
3. Scene index order is correct
4. No compilation errors

---

## 💡 Advanced Customization

### Change Default Location

Edit GameManager or MainMenuController:
```csharp
public double defaultLatitude = 51.5074;  // London
public double defaultLongitude = -0.1278;
```

### Add More Modes

Create new button and handler in MainMenuController:
```csharp
public Button customLocationButton;

private void OnCustomLocationPressed()
{
    // Show lat/lng input panel
    // Get coordinates from user
    // Call GameManager.Instance.SetStartLocation(lat, lng)
    // Load game scene
}
```

### Custom Loading Screen

Add between menu and game:
```csharp
private IEnumerator LoadGameScene()
{
    // Load loading scene first
    SceneManager.LoadScene("LoadingScene");
    yield return new WaitForSeconds(0.5f);

    // Then load game scene
    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
    // ...
}
```

---

## 🚀 Benefits of This Approach

### vs. Previous In-Game Search:

| Feature | Previous (In-Game) | New (Main Menu) |
|---------|-------------------|-----------------|
| **Map Generation** | Generates twice (default + new location) | ✅ Generates once at correct location |
| **Loading Time** | Slower (wasted generation) | ✅ Faster (direct initialization) |
| **User Experience** | Confusing (map relocates) | ✅ Clear (choose before loading) |
| **Code Complexity** | High (map relocation logic) | ✅ Lower (simple initialization) |
| **Flexibility** | Limited to in-game search | ✅ Can add more modes easily |

---

## 📚 Related Files

| File | Purpose |
|------|---------|
| `MainMenuController.cs` | Main menu UI and logic |
| `GameManager.cs` | Cross-scene data storage |
| `MapInitializer.cs` | Map initialization with custom coordinates |
| `BiodiversityUI.cs` | In-game UI (can keep or remove search feature) |
| `INaturalistMapController.cs` | Observation loading |

---

## 🎯 Next Steps

After setup:

1. **Test thoroughly** with different usernames
2. **Customize UI** to match your aesthetic
3. **Add About content** with project info
4. **Consider adding**:
   - Search history
   - Favorite locations
   - Location presets
   - Tutorial/Help button
5. **Remove old search feature** from BiodiversityUI if desired

---

## ✅ Success Criteria

Your main menu is working when:
- [ ] Can launch app and see main menu
- [ ] Exploration mode loads at default location
- [ ] User search finds valid users
- [ ] Error messages show for invalid input
- [ ] Map initializes at searched user's location
- [ ] User's observations load first
- [ ] No double-loading or relocation
- [ ] Clean scene transitions
- [ ] All buttons work
- [ ] Keyboard shortcuts work (Enter, Escape)

---

**Congratulations!** You now have a professional main menu system with seamless map initialization! 🎉
