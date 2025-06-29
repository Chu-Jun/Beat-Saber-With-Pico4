using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Configuration")]
    public string mapDirectoryPath = "Assets/CustomMaps/HeavyIsTheCrown";
    public int difficultyLevel = 0;
    public bool autoStartOnLoad = true;

    [Header("Dependencies")]
    [SerializeField] private MapLoader mapLoader;
    [SerializeField] private BlockSpawner blockSpawner;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private UIManager uiManager;    

    // Events for external systems
    public event System.Action OnMapLoadStarted;
    public event System.Action OnMapLoadCompleted;
    public event System.Action<string> OnMapLoadFailed;
    public event System.Action OnGameStarted;
    public event System.Action OnGameCompleted;

    private MapLoader.MapLoadResult currentMapData;
    private bool isGameActive = false;
    private bool isGameCompleted = false;

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
        // Auto-find dependencies if not assigned
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
                return;
            }
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("GameManager: AudioSource component required!");
                return;
            }
        }

        // Subscribe to spawning events
        if (blockSpawner != null)
        {
            blockSpawner.OnBlockMissed += HandleBlockMissed;
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
        // Validate map data
        if (currentMapData?.Success != true)
        {
            Debug.LogError("Cannot start game: No valid map data loaded");
            return;
        }

        // Validate audio clip
        if (currentMapData.AudioClip == null)
        {
            Debug.LogError("Cannot start game: No audio clip loaded");
            return;
        }

        // Check if game is already active
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
        OnGameStarted?.Invoke();

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
        // Early exit if no game state to stop
        if (!isGameActive && !isGameCompleted) return;

        Debug.Log("Stopping game...");

        // Reset game state
        isGameActive = false;
        isGameCompleted = false;
        
        // Stop all game systems
        StopAllCoroutines();
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        if (blockSpawner != null)
        {
            blockSpawner.CleanupActiveBlocks();
        }
        
        // Notify UI
        // uiManager?.OnGameStopped();
        
        Debug.Log("Game stopped successfully");
    }

    private void CompleteGame()
    {
        if (isGameCompleted) return;
        
        Debug.Log("Ending game...");

        // End game state
        isGameCompleted = true;
        isGameActive = false;

        // Stop all game systems
        StopAllCoroutines();
        
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        if (blockSpawner != null)
        {
            blockSpawner.CleanupActiveBlocks();
        }
        
        // Notify UI
        // uiManager?.OnGameComplete();
        OnGameCompleted?.Invoke();
        
        Debug.Log("Game completed successfully!");
    }

    private void HandleBlockMissed(BeatSaberBlock block)
    {
        // Notify UI
        uiManager?.OnNoteMiss();
        // Debug.Log($"Block missed at position: {block.transform.position}");
    }

    void OnDestroy()
    {
        // Stop game if still active
        if (isGameActive)
        {
            StopGame();
        }

        // Unsubscribe from all events to prevent memory leaks
        if (blockSpawner != null)
        {
            blockSpawner.OnBlockMissed -= HandleBlockMissed;
        }
        
        // Clear all event subscribers
        OnMapLoadStarted = null;
        OnMapLoadCompleted = null;
        OnMapLoadFailed = null;
        OnGameStarted = null;
        OnGameCompleted = null;
    }
}