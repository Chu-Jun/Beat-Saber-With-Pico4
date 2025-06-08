using UnityEngine;

public class SlicedBlockPieces : MonoBehaviour
{
    [Header("Pieces")]
    public GameObject leftPiece;
    public GameObject rightPiece;
    
    [Header("Physics Settings")]
    public float forceMultiplier = 1f;
    public float torqueMultiplier = 1f;
    public float lifetime = 3f;
    
    private Rigidbody leftRB;
    private Rigidbody rightRB;
    
    void Start()
    {
        // Get rigidbodies
        if (leftPiece != null)
            leftRB = leftPiece.GetComponent<Rigidbody>();
        if (rightPiece != null)
            rightRB = rightPiece.GetComponent<Rigidbody>();
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    public void Initialize(BlockData.BlockType blockType, Vector3 sliceForce)
    {
        // Set colors for both pieces
        SetPieceColor(leftPiece, blockType);
        SetPieceColor(rightPiece, blockType);
        
        // Apply physics forces
        ApplySliceForces(sliceForce);
    }
    
    private void SetPieceColor(GameObject piece, BlockData.BlockType blockType)
    {
        if (piece == null) return;
        
        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(renderer.material);
            
            switch (blockType)
            {
                case BlockData.BlockType.Red:
                    material.color = Color.red;
                    break;
                case BlockData.BlockType.Blue:
                    material.color = Color.blue;
                    break;
            }
            
            renderer.material = material;
        }
    }
    
    private void ApplySliceForces(Vector3 sliceForce)
    {
        if (leftRB != null)
        {
            leftRB.AddForce(Vector3.left * 100f + sliceForce * forceMultiplier);
            leftRB.AddTorque(Random.insideUnitSphere * 50f * torqueMultiplier);
        }
        
        if (rightRB != null)
        {
            rightRB.AddForce(Vector3.right * 100f + sliceForce * forceMultiplier);
            rightRB.AddTorque(Random.insideUnitSphere * 50f * torqueMultiplier);
        }
    }
}