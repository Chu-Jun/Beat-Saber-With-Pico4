using UnityEngine;
using System.Collections.Generic;

public class SaberController : MonoBehaviour
{
    [Header("Saber Properties")]
    public BlockData.BlockType saberType;
    public LayerMask blockLayer = -1;
    
    [Header("Collision Detection")]
    public float velocityThreshold = 2f;
    public float sliceEffectiveness = 1f;
    
    [Header("Visual Feedback")]
    public ParticleSystem sliceParticles;
    public AudioSource sliceAudioSource;
    public AudioClip successSliceSound;
    public AudioClip failSliceSound;
    
    [Header("Haptic Feedback (VR)")]
    public float hapticIntensity = 0.8f;
    public float hapticDuration = 0.1f;
    
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    private List<Vector3> velocityHistory = new List<Vector3>();
    private const int VELOCITY_HISTORY_SIZE = 5;
    
    void Start()
    {
        previousPosition = transform.position;
        
        // Initialize audio
        if (sliceAudioSource == null)
            sliceAudioSource = GetComponent<AudioSource>();
    }
    
    void Update()
    {
        CalculateVelocity();
    }
    
    private void CalculateVelocity()
    {
        currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        previousPosition = transform.position;
        
        // Maintain velocity history for smoother direction calculation
        velocityHistory.Add(currentVelocity);
        if (velocityHistory.Count > VELOCITY_HISTORY_SIZE)
        {
            velocityHistory.RemoveAt(0);
        }
    }
    
    private Vector3 GetAverageVelocity()
    {
        if (velocityHistory.Count == 0)
            return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        foreach (Vector3 vel in velocityHistory)
        {
            sum += vel;
        }
        return sum / velocityHistory.Count;
    }
    
    void OnTriggerEnter(Collider other)
    {
        BeatSaberBlock block = other.GetComponent<BeatSaberBlock>();
        if (block == null)
            return;
        
        Vector3 averageVelocity = GetAverageVelocity();
        
        // Check if we're moving fast enough
        if (averageVelocity.magnitude < velocityThreshold)
        {
            Debug.Log("Saber moving too slowly to slice!");
            return;
        }
        
        // Calculate slice direction and point
        Vector3 sliceDirection = averageVelocity.normalized;
        Vector3 slicePoint = other.ClosestPoint(transform.position);
        
        // Attempt to slice the block
        bool sliceSuccessful = block.TrySliceBlock(sliceDirection, slicePoint, saberType);
        
        // Provide feedback
        if (sliceSuccessful)
        {
            OnSuccessfulSlice(slicePoint, sliceDirection);
        }
        else
        {
            OnFailedSlice();
        }
    }
    
    private void OnSuccessfulSlice(Vector3 position, Vector3 direction)
    {
        // Particle effects
        if (sliceParticles != null)
        {
            sliceParticles.transform.position = position;
            sliceParticles.transform.LookAt(position + direction);
            sliceParticles.Play();
        }
        
        // Audio
        if (sliceAudioSource != null && successSliceSound != null)
        {
            sliceAudioSource.PlayOneShot(successSliceSound);
        }
        
        // Haptic feedback (VR)
        TriggerHapticFeedback(hapticIntensity, hapticDuration);
        
        Debug.Log("Successful slice!");
    }
    
    private void OnFailedSlice()
    {
        // Audio
        if (sliceAudioSource != null && failSliceSound != null)
        {
            sliceAudioSource.PlayOneShot(failSliceSound);
        }
        
        // Lighter haptic feedback
        TriggerHapticFeedback(hapticIntensity * 0.3f, hapticDuration * 0.5f);
        
        Debug.Log("Failed slice!");
    }
    
    private void TriggerHapticFeedback(float intensity, float duration)
    {
        // For Pico VR - you'll need to implement based on Pico SDK
        // Example for generic VR:
        /*
        if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid)
        {
            UnityEngine.XR.HapticCapabilities capabilities;
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, intensity, duration);
                }
            }
        }
        */
        
        Debug.Log($"Haptic feedback: {intensity} intensity for {duration} seconds");
    }
}