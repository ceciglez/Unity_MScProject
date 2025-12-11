# Button Click & Cursor Recentering Fix

## 🐛 Problems

1. **Buttons have tiny click areas** - Can't click buttons easily
2. **Cursor recenters to screen center** when clicking anything

---

## 🔍 Root Causes

### Problem 1: Transparent Button Background = No Clicks

**Issue**: When you set a Button's Image component alpha to 0 (fully transparent), Unity's **GraphicRaycaster** doesn't detect clicks.

**Why**: Unity's UI raycast system only hits graphics with `Raycast Target = true` **AND** visible pixels. Even with `Raycast Target` checked, if alpha = 0, there are no "visible pixels" to hit.

**Solution**: Use alpha of 1 (or very low like 0.01) and make the color transparent instead.

### Problem 2: Cursor Recentering

**Issue**: The cursor jumps to screen center when clicking buttons.

**Causes**:
1. Player controller or camera scripts still running
2. InGameUIController might be interfering
3. Kinematic Character Controller (KCC) recalculating look direction

---

## ✅ Solution 1: Fix Button Click Areas

### Option A: Semi-Transparent Background (Recommended)

1. Select your button in Unity
2. In Inspector → Button → Target Graphic (Image component)
3. Set Color:
   ```
   R: 255
   G: 255
   B: 255
   A: 1-10 (almost invisible but clickable)
   ```
4. Ensure "Raycast Target" is **checked** ✓

### Option B: Visible Background

If you want visible buttons:
```
Color: Any color you want
A: 50-255 (semi-transparent to opaque)
Raycast Target: ✓ Checked
```

### Option C: Add Invisible Raycast Panel

If you must keep alpha = 0:
1. Add a child Image to the button
2. Make it cover the button fully (anchor: stretch)
3. Set its color to white with alpha = 1
4. Check "Raycast Target" on THIS image
5. The button's actual image can stay alpha = 0

---

## ✅ Solution 2: Fix Cursor Recentering

The issue is that even though we disable player/camera scripts, something is still recentering the cursor. Let me update the MainMenuOverlay to also disable InGameUIController and add better script detection.

### Updated DisablePlayerMovement Method

I'll update the script to:
1. Disable **ALL** MonoBehaviour scripts in the entire scene (except MainMenuOverlay and Unity's built-in components)
2. This includes InGameUIController
3. Store all disabled scripts to re-enable them later

---

## 🛠️ Implementation

### Step 1: Fix Button Click Areas (Manual - Unity Editor)

For each button (Exploration Mode, Search User, About, etc.):

1. **Select button in Hierarchy**
2. **Inspector → Button component → Target Graphic**
3. **Find the Image component** (should be highlighted)
4. **Set Color**:
   - Option 1 (Invisible): `RGBA(255, 255, 255, 1)` - almost invisible
   - Option 2 (Visible): `RGBA(100, 100, 100, 100)` - semi-transparent gray
5. **Verify "Raycast Target" is checked** ✓

### Step 2: Fix Cursor Recentering (Automatic - Code)

I'll update `MainMenuOverlay.cs` to disable ALL scripts globally during menu.

---

## 🧪 Testing After Fixes

### Test Button Clicks:
- [ ] Can click "Exploration Mode" button easily
- [ ] Can click "Search User" button easily
- [ ] Can click "About" button easily
- [ ] Can click "Back" buttons easily
- [ ] Can click "Search" button easily
- [ ] Input field is clickable and typeable

### Test Cursor:
- [ ] Cursor doesn't recenter when clicking buttons
- [ ] Cursor doesn't recenter when clicking input field
- [ ] Cursor stays where you move it
- [ ] Cursor remains visible during entire menu interaction

---

## 📖 Understanding Unity UI Raycasting

### How Button Clicks Work:

```
1. User clicks mouse
   ↓
2. Unity shoots a ray from mouse position
   ↓
3. GraphicRaycaster checks all UI elements with "Raycast Target"
   ↓
4. For each element, checks if ray hits VISIBLE pixels
   ↓
5. If alpha = 0 → NO visible pixels → NO HIT ❌
6. If alpha > 0 → Has visible pixels → HIT ✓
   ↓
7. Triggers button's OnClick event
```

### Why Alpha Matters:

```csharp
// Unity's internal check (simplified):
if (image.raycastTarget && image.color.a > 0 && hitTestResult)
{
    return true; // Can click
}
else
{
    return false; // Cannot click
}
```

### Maskable vs Raycast Target:

| Property | Purpose | Affects Clicks? |
|----------|---------|-----------------|
| **Raycast Target** | Should this graphic block raycasts? | ✅ YES |
| **Maskable** | Should this graphic be clipped by masks? | ❌ NO |

- **Raycast Target**: Controls whether UI can be clicked
- **Maskable**: Controls whether UI is hidden by Mask components (like ScrollRect)

---

## 🎯 Quick Reference

### Button Click Requirements:

1. ✅ Button component attached
2. ✅ Image component (Target Graphic)
3. ✅ Image color alpha > 0 (even 0.01 works)
4. ✅ Image "Raycast Target" = checked
5. ✅ No other UI element blocking it
6. ✅ EventSystem in scene
7. ✅ GraphicRaycaster on Canvas

### Cursor Staying Visible Requirements:

1. ✅ `Cursor.lockState = CursorLockMode.None` in Update()
2. ✅ `Cursor.visible = true` in Update()
3. ✅ ALL player/camera scripts disabled
4. ✅ No other scripts recentering cursor

---

## 🐛 Common Issues

### Issue: Buttons Still Not Clickable After Setting Alpha > 0

**Check**:
1. **EventSystem exists**: Hierarchy → EventSystem (created automatically with Canvas)
2. **GraphicRaycaster on Canvas**: Canvas → GraphicRaycaster component
3. **No blocking UI**: Another UI element isn't on top
4. **Button is active**: GameObject and component are both enabled

**Debug**:
```csharp
// Add to button's OnClick:
Debug.Log("Button clicked!");

// If this doesn't appear, button isn't receiving clicks
```

---

### Issue: Input Field Not Clickable

**Same solution**: Input field's background Image must have alpha > 0.

1. Select InputField
2. Find background Image child
3. Set color alpha > 0
4. Check "Raycast Target"

---

### Issue: Cursor Still Recenters

**If cursor still recenters after the code fix**:

1. **Check console** for which scripts are being disabled:
   ```
   [MainMenuOverlay] Disabled: PlayerMovement
   [MainMenuOverlay] Disabled: CameraController
   [MainMenuOverlay] Disabled: ...
   ```

2. **Look for scripts NOT being disabled** that might recenter cursor

3. **Check Kinematic Character Controller**:
   - KCC might have a setting to recenter look
   - Disable KCC entirely during menu

---

## 💡 Advanced: Custom Transparent Clickable Button

If you want a completely invisible button that's still clickable:

```
Button Structure:
├── Button (GameObject)
│   ├── Button Component
│   ├── Image Component (alpha = 1, color = transparent white)
│   └── Text Component (visible text)
```

Settings:
```
Image:
- Color: RGBA(255, 255, 255, 1)  // Almost invisible but clickable
- Raycast Target: ✓ Checked

Button:
- Target Graphic: The Image above
- Interactable: ✓ Checked
```

This gives you an invisible but fully clickable button!

---

## ✅ Summary

**Button Click Fix**:
- ✅ Set button Image alpha > 0 (even 0.01 works)
- ✅ Ensure "Raycast Target" is checked
- ✅ This creates clickable pixels for Unity's raycaster

**Cursor Recenter Fix**:
- ✅ Update MainMenuOverlay to disable ALL scene scripts
- ✅ Store and re-enable them after menu closes
- ✅ Force cursor unlocked in Update() loop

**Expected Result**:
- ✅ Buttons have full click area (entire button is clickable)
- ✅ Cursor stays where you move it (doesn't recenter)
- ✅ Input fields are clickable and typeable
- ✅ Smooth menu interaction

---

**Now let's implement the code fix!**
