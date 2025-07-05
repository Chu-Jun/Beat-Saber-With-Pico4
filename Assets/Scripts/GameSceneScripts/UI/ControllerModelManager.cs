using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ControllerModelManager : MonoBehaviour
{
    [Header("Saber Components")]
    [SerializeField] private GameObject leftSaber;
    [SerializeField] private GameObject rightSaber;
   
    [Header("Pico Controller Components")]
    [SerializeField] private GameObject leftPicoController;
    [SerializeField] private GameObject rightPicoController;
   
    [Header("XR Interaction GameObjects")]
    [SerializeField] private GameObject leftDirectInteractorGameObject;
    [SerializeField] private GameObject rightDirectInteractorGameObject;
    [SerializeField] private GameObject leftRayInteractorGameObject;
    [SerializeField] private GameObject rightRayInteractorGameObject;
   
    private bool isInGameMode = true;
   
    // Events
    public event System.Action OnSwitchToSabers;
    public event System.Action OnSwitchToControllers;
   
    void Start()
    {
        // Find GameManager and subscribe to game state events
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStarted += SwitchToSaberMode;
            gameManager.OnGameCompleted += SwitchToControllerMode;
        }
       
        // Start in saber mode if game is active by default
        if (isInGameMode)
        {
            // SwitchToSaberMode();
            SwitchToControllerMode();
        }
        else
        {
            SwitchToControllerMode();
        }
    }
   
    public void SwitchToSaberMode()
    {
        Debug.Log("Switching to Saber Mode");
        isInGameMode = true;
       
        // Enable sabers
        SetSaberActive(true);
       
        // Disable Pico controllers
        SetPicoControllerActive(false);
       
        // Disable interaction GameObjects
        SetInteractionGameObjectsActive(false);
       
        OnSwitchToSabers?.Invoke();
    }
   
    public void SwitchToControllerMode()
    {
        Debug.Log("Switching to Controller Mode");
        isInGameMode = false;
       
        // Disable sabers
        SetSaberActive(false);
       
        // Enable Pico controllers
        SetPicoControllerActive(true);
       
        // Enable interaction GameObjects
        SetInteractionGameObjectsActive(true);
       
        OnSwitchToControllers?.Invoke();
    }
   
    private void SetSaberActive(bool active)
    {
        if (leftSaber != null)
        {
            leftSaber.SetActive(active);
        }
       
        if (rightSaber != null)
        {
            rightSaber.SetActive(active);
        }
    }
   
    private void SetPicoControllerActive(bool active)
    {
        if (leftPicoController != null)
        {
            leftPicoController.SetActive(active);
        }
       
        if (rightPicoController != null)
        {
            rightPicoController.SetActive(active);
        }
    }
   
    private void SetInteractionGameObjectsActive(bool active)
    {
        // Enable/Disable Direct Interactor GameObjects (for poke interaction)
        if (leftDirectInteractorGameObject != null)
        {
            leftDirectInteractorGameObject.SetActive(active);
        }
       
        if (rightDirectInteractorGameObject != null)
        {
            rightDirectInteractorGameObject.SetActive(active);
        }
       
        // Enable/Disable Ray Interactor GameObjects (for far interaction)
        if (leftRayInteractorGameObject != null)
        {
            leftRayInteractorGameObject.SetActive(active);
        }
       
        if (rightRayInteractorGameObject != null)
        {
            rightRayInteractorGameObject.SetActive(active);
        }
    }
   
    // Public method to manually toggle between modes
    public void ToggleControllerMode()
    {
        if (isInGameMode)
        {
            SwitchToControllerMode();
        }
        else
        {
            SwitchToSaberMode();
        }
    }
   
    // Get current mode
    public bool IsInGameMode()
    {
        return isInGameMode;
    }
   
    void OnDestroy()
    {
        // Clean up event subscriptions
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStarted -= SwitchToSaberMode;
            gameManager.OnGameCompleted -= SwitchToControllerMode;
        }
    }
}


