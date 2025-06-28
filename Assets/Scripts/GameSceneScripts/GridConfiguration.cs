using UnityEngine;

[System.Serializable]
public class GridConfiguration
{
    [Header("Grid Settings")]
    public int columns = 4;
    public int rows = 3;
    
    [Header("Grid Spacing")]
    public float columnSpacing = 1f;
    public float rowSpacing = 1f;
    
    [Header("Grid Position")]
    public float gridCenterY = 1f; // Center Y position of the grid
    
    // Accepts a custom Z-coordinate for spawning
    // Adjustable for custom platform lengths
    public Vector3 GetGridPosition(int column, int row, float zCoordinate)
    {
        // Calculate centered positions
        float startX = -(columns - 1) * columnSpacing * 0.5f;
        float startY = gridCenterY - (rows - 1) * rowSpacing * 0.5f;
        
        float x = startX + column * columnSpacing;
        float y = startY + row * rowSpacing;
        
        return new Vector3(x, y, zCoordinate);
    }
    
    // Original GetGridPosition for backward compatibility if needed elsewhere
    // This is optional; you could remove this if this is the only use.
    public Vector3 GetGridPosition(int column, int row)
    {
        // If a default Z is ever needed.
        return GetGridPosition(column, row, 30f);
    }

    public bool IsValidGridPosition(int column, int row)
    {
        return column >= 0 && column < columns && row >= 0 && row < rows;
    }
}