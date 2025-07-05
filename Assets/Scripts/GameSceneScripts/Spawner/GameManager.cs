using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Required for Button component
using TMPro; // Required for TextMeshProUGUI

/// <summary>
/// GameManager handles the core game flow including map loading, game state management, and UI transitions.
///
/// EndGame UI System:
/// - Uses EndGamePrefab instead of UIManager's game over canvas
/// - Instantiates EndGamePrefab as a child of MainCamera when game ends
/// - Automatically positions the UI 2 units in front of the camera
/// - Cleans up EndGame UI when starting new games or changing scenes
/// - Falls back to UIManager game over screen if EndGamePrefab is not assigned
///
/// Setup Requirements:
/// - Assign EndGamePrefab in inspector (Canvas prefab)
/// - Assign MainCamera in inspector (will auto-find Camera.main if not assigned)
/// - Ensure the EndGamePrefab contains a TextMeshProUGUI object named "FinalScoreText" (or your chosen name) for displaying the score.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Map Configuration")]
    [SerializeField] private string mapDirectoryPath = "Assets/CustomMaps/HeavyIsTheCrown";
    [SerializeField] private int difficultyLevel = 0;
    [SerializeField] private bool autoStartOnLoad = true;

    [Header("Component Dependencies")]
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private BlockSpawner blockSpawner;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private UIManager uiManager;
   
    [Header("End Game UI")]
    [SerializeField] private GameObject endGamePrefab;
    [SerializeField] private Camera mainCamera;
    // Removed: [SerializeField] private TextMeshProUGUI endGameScoreText; // No longer needed as we find it dynamically
   
    // Events for external systems
    public event System.Action OnMapLoadStarted;
    public event System.Action OnMapLoadCompleted;
    public event System.Action<string> OnMapLoadFailed;
    public event System.Action OnGameStarted;
    public event System.Action OnGameCompleted;

    private MapLoader.MapLoadResult currentMapData;
    private bool isGameActive = false;
    private bool isGameCompleted = false;
    private GameObject instantiatedEndGameUI;

    // Public getters for external systems
    public bool IsGameActive => isGameActive;
    public float CurrentBPM => currentMapData?.BPM ?? 0f;
    public bool IsMapLoaded => currentMapData?.Success == true;

    void Start()
    {
        InitializeDependencies();
       
        if (autoStartOnLoad)
        {
            LoadMap();
        }
    }
   
    private void InitializeDependencies()
    {
        // Auto-find and validate dependencies
        if (mapLoader == null)
            mapLoader = GetComponent<MapLoader>() ?? gameObject.AddComponent<MapLoader>();
       
        if (blockSpawner == null)
            blockSpawner = GetComponent<BlockSpawner>() ?? gameObject.AddComponent<BlockSpawner>();
       
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("GameManager: UIManager not found in scene!");
            }
        }
       
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("GameManager: AudioSource component required!");
            }
        }
       
        // Find MainCamera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("GameManager: MainCamera not found! Please assign MainCamera in inspector.");
            }
        }
       
        // Validate EndGamePrefab
        if (endGamePrefab == null)
        {
            Debug.LogError("GameManager: EndGamePrefab not assigned! Please assign the EndGamePrefab in inspector.");
        }

        // Subscribe to events
        if (blockSpawner != null)
        {
            blockSpawner.OnBlockMissed += HandleBlockMissed;
        }
       
        if (uiManager != null)
        {
            uiManager.OnPlayerHealthDepleted += HandlePlayerHealthDepleted;
        }
    }

    public void LoadMap(string mapPath = null, int difficulty = -1)
    {
        if (isGameActive)
        {
            StopGame();
        }

        string targetPath = mapPath ?? mapDirectoryPath;
        int targetDifficulty = difficulty >= 0 ? difficulty : difficultyLevel;

        StartCoroutine(LoadMapCoroutine(targetPath, targetDifficulty));
    }

    private IEnumerator LoadMapCoroutine(string mapPath, int difficulty)
    {
        OnMapLoadStarted?.Invoke();
       
        yield return StartCoroutine(mapLoader.LoadMapAsync(mapPath, difficulty));
        currentMapData = mapLoader.GetLastResult();

        if (currentMapData != null && currentMapData.Success)
        {
            if (audioSource != null && currentMapData?.AudioClip != null)
            {
                audioSource.clip = currentMapData.AudioClip;
            }
            OnMapLoadCompleted?.Invoke();
            Debug.Log($"Map loaded successfully: BPM={currentMapData.BPM}, Notes={currentMapData.MapData._notes.Count}");

            StartGame();
        }
        else
        {
            string errorMsg = currentMapData?.ErrorMessage ?? "Unknown error occurred";
            OnMapLoadFailed?.Invoke(errorMsg);
            Debug.LogError($"Failed to load map: {errorMsg}");
        }
    }

    public void StartGame()
    {
        // Validate requirements
        if (currentMapData?.Success != true)
        {
            Debug.LogError("Cannot start game: No valid map data loaded");
            return;
        }

        if (currentMapData.AudioClip == null)
        {
            Debug.LogError("Cannot start game: No audio clip loaded");
            return;
        }

        if (isGameActive)
        {
            Debug.LogWarning("Game is already active");
            return;
        }

        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        isGameActive = true;
        isGameCompleted = false;
       
        // Clean up any existing end game UI
        if (instantiatedEndGameUI != null)
        {
            Destroy(instantiatedEndGameUI);
            instantiatedEndGameUI = null;
        }
       
        OnGameStarted?.Invoke();

        // Reset UI state for new game
        if (uiManager != null)
        {
            uiManager.ResetGameState();
        }

        // Start audio
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogError("Cannot start audio: AudioSource or clip is null");
            StopGame();
            yield break;
        }
       
        // Start spawning blocks
        if (blockSpawner != null)
        {
            blockSpawner.StartSpawning(
                currentMapData.MapData,
                currentMapData.BPM,
                currentMapData.NoteJumpMovementSpeed,
                currentMapData.NoteJumpStartBeatOffset
            );
        }
        else
        {
            Debug.LogError("Cannot spawn blocks: BlockSpawner is null");
            StopGame();
            yield break;
        }

        // Wait for audio to finish playing
        while (audioSource != null && audioSource.isPlaying && isGameActive)
        {
            yield return null;
        }

        // Only complete if game wasn't manually stopped
        if (isGameActive)
        {
            CompleteGame();
        }
    }

    public void StopGame()
    {
        if (!isGameActive && !isGameCompleted) return;

        Debug.Log("Stopping game...");

        isGameActive = false;
        isGameCompleted = false;
       
        StopAllCoroutines();
       
        if (audioSource != null)
        {
            audioSource.Stop();
        }
       
        if (blockSpawner != null)
        {
            blockSpawner.StopSpawning();
            blockSpawner.CleanupActiveBlocks();
        }
       
        // Show EndGame UI instead of UIManager game over screen
        ShowEndGameUI();
       
        Debug.Log("Game stopped successfully");
    }

    private void CompleteGame()
    {
        if (isGameCompleted) return;
       
        Debug.Log("Game completed successfully!");

        isGameCompleted = true;
        isGameActive = false;

        StopAllCoroutines();
       
        if (audioSource != null)
        {
            audioSource.Stop();
        }
       
        if (blockSpawner != null)
        {
            blockSpawner.StopSpawning();
            blockSpawner.CleanupActiveBlocks();
        }

        OnGameCompleted?.Invoke();
       
        // Show EndGame UI instead of UIManager game over screen
        ShowEndGameUI();
    }
   
    private void ShowEndGameUI()
    {
        // Get final score from UIManager if available
        int finalScore = 0;
        if (uiManager != null)
        {
            finalScore = uiManager.GetCurrentScore();
        }
       
        // Clean up any existing end game UI
        if (instantiatedEndGameUI != null)
        {
            Destroy(instantiatedEndGameUI);
        }
       
        // Instantiate end game UI prefab
        if (endGamePrefab != null && mainCamera != null)
        {
            instantiatedEndGameUI = Instantiate(endGamePrefab);
           
            // Set the parent to the main camera
            instantiatedEndGameUI.transform.SetParent(mainCamera.transform, false);
           
            // Position the UI in front of the camera
            instantiatedEndGameUI.transform.localPosition = Vector3.forward;
            instantiatedEndGameUI.transform.localRotation = Quaternion.identity;
           
            Debug.Log($"EndGame UI instantiated with final score: {finalScore}");

            // --- START MODIFIED CODE FOR BUTTON AND SCORE DISPLAY ---

            // Find the button intended to send the user back to the main menu.
            // Replace "MainMenuButton" with the actual name of your button GameObject within the prefab.
            // If the button is a direct child of the root of the prefab:
            Transform mainMenuButtonTransform = instantiatedEndGameUI.transform.Find("MainMenuButton");
            Button mainMenuButton = null;
            if (mainMenuButtonTransform != null)
            {
                mainMenuButton = mainMenuButtonTransform.GetComponent<Button>();
            }
            else
            {
                // Fallback: If not a direct child, try GetComponentInChildren (less specific but might work)
                mainMenuButton = instantiatedEndGameUI.GetComponentInChildren<Button>();
            }

            if (mainMenuButton != null)
            {
                // Attach the GoToMainMenu function to the button's onClick event.
                mainMenuButton.onClick.AddListener(GoToMainMenu);
                Debug.Log("GoToMainMenu function attached to EndGame UI button.");
            }
            else
            {
                Debug.LogWarning("GameManager: Main Menu Button not found in EndGame UI prefab. Please ensure it has a Button component and is accessible and its name matches 'MainMenuButton' or adjust the code accordingly.");
            }

            // Find the TextMeshProUGUI object for displaying the final score.
            // IMPORTANT: Replace "FinalScoreText" with the actual name of your TextMeshProUGUI GameObject in your prefab.
            Transform scoreTextTransform = instantiatedEndGameUI.transform.Find("FinalScoreText");
            TextMeshProUGUI scoreDisplay = null;

            if (scoreTextTransform != null)
            {
                scoreDisplay = scoreTextTransform.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                // Fallback: If not a direct child, try GetComponentInChildren (less specific but might work)
                scoreDisplay = instantiatedEndGameUI.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (scoreDisplay != null)
            {
                scoreDisplay.text = $"Game Over\nFinal Score\n{finalScore:N0}"; // Format the score nicely
                Debug.Log($"EndGame UI score updated to: {finalScore}");
            }
            else
            {
                Debug.LogWarning("GameManager: End Game Score TextMeshProUGUI not found in instantiated EndGame UI prefab. Please ensure it has a TextMeshProUGUI component and its name matches 'FinalScoreText' or adjust the code accordingly.");
            }
            // --- END MODIFIED CODE FOR BUTTON AND SCORE DISPLAY ---
        }
        else
        {
            Debug.LogError("EndGamePrefab or MainCamera not assigned! Cannot show end game UI.");
           
            // Fallback to UIManager if EndGamePrefab is not available
            if (uiManager != null)
            {
                uiManager.ShowGameOverScreen(finalScore);
                Debug.Log("Falling back to UIManager game over screen");
            }
        }
    }

    private void HandleBlockMissed(BeatSaberBlock block)
    {
        uiManager?.OnNoteMiss();
    }

    private void HandlePlayerHealthDepleted()
    {
        Debug.Log("Player health depleted - triggering game over");
       
        // Trigger controller mode switch via event
        OnGameCompleted?.Invoke();
       
        StopGame();
    }

    // Public methods for UI buttons
    public void RestartCurrentLevel()
    {
        // Clean up EndGame UI before restarting
        if (instantiatedEndGameUI != null)
        {
            Destroy(instantiatedEndGameUI);
            instantiatedEndGameUI = null;
        }
       
        if (currentMapData?.Success == true)
        {
            StartGame();
        }
        else
        {
            Debug.LogWarning("No valid map data to restart");
            LoadMap();
        }
    }
   
    public void GoToMainMenu()
    {
        // Clean up EndGame UI before going to main menu
        if (instantiatedEndGameUI != null)
        {
            Destroy(instantiatedEndGameUI);
            instantiatedEndGameUI = null;
        }
       
        StopGame();
        SceneManager.LoadScene("MainMenu");
    }
   
    /// <summary>
    /// Public method to manually hide the EndGame UI
    /// </summary>
    public void HideEndGameUI()
    {
        if (instantiatedEndGameUI != null)
        {
            Destroy(instantiatedEndGameUI);
            instantiatedEndGameUI = null;
            Debug.Log("EndGame UI manually hidden");
        }
    }

    void OnDestroy()
    {
        if (isGameActive)
        {
            StopGame();
        }
       
        // Clean up instantiated EndGame UI
        if (instantiatedEndGameUI != null)
        {
            Destroy(instantiatedEndGameUI);
            instantiatedEndGameUI = null;
        }

        // Unsubscribe from all events to prevent memory leaks
        if (blockSpawner != null)
        {
            blockSpawner.OnBlockMissed -= HandleBlockMissed;
        }
       
        if (uiManager != null)
        {
            uiManager.OnPlayerHealthDepleted -= HandlePlayerHealthDepleted;
        }
       
        // Clear all event subscribers
        OnMapLoadStarted = null;
        OnMapLoadCompleted = null;
        OnMapLoadFailed = null;
        OnGameStarted = null;
        OnGameCompleted = null;
    }
}
