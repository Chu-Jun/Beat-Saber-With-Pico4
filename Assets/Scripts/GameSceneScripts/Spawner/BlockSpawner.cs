using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public GameObject blockPrefab;
    public GridConfiguration gridConfig = new GridConfiguration();
    public float playerPositionZ = 0f;

    [Header("Miss Detection")]
    public float missThreshold = 0.5f;

    [Header("Timing Configuration")]
    public float beatToSecondsMultiplier = 60f;

    public event System.Action<BeatSaberBlock> OnBlockSpawned;
    public event System.Action<BeatSaberBlock> OnBlockMissed;

    private List<BeatSaberBlock> activeBlocks = new List<BeatSaberBlock>();
    
    private Coroutine spawnCoroutine;
    private Dictionary<BeatSaberBlock, Coroutine> monitorCoroutines = new Dictionary<BeatSaberBlock, Coroutine>();

    private float noteJumpMovementSpeed;
    private float songStartTime;
    private float currentBpm;


    public void StartSpawning(MapData mapData, float bpm, float noteJumpMovementSpeed, float noteJumpStartBeatOffset)
    {
        // Clean up existing blocks and coroutines
        StopSpawning();

        this.noteJumpMovementSpeed = noteJumpMovementSpeed;
        this.currentBpm = bpm;

        float spawnZ = CalculateSpawnZ(bpm, noteJumpMovementSpeed, noteJumpStartBeatOffset);
        spawnCoroutine = StartCoroutine(SpawnBlocksFromMap(mapData, bpm, spawnZ));
    }   

    private float CalculateSpawnZ(float bpm, float noteJumpMovementSpeed, float noteJumpStartBeatOffset)
    {
        float secondsPerBeat = beatToSecondsMultiplier / bpm;

        // Calculate how much time the block has to travel (from noteJumpStartBeatOffset)
        float travelTimeInBeats = noteJumpStartBeatOffset;
        float travelTimeInSeconds = travelTimeInBeats * secondsPerBeat;

        // Calculate spawn distance: how far the block travels in that time
        float spawnDistanceFromPlayer = noteJumpMovementSpeed * travelTimeInSeconds;

        // Spawn position is that distance ahead of the player
        return playerPositionZ + spawnDistanceFromPlayer;
    }

    public IEnumerator SpawnBlocksFromMap(MapData mapData, float bpm, float spawnZ)
    {
        if (mapData?._notes == null || mapData._notes.Count == 0)
        {
            Debug.LogError("No notes found in map data");
            yield break;
        }

        float songStartTime = (float)AudioSettings.dspTime;
        float secondsPerBeat = beatToSecondsMultiplier / bpm;

        // Calculate how long it takes for a block to travel from spawn to player
        float spawnToPlayerDistance = spawnZ - playerPositionZ;
        float travelTime = spawnToPlayerDistance / noteJumpMovementSpeed;

        for (int noteIndex = 0; noteIndex < mapData._notes.Count; noteIndex++)
        {
            MapData.Note note = mapData._notes[noteIndex];
            float noteReachTimeInSeconds = note._time * secondsPerBeat;

            // Calculate when to spawn the block (reach time - travel time)
            float spawnTimeInSeconds = noteReachTimeInSeconds - travelTime;

            float currentTime = (float)AudioSettings.dspTime - songStartTime;
            float timeToWait = spawnTimeInSeconds - currentTime;

            if (timeToWait > 0)
            {
                yield return new WaitForSeconds(timeToWait);
            }

            // Check if spawning was stopped during wait
            if (spawnCoroutine == null)
            {
                yield break;
            }

            SpawnBlock(note, spawnZ);
        }
    }


    private void SpawnBlock(MapData.Note noteData, float spawnZ)
    {
        Vector3 spawnPosition = gridConfig.GetGridPosition(noteData._lineIndex, noteData._lineLayer, spawnZ);

        // Check if position calculation failed
        if (spawnPosition == Vector3.zero)
        {
            Debug.LogWarning($"Skipping block spawn due to invalid position calculation for note at time {noteData._time}");
            return;
        }

        GameObject newBlock = Instantiate(blockPrefab, spawnPosition, Quaternion.identity);

        // Try to get component and validate
        if (!newBlock.TryGetComponent<BeatSaberBlock>(out BeatSaberBlock blockScript))
        {
            Debug.LogError("Block prefab missing BeatSaberBlock script!");
            Destroy(newBlock);
            return;
        }

        // Initialize block with validated data
        BlockData data = CreateBlockData(noteData);
        blockScript.Initialize(data, noteJumpMovementSpeed);

        // Set timing info for real-time beat calculation
        blockScript.SetTimingInfo(songStartTime, currentBpm);

        // Add to tracking collections
        activeBlocks.Add(blockScript);
        Coroutine monitorCoroutine = StartCoroutine(MonitorBlockForMiss(blockScript));
        monitorCoroutines[blockScript] = monitorCoroutine;

        // Notify listeners
        OnBlockSpawned?.Invoke(blockScript);
    }

    private BlockData CreateBlockData(MapData.Note noteData)
    {
        return new BlockData(
            (BlockData.BlockType)noteData._type,
            (BlockData.CutDirection)noteData._cutDirection,
            noteData._lineIndex,
            noteData._lineLayer,
            noteData._time
        );
    }

    public void StopSpawning()
    {
        // Stop main spawn coroutine
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        // Stop all monitoring coroutines
        StopAllMonitoringCoroutines();
    }

    public void CleanupActiveBlocks()
    {
        // Stop all monitoring coroutines
        StopAllMonitoringCoroutines();
        
        // Destroy all active blocks
        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            var block = activeBlocks[i];
            if (block != null && block.gameObject != null)
            {
                Destroy(block.gameObject);
            }
        }
        activeBlocks.Clear();
    }

    private void StopAllMonitoringCoroutines()
    {
        foreach (var kvp in monitorCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        monitorCoroutines.Clear();
    }

    private IEnumerator MonitorBlockForMiss(BeatSaberBlock block)
    {
        if (block == null)
        {
            yield break;
        }

        // Store the expected reach time for this block
        float expectedReachTime = Time.time + ((block.transform.position.z - playerPositionZ) / noteJumpMovementSpeed);

        // Subscribe to block slice event
        System.Action onBlockSliced = () =>
        {
            if (activeBlocks.Contains(block))
            {
                activeBlocks.Remove(block);
            }

            // Remove monitoring coroutine reference when block is sliced
            if (monitorCoroutines.ContainsKey(block))
            {
                monitorCoroutines.Remove(block);
            }
        };

        block.OnSliced += onBlockSliced;

        try
        {
            // Wait until the block should have reached the player
            yield return new WaitForSeconds(expectedReachTime - Time.time);

            // Give a small grace period for slicing
            yield return new WaitForSeconds(0.1f);

            // Handle miss case - check if block passed the player position and wasn't sliced
            if (block != null && !block.IsSliced && block.transform.position.z <= playerPositionZ)
            {
                activeBlocks.Remove(block);
                OnBlockMissed?.Invoke(block);
                Destroy(block.gameObject);
            }
        }
        finally
        {
            // Always clean up subscription and coroutine reference
            if (block != null)
            {
                block.OnSliced -= onBlockSliced;
            }

            if (monitorCoroutines.ContainsKey(block))
            {
                monitorCoroutines.Remove(block);
            }
        }
    }   

    private void OnDestroy()
    {
        // Stop all spawning operations
        StopSpawning();

        // Clean up all active blocks
        CleanupActiveBlocks();
        
        // Clear events to prevent memory leaks
        OnBlockSpawned = null;
        OnBlockMissed = null;
    }
}