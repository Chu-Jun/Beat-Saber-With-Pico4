using UnityEngine;

[System.Serializable]
public class BlockData
{
    public enum BlockType
    {
        Red = 0,
        Blue = 1
    }
    
    public enum CutDirection
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
        UpLeft = 4,
        UpRight = 5,
        DownLeft = 6,
        DownRight = 7,
        Any = 8
    }
    
    public BlockType blockType;
    public CutDirection cutDirection;
    public int column;
    public int row;
    public float sliceTime;
    
    public BlockData(BlockType blockType, CutDirection cutDirection, int column, int row, float sliceTime)
    {
        this.blockType = blockType;
        this.cutDirection = cutDirection;
        this.column = column;
        this.row = row;
        this.sliceTime = sliceTime;
    }
}