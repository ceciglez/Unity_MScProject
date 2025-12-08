using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller with Start, Exit, About, and Credits sections
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject aboutPanel;
    public GameObject creditsPanel;
    public GameObject controlsPanel;
    
    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button aboutButton;
    public Button creditsButton;
    public Button controlsButton;
    public Button exitButton;
    
    [Header("Back Buttons")]
    public Button aboutBackButton;
    public Button creditsBackButton;
    public Button controlsBackButton;
    
    [Header("About Text")]
    public Text aboutText;
    public Text creditsText;
    
    [Header("Audio")]
    public AudioSource buttonClickSound;

    void Start()
    {
        // Setup button listeners
        if (startButton) startButton.onClick.AddListener(StartGame);
        if (aboutButton) aboutButton.onClick.AddListener(ShowAbout);
        if (creditsButton) creditsButton.onClick.AddListener(ShowCredits);
        if (controlsButton) controlsButton.onClick.AddListener(ShowControls);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);
        
        if (aboutBackButton) aboutBackButton.onClick.AddListener(ShowMainMenu);
        if (creditsBackButton) creditsBackButton.onClick.AddListener(ShowMainMenu);
        if (controlsBackButton) controlsBackButton.onClick.AddListener(ShowMainMenu);
        
        // Set initial state
        ShowMainMenu();
        
        // Setup text content
        SetupTextContent();
    }
    
    private void SetupTextContent()
    {
        if (aboutText)
        {
            aboutText.text = "iNaturalist Network Visualization\\n\\n" +
                           "An interactive 3D visualization of biodiversity observations from the iNaturalist platform. " +
                           "Explore real-world nature observations and discover connections between different species and locations.\\n\\n" +
                           "This project demonstrates spatial relationships in biodiversity data through dynamic network connections.";
        }
        
        if (creditsText)
        {
            creditsText.text = "Credits\\n\\n" +
                             "Data Source: iNaturalist.org\\n" +
                             "Mapping: Mapbox\\n\\n" +
                             "Special Thanks:\\n" +
                             "• iNaturalist community for observations\\n" +
                             "• Unity Technologies\\n" +
                             "• Mapbox for mapping services\\n\\n" +
                             "Developed for academic research purposes.";
        }
    }
    
    public void StartGame()
    {
        PlayButtonSound();
        
        // Hide menu and start game
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        
        // Enable game components
        EnableGameComponents();
        
        // Show controls hint briefly
        if (controlsPanel)
        {
            StartCoroutine(ShowControlsHintBriefly());
        }
    }
    
    public void ShowAbout()
    {
        PlayButtonSound();
        HideAllPanels();
        if (aboutPanel) aboutPanel.SetActive(true);
    }
    
    public void ShowCredits()
    {
        PlayButtonSound();
        HideAllPanels();
        if (creditsPanel) creditsPanel.SetActive(true);
    }
    
    public void ShowControls()
    {
        PlayButtonSound();
        HideAllPanels();
        if (controlsPanel) controlsPanel.SetActive(true);
    }
    
    public void ShowMainMenu()
    {
        PlayButtonSound();
        HideAllPanels();
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }
    
    public void ExitGame()
    {
        PlayButtonSound();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void HideAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (aboutPanel) aboutPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        if (controlsPanel) controlsPanel.SetActive(false);
    }
    
    private void EnableGameComponents()
    {
        // Enable player controller
        var playerController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
        if (playerController) playerController.enabled = true;
        
        // Enable camera controller
        var cameraController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterCamera>();
        if (cameraController) cameraController.enabled = true;
        
        // Enable network manager
        var networkManager = FindObjectOfType<ObservationNetworkManager>();
        if (networkManager) networkManager.enabled = true;
        
        // Enable map controller
        var mapController = FindObjectOfType<INaturalistMapController>();
        if (mapController) mapController.enabled = true;
    }
    
    private System.Collections.IEnumerator ShowControlsHintBriefly()
    {
        if (controlsPanel)
        {
            controlsPanel.SetActive(true);
            yield return new WaitForSeconds(3f);
            controlsPanel.SetActive(false);
        }
    }
    
    private void PlayButtonSound()
    {
        if (buttonClickSound)
        {
            buttonClickSound.Play();
        }
    }
}