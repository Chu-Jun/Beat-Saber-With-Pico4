using UnityEngine;
using Unity.XR.PXR;
using System.Collections;

public class BeatSaberSaber : MonoBehaviour
{
    [Header("Saber Configuration")]
    public SaberType saberType = SaberType.Left;
    
    [Header("Saber Components")]
    public Collider saberCollider;
    
    [Header("Swing Detection")]
    [Tooltip("Minimum speed required for a valid swing")]
    public float minSwingSpeed = 2f;
    [Tooltip("How often to check swing direction (lower = more accurate)")]
    public float swingCheckInterval = 0.02f;
    [Tooltip("Angle tolerance for swing direction (0-1, where 0.5 ≈ 60 degrees)")]
    public float angleThreshold = 0.5f;

    [Header("Cut Quality Thresholds")]
    [Tooltip("Maximum distance from block center for a center cut")]
    public float centerCutThreshold = 0.1f;
    [Tooltip("Minimum cross product magnitude for valid slice normal")]
    public float minSliceNormalMagnitude = 0.1f;

    // UI Manager Reference
    [Header("UI Manager Reference")]
    [SerializeField] private UIManager uiManager;
    
    // Swing detection variables
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    private float lastSwingCheck;
    
    // Controller reference for PICO SDK
    private PXR_Input.Controller controllerHand;
    
    public enum SaberType
    {
        Left,
        Right
    }
    
    void Start()
    {
        InitializeSaber();
        SetupControllerReference();

         // Find the UI Manager if not assigned in Inspector
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("BeatSaberSaber: UIManager not found in the scene! UI updates will not function.");
            }
        }
    }
    
    private void InitializeSaber()
    {
        // Get collider if not assigned
        if (saberCollider == null)
        {
            saberCollider = GetComponent<Collider>();
        }
        
        // Ensure collider is set as trigger
        if (saberCollider != null)
        {
            saberCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError("BeatSaberSaber: No collider found! Please assign a collider.");
        }
        
        // Initialize position tracking
        previousPosition = transform.position;
        lastSwingCheck = Time.time;
    }
    
    private void SetupControllerReference()
    {
        // Set controller hand based on saber type
        controllerHand = saberType == SaberType.Left ? 
            PXR_Input.Controller.LeftController : 
            PXR_Input.Controller.RightController;

        Debug.Log($"{controllerHand} is assigned.");
    }
    
    void Update()
    {
        UpdateSwingDetection();
    }
    
    private void UpdateSwingDetection()
    {
        // Calculate velocity based on position change
        if (Time.time - lastSwingCheck >= swingCheckInterval)
        {
            Vector3 positionDelta = transform.position - previousPosition;
            float timeDelta = Time.time - lastSwingCheck;
            
            if (timeDelta > 0.001f)
            {
                currentVelocity = positionDelta / timeDelta;
            }
            
            previousPosition = transform.position;
            lastSwingCheck = Time.time;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit a block
        BeatSaberBlock block = other.GetComponent<BeatSaberBlock>();
        if (block != null)
        {
            AttemptSlice(block);
        }
    }
    
    private void AttemptSlice(BeatSaberBlock block)
    {
        string missKeyword = "MISSED_BLOCK";

        // Check if this saber can slice this block type
        if (!CanSliceBlock(block))
        {
            Debug.Log($"{missKeyword} {saberType} saber cannot slice {block.blockType} block");
            uiManager?.OnBadCut();
            block.PlayFailSound();
            return;
        }
        
        // Check if swing speed is sufficient
        float swingSpeed = currentVelocity.magnitude;
        if (swingSpeed < minSwingSpeed)
        {
            Debug.Log($"{missKeyword} Swing too slow: {swingSpeed:F2} < {minSwingSpeed}");
            uiManager?.OnBadCut();
            block.PlayFailSound();
            return;
        }
        
        // Check if swing direction is correct
        if (!IsCorrectSwingDirection(block, currentVelocity))
        {
            Debug.Log($"{missKeyword} Wrong swing direction!");
            uiManager?.OnBadCut();
            block.PlayFailSound();
            return;
        }
        
        // Successful slice!
        PerformSlice(block);
    }
    
    private bool CanSliceBlock(BeatSaberBlock block)
    {
        // Left saber slices red blocks, right saber slices blue blocks
        switch (saberType)
        {
            case SaberType.Left:
                return block.blockType == BlockData.BlockType.Red;
            case SaberType.Right:
                return block.blockType == BlockData.BlockType.Blue;
            default:
                return false;
        }
    }
    
    private bool IsCorrectSwingDirection(BeatSaberBlock block, Vector3 swingVelocity)
    {
        // If block has "Any" direction, any swing is valid
        if (block.cutDirection == BlockData.CutDirection.Any)
        {
            return true;
        }
        
        // Normalize swing velocity to get direction
        Vector3 swingDirection = swingVelocity.normalized;
        
        // Get required swing direction based on block's cut direction
        Vector3 requiredDirection = GetRequiredSwingDirection(block.cutDirection);
        
        // Check if swing direction matches required direction (with some tolerance)
        float dotProduct = Vector3.Dot(swingDirection, requiredDirection);        
        return dotProduct > angleThreshold;
    }
    
    private Vector3 GetRequiredSwingDirection(BlockData.CutDirection cutDirection)
    {
        // Convert block cut direction to world space swing direction
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
                return Vector3.zero;
        }
    }
    
    private void PerformSlice(BeatSaberBlock block)
    {
        // Calculate slice parameters based on swing direction
        Vector3 sliceNormal = CalculateSliceNormal(currentVelocity);
        Vector3 sliceOrigin = transform.position;
        
        // Attempt to slice the block
        bool sliceSuccess = block.AttemptSlice(sliceOrigin, sliceNormal);
        
        if (sliceSuccess)
        {
            // Debug.Log($"Successfully sliced {block.blockType} block with {saberType} saber!");

            // Calculate actual cut quality metrics
            float cutAngle = CalculateCutAngle(block, currentVelocity);
            float cutDistance = CalculateCutDistance(block, sliceOrigin);
            bool centerCut = IsCenterCut(block, sliceOrigin);
            
            uiManager?.OnNoteCut(cutAngle, cutDistance, centerCut);
        }
        else
        {
            // Debug.Log($"Failed to slice {block.blockType} block with {saberType} saber!");
            uiManager?.OnBadCut(); // Treat any slicing failure as a bad cut for UI
        }
    }

    private float CalculateCutAngle(BeatSaberBlock block, Vector3 swingVelocity)
    {
        Vector3 requiredDirection = GetRequiredSwingDirection(block.cutDirection);
        Vector3 actualDirection = swingVelocity.normalized;
        return Vector3.Angle(requiredDirection, actualDirection);
    }

    private float CalculateCutDistance(BeatSaberBlock block, Vector3 sliceOrigin)
    {
        return Vector3.Distance(sliceOrigin, block.transform.position);
    }

    private bool IsCenterCut(BeatSaberBlock block, Vector3 sliceOrigin)
    {
        float distanceFromCenter = Vector3.Distance(sliceOrigin, block.transform.position);
        return distanceFromCenter < centerCutThreshold;
    }
    
    private Vector3 CalculateSliceNormal(Vector3 swingVelocity)
    {
        // Calculate slice normal based on swing direction
        // The slice normal should be perpendicular to both the swing direction and forward vector
        Vector3 sliceNormal = Vector3.Cross(swingVelocity.normalized, Vector3.forward).normalized;
        
        // If the cross product is zero (swing is parallel to forward), use up vector
        if (sliceNormal.magnitude < minSliceNormalMagnitude)
        {
            sliceNormal = Vector3.Cross(swingVelocity.normalized, Vector3.up).normalized;
        }
        
        return sliceNormal;
    }

    // Public method to change saber type at runtime
    public void SetSaberType(SaberType newType)
    {
        saberType = newType;
        SetupControllerReference();
        Debug.Log($"Saber type changed to: {saberType}");
    }
    
    // Public method to get current swing speed (useful for debugging)
    public float GetCurrentSwingSpeed()
    {
        return currentVelocity.magnitude;
    }
    
    // Debug method to visualize swing direction
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = saberType == SaberType.Left ? Color.red : Color.blue;
            Gizmos.DrawRay(transform.position, currentVelocity * 0.5f);
        }
    }    
}