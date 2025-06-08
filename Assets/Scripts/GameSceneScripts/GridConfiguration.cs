using UnityEngine;

[System.Serializable]
public class GridConfiguration
{
    [Header("Grid Settings")]
    public int columns = 4;
    public int rows = 3;
    
    [Header("Spacing (Easy to Edit)")]
    public float columnSpacing = 1.5f;
    public float rowSpacing = 1.2f;
    
    [Header("Grid Position")]
    public float spawnZ = 30f;
    public float gridCenterY = 2f; // Adjust based on your track center height
    
    public Vector3 GetGridPosition(int column, int row)
    {
        // Calculate centered positions
        float startX = -(columns - 1) * columnSpacing * 0.5f;
        float startY = gridCenterY - (rows - 1) * rowSpacing * 0.5f;
        
        float x = startX + column * columnSpacing;
        float y = startY + row * rowSpacing;
        
        return new Vector3(x, y, spawnZ);
    }
    
    public bool IsValidGridPosition(int column, int row)
    {
        return column >= 0 && column < columns && row >= 0 && row < rows;
    }
}