using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller for biodiversity visualization system
/// Displays real-time biodiversity metrics and provides user controls
///
/// FUNCTIONALITY:
/// - Displays Simpson's Diversity Index at player position with qualitative labels
/// - Shows observation count and species count in current grid cell
/// - Provides diversity intensity slider (0-2x multiplier)
/// - Manual recalculation button for biodiversity system
/// - Toggle UI visibility with 'B' key (configurable)
///
/// UI ELEMENTS:
/// - Simpson's Index text with color-coded labels:
///   * 0.0-0.2: "Very Low Diversity" (red)
///   * 0.2-0.4: "Low Diversity" (orange)
///   * 0.4-0.6: "Moderate Diversity" (yellow)
///   * 0.6-0.8: "High Diversity" (light green)
///   * 0.8-1.0: "Very High Diversity" (green)
/// - Observation count (total iNaturalist observations in cell)
/// - Species count (unique species in cell)
///
/// INTEGRATION:
/// - Queries BiodiversityScoreManager for real-time data
/// - Updates every frame based on player position
/// - Calls UpdateBiodiversityScores() when settings change
///
/// SOURCE:
/// - Unity UI system documentation
/// - Custom implementation for biodiversity data display
///
/// AI CONTRIBUTION: ~60% - UI update logic, color coding, query methods
/// HUMAN CONTRIBUTION: ~40% - UI layout, text formatting, user controls
/// </summary>
public class BiodiversityUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Text simpsonsIndexText;
    public Text observationCountText;
    public Text speciesCountText;
    public Slider diversityIntensitySlider;
    public Toggle enableBiodiversityToggle;
    public Button recalculateButton;
    
    [Header("Display Settings")]
    public bool showBiodiversityInfo = true;
    public KeyCode toggleUIKey = KeyCode.B;
    public GameObject uiPanel;
    
    private BiodiversityScoreManager biodiversityManager;
    private Transform playerTransform;
    private bool uiVisible = true;
    
    void Start()
    {
        // Find components
        biodiversityManager = FindObjectOfType<BiodiversityScoreManager>();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        
        // Setup UI
        SetupUI();
        
        // Initial UI state
        if (uiPanel != null)
            uiPanel.SetActive(showBiodiversityInfo);
    }
    
    void Update()
    {
        // Toggle UI visibility
        if (Input.GetKeyDown(toggleUIKey))
        {
            ToggleUI();
        }
        
        // Update biodiversity information
        if (uiVisible && showBiodiversityInfo && biodiversityManager != null && playerTransform != null)
        {
            UpdateBiodiversityDisplay();
        }
    }
    
    private void SetupUI()
    {
        // Setup slider
        if (diversityIntensitySlider != null)
        {
            diversityIntensitySlider.minValue = 0f;
            diversityIntensitySlider.maxValue = 2f;
            diversityIntensitySlider.value = 1f;
            diversityIntensitySlider.onValueChanged.AddListener(OnDiversityIntensityChanged);
        }
        
        // Setup toggle
        if (enableBiodiversityToggle != null)
        {
            enableBiodiversityToggle.isOn = true;
            enableBiodiversityToggle.onValueChanged.AddListener(OnBiodiversityToggled);
        }
        
        // Setup button
        if (recalculateButton != null)
        {
            recalculateButton.onClick.AddListener(OnRecalculatePressed);
        }
        
        // Setup default text
        if (simpsonsIndexText != null)
            simpsonsIndexText.text = "Simpson's Index: Calculating...";
            
        if (observationCountText != null)
            observationCountText.text = "Observations: 0";
            
        if (speciesCountText != null)
            speciesCountText.text = "Species: 0";
    }
    
    private void UpdateBiodiversityDisplay()
    {
        Vector3 playerPos = playerTransform.position;
        
        // Get Simpson's diversity data at player position
        float simpsonsIndex = biodiversityManager.GetBiodiversityAtPosition(playerPos);
        int observationCount = biodiversityManager.GetObservationCountAtPosition(playerPos);
        int speciesCount = biodiversityManager.GetSpeciesCountAtPosition(playerPos);
        
        // Update text displays
        if (simpsonsIndexText != null)
        {
            string indexText = $"Simpson's Index: {simpsonsIndex:F3}";
            if (simpsonsIndex > 0.8f)
                indexText += " (Very High Diversity)";
            else if (simpsonsIndex > 0.6f)
                indexText += " (High Diversity)";
            else if (simpsonsIndex > 0.3f)
                indexText += " (Moderate Diversity)";
            else if (simpsonsIndex > 0f)
                indexText += " (Low Diversity)";
            else
                indexText += " (No Data)";
                
            simpsonsIndexText.text = indexText;
        }
        
        if (observationCountText != null)
        {
            observationCountText.text = $"Total Observations: {observationCount}";
        }
        
        if (speciesCountText != null)
        {
            speciesCountText.text = $"Unique Species: {speciesCount}";
        }
    }
    
    private void OnDiversityIntensityChanged(float value)
    {
        // Apply global intensity to diversity visualization
        Shader.SetGlobalFloat("_DiversityIntensity", value);
        
        if (biodiversityManager != null)
        {
            // Force immediate update
            biodiversityManager.UpdateBiodiversityScores();
        }
    }
    
    private void OnBiodiversityToggled(bool enabled)
    {
        if (biodiversityManager != null)
        {
            biodiversityManager.enabled = enabled;
        }
        
        // Set global shader property to disable/enable effects
        Shader.SetGlobalFloat("_GlobalSaturation", enabled ? 1f : 1f);
    }
    
    private void OnRecalculatePressed()
    {
        if (biodiversityManager != null)
        {
            biodiversityManager.UpdateBiodiversityScores();
            Debug.Log("Biodiversity scores recalculated manually");
        }
    }
    
    public void ToggleUI()
    {
        uiVisible = !uiVisible;
        
        if (uiPanel != null)
            uiPanel.SetActive(uiVisible && showBiodiversityInfo);
    }
    
    public void SetUIVisibility(bool visible)
    {
        uiVisible = visible;
        
        if (uiPanel != null)
            uiPanel.SetActive(visible && showBiodiversityInfo);
    }
}