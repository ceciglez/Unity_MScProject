# Debug Guide: Username Search Feature

## 🔍 Quick Debug Steps

### Step 1: Press Play and Check Console

When you press Play, look for these messages in Console:

```
[BiodiversityUI] === AUTO-FIND UI ELEMENTS ===
[BiodiversityUI] Searching for 'UsernameSearchInput': FOUND
[BiodiversityUI] ✓ InputField component found!
[BiodiversityUI] Searching for 'SearchUserButton': FOUND
[BiodiversityUI] ✓ Button component found!
[BiodiversityUI] Searching for 'SearchStatusText': FOUND
[BiodiversityUI] ✓ Text component found!
[BiodiversityUI] === AUTO-FIND COMPLETE ===
```

**✅ If you see this:** UI elements are found correctly!

**❌ If you see "NOT FOUND":** UI elements are missing or named incorrectly.

---

### Step 2: Press D Key (Debug Status)

In Play mode, press **D** to get a full status report:

```
========================================
[BiodiversityUI] === UI STATUS DEBUG ===
========================================
✓ usernameSearchInput: ASSIGNED
  - Name: UsernameSearchInput
  - GameObject path: Canvas/BiodiversityPanel/UsernameSearchInput
  - Active in hierarchy: True
  - Component enabled: True
  - Interactable: True
  - Current text: ''
✓ searchUserButton: ASSIGNED
✓ searchStatusText: ASSIGNED
✓ EventSystem: FOUND
✓ uiPanel: ASSIGNED
========================================
```

This shows:
1. ✓ = Working correctly
2. ✗ = Problem found
3. ⚠ = Warning (might be ok)

---

### Step 3: Press U Key (Activate Search)

Press **U** to activate the search input. You should see:

```
[BiodiversityUI] U key pressed! Attempting to activate search input...
[BiodiversityUI] === ACTIVATE SEARCH INPUT DEBUG ===
[BiodiversityUI] ✓ usernameSearchInput found: UsernameSearchInput
[BiodiversityUI] - GameObject active: True
[BiodiversityUI] - Component enabled: True
[BiodiversityUI] - Interactable: True
[BiodiversityUI] ✓ EventSystem found: EventSystem
[BiodiversityUI] Calling ActivateInputField()...
[BiodiversityUI] Calling Select()...
[BiodiversityUI] ✓ SUCCESS! Input field is now selected and active!
[BiodiversityUI] === ACTIVATION DEBUG COMPLETE ===
```

**✅ If you see "SUCCESS":** The input field is now active and ready for typing!

**❌ If you see errors:** See troubleshooting below.

---

## 🐛 Common Issues & Solutions

### Issue 1: "UsernameSearchInput GameObject not found"

**Cause:** UI element doesn't exist or is named wrong

**Solution:**
1. Make sure you created the InputField
2. Name it **exactly** `UsernameSearchInput` (case-sensitive!)
3. It must be active in hierarchy (not disabled)

**Fix in Unity:**
- Hierarchy → Right-click → UI → Input Field
- Rename to `UsernameSearchInput`
- Check it's enabled (checkbox in Inspector)

---

### Issue 2: "GameObject found but NO InputField component"

**Cause:** You created a Text instead of InputField, or component was removed

**Solution:**
1. Delete the GameObject
2. Create new: Hierarchy → UI → **Input Field** (not Text!)
3. Rename to `UsernameSearchInput`

---

### Issue 3: "NO EventSystem found!"

**Cause:** EventSystem is missing from scene

**Solution:**
- GameObject → UI → Event System
- Only need ONE EventSystem per scene

**Check:**
```
Hierarchy:
├── Canvas
├── EventSystem ← Must exist!
└── Other objects...
```

---

### Issue 4: U Key Pressed But Nothing Happens

**Symptoms:**
- No console messages when pressing U
- Debug shows input field is null

**Possible Causes:**

1. **BiodiversityUI script not active**
   - Select GameObject with BiodiversityUI
   - Check script component is enabled (checkbox in Inspector)

2. **Input consumed by another system**
   - Check if cursor is locked
   - Check if another script is capturing input

3. **UI elements inactive**
   - Press D to check if elements are active in hierarchy
   - Check parent GameObjects aren't disabled

---

### Issue 5: "Input field activated but EventSystem selected: null"

**Cause:** Something is preventing input field from being selected

**Solution:**
1. Check InputField is **Interactable** (checkbox in Inspector)
2. Check InputField has **no canvas group** blocking it
3. Check InputField is **not behind** other UI elements

**Fix:**
```
Select InputField → Inspector:
✓ Interactable: Checked
✓ Raycast Target: Checked (on child Text)
```

---

### Issue 6: Can See Cursor But Can't Type

**Cause:** Input field is selected but not activated

**Solution:**
- Make sure you're calling `ActivateInputField()` not just `Select()`
- Our code does both, so check console for errors

**Test:**
1. Press D (check status)
2. Press U (activate)
3. Check console: Should say "SUCCESS!"
4. Try typing - cursor should appear

---

## 📋 Debugging Checklist

Run through this list:

- [ ] Press Play
- [ ] Check Console for auto-find messages
- [ ] All 3 elements found? (InputField, Button, Text)
- [ ] Press D - Check status report
- [ ] EventSystem exists?
- [ ] Input field interactable?
- [ ] Press U - See activation messages?
- [ ] See "SUCCESS!" message?
- [ ] Can you type in the input field?

---

## 🔧 Manual Debug in Inspector

### Check BiodiversityUI Component:

1. Select GameObject with BiodiversityUI
2. In Inspector, expand all sections
3. Check:

```
User Search Elements:
├─ Username Search Input: [Assigned or None]
├─ Search User Button: [Assigned or None]
└─ Search Status Text: [Assigned or None]

Auto-Find UI (Fallback):
└─ Auto Find UI Elements: ✓ Checked

User Search Hotkeys:
├─ Activate Search Key: U
└─ Search On Enter: ✓ Checked
```

---

## 📊 Console Message Reference

### Auto-Find Messages:

| Message | Meaning |
|---------|---------|
| `✓ InputField component found!` | Good - found correctly |
| `✗ UsernameSearchInput GameObject not found` | Bad - create the UI element |
| `✗ GameObject found but NO InputField component!` | Bad - wrong component type |

### Activation Messages:

| Message | Meaning |
|---------|---------|
| `U key pressed! Attempting to activate...` | U key detected |
| `✓ SUCCESS! Input field is now selected` | Working perfectly |
| `✗ Cannot activate - usernameSearchInput is NULL!` | UI not found |
| `✗ NO EventSystem found!` | Need to add EventSystem |

---

## 🎯 Testing Procedure

1. **Open Unity**
2. **Press Play**
3. **Open Console** (Ctrl+Shift+C / Cmd+Shift+C)
4. **Look for auto-find messages**
5. **Press D** - Check full status
6. **Press U** - Try to activate
7. **Check activation messages**
8. **Try typing**

---

## 💡 Pro Tips

### Tip 1: Filter Console
- Click the Console search bar
- Type: `[BiodiversityUI]`
- See only relevant messages

### Tip 2: Use Right-Click Menu
- Right-click BiodiversityUI in Inspector
- Select "Debug UI Status"
- Same as pressing D in Play mode

### Tip 3: Check Hierarchy Path
- D key shows full path like:
  `Canvas/BiodiversityPanel/UsernameSearchInput`
- Verify this matches your setup

### Tip 4: Test in Editor First
- Easier to debug in Editor
- WebGL build can have different issues
- Get it working in Editor first

---

## 🆘 Still Not Working?

### Copy this info and share:

1. **Press D** in Play mode
2. **Copy entire Console output**
3. Look for these specific messages:
   - Auto-find results
   - EventSystem status
   - UI element paths
   - Any red errors

### Common solutions we can try:

1. Manually assign UI elements in Inspector
2. Check for conflicting scripts
3. Verify UI hierarchy structure
4. Test with a minimal scene
5. Check Unity version compatibility

---

## 📝 Expected Successful Output

When everything works, you should see:

```
[BiodiversityUI] === AUTO-FIND UI ELEMENTS ===
[BiodiversityUI] Searching for 'UsernameSearchInput': FOUND
[BiodiversityUI] - GameObject found, active: True
[BiodiversityUI] ✓ InputField component found!
[BiodiversityUI] - InputField enabled: True
[BiodiversityUI] - InputField interactable: True
[BiodiversityUI] Searching for 'SearchUserButton': FOUND
[BiodiversityUI] - GameObject found, active: True
[BiodiversityUI] ✓ Button component found!
[BiodiversityUI] - Button interactable: True
[BiodiversityUI] Searching for 'SearchStatusText': FOUND
[BiodiversityUI] - GameObject found, active: True
[BiodiversityUI] ✓ Text component found!
[BiodiversityUI] === AUTO-FIND COMPLETE ===

[User presses U]

[BiodiversityUI] U key pressed! Attempting to activate search input...
[BiodiversityUI] === ACTIVATE SEARCH INPUT DEBUG ===
[BiodiversityUI] ✓ usernameSearchInput found: UsernameSearchInput
[BiodiversityUI] - GameObject active: True
[BiodiversityUI] - Component enabled: True
[BiodiversityUI] - Interactable: True
[BiodiversityUI] UI Panel: BiodiversityPanel, active: True
[BiodiversityUI] ✓ EventSystem found: EventSystem
[BiodiversityUI] Calling ActivateInputField()...
[BiodiversityUI] Calling Select()...
[BiodiversityUI] ✓ SUCCESS! Input field is now selected and active!
[BiodiversityUI] Username search input activation complete (press Enter to search, Escape to cancel)
[BiodiversityUI] === ACTIVATION DEBUG COMPLETE ===
```

If you see this, you're ready to search! Type a username and press Enter.
