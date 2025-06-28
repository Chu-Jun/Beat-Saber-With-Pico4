using UnityEngine;

public class FixedUIPositioner : MonoBehaviour
{
    [Header("UI Position Settings")]
    [SerializeField] private Vector3 uiPosition = new Vector3(0f, 2.5f, 4f);
    [SerializeField] private Vector3 uiRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 uiScale = new Vector3(0.005f, 0.005f, 0.005f);
    
    [Header("Play Area Reference")]
    [SerializeField] private Transform playAreaCenter; // Optional: reference to play area center
    
    private void Start()
    {
        PositionUI();
    }
    
    private void PositionUI()
    {
        // Set fixed position relative to play area center or world origin
        Vector3 finalPosition = uiPosition;
        
        if (playAreaCenter != null)
        {
            finalPosition = playAreaCenter.position + uiPosition;
        }
        
        transform.position = finalPosition;
        transform.rotation = Quaternion.Euler(uiRotation);
        transform.localScale = uiScale;
    }
    
    // Call this method if you need to reposition UI during gameplay
    public void ResetToFixedPosition()
    {
        PositionUI();
    }
    
    // Method to adjust position during development
    [ContextMenu("Update UI Position")]
    private void UpdateUIPosition()
    {
        PositionUI();
    }
}