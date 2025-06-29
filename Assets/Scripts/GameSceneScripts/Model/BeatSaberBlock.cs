using UnityEngine;
using EzySlice;
using System;
using System.Collections;

public class BeatSaberBlock : MonoBehaviour
{
    [Header("Block Properties")]
    public BlockData.BlockType blockType;
    public BlockData.CutDirection cutDirection;

    [Tooltip("The speed at which the block moves towards the player. Determined by InfoData.")]
    public float moveSpeed; 

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

    [Header("Audio")]
    [Tooltip("Sound played when block is successfully sliced")]
    public AudioClip sliceSound;
    [Tooltip("Sound played when slice attempt fails")]
    public AudioClip failSound;
    public AudioSource audioSource;

    public bool IsSliced { get; private set; } 
    public event Action OnSliced;

    // Reference to the block mesh filter that will be sliced
    private MeshFilter cubeMeshFilter;
    private MeshFilter arrowMeshFilter;

    // Direction the block moves in (default is backward)
    private Vector3 moveDirection = Vector3.back;

    private bool hasLoggedReach = false;
    private float songStartTime;
    private float bpm;
    private float beatToSecondsMultiplier = 60f;

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
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) 
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

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
        if (!enabled) return;

        if (!IsSliced)
        {
            // Store previous position before moving
            float previousZ = transform.position.z; 

            transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

            // Check if block has reached the player (crossed z = 0)
            if (!hasLoggedReach && previousZ > 0f && transform.position.z <= 0f)
            {
                hasLoggedReach = true;

                // Calculate real-time beat
                float currentTime = (float)AudioSettings.dspTime - songStartTime;
                float currentBeat = currentTime * bpm / beatToSecondsMultiplier;
                Debug.Log($"Block reached player at beat: {currentBeat:F2}");
            }

            // Check if the block has moved out of bounds
            if (transform.position.z < -5f)
            {
                // Using gameObject.name for better debug logging
                Debug.Log($"Destroying out-of-bounds block: {gameObject.name}");
                Destroy(gameObject);
            }
        }
    }

    public void SetTimingInfo(float songStartTime, float bpm)
    {
        this.songStartTime = songStartTime;
        this.bpm = bpm;
    }

    public void Initialize(BlockData data, float movementSpeed)
    {
        blockType = data.blockType;
        cutDirection = data.cutDirection;
        moveSpeed = movementSpeed;

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

    public bool AttemptSlice(Vector3 sliceOriginPos, Vector3 sliceNormalDir)
    {
        bool success = SliceBlock(sliceOriginPos, sliceNormalDir);

        if (success)
        {
            PlaySliceSound();
            OnSliced?.Invoke();
        }
        else
        {
            PlayFailSound();
        }
        
        return success;
    }

    private bool SliceBlock(Vector3 sliceOriginPos, Vector3 sliceNormalDir)
    {
        if (IsSliced) return false;

        // Create a slicing plane
        SlicedHull hull = SliceObject(gameObject, sliceOriginPos, sliceNormalDir);

        if (hull != null)
        {
            CreateSlicedPiece(hull.CreateUpperHull(), sliceNormalDir, true);
            CreateSlicedPiece(hull.CreateLowerHull(), sliceNormalDir, false);

            // Mark as sliced but delay destruction to allow sound to play
            IsSliced = true;
            
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

    private SlicedHull SliceObject(GameObject obj, Vector3 sliceOriginPos, Vector3 sliceNormalDir)
    {
        // Get the cube mesh object (which is a child of the main GameObject)
        if (cubeMeshFilter == null || cubeMeshFilter.gameObject == null)
        {
            Debug.LogError("No cube mesh found to slice!");
            return null;
        }

        // Use the cube GameObject for slicing instead of the parent
        return cubeMeshFilter.gameObject.Slice(sliceOriginPos, sliceNormalDir, insideMaterial);
    }

    private void CreateSlicedPiece(GameObject piece, Vector3 sliceNormalDir, bool isUpperHalf)
    {
        if (piece == null) return;

        // CRITICAL: Reposition the hull piece to match the parent Block's world position
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
        Vector3 separationDir = isUpperHalf ? sliceNormalDir : -sliceNormalDir;

        // Apply forces
        rb.AddForce(separationDir * separationForce, ForceMode.Impulse);
        rb.AddForce(Vector3.up * liftForce, ForceMode.Impulse);
        rb.AddTorque(
            new Vector3(
                UnityEngine.Random.Range(-sliceTorque, sliceTorque),
                UnityEngine.Random.Range(-sliceTorque, sliceTorque),
                UnityEngine.Random.Range(-sliceTorque, sliceTorque)
            ),
            ForceMode.Impulse
        );

        Destroy(piece, slicedLifetime);
    }    
   
    public void PlaySliceSound()
    {
        if (audioSource != null && sliceSound != null)
        {
            audioSource.PlayOneShot(sliceSound);
        }
    }

    public void PlayFailSound()
    {
        if (audioSource != null && failSound != null)
        {
            audioSource.PlayOneShot(failSound);
        }
    }
}