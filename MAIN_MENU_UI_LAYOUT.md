# Main Menu UI Layout Guide

## 🎨 Visual Layout Reference

This guide shows exactly how to layout your main menu UI in Unity.

---

## Main Menu Panel Layout

```
┌─────────────────────────────────────────────────────────────┐
│                                                               │
│                                                               │
│                   BIODIVERSITY EXPLORER                       │
│                                                               │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │   Exploration Mode    │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │ Search iNaturalist    │                   │
│                  │        User           │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │        About          │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │         Quit          │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Unity Hierarchy:
```
Canvas
├── MainMenuPanel
│   ├── TitleText
│   ├── ExplorationModeButton
│   │   └── Text
│   ├── SearchUserModeButton
│   │   └── Text
│   ├── AboutButton
│   │   └── Text
│   └── QuitButton
│       └── Text
```

### Recommended Settings:

**TitleText:**
- Font Size: 48-64
- Alignment: Center
- Position: Top center
- Color: White or your accent color
- Font: Bold

**Buttons:**
- Width: 300-400px
- Height: 60-80px
- Font Size: 24-28
- Spacing: 20px between buttons
- Position: Centered vertically and horizontally

---

## User Search Panel Layout

```
┌─────────────────────────────────────────────────────────────┐
│                                                               │
│                  Search iNaturalist User                      │
│                                                               │
│            Enter a username (no underscores)                  │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │  Enter username...    │ ← Input Field     │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │   Search & Start      │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │         Back          │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
│                                                               │
│   [Status messages appear here in colored text]              │
│                                                               │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Unity Hierarchy:
```
Canvas
├── UserSearchPanel (Active: false)
│   ├── TitleText
│   ├── InstructionsText
│   ├── UsernameInput (InputField)
│   │   ├── Placeholder
│   │   └── Text
│   ├── SearchButton
│   │   └── Text
│   ├── BackButton
│   │   └── Text
│   └── SearchStatusText
```

### Recommended Settings:

**InputField:**
- Width: 400-500px
- Height: 50-60px
- Font Size: 20-24
- Placeholder: "Enter username..."
- Text Color: Black on white background

**SearchStatusText:**
- Font Size: 18-22
- Alignment: Center
- Position: Bottom third of screen
- Initial text: "" (empty)
- Will show colored messages dynamically

---

## About Panel Layout

```
┌─────────────────────────────────────────────────────────────┐
│                                                               │
│              About Biodiversity Explorer                      │
│                                                               │
│                                                               │
│   This project explores biodiversity data from                │
│   iNaturalist, allowing users to visualize species           │
│   observations in a 3D environment.                           │
│                                                               │
│   Features:                                                   │
│   • Real-time biodiversity metrics                            │
│   • iNaturalist API integration                               │
│   • 3D terrain visualization                                  │
│   • User observation search                                   │
│                                                               │
│   Developed by: [Your Name]                                   │
│   University: [Your University]                               │
│   Year: 2024-2025                                             │
│                                                               │
│                                                               │
│                  ┌───────────────────────┐                   │
│                  │                       │                   │
│                  │         Back          │                   │
│                  │                       │                   │
│                  └───────────────────────┘                   │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### Unity Hierarchy:
```
Canvas
├── AboutPanel (Active: false)
│   ├── TitleText
│   ├── DescriptionText (larger, multiline)
│   └── AboutBackButton
│       └── Text
```

---

## 🎨 Color Schemes

### Option 1: Nature/Green Theme
```
Background: Dark Green (#1a3a1a)
Buttons: Medium Green (#2d5a2d)
Button Hover: Bright Green (#3d7a3d)
Text: White (#ffffff)
Accent: Light Green (#7fbf7f)
```

### Option 2: Modern/Blue Theme
```
Background: Dark Blue (#1a2332)
Buttons: Medium Blue (#2d4a6a)
Button Hover: Bright Blue (#3d6a9a)
Text: White (#ffffff)
Accent: Light Blue (#7fbfff)
```

### Option 3: Minimalist/Neutral
```
Background: Dark Gray (#2a2a2a)
Buttons: Medium Gray (#4a4a4a)
Button Hover: Light Gray (#6a6a6a)
Text: White (#ffffff)
Accent: Orange (#ff8833)
```

---

## 📐 Detailed Measurements

### Canvas Setup:
```
Canvas Scaler:
- UI Scale Mode: Scale With Screen Size
- Reference Resolution: 1920 x 1080
- Screen Match Mode: Match Width Or Height
- Match: 0.5
```

### Main Menu Panel:
```
Panel:
- Anchor: Stretch (full screen)
- Offset: 0, 0, 0, 0
- Background: Semi-transparent or solid

Title:
- Position Y: 300
- Width: 800
- Height: 100
- Font Size: 60

Buttons:
- Width: 400
- Height: 70
- Spacing: 90 (center to center)
- Start Position Y: 100
- Font Size: 28
```

### User Search Panel:
```
Panel:
- Anchor: Stretch (full screen)
- Offset: 0, 0, 0, 0

Title:
- Position Y: 300
- Font Size: 48

Instructions:
- Position Y: 230
- Font Size: 22

Input Field:
- Position Y: 100
- Width: 500
- Height: 60

Search Button:
- Position Y: 0
- Width: 400
- Height: 70

Back Button:
- Position Y: -100
- Width: 300
- Height: 60

Status Text:
- Position Y: -220
- Width: 800
- Height: 60
```

---

## 🖼️ Unity Setup Step-by-Step

### Creating Main Menu Panel:

1. **Create Panel:**
   ```
   Right-click Canvas → UI → Panel
   Name: "MainMenuPanel"
   ```

2. **Add Title:**
   ```
   Right-click MainMenuPanel → UI → Text
   Name: "TitleText"
   Text: "BIODIVERSITY EXPLORER"
   Anchor: Top Center
   Position: (0, -100, 0)
   Width: 800, Height: 100
   Font Size: 60
   Alignment: Center
   Color: White
   ```

3. **Add Buttons:**
   ```
   Right-click MainMenuPanel → UI → Button

   Button 1:
   Name: "ExplorationModeButton"
   Position: (0, 50, 0)
   Width: 400, Height: 70
   Text: "Exploration Mode"

   Button 2:
   Name: "SearchUserModeButton"
   Position: (0, -50, 0)
   Width: 400, Height: 70
   Text: "Search iNaturalist User"

   Button 3:
   Name: "AboutButton"
   Position: (0, -150, 0)
   Width: 400, Height: 70
   Text: "About"

   Button 4:
   Name: "QuitButton"
   Position: (0, -250, 0)
   Width: 400, Height: 70
   Text: "Quit"
   ```

### Creating User Search Panel:

1. **Create Panel:**
   ```
   Right-click Canvas → UI → Panel
   Name: "UserSearchPanel"
   Active: Unchecked ✗
   ```

2. **Add InputField:**
   ```
   Right-click UserSearchPanel → UI → Input Field
   Name: "UsernameInput"
   Position: (0, 100, 0)
   Width: 500, Height: 60
   Placeholder Text: "Enter username..."
   ```

3. **Add Buttons and Status Text:**
   ```
   Similar to Main Menu buttons
   Add SearchStatusText at bottom
   ```

---

## 💡 Tips

### Alignment:
- Use Anchors for responsive design
- Center elements for clean look
- Use Vertical Layout Group for automatic spacing

### Visual Polish:
- Add subtle shadows to text
- Use rounded corners on buttons (import sprites)
- Add hover effects with Button Transition: Color Tint
- Consider adding a background image

### Testing Different Resolutions:
- Use Game view aspect ratio dropdown
- Test: 16:9, 16:10, 4:3
- Ensure all text is readable
- Check button positions don't overlap

---

## 🎯 Quick Unity Setup

**Fastest way to create the UI:**

1. Create Canvas
2. Import this script to auto-generate:

```csharp
// MenuUIGenerator.cs - Run once to create UI
[MenuItem("Tools/Generate Main Menu UI")]
static void GenerateUI()
{
    // Auto-create all panels and buttons
    // (Full implementation available on request)
}
```

Or manually follow the layouts above!

---

## ✅ Checklist

Layout checklist:
- [ ] Canvas set to Scale With Screen Size
- [ ] MainMenuPanel created with 4 buttons
- [ ] UserSearchPanel created (inactive)
- [ ] AboutPanel created (inactive)
- [ ] All elements centered properly
- [ ] Font sizes readable
- [ ] Colors match theme
- [ ] Tested at different resolutions
- [ ] All UI elements named correctly
- [ ] Ready to assign to MainMenuController!

---

**Your UI is ready for scripting!** Assign all elements to MainMenuController in the Inspector.
