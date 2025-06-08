using UnityEngine;
using EzySlice;
using System.Collections;

public class BeatSaberBlock : MonoBehaviour
{
    [Header("Block Properties")]
    public BlockData.BlockType blockType;
    public BlockData.CutDirection cutDirection;
    public float moveSpeed = 5f;
    
    [Header("Visual Components")]
    public Renderer blockRenderer;
    public GameObject arrowIndicator;
    
    [Header("Slicing Materials")]
    [Tooltip("Material for the inside/cut surface of the block")]
    public Material insideMaterial;
    [Tooltip("Original block materials (Red/Blue)")]
    public Material redMaterial;
    public Material blueMaterial;
    
    [Header("Physics")]
    public Rigidbody blockRigidbody;
    public Collider blockCollider;
    
    [Header("Slicing Settings")]
    [Tooltip("How far apart the sliced pieces should separate")]
    public float separationForce = 300f;
    [Tooltip("Random torque applied to sliced pieces")]
    public float sliceTorque = 100f;
    [Tooltip("How long sliced pieces stay before cleanup")]
    public float slicedLifetime = 3f;
    
    private bool isSliced = false;
    private Vector3 moveDirection = Vector3.back;
    private MeshFilter meshFilter;
    private bool canBeSliced = true;
    
    void Start()
    {
        InitializeComponents();
        SetupPhysics();
        ValidateArrowAttachment();
    }
    
    private void InitializeComponents()
    {
        if (blockRigidbody == null)
            blockRigidbody = GetComponent<Rigidbody>();
        if (blockCollider == null)
            blockCollider = GetComponent<Collider>();
        
        // Find the Box mesh specifically, not the Arrow
        // Method 1: Skip the arrow object
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            // Skip the arrow - we want the box/cube mesh
            if (mf.gameObject == arrowIndicator)
                continue;
                
            // This should be our box mesh
            meshFilter = mf;
            break;
        }
        
        // Method 2: Alternative - find by name (uncomment if Method 1 doesn't work)
        /*
        Transform boxTransform = transform.Find("Cube/Box");
        if (boxTransform != null)
        {
            meshFilter = boxTransform.GetComponent<MeshFilter>();
        }
        */
        
        if (meshFilter == null)
        {
            Debug.LogError("BeatSaberBlock: No sliceable MeshFilter found! Make sure there's a mesh other than the arrow.");
        }
        else
        {
            Debug.Log($"Found sliceable mesh on: {meshFilter.gameObject.name}");
        }
        
        // Get renderer if not assigned
        if (blockRenderer == null)
            blockRenderer = GetComponentInChildren<Renderer>();
    }
    
    private void SetupPhysics()
    {
        if (blockRigidbody != null)
        {
            blockRigidbody.isKinematic = true;
        }
    }
    
    private void ValidateArrowAttachment()
    {
        if (arrowIndicator == null)
        {
            Debug.LogError("Arrow indicator not assigned!");
            return;
        }
        
        // Ensure arrow doesn't interfere with slicing
        Rigidbody arrowRB = arrowIndicator.GetComponent<Rigidbody>();
        if (arrowRB != null)
        {
            DestroyImmediate(arrowRB);
        }
    }
    
    void Update()
    {
        if (!isSliced)
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
            
            if (transform.position.z < -5f)
            {
                DestroyBlock();
            }
        }
    }
    
    public void Initialize(BlockData data)
    {
        blockType = data.blockType;
        cutDirection = data.cutDirection;
        
        SetBlockColor();
        SetArrowDirection();
    }
    
    private void SetBlockColor()
    {
        if (blockRenderer != null)
        {
            Material targetMaterial = blockType == BlockData.BlockType.Red ? redMaterial : blueMaterial;
            
            if (targetMaterial != null)
            {
                blockRenderer.material = targetMaterial;
            }
            else
            {
                // Fallback to color change
                Material materialInstance = new Material(blockRenderer.material);
                materialInstance.color = blockType == BlockData.BlockType.Red ? Color.red : Color.blue;
                blockRenderer.material = materialInstance;
            }
        }
    }
    
    private void SetArrowDirection()
    {
        if (arrowIndicator != null)
        {
            Vector3 originalLocalPosition = arrowIndicator.transform.localPosition;
            Vector3 localRotation = GetArrowRotationForDirection(cutDirection);
            
            arrowIndicator.transform.localRotation = Quaternion.Euler(localRotation);
            arrowIndicator.transform.localPosition = originalLocalPosition;
            
            if (cutDirection == BlockData.CutDirection.Any)
            {
                arrowIndicator.SetActive(false);
                return;
            }
            
            arrowIndicator.SetActive(true);
        }
    }
    
    private Vector3 GetArrowRotationForDirection(BlockData.CutDirection direction)
    {
        switch (direction)
        {
            case BlockData.CutDirection.Up:
                return new Vector3(0, 0, 180); 
            case BlockData.CutDirection.Down:
                return new Vector3(0, 0, 0); 
            case BlockData.CutDirection.Left:
                return new Vector3(0, 0, 90);
            case BlockData.CutDirection.Right:
                return new Vector3(0, 0, -90);
            case BlockData.CutDirection.UpLeft:
                return new Vector3(0, 0, 45);
            case BlockData.CutDirection.UpRight:
                return new Vector3(0, 0, -45);
            case BlockData.CutDirection.DownLeft:
                return new Vector3(0, 0, 135);
            case BlockData.CutDirection.DownRight:
                return new Vector3(0, 0, -135);
            default:
                return Vector3.zero;
        }
    }
    
    public bool TrySliceBlock(Vector3 sliceDirection, Vector3 slicePoint, BlockData.BlockType saberType)
    {
        if (!canBeSliced || isSliced)
            return false;
        
        // Check if saber color matches block color
        if (blockType != saberType)
        {
            Debug.Log("Wrong saber color!");
            return false;
        }
        
        // Validate cut direction (optional - comment out for easier gameplay)
        /*
        if (!IsValidCutDirection(sliceDirection))
        {
            Debug.Log("Wrong cut direction!");
            return false;
        }
        */
        
        // Perform the actual slicing
        SliceBlock(sliceDirection, slicePoint);
        return true;
    }
    
    private bool IsValidCutDirection(Vector3 sliceDirection)
    {
        if (cutDirection == BlockData.CutDirection.Any)
            return true;
        
        Vector3 requiredDirection = GetRequiredCutDirection();
        float dotProduct = Vector3.Dot(sliceDirection.normalized, requiredDirection.normalized);
        
        // Allow some tolerance (0.7 ≈ 45 degree tolerance)
        return dotProduct > 0.7f;
    }
    
    private Vector3 GetRequiredCutDirection()
    {
        switch (cutDirection)
        {
            case BlockData.CutDirection.Up:
                return Vector3.up;
            case BlockData.CutDirection.Down:
                return Vector3.down;
            case BlockData.CutDirection.Left:
                return Vector3.left;
            case BlockData.CutDirection.Right:
                return Vector3.right;
            case BlockData.CutDirection.UpLeft:
                return (Vector3.up + Vector3.left).normalized;
            case BlockData.CutDirection.UpRight:
                return (Vector3.up + Vector3.right).normalized;
            case BlockData.CutDirection.DownLeft:
                return (Vector3.down + Vector3.left).normalized;
            case BlockData.CutDirection.DownRight:
                return (Vector3.down + Vector3.right).normalized;
            default:
                return Vector3.up;
        }
    }
    
    private void CreateSlicedPieces(SlicedHull hull, Vector3 sliceDirection)
    {
        // Create upper piece
        if (hull.upperHull != null)
        {
            GameObject upperPiece = CreatePieceFromMesh(hull.upperHull, "UpperPiece");
            ApplySlicePhysics(upperPiece, sliceDirection, true);
        }
        
        // Create lower piece
        if (hull.lowerHull != null)
        {
            GameObject lowerPiece = CreatePieceFromMesh(hull.lowerHull, "LowerPiece");
            ApplySlicePhysics(lowerPiece, -sliceDirection, false);
        }
    }
    
    private GameObject CreatePieceFromMesh(Mesh mesh, string name)
    {
        GameObject piece = new GameObject(name);
        piece.transform.position = transform.position;
        piece.transform.rotation = transform.rotation;
        
        // Add mesh components
        MeshFilter pieceMeshFilter = piece.AddComponent<MeshFilter>();
        MeshRenderer pieceMeshRenderer = piece.AddComponent<MeshRenderer>();
        MeshCollider pieceCollider = piece.AddComponent<MeshCollider>();
        Rigidbody pieceRigidbody = piece.AddComponent<Rigidbody>();
        
        // Set mesh
        pieceMeshFilter.mesh = mesh;
        
        // Set materials (original material + inside material for cut surface)
        Material[] materials = new Material[mesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = i == 0 ? blockRenderer.material : insideMaterial;
        }
        pieceMeshRenderer.materials = materials;
        
        // Setup collider
        pieceCollider.convex = true;
        
        // Setup rigidbody
        pieceRigidbody.mass = blockRigidbody.mass * 0.5f;
        
        // Cleanup after some time
        StartCoroutine(CleanupPiece(piece, slicedLifetime));
        
        return piece;
    }
    
    private void ApplySlicePhysics(GameObject piece, Vector3 direction, bool isUpper)
    {
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Apply separation force
            Vector3 separationDir = direction.normalized + (isUpper ? Vector3.up : Vector3.down) * 0.5f;
            rb.AddForce(separationDir * separationForce);
            
            // Add random torque for realistic tumbling
            Vector3 randomTorque = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized * sliceTorque;
            
            rb.AddTorque(randomTorque);
            
            // Continue moving in original direction but slower
            rb.AddForce(moveDirection * moveSpeed * 50f);
        }
    }
    
    private void CreateSimpleSliceEffect(Vector3 sliceDirection)
    {
        // Simple fallback - just add physics without actual slicing
        if (blockRigidbody != null)
        {
            blockRigidbody.isKinematic = false;
            blockRigidbody.AddForce(sliceDirection * separationForce);
            blockRigidbody.AddTorque(Random.insideUnitSphere * sliceTorque);
        }
        
        // Destroy after delay
        Destroy(gameObject, slicedLifetime);
    }
    
    private IEnumerator CleanupPiece(GameObject piece, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (piece != null)
        {
            // Optional: Add fade out effect here
            Destroy(piece);
        }
    }
    
    private void DestroyBlock()
    {
        // Handle missed block
        Destroy(gameObject);
    }
    
    private void SliceBlock(Vector3 sliceDirection, Vector3 slicePoint)
    {
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("Cannot slice: No mesh found!");
            return;
        }
        
        isSliced = true;
        canBeSliced = false;
        
        // Hide arrow when slicing
        if (arrowIndicator != null)
            arrowIndicator.SetActive(false);
        
        GameObject objectToSlice = meshFilter.gameObject;
        
        // For Beat Saber slicing, we want the plane to be perpendicular to the slice direction
        // and positioned to cut through the block center
        
        // Get the mesh center in world space
        Bounds worldBounds = meshFilter.GetComponent<Renderer>().bounds;
        Vector3 worldCenter = worldBounds.center;
        
        // The plane normal should be perpendicular to the slice direction
        // For example: if slicing right, the plane should face up/down or forward/back
        Vector3 sliceDir = sliceDirection.normalized;
        
        // Choose the best perpendicular direction for the plane normal
        Vector3 planeNormal;
        
        // Try to make the plane normal perpendicular to slice direction and meaningful for visual cuts
        if (Mathf.Abs(sliceDir.x) > 0.7f) // Horizontal slice (left/right)
        {
            // For horizontal slices, use a vertical plane normal (up/down with slight forward component)
            planeNormal = new Vector3(0, 0.7f, 0.3f).normalized;
        }
        else if (Mathf.Abs(sliceDir.y) > 0.7f) // Vertical slice (up/down)
        {
            // For vertical slices, use a horizontal plane normal
            planeNormal = new Vector3(0.7f, 0, 0.3f).normalized;
        }
        else if (Mathf.Abs(sliceDir.z) > 0.7f) // Forward/backward slice
        {
            // For forward/back slices, use a side-to-side plane normal
            planeNormal = new Vector3(0.7f, 0.3f, 0).normalized;
        }
        else // Diagonal slices
        {
            // For diagonal slices, create a plane normal that's perpendicular to the slice
            // Use the cross product with a reasonable reference vector
            Vector3 reference = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(sliceDir, Vector3.up)) > 0.9f)
            {
                reference = Vector3.right;
            }
            planeNormal = Vector3.Cross(sliceDir, reference).normalized;
        }
        
        // Convert to local space
        Vector3 localCenter = objectToSlice.transform.InverseTransformPoint(worldCenter);
        Vector3 localNormal = objectToSlice.transform.InverseTransformDirection(planeNormal).normalized;
        
        // Create the slicing plane at the center of the object
        EzySlice.Plane slicingPlane = new EzySlice.Plane(localNormal, localCenter);
        
        Debug.Log($"Slice direction: {sliceDirection}");
        Debug.Log($"Plane normal: {planeNormal}");
        Debug.Log($"Local center: {localCenter}");
        Debug.Log($"Local normal: {localNormal}");
        
        // Perform the slice
        SlicedHull hull = objectToSlice.Slice(slicingPlane, insideMaterial);
        
        if (hull != null)
        {
            Debug.Log("Slice successful!");
            CreateSlicedPieces(hull, sliceDirection);
        }
        else
        {
            Debug.LogWarning("Slice failed, trying simplified approach...");
            
            // Fallback: try with basic perpendicular normals
            Vector3[] simpleNormals = {
                Vector3.Cross(sliceDir, Vector3.up).normalized,
                Vector3.Cross(sliceDir, Vector3.right).normalized,
                Vector3.Cross(sliceDir, Vector3.forward).normalized
            };
            
            foreach (Vector3 normal in simpleNormals)
            {
                if (normal.magnitude > 0.1f) // Valid cross product
                {
                    Vector3 localFallbackNormal = objectToSlice.transform.InverseTransformDirection(normal).normalized;
                    EzySlice.Plane fallbackPlane = new EzySlice.Plane(localFallbackNormal, localCenter);
                    hull = objectToSlice.Slice(fallbackPlane, insideMaterial);
                    
                    if (hull != null)
                    {
                        Debug.Log($"Fallback slice successful with normal: {normal}");
                        CreateSlicedPieces(hull, sliceDirection);
                        break;
                    }
                }
            }
            
            if (hull == null)
            {
                Debug.LogWarning("All slice attempts failed. Using simple destruction.");
                CreateSimpleSliceEffect(sliceDirection);
            }
        }
        
        // Destroy original block
        Destroy(gameObject);
    }

    // Add a few different test methods to see different slice orientations
    [ContextMenu("Test Slice Right")]
    public void TestSliceRight()
    {
        SliceBlock(Vector3.right, meshFilter.transform.position);
    }

    [ContextMenu("Test Slice Up")]
    public void TestSliceUp()
    {
        SliceBlock(Vector3.up, meshFilter.transform.position);
    }

    [ContextMenu("Test Slice Diagonal")]
    public void TestSliceDiagonal()
    {
        Vector3 diagonal = (Vector3.right + Vector3.down).normalized;
        SliceBlock(diagonal, meshFilter.transform.position);
    }

    // Update the test method to use the regular SliceBlock now
    [ContextMenu("Test Slice")]
    public void TestSlice()
    {
        Vector3 testDirection = Vector3.right;
        Vector3 testPoint = meshFilter.transform.position;
        SliceBlock(testDirection, testPoint);
    }

    [ContextMenu("Debug Hierarchy")]
    public void DebugHierarchy()
    {
        Debug.Log("=== HIERARCHY DEBUG ===");
        Debug.Log($"Block (this): {transform.position} | Local: {transform.localPosition}");
        
        Transform cubeTransform = transform.Find("Cube");
        if (cubeTransform != null)
        {
            Debug.Log($"Cube: {cubeTransform.position} | Local: {cubeTransform.localPosition}");
            
            // Find the mesh object
            if (meshFilter != null)
            {
                Debug.Log($"Mesh Object ({meshFilter.name}): {meshFilter.transform.position} | Local: {meshFilter.transform.localPosition}");
                Debug.Log($"Mesh relative to Block: {transform.InverseTransformPoint(meshFilter.transform.position)}");
            }
        }
    }

    [ContextMenu("Test Slice Alternative")]
    public void TestSliceAlternative()
    {
        // Try slicing with an offset from the mesh center
        Vector3 meshWorldPos = meshFilter.transform.position;
        Vector3 slicePoint = meshWorldPos + Vector3.right * 0.1f; // Offset slightly to the right
        Vector3 sliceDirection = Vector3.right;
        
        Debug.Log($"Alternative test - Mesh pos: {meshWorldPos}, Slice point: {slicePoint}");
        SliceBlockAlternative(sliceDirection, slicePoint);
    }

    private void SliceBlockAlternative(Vector3 sliceDirection, Vector3 slicePoint)
    {
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("Cannot slice: No mesh found!");
            return;
        }
        
        isSliced = true;
        canBeSliced = false;
        
        // Hide arrow when slicing
        if (arrowIndicator != null)
            arrowIndicator.SetActive(false);
        
        // Try a completely different approach - slice through the middle with different orientations
        GameObject objectToSlice = meshFilter.gameObject;
        
        // Get mesh bounds in world space
        Bounds worldBounds = meshFilter.GetComponent<Renderer>().bounds;
        Vector3 worldCenter = worldBounds.center;
        
        Debug.Log($"World bounds center: {worldCenter}");
        Debug.Log($"World bounds size: {worldBounds.size}");
        
        // Try multiple slice orientations
        Vector3[] normals = {
            Vector3.up,      // Horizontal slice
            Vector3.right,   // Vertical slice (left-right)
            Vector3.forward, // Vertical slice (front-back)
            new Vector3(1, 1, 0).normalized, // Diagonal
        };
        
        SlicedHull hull = null;
        
        for (int i = 0; i < normals.Length && hull == null; i++)
        {
            // Convert to local space
            Vector3 localCenter = objectToSlice.transform.InverseTransformPoint(worldCenter);
            Vector3 localNormal = objectToSlice.transform.InverseTransformDirection(normals[i]).normalized;
            
            Debug.Log($"Attempt {i + 1}: Local center: {localCenter}, Local normal: {localNormal}");
            
            EzySlice.Plane plane = new EzySlice.Plane(localNormal, localCenter);
            hull = objectToSlice.Slice(plane, insideMaterial);
            
            if (hull != null)
            {
                Debug.Log($"Success with normal {normals[i]}!");
                break;
            }
        }
        
        if (hull != null)
        {
            CreateSlicedPieces(hull, sliceDirection);
        }
        else
        {
            Debug.LogError("All slice attempts failed!");
            
            // Let's also check if the mesh itself has issues
            Mesh mesh = meshFilter.mesh;
            Debug.Log($"Mesh info:");
            Debug.Log($"- Vertices: {mesh.vertexCount}");
            Debug.Log($"- Triangles: {mesh.triangles.Length / 3}");
            Debug.Log($"- SubMeshes: {mesh.subMeshCount}");
            Debug.Log($"- Readable: {mesh.isReadable}");
            Debug.Log($"- Normals: {mesh.normals?.Length ?? 0}");
            
            CreateSimpleSliceEffect(sliceDirection);
        }
        
        // Destroy original block
        Destroy(gameObject);
    }
}