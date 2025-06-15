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
    public Renderer blockRenderer;  // Handles the visual appearance of the block
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
    public float separationForce = 1f;  // Force applied to separate sliced pieces
    [Tooltip("Upward force applied to sliced pieces")]
    public float liftForce = 0.5f;      // New variable for upward movement
    [Tooltip("Random torque applied to sliced pieces")]
    public float sliceTorque = 2f;     // Torque applied to sliced pieces for spinning effect
    [Tooltip("How long sliced pieces stay before cleanup")]
    public float slicedLifetime = 3f;   // Time before sliced pieces are destroyed

    private bool isSliced = false;

    // Reference to the block mesh filter that will be sliced
    private MeshFilter cubeMeshFilter;
    private MeshFilter arrowMeshFilter;

    // Direction the block moves in (default is backward)
    private Vector3 moveDirection = Vector3.back;

    void Start()
    {
        InitializeComponents();
        SetupPhysics();
        ValidateArrowAttachment();
    }

    private void InitializeComponents()
    {
        // Initialize block properties
        if (blockRigidbody == null)
            blockRigidbody = GetComponent<Rigidbody>();
        if (blockCollider == null)
            blockCollider = GetComponent<Collider>();
        if (blockRenderer == null)
            blockRenderer = GetComponentInChildren<Renderer>();

        // Initialize audio source
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            Debug.Log($"Found existing AudioSource: {audioSource != null}");
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("Added new AudioSource component");

        Debug.Log($"AudioSource enabled: {audioSource.enabled}, GameObject active: {gameObject.activeInHierarchy}");

        // Initialize particle system
        if (sliceParticles == null)
            sliceParticles = GetComponentInChildren<ParticleSystem>();

        // Find both cube and arrow meshes
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.gameObject == arrowIndicator)
            {
                arrowMeshFilter = mf;
            }
            else
            {
                cubeMeshFilter = mf;
            }
        }

        // Validation
        if (cubeMeshFilter == null)
        {
            Debug.LogError("BeatSaberBlock: No cube MeshFilter found!");
        }
        if (arrowMeshFilter == null)
        {
            Debug.LogError("BeatSaberBlock: No arrow MeshFilter found!");
        }
    }

    private void SetupPhysics()
    {
        if (blockRigidbody != null)
        {
            blockRigidbody.isKinematic = true;
            blockRigidbody.useGravity = false;
        }
    }

    private void ValidateArrowAttachment()
    {
        if (arrowIndicator == null)
        {
            Debug.LogError("Arrow indicator not assigned!");
            return;
        }
    }

    void Update()
    {
        // Check if component is enabled/active
        if (!enabled || gameObject == null)
            return;

        if (!isSliced)
        {
            // Move the block
            if (transform != null)
            {
                transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
            }

            // Check if the block has moved out of bounds
            if (transform != null && transform.position.z < -5f)
            {
                // Using gameObject.name for better debug logging
                Debug.Log($"Destroying out-of-bounds block: {gameObject.name}");
                Destroy(gameObject);
            }
        }
    }

    // Referenced by the BlockSpawner to initialize the block with specific data
    public void Initialize(BlockData data)
    {
        blockType = data.blockType;
        cutDirection = data.cutDirection;

        SetBlockColor();
        SetBlockRotation();
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
                // Fallback to default color
                Material materialInstance = new Material(blockRenderer.material);
                materialInstance.color = blockType == BlockData.BlockType.Red ? Color.red : Color.blue;
                blockRenderer.material = materialInstance;
            }
        }
    }

    private void SetBlockRotation()
    {
        Vector3 rotationAngles = GetRotationForDirection(cutDirection);
        transform.localRotation = Quaternion.Euler(rotationAngles);

        // Only handle arrow visibility
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(cutDirection != BlockData.CutDirection.Any);
        }
    }

    private Vector3 GetRotationForDirection(BlockData.CutDirection direction)
    {
        switch (direction)
        {
            case BlockData.CutDirection.Up:
                return new Vector3(0, 0, 180);
            case BlockData.CutDirection.Down:
                return new Vector3(0, 0, 0);
            case BlockData.CutDirection.Left:
                return new Vector3(0, 0, -90);
            case BlockData.CutDirection.Right:
                return new Vector3(0, 0, 90);
            case BlockData.CutDirection.UpLeft:
                return new Vector3(0, 0, -135);
            case BlockData.CutDirection.UpRight:
                return new Vector3(0, 0, 135);
            case BlockData.CutDirection.DownLeft:
                return new Vector3(0, 0, -45);
            case BlockData.CutDirection.DownRight:
                return new Vector3(0, 0, 45);
            default:
                return Vector3.zero;
        }
    }

    // Properties and Methods to slice the block
    private Vector3 sliceNormal = Vector3.right;
    private Vector3 sliceOrigin = Vector3.zero;

    // Make slice parameters accessible (For Saber Integration)
    public Vector3 SliceNormal 
    { 
        get { return sliceNormal; } 
        set { sliceNormal = value; } 
    }

    public Vector3 SliceOrigin 
    { 
        get { return sliceOrigin; } 
        set { sliceOrigin = value; } 
    }

    [ContextMenu("Test Slice Red Block (Top-Down)")]
    public void TestSliceRedBlock()
    {
        if (blockType != BlockData.BlockType.Red)
        {
            Debug.LogWarning("This is not a red block!");
            return;
        }

        // Slice vertically from top to bottom
        sliceNormal = Vector3.right;
        sliceOrigin = transform.position + Vector3.up;
        SliceBlock();
    }

    [ContextMenu("Test Slice Blue Block (Right-Left)")]
    public void TestSliceBlueBlock()
    {
        if (blockType != BlockData.BlockType.Blue)
        {
            Debug.LogWarning("This is not a blue block!");
            return;
        }

        // Slice horizontally from right to left
        sliceNormal = Vector3.up;
        sliceOrigin = transform.position + Vector3.right;
        SliceBlock();
    }

    private bool SliceBlock()
    {
        if (isSliced) return false;

        // Create a slicing plane
        SlicedHull hull = SliceObject(gameObject, sliceOrigin, sliceNormal);

        if (hull != null)
        {
            CreateSlicedPiece(hull.CreateUpperHull(), true);
            CreateSlicedPiece(hull.CreateLowerHull(), false);

            // // Destroy the original block
            // Destroy(gameObject);
            // isSliced = true;
            // return true;

            // Mark as sliced but delay destruction to allow sound to play
            isSliced = true;
            
            // Hide the renderer instead of destroying immediately
            if (blockRenderer != null)
                blockRenderer.enabled = false;
            if (blockCollider != null)
                blockCollider.enabled = false;
                
            // Destroy after a short delay to allow sound to finish
            StartCoroutine(DelayedDestroy(0.5f));
            
            return true;
        }

        return false;
    }

    private IEnumerator DelayedDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private SlicedHull SliceObject(GameObject obj, Vector3 planeOrigin, Vector3 planeNormal)
    {
        // Get the cube mesh object (which is a child of the main GameObject)
        if (cubeMeshFilter == null || cubeMeshFilter.gameObject == null)
        {
            Debug.LogError("No cube mesh found to slice!");
            return null;
        }

        // Use the cube GameObject for slicing instead of the parent
        return cubeMeshFilter.gameObject.Slice(planeOrigin, planeNormal, insideMaterial);
    }

    private void CreateSlicedPiece(GameObject piece, bool isUpperHalf)
    {
        if (piece == null) return;

        // CRITICAL: Reposition the hull piece to match the parent Block's world position
        // The hull piece is created at the child mesh's local position (0,0,0)
        // We need to move it to the parent Block's world position
        piece.transform.position = transform.position;
        piece.transform.rotation = transform.rotation;

        // Maintain the original scale from the mesh
        if (cubeMeshFilter != null)
        {
            piece.transform.localScale = cubeMeshFilter.transform.lossyScale;
        }

        Rigidbody rb = piece.AddComponent<Rigidbody>();
        rb.useGravity = true;

        MeshCollider meshCollider = piece.AddComponent<MeshCollider>();
        meshCollider.convex = true;

        // Calculate separation direction based on which half it is
        Vector3 separationDir = isUpperHalf ? sliceNormal : -sliceNormal;

        // Apply forces
        // 1. Small separation force in opposite directions
        rb.AddForce(separationDir * separationForce, ForceMode.Impulse);

        // 2. Small upward force to simulate lift
        rb.AddForce(Vector3.up * liftForce, ForceMode.Impulse);

        // 3. Minimal random rotation
        rb.AddTorque(
            new Vector3(
                Random.Range(-sliceTorque, sliceTorque),
                Random.Range(-sliceTorque, sliceTorque),
                Random.Range(-sliceTorque, sliceTorque)
            ),
            ForceMode.Impulse
        );

        Destroy(piece, slicedLifetime);
    }

    // Properties and Methods for sound/visual effects
    [Header("Audio")]
    [Tooltip("Sound played when block is successfully sliced")]
    public AudioClip sliceSound;
    [Tooltip("Sound played when slice attempt fails")]
    public AudioClip failSound;
    public AudioSource audioSource;

    [Header("Visual Effects")]
    [Tooltip("Particle system for slice effect")]
    public ParticleSystem sliceParticles;
    [Tooltip("Colors for particle effects")]
    public Color redParticleColor = Color.red;
    public Color blueParticleColor = Color.blue;

    public bool AttemptSlice(Vector3 sliceOriginPos, Vector3 sliceNormalDir, BeatSaberSaber saber = null)
    {
        if (isSliced)
        {
            PlayFailSound();
            TriggerHapticFeedback(saber, false);
            return false;
        }

        sliceOrigin = sliceOriginPos;
        sliceNormal = sliceNormalDir;
        
        bool success = SliceBlock();
        
        if (success)
        {
            PlaySliceSound();
            PlaySliceParticles();
            TriggerHapticFeedback(saber, true);
        }
        else
        {
            PlayFailSound();
            TriggerHapticFeedback(saber, false);
        }
        
        return success;
    }

    public void PlaySliceFailure(BeatSaberSaber saber = null)
    {
        PlayFailSound();
        TriggerHapticFeedback(saber, false);
    }

    private void PlaySliceSound()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null!");
            return;
        }
        
        if (!audioSource.enabled)
        {
            Debug.LogError("AudioSource is disabled!");
            audioSource.enabled = true; // Try to enable it
        }
        
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("GameObject is not active in hierarchy!");
            return;
        }
        
        if (sliceSound == null)
        {
            Debug.LogError("Slice sound clip is null!");
            return;
        }
        
        Debug.Log("Playing slice sound...");
        audioSource.PlayOneShot(sliceSound);
        // if (audioSource != null && sliceSound != null)
        // {
        //     audioSource.PlayOneShot(sliceSound);
        // }
    }

    private void PlayFailSound()
    {
        if (audioSource != null && failSound != null)
        {
            audioSource.PlayOneShot(failSound);
        }
    }

    private void PlaySliceParticles()
    {
        if (sliceParticles != null)
        {
            // Set particle color based on block type
            var main = sliceParticles.main;
            main.startColor = blockType == BlockData.BlockType.Red ? redParticleColor : blueParticleColor;
            Debug.Log("BeatSaberBlock: Block Color is {main.startColor}");

            // Position particles at slice point
            sliceParticles.transform.position = sliceOrigin;
            Debug.Log("BeatSaberBlock: Particle Effect happens at {sliceParticles.transform.position}");
            
            // Play the particle effect
            sliceParticles.Play();
        }
        else 
        {
            Debug.LogError("BeatSaberBlock: No slice particles found!");
        }
    }

    // Properties and Methods for Haptic Feedback
    [Header("Haptic Feedback")]
    [Tooltip("Haptic strength for successful slice (0-1)")]
    [Range(0f, 1f)]
    public float sliceHapticStrength = 0.8f;
    [Tooltip("Haptic duration for successful slice in seconds")]
    public float sliceHapticDuration = 0.1f;
    [Tooltip("Haptic strength for failed slice (0-1)")]
    [Range(0f, 1f)]
    public float failHapticStrength = 0.3f;
    [Tooltip("Haptic duration for failed slice in seconds")]
    public float failHapticDuration = 0.05f;

    private void TriggerHapticFeedback(BeatSaberSaber saber, bool isSuccess)
    {
        if (saber != null)
        {
            // if (isSuccess)
            // {
            //     saber.TriggerHapticFeedback(sliceHapticStrength, sliceHapticDuration);
            // }
            // else
            // {
            //     saber.TriggerHapticFeedback(failHapticStrength, failHapticDuration);
            // }
        }
    }
}