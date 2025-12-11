using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to diagnose BiodiversityUI setup issues
/// Attach this to the same GameObject as BiodiversityUI and press H in Play mode
/// </summary>
public class BiodiversityUIHelper : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            DiagnoseSetup();
        }
    }

    [ContextMenu("Diagnose UI Setup")]
    public void DiagnoseSetup()
    {
        Debug.Log("=== BIODIVERSITY UI DIAGNOSTIC ===");

        // Check for BiodiversityUI component
        BiodiversityUI bioUI = GetComponent<BiodiversityUI>();
        if (bioUI == null)
        {
            Debug.LogError("❌ BiodiversityUI component not found on this GameObject!");
            return;
        }

        Debug.Log("✓ BiodiversityUI component found");

        // Check for required UI elements in the scene
        Debug.Log("\n--- Searching for UI Elements by Name ---");

        InputField foundInput = GameObject.Find("UsernameSearchInput")?.GetComponent<InputField>();
        if (foundInput != null)
            Debug.Log($"✓ Found UsernameSearchInput: {foundInput.gameObject.name} (Active: {foundInput.gameObject.activeInHierarchy})");
        else
            Debug.LogWarning("⚠ UsernameSearchInput not found in scene (searching by name)");

        Button foundButton = GameObject.Find("SearchUserButton")?.GetComponent<Button>();
        if (foundButton != null)
            Debug.Log($"✓ Found SearchUserButton: {foundButton.gameObject.name} (Active: {foundButton.gameObject.activeInHierarchy})");
        else
            Debug.LogWarning("⚠ SearchUserButton not found in scene (searching by name)");

        Text foundText = GameObject.Find("SearchStatusText")?.GetComponent<Text>();
        if (foundText != null)
            Debug.Log($"✓ Found SearchStatusText: {foundText.gameObject.name} (Active: {foundText.gameObject.activeInHierarchy})");
        else
            Debug.LogWarning("⚠ SearchStatusText not found in scene (searching by name)");

        // Check what's currently assigned
        Debug.Log("\n--- Currently Assigned in Inspector ---");
        Debug.Log($"usernameSearchInput: {(bioUI.usernameSearchInput != null ? "✓ ASSIGNED" : "❌ NULL")}");
        Debug.Log($"searchUserButton: {(bioUI.searchUserButton != null ? "✓ ASSIGNED" : "❌ NULL")}");
        Debug.Log($"searchStatusText: {(bioUI.searchStatusText != null ? "✓ ASSIGNED" : "❌ NULL")}");

        // Check auto-find setting
        Debug.Log($"\nautoFindUIElements: {bioUI.autoFindUIElements}");

        // List all InputFields in scene
        Debug.Log("\n--- All InputFields in Scene ---");
        InputField[] allInputs = FindObjectsOfType<InputField>(true);
        Debug.Log($"Found {allInputs.Length} InputFields:");
        foreach (var input in allInputs)
        {
            Debug.Log($"  - {GetGameObjectPath(input.gameObject)} (Active: {input.gameObject.activeInHierarchy})");
        }

        // List all Buttons with "Search" in name
        Debug.Log("\n--- Buttons with 'Search' in Name ---");
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (var btn in allButtons)
        {
            if (btn.name.ToLower().Contains("search"))
            {
                Debug.Log($"  - {GetGameObjectPath(btn.gameObject)} (Active: {btn.gameObject.activeInHierarchy})");
            }
        }

        // List all Text components in scene (searching for status text)
        Debug.Log("\n--- Text Components with 'Status' or 'Search' in Name ---");
        Text[] allTexts = FindObjectsOfType<Text>(true);
        foreach (var txt in allTexts)
        {
            if (txt.name.ToLower().Contains("status") || txt.name.ToLower().Contains("search"))
            {
                Debug.Log($"  - {GetGameObjectPath(txt.gameObject)} (Active: {txt.gameObject.activeInHierarchy})");
            }
        }

        // Check for other BiodiversityUI components
        Debug.Log("\n--- Other Components ---");
        var mapController = FindObjectOfType<INaturalistMapController>();
        Debug.Log($"INaturalistMapController: {(mapController != null ? "✓ FOUND" : "❌ NOT FOUND")}");

        var biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
        Debug.Log($"BiodiversityScoreManager: {(biodiversityManager != null ? "✓ FOUND" : "❌ NOT FOUND")}");

        Debug.Log("\n=== DIAGNOSTIC COMPLETE ===");
        Debug.Log("TIP: If elements exist but aren't assigned, make sure names match exactly (case-sensitive)");
        Debug.Log("TIP: Press H again to re-run diagnostic");
    }

    /// <summary>
    /// Gets the full hierarchy path of a GameObject
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
