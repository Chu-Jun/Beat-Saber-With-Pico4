using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject blockPrefab;
        
    [Header("Spawning Settings")]
    public float spawnInterval = 1f; // Time between spawns
    public bool autoSpawn = true;
    
    [Header("Grid Configuration")]
    public GridConfiguration gridConfig = new GridConfiguration();

    [Header("Future Beat Map Integration")]
    [Tooltip("This will be used for custom Beat Saber maps")]
    public List<BlockData> customPattern = new List<BlockData>();
    
    private float nextSpawnTime = 0f;
    private int currentPatternIndex = 0;
    
    void Start()
    {
        if (autoSpawn)
        {
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    void Update()
    {
        if (autoSpawn && Time.time >= nextSpawnTime)
        {
            SpawnRandomBlock();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    public void SpawnRandomBlock()
    {
        // Generate random block data
        BlockData.BlockType randomType = (BlockData.BlockType)Random.Range(0, 2);
        BlockData.CutDirection randomDirection = (BlockData.CutDirection)Random.Range(0, 9);
        int randomColumn = Random.Range(0, gridConfig.columns);
        int randomRow = Random.Range(0, gridConfig.rows);
        
        BlockData blockData = new BlockData(randomType, randomDirection, randomColumn, randomRow, Time.time);
        SpawnBlock(blockData);
    }
    
    public void SpawnBlock(BlockData blockData)
    {
        if (!gridConfig.IsValidGridPosition(blockData.column, blockData.row))
        {
            Debug.LogWarning($"Invalid grid position: Column {blockData.column}, Row {blockData.row}");
            Debug.LogWarning($"Grid position is zero-indexed, ensure values are within range.");
            return;
        }
        
        // Get spawn position from grid
        Vector3 spawnPosition = gridConfig.GetGridPosition(blockData.column, blockData.row);
        
        // Instantiate block
        GameObject newBlock = Instantiate(blockPrefab, spawnPosition, Quaternion.identity);
        
        // Initialize block
        BeatSaberBlock blockScript = newBlock.GetComponent<BeatSaberBlock>();
        if (blockScript != null)
        {
            blockScript.Initialize(blockData);
        }
        else
        {
            Debug.LogError("Block prefab missing BeatSaberBlock script!");
        }
    }
    
    // Method for future Beat Map integration
    public void LoadCustomPattern(List<BlockData> pattern)
    {
        customPattern = pattern;
        currentPatternIndex = 0;
    }
    
    public void SpawnFromPattern()
    {
        if (customPattern.Count > 0 && currentPatternIndex < customPattern.Count)
        {
            SpawnBlock(customPattern[currentPatternIndex]);
            currentPatternIndex++;
        }
    }
    
    // Manual spawning methods for testing
    [ContextMenu("Spawn Test Red Block - Down")]
    public void SpawnTestRedBlockDown()
    {
        BlockData testBlock = new BlockData(BlockData.BlockType.Red, BlockData.CutDirection.Down, 1, 1, Time.time);
        SpawnBlock(testBlock);
    }

    [ContextMenu("Spawn Test Red Block - Left")]
    public void SpawnTestRedBlockLeft()
    {
        BlockData testBlock = new BlockData(BlockData.BlockType.Red, BlockData.CutDirection.Left, 1, 1, Time.time);
        SpawnBlock(testBlock);
    }
    
    [ContextMenu("Spawn Test Blue Block - Up")]
    public void SpawnTestBlueBlockUp()
    {
        BlockData testBlock = new BlockData(BlockData.BlockType.Blue, BlockData.CutDirection.Up, 2, 1, Time.time);
        SpawnBlock(testBlock);
    }

    [ContextMenu("Spawn Test Blue Block - Right")]
    public void SpawnTestBlueBlockRight()
    {
        BlockData testBlock = new BlockData(BlockData.BlockType.Blue, BlockData.CutDirection.Right, 2, 1, Time.time);
        SpawnBlock(testBlock);
    }
}