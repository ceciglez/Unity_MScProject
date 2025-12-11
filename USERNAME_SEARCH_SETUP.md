# iNaturalist Username Search Setup Guide

This guide explains how to set up the new username search functionality in the Unity Canvas UI.

## Overview

The username search feature allows users to:
1. Enter an iNaturalist username in a search bar
2. Find that user's last observation location
3. Teleport the player to those coordinates
4. Load observations with that user's observations prioritized (shown first)

## Code Changes Made

### 1. BiodiversityUI.cs
Added three new UI elements:
- `usernameSearchInput` (InputField) - Text input for username
- `searchUserButton` (Button) - Search button
- `searchStatusText` (Text) - Status/feedback text

New functionality:
- `SearchAndTeleportToUser()` - Queries iNaturalist API for user's last observation
- `ProcessUserSearchResponse()` - Handles the API response
- `TeleportPlayerToCoordinates()` - Moves player to observation location
- `ReloadObservationsWithUserFilter()` - Reloads observations with user priority

### 2. INaturalistMapController.cs
Added:
- `filterByUsername` (public string) - Username filter field
- `SetUsernameFilter()` - Sets username filter and reloads
- `ClearUsernameFilter()` - Clears filter and reloads
- `LoadObservationsWithUserPriority()` - Loads user's observations first, then area observations
- Updated `BuildApiUrl()` - Includes username filter in API queries

## Unity Setup Instructions

### Step 1: Add UI Elements to Canvas

1. Open your scene with the BiodiversityUI Canvas
2. Select the BiodiversityUI panel (or create a new section)

#### Add InputField for Username:
1. Right-click in Hierarchy → UI → Input Field
2. Rename it to "UsernameSearchInput"
3. Configure:
   - Placeholder text: "Enter iNaturalist username..."
   - Character Limit: 50 (optional)
   - Content Type: Standard

#### Add Search Button:
1. Right-click in Hierarchy → UI → Button
2. Rename it to "SearchUserButton"
3. Configure the button's Text child:
   - Text: "Search User"

#### Add Status Text:
1. Right-click in Hierarchy → UI → Text
2. Rename it to "SearchStatusText"
3. Configure:
   - Text: "" (empty)
   - Font Size: 14
   - Color: Yellow or White
   - Alignment: Center

### Step 2: Link UI Elements to BiodiversityUI Script

1. Select the GameObject with the BiodiversityUI component
2. In the Inspector, find the "User Search Elements" section
3. Drag and drop:
   - UsernameSearchInput → `Username Search Input` field
   - SearchUserButton → `Search User Button` field
   - SearchStatusText → `Search Status Text` field

### Step 3: Recommended Layout

Here's a suggested layout structure:

```
BiodiversityPanel
├── Simpson's Index Text
├── Observation Count Text
├── Species Count Text
├── Diversity Intensity Slider
├── Enable Biodiversity Toggle
├── Recalculate Button
├── [Separator/Spacer]
├── Username Search Section
│   ├── UsernameSearchInput (width: 200px)
│   ├── SearchUserButton (width: 100px)
│   └── SearchStatusText (width: 300px, below)
```

### Step 4: Optional - Add a Clear Filter Button

You can add a "Clear Filter" button that calls:
```csharp
mapController.ClearUsernameFilter();
```

## Usage

### For Players (Keyboard - Recommended):
1. Press **U** key to activate the search input
2. Type an iNaturalist username (e.g., "kueda")
3. Press **Enter** to search
4. Press **Escape** to cancel (if needed)

### For Players (Mouse):
1. Click in the username input field
2. Type an iNaturalist username (e.g., "naturalist123")
3. Click "Search User" button

### What Happens:
1. The system will:
   - Find the user's last observation
   - Teleport the player to that location
   - Load observations with that user's observations shown first
2. Status messages will appear below the search bar

### Keyboard Shortcuts:
- **U** - Activate search input
- **Enter** - Submit search (when input is focused)
- **Escape** - Cancel/deactivate input
- **B** - Toggle biodiversity UI panel
- **O** - Reload observations

See [KEYBOARD_SHORTCUTS.md](KEYBOARD_SHORTCUTS.md) for complete reference.

### For Developers:

#### Programmatic Search:
```csharp
// Get reference to BiodiversityUI
BiodiversityUI ui = FindObjectOfType<BiodiversityUI>();

// Trigger search programmatically
ui.usernameSearchInput.text = "username";
ui.OnSearchUserPressed(); // This is private, so use the button click
```

#### Filter Only (No Teleport):
```csharp
INaturalistMapController controller = FindObjectOfType<INaturalistMapController>();

// Filter by username
controller.SetUsernameFilter("username");

// Clear filter
controller.ClearUsernameFilter();
```

#### Manual User Priority Load:
```csharp
INaturalistMapController controller = FindObjectOfType<INaturalistMapController>();

// Load with user priority at specific coordinates
StartCoroutine(controller.LoadObservationsWithUserPriority(
    "username",  // iNaturalist username
    51.505,      // latitude
    -0.09,       // longitude
    5f           // radius in km
));
```

## API Endpoints Used

1. **User Search**: `https://api.inaturalist.org/v1/observations?user_login={username}&order=desc&order_by=observed_on&per_page=1`
   - Returns the user's most recent observation

2. **Filtered Observations**: The existing observation loading system with `&user_login={username}` parameter added

## Troubleshooting

### "No observations found for user"
- Check if the username is correct (case-sensitive)
- Verify the user has public observations
- Check internet connection

### "User's last observation has no location data"
- Some observations don't include precise coordinates
- Try searching for a different user

### "Map controller not found"
- Ensure INaturalistMapController exists in your scene
- Check that it's properly initialized before searching

### Player doesn't teleport
- Verify the player has the "Player" tag
- Check that AbstractMap component exists in the scene
- Ensure Mapbox is properly initialized

## WebGL Support

The implementation includes full WebGL support using the `WebGLNetworkBridge` for CORS-compliant API requests. Both editor and WebGL builds will work correctly.

## Performance Notes

- User searches query the API in real-time
- Observation loading uses existing caching mechanisms
- User-priority loading makes two API calls (user observations, then area observations)
- Consider rate limiting if allowing rapid searches

## Future Enhancements

Potential improvements:
1. User autocomplete/suggestions
2. Search history
3. Multiple user filters
4. User observation count display
5. "Follow user" mode that continuously updates
6. Save favorite users

## Credits

Implementation includes:
- iNaturalist API v1 integration
- Mapbox Unity SDK for coordinate conversion
- Unity UI system
- WebGL network bridge for browser compatibility
