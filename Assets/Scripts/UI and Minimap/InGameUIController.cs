using UnityEngine;
using UnityEngine.UI;

public class InGameUIController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject controlsHelpPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameUI;
    
    [Header("Controls Help")]
    public Text controlsText;
    public Button toggleControlsButton;
    public KeyCode toggleControlsKey = KeyCode.H;
    
    [Header("Pause Menu")]
    public Button resumeButton;
    public Button mainMenuButton;
    public Button exitButton;
    
    [Header("Game UI Elements")]
    public Text instructionsText;
    public Text networkStatusText;
    
    private bool isPaused = false;
    private bool controlsVisible = false;
    private MainMenuManager mainMenu;

    void Start()
    {
        // Find main menu manager
        mainMenu = FindObjectOfType<MainMenuManager>();
        
        // Setup button listeners
        if (toggleControlsButton) toggleControlsButton.onClick.AddListener(ToggleControls);
        if (resumeButton) resumeButton.onClick.AddListener(ResumeGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (exitButton) exitButton.onClick.AddListener(ExitGame);
        
        // Setup controls text
        SetupControlsText();
        
        // Setup initial UI state
        SetupInitialState();
    }
    
    void Update()
    {
        // Handle escape key for pause menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
        
        // Handle help toggle
        if (Input.GetKeyDown(toggleControlsKey))
        {
            ToggleControls();
        }
        
        // Update network status
        UpdateNetworkStatus();
    }
    
    private void SetupControlsText()
    {
        if (controlsText)
        {
            controlsText.text = 
                "🎮 CONTROLS\\n\\n" +
                "Movement:\\n" +
                "• WASD - Move forward/back/left/right\\n" +
                "• Mouse - Look around\\n" +
                "• Shift - Run faster\\n" +
                "• Space - Jump\\n\\n" +
                "Network Visualization:\\n" +
                "• N - Toggle 3D network connections\\n" +
                "• L - Toggle 2D minimap connections\\n\\n" +
                "Biodiversity Visualization:\\n" +
                "• B - Toggle biodiversity display\\n\\n" +
                "Interface:\\n" +
                "• H - Toggle this help panel\\n" +
                "• ESC - Pause menu\\n\\n" +
                "💡 Get close to observations to see details!\\n" +
                "🌱 Areas with higher biodiversity appear more colorful!";
        }
        
        if (instructionsText)
        {
            instructionsText.text = "Press H for controls • Walk near observations to explore • Press N for networks • Press B for biodiversity";
        }
    }
    
    private void SetupInitialState()
    {
        // Hide all panels initially
        if (controlsHelpPanel) controlsHelpPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        
        // Show game UI
        if (gameUI) gameUI.SetActive(true);
        
        // Set initial states
        isPaused = false;
        controlsVisible = false;
        Time.timeScale = 1f;
    }
    
    public void ToggleControls()
    {
        controlsVisible = !controlsVisible;
        if (controlsHelpPanel) controlsHelpPanel.SetActive(controlsVisible);
    }
    
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuPanel) pauseMenuPanel.SetActive(true);
        if (gameUI) gameUI.SetActive(false);
        
        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameUI) gameUI.SetActive(true);
        
        // Lock cursor for game play
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        
        if (mainMenu)
        {
            // Disable game components
            DisableGameComponents();
            
            // Show main menu
            mainMenu.ShowMainMenu();
            
            // Hide in-game UI
            if (gameUI) gameUI.SetActive(false);
            if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
            if (controlsHelpPanel) controlsHelpPanel.SetActive(false);
        }
    }
    
    public void ExitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    private void DisableGameComponents()
    {
        // Disable player controller
        var playerController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterController>();
        if (playerController) playerController.enabled = false;
        
        // Disable camera controller
        var cameraController = FindObjectOfType<KinematicCharacterController.Examples.ExampleCharacterCamera>();
        if (cameraController) cameraController.enabled = false;
        
        // Clear network connections
        var networkManager = FindObjectOfType<ObservationNetworkManager>();
        if (networkManager)
        {
            networkManager.ClearAllConnections();
        }
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void UpdateNetworkStatus()
    {
        if (networkStatusText)
        {
            var networkManager = FindObjectOfType<ObservationNetworkManager>();
            if (networkManager)
            {
                int activeConnections = networkManager.GetActiveConnectionCount();
                networkStatusText.text = $"Network: {activeConnections} connections";
            }
        }
    }
}