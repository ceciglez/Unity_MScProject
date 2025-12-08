# Unity UI Setup Guide

## Step 1: Create Main Menu Canvas

### Create Canvas Structure:
1. **Right-click in Hierarchy** → UI → Canvas
2. **Name it**: "MainMenuCanvas"
3. **Canvas Component Settings**:
   - Render Mode: Screen Space - Overlay
   - Pixel Perfect: ✓ (checked)
   - Sort Order: 10

### Create Main Menu Panel:
1. **Right-click MainMenuCanvas** → UI → Panel
2. **Name it**: "MainMenuPanel"
3. **RectTransform**: Stretch to full screen (anchor preset: stretch-stretch)
4. **Image Component**: Color = black with alpha 0.8

### Create Main Menu Buttons:
**For each button (Start, About, Credits, Controls, Exit):**

1. **Right-click MainMenuPanel** → UI → Button
2. **Names**: "StartButton", "AboutButton", etc.
3. **Layout**: 
   - Width: 200, Height: 50
   - Position them vertically (Y positions: 100, 50, 0, -50, -100)

**Button Text Setup:**
- Font Size: 18
- Color: White
- Text: "Start Game", "About", "Credits", "Controls", "Exit"

---

## Step 2: Create Sub-Panels

### About Panel:
1. **Right-click MainMenuCanvas** → UI → Panel  
2. **Name**: "AboutPanel"
3. **RectTransform**: Full screen
4. **Add ScrollRect** component for scrolling
5. **Add Text child** with about content
6. **Add "Back" button** at bottom

### Credits Panel:
- Same setup as About Panel
- Different text content

### Controls Panel:
- Same setup as About Panel  
- Will show controls information

---

## Step 3: Create In-Game UI Canvas

### Create In-Game Canvas:
1. **Create new Canvas**: "InGameUICanvas"
2. **Sort Order**: 5 (below main menu)

### Controls Help Panel:
1. **Right-click InGameUICanvas** → UI → Panel
2. **Name**: "ControlsHelpPanel"
3. **Position**: Top-left corner
4. **Size**: 400x300
5. **Add Text child** for controls info

### Pause Menu Panel:
1. **Right-click InGameUICanvas** → UI → Panel
2. **Name**: "PauseMenuPanel"  
3. **RectTransform**: Center screen
4. **Add buttons**: Resume, Main Menu, Exit

### Game Status UI:
1. **Create Text elements** for:
   - Instructions (bottom of screen)
   - Network status (top-right)
   - Controls hint (bottom-left)

---

## Step 4: Connect Scripts to UI

### Main Menu Manager:
1. **Create empty GameObject**: "MainMenuManager"
2. **Add MainMenuManager script**
3. **Assign all UI references** in inspector:
   - Drag panels to Panel slots
   - Drag buttons to Button slots

### In-Game UI Controller:
1. **Create empty GameObject**: "InGameUIController"
2. **Add InGameUIController script**
3. **Assign UI references**

---

## Step 5: Configure Game Components

### Disable Game Components Initially:
Select these and **uncheck "enabled"**:
- Player Controller
- Camera Controller  
- Network Manager
- Map Controller

### Setup Layer Order:
- Main Menu Canvas: Sort Order 10
- In-Game UI Canvas: Sort Order 5
- Observation Canvases: Sort Order 1

---

## Step 6: Test the UI Flow

### Testing Checklist:
- ✓ Main menu appears on start
- ✓ All menu buttons work
- ✓ About/Credits/Controls panels show and hide
- ✓ Start button enables game and hides menu
- ✓ ESC key shows pause menu
- ✓ H key toggles controls help
- ✓ Return to main menu works
- ✓ Exit buttons work

---

## Step 7: Enhanced Observation Display

### The ObservationDisplay now shows:
- Species common name
- Scientific name (italics)
- Observer name and date (green text)

### This automatically appears when you:
- Walk near observations
- Data loads from iNaturalist API

---

## Step 8: Final Polish

### Audio (Optional):
1. Add AudioSource to MainMenuManager
2. Assign button click sound
3. Set volume appropriately

### Styling:
- Customize colors, fonts, sizes
- Add background images if desired
- Adjust transparency and layout

---

## Quick Setup Summary:

1. **Create 2 Canvas objects** (MainMenu, InGame)
2. **Add 5 panels** (MainMenu, About, Credits, Controls, PauseMenu)  
3. **Add 8+ buttons** with proper text
4. **Create 2 manager GameObjects** with scripts
5. **Drag and assign** all UI references in inspectors
6. **Disable game components** initially
7. **Test the complete UI flow**

The scripts handle all the logic - you just need to create the UI structure and connect the references!