# Cursor Handling - Menu Integration

## 🖱️ Problem Solved

**Issue**: Player controller was locking/hiding the cursor when clicking on the menu, making it impossible to interact with UI.

**Solution**: MainMenuOverlay now forcibly keeps the cursor visible and unlocked during the menu, and only allows it to be locked after the game starts!

---

## ✨ How It Works

### During Menu:
```
Every Frame (Update())
    ↓
Check: hasSelectedMode == false?
    ↓
YES → Force cursor visible and unlocked
    ↓
Cursor.lockState = CursorLockMode.None
Cursor.visible = true
    ↓
Menu is ALWAYS clickable ✓
```

### After Game Starts:
```
User selects mode
    ↓
Game initializes
    ↓
Check: lockCursorAfterStart setting
    ↓
YES → Lock cursor (first-person mode)
NO → Keep cursor visible
```

---

## 🛠️ Configuration

In the Unity Inspector, MainMenuOverlay has a new setting:

```
Main Menu Overlay (Script)
├── Settings
│   ├── Hide After Selection: ✓
│   ├── Block Player Movement: ✓
│   └── Lock Cursor After Start: ✓ ← NEW!
```

### Lock Cursor After Start:

**✓ Checked (Default):**
- Cursor locks after game starts
- Cursor becomes invisible
- First-person camera control mode
- **Use for**: First-person games, flight sims, FPS

**✗ Unchecked:**
- Cursor stays visible after game starts
- Cursor remains unlocked
- Can still click UI during gameplay
- **Use for**: Strategy games, RTS, UI-heavy games

---

## 🎮 Behavior Details

### During Menu (Always):
```csharp
if (!hasSelectedMode)
{
    Cursor.lockState = CursorLockMode.None;  // Unlocked
    Cursor.visible = true;                    // Visible
}
```

**Result:**
- ✅ Can click all buttons
- ✅ Can type in input fields
- ✅ Cursor moves freely
- ✅ Menu is fully interactive

**Even if player controller tries to lock cursor:**
- Menu overrides it every frame
- Cursor stays visible
- Player controller can't interfere

---

### After Game Starts:

#### Option 1: Lock Cursor (Default)
```csharp
if (lockCursorAfterStart)
{
    Cursor.lockState = CursorLockMode.Locked;  // Locked
    Cursor.visible = false;                     // Hidden
}
```

**Result:**
- ✅ Cursor disappears
- ✅ Mouse look controls camera
- ✅ First-person gameplay
- ✅ Standard FPS controls

#### Option 2: Keep Cursor Visible
```csharp
else
{
    Cursor.lockState = CursorLockMode.None;  // Unlocked
    Cursor.visible = true;                    // Visible
}
```

**Result:**
- ✅ Cursor stays visible
- ✅ Can still click UI
- ✅ Mixed gameplay + UI interaction
- ✅ Good for strategy/sim games

---

## 🧪 Testing

### Test Menu Cursor:
1. Press Play
2. Menu appears
3. Move mouse → **Cursor visible** ✓
4. Click buttons → **Buttons respond** ✓
5. Click input field → **Can type** ✓
6. Click anywhere → **Cursor doesn't disappear** ✓

### Test Game Cursor (Default):
1. Select "Exploration Mode"
2. Game starts
3. Cursor should **lock and hide** ✓
4. Move mouse → **Camera rotates** ✓

### Test Game Cursor (Unlocked):
1. Uncheck "Lock Cursor After Start" in Inspector
2. Select "Exploration Mode"
3. Game starts
4. Cursor should **stay visible** ✓
5. Can still click UI elements ✓

---

## 🐛 Common Issues

### Issue: Cursor Recenters to Screen Center When Clicking

**Cause**: Player/Camera controller is recentering the cursor

**Solution**: Already handled! The script now disables:
- ALL player scripts
- ALL camera scripts
- This prevents cursor recentering

**Verify:**
```
Console should show:
[MainMenuOverlay] Player movement and camera control disabled
[MainMenuOverlay] Camera scripts also disabled
```

---

### Issue: Cursor Still Disappears on Menu

**Possible Cause**: Player controller is very aggressive about locking cursor

**Solutions:**

**1. Check Update() is Running:**
- MainMenuOverlay Update() should run every frame
- Look for console errors

**2. Disable Player Controller During Menu:**
- MainMenuOverlay already does this with `DisablePlayerMovement()`
- Verify `blockPlayerMovement` is checked

**3. Check Execution Order:**
- Edit → Project Settings → Script Execution Order
- Set MainMenuOverlay to run AFTER player controller

---

### Issue: Can't Look Around After Game Starts

**Cause**: Cursor not locking after game starts

**Solutions:**

**1. Check Setting:**
- Inspector → MainMenuOverlay → Lock Cursor After Start
- Should be **checked** for first-person gameplay

**2. Check Console:**
- Should see: `[MainMenuOverlay] Cursor locked for gameplay`
- If you see "remains unlocked", setting is off

**3. Manually Lock:**
- Press Escape during gameplay
- Many player controllers unlock on Escape
- Click screen to re-lock

---

### Issue: UI Still Accessible During Gameplay

**This is actually by design if you want it!**

**To Hide UI After Start:**
- Check "Hide After Selection" in MainMenuOverlay settings
- UI will disappear after game starts
- Can still press B to toggle back (BiodiversityUI)

---

## 💡 Advanced: Custom Cursor Control

If you want more control over cursor behavior:

### Option 1: Unlock with Key During Gameplay

Add to MainMenuOverlay Update():
```csharp
// Press Tab to unlock cursor during gameplay
if (Input.GetKeyDown(KeyCode.Tab) && hasSelectedMode)
{
    if (Cursor.lockState == CursorLockMode.Locked)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    else
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
```

### Option 2: Conditional Locking

Only lock if player controller wants to:
```csharp
// After game starts, check if player controller needs locked cursor
var playerController = player.GetComponent<YourPlayerController>();
if (playerController != null && playerController.needsCursorLocked)
{
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}
```

---

## 🎯 Best Practices

### For First-Person Games:
```
Settings:
✓ Block Player Movement: true
✓ Lock Cursor After Start: true
✓ Hide After Selection: true
```

**Result:**
- Menu: Cursor visible, player can't move
- Game: Cursor locked, full first-person control

---

### For Strategy/Top-Down Games:
```
Settings:
✓ Block Player Movement: true
✗ Lock Cursor After Start: false
✗ Hide After Selection: false
```

**Result:**
- Menu: Cursor visible
- Game: Cursor visible, can click UI anytime

---

### For Mixed Gameplay:
```
Settings:
✓ Block Player Movement: true
✓ Lock Cursor After Start: true
✗ Hide After Selection: false
```

**Result:**
- Menu: Cursor visible
- Game: Cursor locked, but UI still visible
- Press B to show/hide UI and unlock cursor

---

## 📊 Cursor State Timeline

```
Scene Loads
    ↓
Menu Appears
    ↓
[Every Frame: Cursor forced visible/unlocked]
    ↓
User clicks button/types
    ↓
[Every Frame: Still forced visible/unlocked]
    ↓
User selects mode
    ↓
hasSelectedMode = true
    ↓
Update() stops forcing cursor visibility
    ↓
Game initialization
    ↓
Check lockCursorAfterStart
    ↓
Lock cursor (or not)
    ↓
Gameplay begins
    ↓
Player controller can now control cursor
```

---

## 🔧 Debugging

### Check Current Cursor State:
```csharp
Debug.Log($"Cursor Lock: {Cursor.lockState}");
Debug.Log($"Cursor Visible: {Cursor.visible}");
Debug.Log($"Has Selected Mode: {hasSelectedMode}");
```

### Expected Console Messages:

**On Menu:**
```
[MainMenuOverlay] Overlay initialized - waiting for user selection
// Every frame: cursor forced visible (no message spam)
```

**On Game Start (Locked):**
```
[MainMenuOverlay] Cursor locked for gameplay
[MainMenuOverlay] Game started!
```

**On Game Start (Unlocked):**
```
[MainMenuOverlay] Cursor remains unlocked
[MainMenuOverlay] Game started!
```

---

## ✅ Summary

**What Changed:**
- ✅ Update() now forces cursor visible during menu (every frame)
- ✅ New `lockCursorAfterStart` setting for cursor control after game
- ✅ Automatic cursor state management
- ✅ Player controller can't interfere during menu

**What You Get:**
- ✅ Menu is always clickable
- ✅ Cursor never disappears during menu
- ✅ Configurable cursor behavior after game starts
- ✅ Works with any player controller

**Setup Required:**
- Nothing! Works automatically
- Optionally configure "Lock Cursor After Start" setting

---

## 📖 Related Settings

| Setting | Default | Purpose |
|---------|---------|---------|
| **Block Player Movement** | ✓ True | Disables player scripts during menu |
| **Lock Cursor After Start** | ✓ True | Locks cursor for first-person gameplay |
| **Hide After Selection** | ✓ True | Hides menu after game starts |

---

**Your menu is now fully interactive!** The cursor will stay visible and clickable no matter what the player controller tries to do! 🖱️✨
