# Username Search Status Messages Guide

## Status Message States

The username search now displays clear, color-coded status messages throughout the search process.

### 1️⃣ Empty Input
**Message:** `⚠️ Please enter a username`
**Color:** Yellow (#FFCC00)
**When:** User presses search with empty input field
**Action:** Enter a username and try again

---

### 2️⃣ Searching
**Message:** `🔍 Searching for user '{username}'...`
**Color:** Orange-Yellow (#FFCC33)
**When:** API request is in progress
**Duration:** Until response received

---

### 3️⃣ No Observations Found
**Message:** `❌ No observations found for '{username}'`
**Color:** Orange-Red (#FF6600)
**When:** API returns no results
**Reasons:**
- Username doesn't exist
- User has no public observations
- Username is misspelled
- User account exists but hasn't made any observations

---

### 4️⃣ User Found - No Location
**Message:** `⚠️ User '{username}' found, but observation has no location`
**Color:** Yellow-Orange (#FFB300)
**When:** User exists but last observation has no coordinates
**Reason:** User obscured location for privacy

---

### 5️⃣ User Found - Success!
**Message:** `✅ Found '{username}'! Observed: {species name}`
**Color:** Green (#33CC33)
**When:** User found and player teleported
**Info:** Shows species name from last observation

---

### 6️⃣ Loading Observations
**Message:** `📍 Loading observations for '{username}'...`
**Color:** Light Blue (#80B3FF)
**When:** Fetching and displaying observations at location
**Duration:** 1-3 seconds

---

### 7️⃣ Complete
**Message:** `✅ Observations loaded! Showing '{username}' first`
**Color:** Green (#33CC33)
**When:** All observations loaded successfully
**Duration:** Displays for 5 seconds, then clears

---

### ❌ Error States

#### Invalid Username Format
**Message:** `❌ Invalid username format. Try removing underscores or special characters`
**Color:** Red (#FF0000)
**When:** Username contains invalid characters (e.g., underscores)
**Fix:**
- Remove underscores: `user_name` → `username` or `user-name`
- Use only lowercase letters, numbers, and hyphens
- See INATURALIST_USERNAME_GUIDE.md for details

#### Network Error
**Message:** `❌ Network error: {error details}`
**Color:** Red (#FF0000)
**When:** Connection failed or timeout
**Fix:** Check internet connection

#### Invalid Location Format
**Message:** `❌ Invalid location format`
**Color:** Red (#FF0000)
**When:** API returns malformed coordinates
**Fix:** Try another user

#### API Parse Error
**Message:** `❌ Error parsing API response`
**Color:** Red (#FF0000)
**When:** JSON response is corrupted
**Fix:** Try again later

---

## Visual Flow

```
[User types "kueda" and presses Enter]
           ↓
🔍 Searching for user 'kueda'...
    (Orange-Yellow, animated)
           ↓
    [API Request]
           ↓
     ┌─────┴─────┐
     ↓           ↓
  SUCCESS     FAILURE
     ↓           ↓
✅ Found!    ❌ Not Found
  (Green)     (Red/Orange)
     ↓
📍 Loading observations...
    (Light Blue)
     ↓
✅ Observations loaded!
    (Green)
     ↓
   [Clears after 5s]
```

---

## Color Legend

| Color | Hex | Meaning | Used For |
|-------|-----|---------|----------|
| 🟢 Green | #33CC33 | Success | Found, Loaded |
| 🟡 Yellow | #FFCC00 | Warning | Empty input |
| 🟠 Orange | #FFCC33 | In Progress | Searching |
| 🟠 Orange-Red | #FF6600 | Not Found | User doesn't exist |
| 🔴 Red | #FF0000 | Error | Network/Parse errors |
| 🔵 Light Blue | #80B3FF | Loading | Fetching data |

---

## Examples

### Successful Search:
```
1. 🔍 Searching for user 'kueda'...
2. ✅ Found 'kueda'! Observed: California Poppy
3. 📍 Loading observations for 'kueda'...
4. ✅ Observations loaded! Showing 'kueda' first
5. [clears]
```

### No Observations Found:
```
1. 🔍 Searching for user 'invaliduser123'...
2. ❌ No observations found for 'invaliduser123'
```

### Invalid Username Format:
```
1. 🔍 Searching for user 'user_name'...
2. ❌ Invalid username format. Try removing underscores or special characters
```

### Empty Input:
```
1. [User presses Enter with empty field]
2. ⚠️ Please enter a username
```

### Network Error:
```
1. 🔍 Searching for user 'kueda'...
2. ❌ Network error: Connection timeout
```

---

## Status Message Timing

| State | Duration |
|-------|----------|
| Searching | Until API responds (1-3s typical) |
| Found | Instant → transitions to Loading |
| Loading | 1-3 seconds |
| Complete | 5 seconds, then clears |
| Errors | Stays until user retries |

---

## User Experience Notes

### Good UX Elements:
✅ **Emoji indicators** - Quick visual recognition
✅ **Color coding** - Instant status understanding
✅ **Username echo** - Confirms search term
✅ **Species name** - Shows what they're viewing
✅ **Auto-clear** - Doesn't clutter after success
✅ **Persistent errors** - Stay visible until fixed

### Progressive Feedback:
Users see 3-4 status updates during a successful search:
1. Searching... (You initiated it)
2. Found! (Success confirmation)
3. Loading... (Processing)
4. Complete! (Ready to explore)

This keeps users informed at every step!

---

## Testing Checklist

- [ ] Empty input shows warning
- [ ] Valid user shows green success
- [ ] Invalid user shows orange-red not found
- [ ] Network error shows red error
- [ ] Status clears after 5 seconds on success
- [ ] Colors are visible on your UI background
- [ ] Emoji display correctly (not boxes)
- [ ] Messages fit in your text field
- [ ] Quick retry doesn't stack messages

---

## Customization

Want to change the messages or colors? Edit these in BiodiversityUI.cs:

```csharp
// Line ~620: Searching
searchStatusText.color = new Color(1f, 0.8f, 0.2f);

// Line ~698: Not Found
searchStatusText.color = new Color(1f, 0.4f, 0f);

// Line ~739: Success
searchStatusText.color = new Color(0.2f, 0.8f, 0.2f);

// Line ~660, 679: Network Error
searchStatusText.color = Color.red;

// Line ~804: Loading
searchStatusText.color = new Color(0.5f, 0.7f, 1f);
```

Enjoy the improved user feedback! 🎉
