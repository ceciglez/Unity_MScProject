# Username Search - Visual Workflow Guide

## 🎮 Player Workflow

```
┌─────────────────────────────────────┐
│  Player exploring the environment   │
└─────────────────────────────────────┘
                 │
                 │ Press 'U' key
                 ▼
┌─────────────────────────────────────┐
│   Search input field activates      │
│   Cursor appears in input box       │
└─────────────────────────────────────┘
                 │
                 │ Type username
                 ▼
┌─────────────────────────────────────┐
│   Username: "kueda"                 │
│   (cursor blinking)                 │
└─────────────────────────────────────┘
                 │
                 │ Press Enter OR Click Button
                 ▼
┌─────────────────────────────────────┐
│   Status: "Searching for user..."   │
│   API call to iNaturalist           │
└─────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   Status: "Found observation!"      │
│   Player position updates           │
└─────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   Player teleported to location     │
│   Observations loading...           │
└─────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────┐
│   User's observations displayed     │
│   Area observations loaded          │
│   Ready to explore!                 │
└─────────────────────────────────────┘
```

---

## 🎹 Keyboard Flow

```
                  ┌──────┐
                  │  U   │  Activate input
                  └──────┘
                      │
                      ▼
              ┌───────────────┐
              │  Input Field  │
              │    Active     │
              └───────────────┘
                      │
              ┌───────┴────────┐
              │                │
              ▼                ▼
         ┌────────┐      ┌─────────┐
         │ Enter  │      │  Escape │
         │ (Search)      │ (Cancel)│
         └────────┘      └─────────┘
              │                │
              ▼                ▼
    ┌──────────────┐    ┌──────────┐
    │ Start Search │    │ Deactivate│
    └──────────────┘    └──────────┘
```

---

## 📱 UI Layout Example

```
┌─────────────────────────────────────────────┐
│          Biodiversity Panel                 │
├─────────────────────────────────────────────┤
│                                             │
│  Simpson's Index: 0.756 (High Diversity)   │
│  Total Observations: 42                     │
│  Unique Species: 18                         │
│                                             │
│  ─────────────────────────────────────────  │
│                                             │
│  🔍 Username Search                         │
│                                             │
│  ┌────────────────────────┐  ┌──────────┐  │
│  │ Enter username...      │  │  Search  │  │
│  └────────────────────────┘  └──────────┘  │
│                                             │
│  Status: Ready to search                   │
│                                             │
│  💡 Press U to activate search             │
│                                             │
└─────────────────────────────────────────────┘
```

---

## 🔄 State Diagram

```
┌──────────────┐
│   Idle       │
│ (No focus)   │
└──────────────┘
       │
       │ Press U
       ▼
┌──────────────┐
│   Active     │◄─────┐
│ (Has focus)  │      │
└──────────────┘      │
       │              │
       ├──Enter──────►│ (Search)
       │              │
       └──Escape─────►│ (Cancel)
```

---

## 🎯 Success States

### ✅ Valid User Found
```
Status: "Found kueda's observation: Eastern Gray Squirrel"
Action: Player teleported → Observations loading
```

### ⚠️ User Not Found
```
Status: "No observations found for user: invaliduser"
Action: Input remains active for retry
```

### ❌ Connection Error
```
Status: "Error: Network request failed"
Action: Check internet connection
```

### 🔒 No Location Data
```
Status: "User's last observation has no location data"
Action: User may have obscured coordinates
```

---

## 🎨 Visual Indicators

### Input Field States

**Inactive** (before pressing U):
```
┌────────────────────────┐
│ Enter username...      │  ← Gray placeholder text
└────────────────────────┘
```

**Active** (after pressing U):
```
┌────────────────────────┐
│ |                      │  ← Blinking cursor
└────────────────────────┘
     Blue border (optional)
```

**Typing**:
```
┌────────────────────────┐
│ kueda|                 │  ← User input + cursor
└────────────────────────┘
```

**Searching**:
```
┌────────────────────────┐
│ kueda                  │
└────────────────────────┘
Status: "Searching for user: kueda..."
        Yellow/Orange text
```

**Success**:
```
┌────────────────────────┐
│ kueda                  │
└────────────────────────┘
Status: "Found kueda's observation: ..."
        Green text
```

**Error**:
```
┌────────────────────────┐
│ invaliduser            │
└────────────────────────┘
Status: "No observations found for user: invaliduser"
        Red text
```

---

## 🛠️ Developer Flow

```
Unity Editor
    │
    ├──► Create UI Elements
    │    ├─ UsernameSearchInput (InputField)
    │    ├─ SearchUserButton (Button)
    │    └─ SearchStatusText (Text)
    │
    ├──► BiodiversityUI Script
    │    └─ Auto-finds elements by name
    │
    └──► Press Play
         │
         ├──► Console logs confirmation:
         │    "Auto-found UsernameSearchInput"
         │    "Auto-found SearchUserButton"
         │    "Auto-found SearchStatusText"
         │
         └──► Feature ready!
              Press U to test
```

---

## 🎲 Example Session

```
[00:00] Player spawns in London
[00:05] Player presses 'U' key
[00:05] Input field activates: "Username search input activated..."
[00:07] Player types: "loarie"
[00:10] Player presses Enter
[00:10] Console: "[BiodiversityUI] Fetching user observations..."
[00:12] Console: "[BiodiversityUI] Teleported to loarie's observation at 37.7749, -122.4194"
[00:12] Status: "Found loarie's observation: California Poppy"
[00:15] Player teleports to San Francisco
[00:15] Console: "[iNaturalist] Loading observations with user priority..."
[00:18] Observations appear (loarie's observations first)
[00:20] Player explores the area
```

---

## 📊 Performance Timeline

```
0ms ───────► Press U
             │
             ├─ Input field activates (instant)
             │
10ms ────────► Type username (user input)
             │
500ms ───────► Press Enter
             │
             ├─ API request starts
             │
700ms ───────► API response received
             │
             ├─ Parse coordinates
             │
710ms ───────► Player teleportation (instant)
             │
             ├─ Start loading observations
             │
2000ms ──────► Observations loaded and displayed
             │
             └─ Complete!
```

---

## 🎯 Tips for Best UX

1. **Quick Access**: Press U from anywhere
2. **Fast Search**: Type + Enter (no mouse needed)
3. **Error Recovery**: Status messages guide you
4. **Escape Hatch**: Press Escape to cancel anytime
5. **Visual Feedback**: Watch status text for progress

---

## 🚀 Power User Tips

- **Quick retry**: If search fails, input stays active - just edit and press Enter again
- **Clear input**: Select all (Ctrl+A) then type new username
- **Known good users**: Keep a list of active users for testing
- **Debug mode**: Enable "Show Debug Info" for detailed Console logs
- **Diagnostic**: Press H (if BiodiversityUIHelper added) to troubleshoot

---

This visual guide should help you understand the complete user journey and system flow!
