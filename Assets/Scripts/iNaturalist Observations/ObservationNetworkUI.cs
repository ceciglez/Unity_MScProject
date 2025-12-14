using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObservationNetworkUI : MonoBehaviour
{
    [Header("Main Controls")]
    [Tooltip("Toggle to enable/disable entire network view")]
    public Toggle networkViewToggle;
    
    [Header("Taxon Category Filters")]
    [Tooltip("Parent transform for taxon category toggles")]
    public Transform categoryListParent;
    
    private ObservationNetworkManager networkManager;
    private Dictionary<string, Toggle> categoryToggles = new Dictionary<string, Toggle>();
    
    public void Initialize(ObservationNetworkManager manager)
    {
        networkManager = manager;
        
        // Setup network view toggle
        if (networkViewToggle != null)
        {
            networkViewToggle.onValueChanged.RemoveAllListeners();
            networkViewToggle.onValueChanged.AddListener(SetNetworkViewEnabled);
        }
        
        Debug.Log("[ObservationNetworkUI] Simplified UI initialized");
    }
    
    public void UpdateSpeciesList(Dictionary<string, bool> speciesStates)
    {
        // Clear existing category toggles
        foreach (var toggle in categoryToggles.Values)
        {
            if (toggle != null)
                Destroy(toggle.gameObject);
        }
        categoryToggles.Clear();
        
        // Create toggles for each category
        foreach (var kvp in speciesStates)
        {
            CreateCategoryToggle(kvp.Key, kvp.Value);
        }
    }
    
    private void CreateCategoryToggle(string categoryName, bool isEnabled)
    {
        if (categoryListParent == null) return;
        
        GameObject toggleObj = new GameObject($"Category_{categoryName}");
        toggleObj.transform.SetParent(categoryListParent, false);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        toggle.isOn = isEnabled;
        
        // Simple visual
        Image background = toggleObj.AddComponent<Image>();
        background.color = isEnabled ? Color.white : Color.gray;
        
        // Category label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = categoryName;
        labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 14;
        labelText.color = Color.black;
        
        // Position label
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(5, 0);
        labelRect.offsetMax = new Vector2(-5, 0);
        
        toggle.targetGraphic = background;
        
        // Toggle listener
        toggle.onValueChanged.AddListener((enabled) => {
            SetCategoryEnabled(categoryName, enabled);
            background.color = enabled ? Color.white : Color.gray;
        });
        
        categoryToggles[categoryName] = toggle;
    }
    
    private void SetNetworkViewEnabled(bool enabled)
    {
        if (networkManager != null)
        {
            networkManager.SetNetworkEnabled(enabled);
        }
        
        // Enable/disable all category controls
        if (categoryListParent != null)
        {
            categoryListParent.gameObject.SetActive(enabled);
        }
    }
    
    private void SetCategoryEnabled(string category, bool enabled)
    {
        if (networkManager != null)
        {
            networkManager.SetCategoryFilter(category, enabled);
        }
        
        Debug.Log($"Category '{category}' {(enabled ? "enabled" : "disabled")}");
    }
}