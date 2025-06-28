using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject blockPrefab;

    [Header("Grid Configuration")]
    public GridConfiguration gridConfig = new GridConfiguration();

    [Header("Audio")]
    [Tooltip("The AudioSource that will play the song.")]
    public AudioSource songAudioSource;

    [Header("Map Configuration")]
    public string mapDirectoryPath = "Assets/CustomMaps/HeavyIsTheCrown";
    public int difficultyLevel = 0; // 0 for Easy, 1 for Normal, etc.

    [Header("Game Synchronization")]
    [Tooltip("The Z-coordinate where the player is expected to cut the blocks. Adjust this based on your player's platform/saber range.")]
    public float playerCuttingZ = 0f; // Default cutting plane Z position

    [Header("UI Manager Reference")]
    [SerializeField]
    private BeatSaberUIManager uiManager;

    private AudioClip songClip;
    private InfoData infoData;
    private MapData mapData;
    private float bpm;
    private float currentNoteJumpMovementSpeed;
    private float currentNoteJumpStartBeatOffset;

    void Start()
    {
        // Start the master coroutine to load everything and then play
        StartCoroutine(LoadAndStartSong());
    }

    private IEnumerator LoadAndStartSong()
    {
        yield return StartCoroutine(LoadMapData());

        if (mapData != null && songClip != null)
        {
            // Assign the loaded clip and play the music
            songAudioSource.clip = songClip;
            songAudioSource.Play();

            // Start spawning blocks in sync with the audio
            StartCoroutine(SpawnBlocksFromMap());
        }
        else
        {
            Debug.LogError("Failed to load map data or song clip. Spawning cancelled.");
        }
    }

    private IEnumerator LoadMapData()
    {
        // 1. Load Info.dat
        string infoPath = Path.Combine(mapDirectoryPath, "Info.dat");
        if (!File.Exists(infoPath))
        {
            Debug.LogError("Info.dat or Info.txt not found in the specified directory: " + mapDirectoryPath);
            yield break;
        }

        string infoJson = File.ReadAllText(infoPath);
        infoData = JsonUtility.FromJson<InfoData>(infoJson);
        bpm = infoData._beatsPerMinute;

        // 2. Load Audio Clip
        string songFileName = infoData._songFilename;
        string oggFileName = Path.ChangeExtension(songFileName, ".ogg");
        string audioPath = Path.Combine(mapDirectoryPath, oggFileName);

        // Use UnityWebRequestMultimedia to load audio from a path
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + Path.GetFullPath(audioPath), AudioType.OGGVORBIS))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error loading audio file: " + uwr.error);
            }
            else
            {
                songClip = DownloadHandlerAudioClip.GetContent(uwr);
                Debug.Log("Audio clip loaded successfully!");
            }
        }

        // 3. Load Difficulty Map
        if (infoData._difficultyBeatmapSets.Count > 0 && infoData._difficultyBeatmapSets[0]._difficultyBeatmaps.Count > difficultyLevel)
        {
            DifficultyBeatmap selectedBeatmap = infoData._difficultyBeatmapSets[0]._difficultyBeatmaps[difficultyLevel];
            string mapFileName = selectedBeatmap._beatmapFilename;

            // Store the note jump movement speed and offset
            currentNoteJumpMovementSpeed = selectedBeatmap._noteJumpMovementSpeed;
            currentNoteJumpStartBeatOffset = selectedBeatmap._noteJumpStartBeatOffset;

            Debug.Log($"Loaded Note Jump Movement Speed: {currentNoteJumpMovementSpeed}");
            Debug.Log($"Loaded Note Jump Start Beat Offset: {currentNoteJumpStartBeatOffset}");

            string mapPath = Path.Combine(mapDirectoryPath, mapFileName);
            if (!File.Exists(mapPath))
            {
                Debug.LogError("Map file not found: " + mapFileName);
                yield break;
            }

            string mapJson = File.ReadAllText(mapPath);

            // For v3 compatibility
            if (mapJson.Contains("\"colorNotes\":"))
            {
                mapJson = mapJson.Replace("\"colorNotes\":", "\"_notes\":");
                mapJson = mapJson.Replace("\"b\":", "\"_time\":");
                mapJson = mapJson.Replace("\"x\":", "\"_lineIndex\":");
                mapJson = mapJson.Replace("\"y\":", "\"_lineLayer\":");
                mapJson = mapJson.Replace("\"c\":", "\"_type\":");
                mapJson = mapJson.Replace("\"d\":", "\"_cutDirection\":");
            }
            mapData = JsonUtility.FromJson<MapData>(mapJson);
        }
        else
        {
            Debug.LogError("Selected difficulty level not found in Info.dat.");
        }
    }

    private IEnumerator SpawnBlocksFromMap()
    {
        if (mapData._notes == null || mapData._notes.Count == 0)
        {
            Debug.LogError("No notes found in the map data. Check JSON parsing.");
            yield break;
        }

        // Use the audio source's time for more accurate synchronization
        float songStartTime = (float)AudioSettings.dspTime;
        int noteIndex = 0;

        while (noteIndex < mapData._notes.Count)
        {
            MapData.Note note = mapData._notes[noteIndex];
            float noteTimeInSeconds = note._time * (60f / bpm);
            float timeToWait = noteTimeInSeconds - ((float)AudioSettings.dspTime - songStartTime);

            if (timeToWait > 0)
            {
                yield return new WaitForSeconds(timeToWait);
            }

            SpawnBlock(note);
            noteIndex++;
        }
    }

    private void SpawnBlock(MapData.Note noteData)
    {
        // Calculate the required travel time based on NJSO and BPM
        float travelTimeSeconds = currentNoteJumpStartBeatOffset * (60f / bpm);

        // Calculate the distance blocks need to travel from spawn to cut plane
        float spawnDistanceFromCut = currentNoteJumpMovementSpeed * travelTimeSeconds;

        // Determine the actual spawn Z-coordinate
        float calculatedSpawnZ = playerCuttingZ + spawnDistanceFromCut;

        // Get the full spawn position
        // Passing the calculated Z-coordinate to GridConfiguration's GetGridPosition method
        Vector3 spawnPosition = gridConfig.GetGridPosition(noteData._lineIndex, noteData._lineLayer, calculatedSpawnZ);

        GameObject newBlock = Instantiate(blockPrefab, spawnPosition, Quaternion.identity);
        BeatSaberBlock blockScript = newBlock.GetComponent<BeatSaberBlock>();
        if (blockScript != null)
        {
            BlockData data = new BlockData(
                (BlockData.BlockType)noteData._type,
                (BlockData.CutDirection)noteData._cutDirection,
                noteData._lineIndex,
                noteData._lineLayer,
                noteData._time
            );

            // Pass the loaded speed and offset to the block's Initialize method
            blockScript.Initialize(data, currentNoteJumpMovementSpeed, currentNoteJumpStartBeatOffset);

            // UI Integration: Subscribe to the block's miss event
            // This assumes your BeatSaberBlock.cs has an event for when it is missed.
            // You will need to implement this event in BeatSaberBlock.cs if it doesn't exist.
            // Example: blockScript.OnMissed += HandleBlockMissed;
            // For now, I'll add a simple mechanism to check if the block was cut.
            StartCoroutine(CheckForMiss(blockScript, calculatedSpawnZ, playerCuttingZ));
        }
        else
        {
            Debug.LogError("Block prefab missing BeatSaberBlock script!");
        }
    }

    // UI Integration: New Coroutine to detect missed blocks
    // This coroutine will check if a block has passed the player cutting Z without being sliced.
    // This is a placeholder and assumes BeatSaberBlock has a public 'isSliced' property.
    // A more robust solution would involve the BeatSaberBlock itself reporting its miss status.
    private IEnumerator CheckForMiss(BeatSaberBlock block, float spawnZ, float cutZ)
    {
        // Wait until the block has passed the cutting Z
        // Adjust the condition based on the block's movement direction (assuming Z-forward towards player)
        while (block != null && block.transform.position.z > (cutZ - 0.5f)) // Give a small buffer
        {
            yield return null; // Wait for next frame
        }

        // If the block still exists and was not sliced (assuming a property like 'IsSliced' on BeatSaberBlock)
        // If your BeatSaberBlock doesn't have IsSliced, you'll need to add it or an equivalent.
        if (block != null && !block.IsSliced)
        {
            uiManager?.OnNoteMiss();
            Debug.Log($"Block missed at Z: {block.transform.position.z}");
            Destroy(block.gameObject); // Destroy the missed block
        }
    }
}