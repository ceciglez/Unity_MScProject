# BiodiversityUI Setup Troubleshooting Guide

## Problem: Can't Drag UI Elements into Inspector

### Solution 1: Use Auto-Find Feature (Easiest)

I've added an automatic UI element finder. Just:

1. Create your UI elements with these **exact names**:
   - `UsernameSearchInput` (InputField)
   - `SearchUserButton` (Button)
   - `SearchStatusText` (Text)

2. Make sure "Auto Find UI Elements" is checked in the BiodiversityUI Inspector (it's on by default)

3. Press Play - the script will automatically find and connect them!

4. Check the Console for confirmation messages like:
   ```
   [BiodiversityUI] Auto-found UsernameSearchInput
   [BiodiversityUI] Auto-found SearchUserButton
   [BiodiversityUI] Auto-found SearchStatusText
   ```

### Solution 2: Use Circle Selector (If Auto-Find Doesn't Work)

1. Select the GameObject with BiodiversityUI script
2. In Inspector, find the "User Search Elements" section
3. Click the small **circle icon (⊙)** next to each field
4. A window will open - select your UI element from the list

### Solution 3: Lock Inspector Method

1. Select the GameObject with BiodiversityUI script
2. Click the **lock icon** (🔒) at the top-right of Inspector
3. Select your UI element (InputField/Button/Text) in the Hierarchy
4. A new Inspector window opens
5. Drag the component from this new window to the locked Inspector

### Solution 4: Check for Compilation Errors

**Open Console (Ctrl+Shift+C or Cmd+Shift+C)**

Look for red error messages. Common issues:

#### Missing References Error
```
error CS0246: The type or namespace name 'WebGLNetworkBridge' could not be found
```

**Fix:** Make sure these files exist:
- `Assets/Scripts/WebGL/WebGLNetworkBridge.cs`
- `Assets/Scripts/WebGL/WebGLCorsHelper.cs`

If they don't exist, let me know and I'll create them.

#### Wrong Unity Version
```
error CS1061: 'UnityWebRequest' does not contain a definition for 'result'
```

**Fix:** You need Unity 2020.1 or later. Update Unity or use the legacy method.

### Solution 5: Manual Code Assignment (Last Resort)

If nothing else works, you can assign via code. Add this to your BiodiversityUI script:

```csharp
void Start()
{
    // Find by path in hierarchy
    usernameSearchInput = transform.Find("Canvas/BiodiversityPanel/UsernameSearchInput").GetComponent<InputField>();
    searchUserButton = transform.Find("Canvas/BiodiversityPanel/SearchUserButton").GetComponent<Button>();
    searchStatusText = transform.Find("Canvas/BiodiversityPanel/SearchStatusText").GetComponent<Text>();

    // Rest of Start() code...
}
```

Replace the path with your actual hierarchy path.

---

## Common Setup Issues

### Issue: UI Elements Exist But Auto-Find Doesn't Work

**Cause:** GameObject names don't match exactly

**Fix:** Rename your UI GameObjects to match exactly:
- `UsernameSearchInput` (case-sensitive!)
- `SearchUserButton`
- `SearchStatusText`

### Issue: Script Shows as "Missing" in Inspector

**Cause:** Script has compilation errors

**Fix:**
1. Open Console
2. Fix all red errors
3. Wait for Unity to recompile
4. Inspector will unlock

### Issue: Fields Are Grayed Out in Inspector

**Cause:** Multiple possible reasons:

1. **Inspector is in Debug mode**
   - Click the three dots (⋮) at top-right of Inspector
   - Select "Normal" mode

2. **Script has compilation errors**
   - Check Console for errors

3. **Prefab is locked**
   - Click "Open Prefab" to edit
   - Or break prefab instance

### Issue: Can Assign Some Fields But Not Others

**Cause:** Type mismatch

**Check:**
- `usernameSearchInput` must be an **InputField** (not Text)
- `searchUserButton` must be a **Button**
- `searchStatusText` must be a **Text** (not InputField)

---

## Creating UI Elements Step-by-Step

### Step 1: Create InputField
```
Hierarchy → Right-click → UI → Input Field
```
1. Rename to `UsernameSearchInput`
2. Set Placeholder text: "Enter iNaturalist username..."
3. Optional: Set Character Limit to 50

### Step 2: Create Button
```
Hierarchy → Right-click → UI → Button
```
1. Rename to `SearchUserButton`
2. Select the Text child
3. Change text to "Search User"

### Step 3: Create Status Text
```
Hierarchy → Right-click → UI → Text
```
1. Rename to `SearchStatusText`
2. Clear the default text (leave empty)
3. Set Color to Yellow or White
4. Set Alignment to Center
5. Set Font Size to 14

### Step 4: Position Elements

Suggested layout:
```
Canvas
└── BiodiversityPanel
    ├── [Your existing UI elements]
    ├── UsernameSearchInput (RectTransform: Width 200, Height 30)
    ├── SearchUserButton (RectTransform: Width 100, Height 30)
    └── SearchStatusText (RectTransform: Width 300, Height 20)
```

---

## Verification Checklist

After setup, verify:

- [ ] BiodiversityUI script is on a GameObject in the scene
- [ ] No compilation errors in Console
- [ ] UI elements are in the same scene (not different scene)
- [ ] UI elements have correct components (InputField, Button, Text)
- [ ] "Auto Find UI Elements" is checked (or fields are manually assigned)
- [ ] Play mode: Check Console for auto-find success messages

---

## Testing

Once set up, test with a real iNaturalist username:

1. **Good test usernames** (active users with many observations):
   - `kueda` (iNaturalist co-founder)
   - `loarie` (iNaturalist staff)
   - `plantaeanaturalist`

2. **Enter username** in the InputField
3. **Click "Search User"**
4. **Watch for:**
   - Status text updates
   - Console logs
   - Player teleports to location
   - Observations load

---

## Debug Mode

To see detailed logs, enable debug info on INaturalistMapController:
1. Select the GameObject with INaturalistMapController
2. Check "Show Debug Info"
3. Run your test again
4. Watch Console for detailed API calls and responses

---

## Still Having Issues?

If none of these solutions work:

1. **Check Unity version**: Need 2020.1+ for full compatibility
2. **Check platform**: Editor vs WebGL may behave differently
3. **Share Console errors**: Copy any red error messages
4. **Check Canvas setup**: Make sure Canvas has EventSystem
5. **Try in new scene**: Create minimal test scene with just Canvas + BiodiversityUI

Let me know what specific error messages you see and I can provide more targeted help!
