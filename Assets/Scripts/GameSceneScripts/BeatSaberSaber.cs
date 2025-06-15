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

    [Header("Lightsaber Trail Effect")]
    [Tooltip("Trail renderer for lightsaber effect")]
    public TrailRenderer saberTrail;
    [Tooltip("Base trail width")]
    public float baseTrailWidth = 0.1f;
    [Tooltip("Maximum trail width at high speed")]
    public float maxTrailWidth = 0.3f;
    [Tooltip("Speed at which max trail width is reached")]
    public float maxTrailSpeed = 10f;
    [Tooltip("Base trail emission intensity")]
    public Color baseTrailColor = Color.white;
    [Tooltip("High-speed trail emission intensity")]
    public Color highSpeedTrailColor = Color.cyan;
    
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
        UpdateTrailEffect();
    }
    
    private void UpdateSwingDetection()
    {
        // Calculate velocity based on position change
        if (Time.time - lastSwingCheck >= swingCheckInterval)
        {
            Vector3 positionDelta = transform.position - previousPosition;
            float timeDelta = Time.time - lastSwingCheck;
            
            if (timeDelta > 0)
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
        // Check if this saber can slice this block type
        if (!CanSliceBlock(block))
        {
            Debug.Log($"{saberType} saber cannot slice {block.blockType} block");
            return;
        }
        
        // Check if swing speed is sufficient
        float swingSpeed = currentVelocity.magnitude;
        if (swingSpeed < minSwingSpeed)
        {
            Debug.Log($"Swing too slow: {swingSpeed:F2} < {minSwingSpeed}");
            return;
        } else {
            Debug.Log($"Good swing !");
        }
        
        // Check if swing direction is correct
        if (!IsCorrectSwingDirection(block, currentVelocity))
        {
            Debug.Log("Wrong swing direction!");
            block.PlaySliceFailure(this); // Pass saber reference for haptic feedback
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
        float angleThreshold = 0.5f; // Roughly 60 degrees tolerance
        
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
        
        // Attempt to slice the block (pass saber reference for haptic feedback)
        bool sliceSuccess = block.AttemptSlice(sliceOrigin, sliceNormal, this);
        
        if (sliceSuccess)
        {
            Debug.Log($"Successfully sliced {block.blockType} block with {saberType} saber!");
        }
        else
        {
            Debug.Log($"Failed to slice {block.blockType} block with {saberType} saber!");
            Debug.LogError($"Investigate if needed");
        }
    }
    
    private Vector3 CalculateSliceNormal(Vector3 swingVelocity)
    {
        // Calculate slice normal based on swing direction
        // The slice normal should be perpendicular to both the swing direction and forward vector
        Vector3 sliceNormal = Vector3.Cross(swingVelocity.normalized, Vector3.forward).normalized;
        
        // If the cross product is zero (swing is parallel to forward), use up vector
        if (sliceNormal.magnitude < 0.1f)
        {
            sliceNormal = Vector3.Cross(swingVelocity.normalized, Vector3.up).normalized;
        }
        
        return sliceNormal;
    }
    
    // Trail effect method
    private void UpdateTrailEffect()
    {
        if (saberTrail == null) return;
        
        float currentSpeed = currentVelocity.magnitude;
        
        // Calculate trail width based on swing speed
        float speedRatio = Mathf.Clamp01(currentSpeed / maxTrailSpeed);
        float trailWidth = Mathf.Lerp(baseTrailWidth, maxTrailWidth, speedRatio);
        saberTrail.widthMultiplier = trailWidth;
        
        // Calculate trail color based on swing speed
        Color trailColor = Color.Lerp(baseTrailColor, highSpeedTrailColor, speedRatio);
        saberTrail.startColor = trailColor;
        saberTrail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        
        // Enable/disable trail based on movement
        bool shouldShowTrail = currentSpeed > minSwingSpeed * 0.3f; // Show trail at 30% of min swing speed
        saberTrail.emitting = shouldShowTrail;
    }
    
    // Haptic feedback method
    // public void TriggerHapticFeedback(float strength, float duration)
    // {
    //     StartCoroutine(PlayHapticFeedback(strength, duration));
    // }
    
    // private IEnumerator PlayHapticFeedback(float strength, float duration)
    // {
    //     // Convert strength to PICO SDK format (0-255)
    //     int hapticStrength = Mathf.RoundToInt(strength * 255f);
        
    //     // Play haptic feedback
    //     PXR_Input.SetControllerVibration(controllerHand, hapticStrength, Mathf.RoundToInt(duration * 1000f));
        
    //     // Wait for duration
    //     yield return new WaitForSeconds(duration);
        
    //     // Stop haptic feedback
    //     PXR_Input.SetControllerVibration(controllerHand, 0, 0);
    // }

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