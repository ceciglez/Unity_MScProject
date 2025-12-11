# Quick Start: Username Search Feature

## 🚀 Fastest Setup Method

### Step 1: Create UI Elements (2 minutes)

In your Unity scene hierarchy, find your BiodiversityUI panel and add:

1. **Right-click → UI → Input Field**
   - Rename to: `UsernameSearchInput`
   - Set Placeholder: "Enter iNaturalist username..."

2. **Right-click → UI → Button**
   - Rename to: `SearchUserButton`
   - Set Button Text: "Search User"

3. **Right-click → UI → Text**
   - Rename to: `SearchStatusText`
   - Clear default text (leave empty)
   - Set Color: Yellow

**⚠️ IMPORTANT:** Names must match EXACTLY (case-sensitive!)

### Step 2: Let Auto-Find Do the Work

1. Find the GameObject with **BiodiversityUI** script
2. In Inspector, make sure **"Auto Find UI Elements"** is checked ✓
3. Press **Play**
4. Check Console - you should see:
   ```
   [BiodiversityUI] Auto-found UsernameSearchInput
   [BiodiversityUI] Auto-found SearchUserButton
   [BiodiversityUI] Auto-found SearchStatusText
   ```

**That's it! You're done!** 🎉

---

## 🧪 Test It

### Method 1: Using Keyboard (Fastest!)
1. Press **U** key to activate the search input
2. Type a username like: `kueda`
3. Press **Enter** to search
4. Press **Escape** to cancel/deactivate input

### Method 2: Using Mouse
1. Click in the input field
2. Type a username like: `kueda`
3. Click "Search User" button

### Expected Result:
- Status text updates
- Player teleports to user's last observation
- Observations load with that user's shown first

---

## ❌ If Auto-Find Didn't Work

### Option A: Manual Inspector Assignment

1. Select GameObject with BiodiversityUI
2. Find "User Search Elements" section
3. Click the **circle icon (⊙)** next to each field
4. Select your UI element from the picker

### Option B: Use the Diagnostic Tool

1. Add the `BiodiversityUIHelper` script to same GameObject
2. Press **Play**
3. Press **H** key
4. Check Console for detailed diagnostic info
5. It will tell you exactly what's missing

---

## 🐛 Common Issues

### "Can't drag elements into Inspector"
- ✓ Use the circle selector (⊙) instead
- ✓ Or rely on auto-find by naming correctly

### "No observations found for user"
- ✓ Try a different username (e.g., `kueda`, `loarie`)
- ✓ Check internet connection
- ✓ Username is case-sensitive

### "Nothing happens when I click Search"
- ✓ Check Console for errors (red messages)
- ✓ Make sure INaturalistMapController exists in scene
- ✓ Verify player has "Player" tag

---

## 📋 What Was Added to Your Project

### Modified Files:
1. **BiodiversityUI.cs** - Added username search functionality
2. **INaturalistMapController.cs** - Added user filtering

### New Files:
1. **BiodiversityUIHelper.cs** - Diagnostic tool (optional)
2. **USERNAME_SEARCH_SETUP.md** - Detailed documentation
3. **UI_SETUP_TROUBLESHOOTING.md** - Full troubleshooting guide
4. **QUICK_START_USERNAME_SEARCH.md** - This file!

---

## 💡 Tips

- **Test usernames**: `kueda`, `loarie`, `plantaeanaturalist`
- **Keyboard shortcuts**:
  - **U** - Activate search input
  - **Enter** - Submit search (when input is focused)
  - **Escape** - Cancel/deactivate input
  - **H** - Run diagnostic tool (if BiodiversityUIHelper is added)
- **Check Console** for detailed logs
- **Enable "Show Debug Info"** on INaturalistMapController for more logs

---

## 🎯 Next Steps

Once it's working, you can:
1. Style the UI elements to match your theme
2. Add a "Clear Filter" button
3. Adjust the search radius (default 5km)
4. Add username autocomplete
5. Save favorite users

---

## 📞 Still Stuck?

1. Run the diagnostic tool (press H)
2. Copy Console output
3. Check the troubleshooting guide
4. Look for compilation errors (red in Console)

The auto-find feature should handle everything automatically if your UI elements are named correctly!
