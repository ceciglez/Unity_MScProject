# Keyboard Shortcuts Reference

## BiodiversityUI Hotkeys

### Main Navigation
| Key | Action | Description |
|-----|--------|-------------|
| **B** | Toggle Biodiversity UI | Shows/hides the biodiversity panel |

### Username Search
| Key | Action | Description |
|-----|--------|-------------|
| **U** | Activate Search Input | Focuses the username search field so you can type |
| **Enter** | Submit Search | Searches for the entered username (when input is focused) |
| **Escape** | Cancel/Deactivate | Unfocuses the input field and cancels typing |

### Observation Controls
| Key | Action | Description |
|-----|--------|-------------|
| **O** | Reload Observations | Manually triggers observation reload from iNaturalist |

### Diagnostic Tools (if BiodiversityUIHelper is added)
| Key | Action | Description |
|-----|--------|-------------|
| **H** | Run Diagnostics | Displays detailed UI setup information in Console |

---

## Usage Workflow

### Quick Username Search:
1. Press **U** - Activates search input
2. Type username (e.g., `kueda`)
3. Press **Enter** - Submits search
4. Player teleports to user's last observation

### If You Make a Typo:
1. Press **Escape** - Deactivates input
2. Press **U** - Reactivates input
3. Type correct username
4. Press **Enter**

---

## Configuration

### Changing Hotkeys (Inspector Settings)

On the **BiodiversityUI** component, you can customize:

- **Toggle UI Key**: Default `B` (KeyCode.B)
- **Activate Search Key**: Default `U` (KeyCode.U)
- **Search On Enter**: Toggle whether Enter key triggers search (enabled by default)

### Example: Change Search Activation to 'S' Key

1. Select GameObject with BiodiversityUI
2. In Inspector, find "User Search Hotkeys"
3. Change "Activate Search Key" from `U` to `S`

---

## Programmatic Access

### From Another Script:

```csharp
// Get reference
BiodiversityUI bioUI = FindObjectOfType<BiodiversityUI>();

// Activate search programmatically
bioUI.ActivateSearchInput();

// Deactivate search programmatically
bioUI.DeactivateSearchInput();

// Toggle UI visibility
bioUI.ToggleUI();
```

---

## Tips

- **Input field must exist**: Make sure you've created the UI elements (`UsernameSearchInput`, `SearchUserButton`, `SearchStatusText`)
- **EventSystem required**: Unity UI requires an EventSystem in the scene (usually auto-created with Canvas)
- **Check Console**: Activation messages appear in Console for debugging
- **Navigation mode**: If using gamepad/keyboard navigation, you can also Tab through UI elements

---

## Troubleshooting

### "U key does nothing"
- ✓ Check that BiodiversityUI script is active
- ✓ Verify `usernameSearchInput` is assigned (check auto-find logs)
- ✓ Make sure UI panel is active in hierarchy

### "Enter key doesn't search"
- ✓ Make sure input field is focused (press U first)
- ✓ Check "Search On Enter" is enabled in Inspector
- ✓ Verify search button callback is set up

### "Can't type in input field"
- ✓ Press U to activate it
- ✓ Check EventSystem exists in scene
- ✓ Verify InputField component is properly configured

### "Escape doesn't work"
- ✓ Input field must be focused first
- ✓ Check Console for deactivation message
- ✓ Try clicking elsewhere to unfocus manually

---

## Advanced: Custom Key Bindings

To add your own custom hotkeys, edit `BiodiversityUI.cs`:

```csharp
void Update()
{
    // Your custom hotkey
    if (Input.GetKeyDown(KeyCode.YourKey))
    {
        // Your custom action
        YourCustomMethod();
    }

    // Existing hotkeys...
}
```

---

## Future Enhancements

Potential keyboard shortcuts to add:
- **Ctrl+U**: Clear search input
- **Ctrl+F**: Open advanced search filters
- **Tab**: Cycle through search history
- **Arrow Keys**: Navigate autocomplete suggestions
