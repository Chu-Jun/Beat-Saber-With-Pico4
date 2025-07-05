using UnityEngine;


public class XRControllerManager : MonoBehaviour
{
    [Header("Controller References")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor leftPokeInteractor;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor rightPokeInteractor;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor leftRayInteractor;
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor rightRayInteractor;
   
    [Header("Controller Models")]
    [SerializeField] private GameObject leftControllerModel;
    [SerializeField] private GameObject rightControllerModel;
    [SerializeField] private GameObject leftSaberModel;
    [SerializeField] private GameObject rightSaberModel;
   
    [Header("Saber Components")]
    [SerializeField] private BeatSaberSaber leftSaber;
    [SerializeField] private BeatSaberSaber rightSaber;
   
    private bool isInSaberMode = false;
   
    public bool IsInSaberMode => isInSaberMode;
   
    void Start()
    {
        // Start in controller mode by default
        SwitchToControllerMode();
    }
   
    public void SwitchToSaberMode()
    {
        if (isInSaberMode) return;
       
        Debug.Log("Switching to Saber Mode");
       
        // Disable controller interactors
        SetInteractorsEnabled(false);
       
        // Switch models
        SetControllerModelsActive(false);
        SetSaberModelsActive(true);
       
        // Enable saber components
        SetSabersEnabled(true);
       
        isInSaberMode = true;
    }
   
    public void SwitchToControllerMode()
    {
        if (!isInSaberMode) return;
       
        Debug.Log("Switching to Controller Mode");
       
        // Disable saber components
        SetSabersEnabled(false);
       
        // Switch models
        SetSaberModelsActive(false);
        SetControllerModelsActive(true);
       
        // Enable controller interactors
        SetInteractorsEnabled(true);
       
        isInSaberMode = false;
    }
   
    private void SetInteractorsEnabled(bool enabled)
    {
        if (leftPokeInteractor != null)
            leftPokeInteractor.enabled = enabled;
       
        if (rightPokeInteractor != null)
            rightPokeInteractor.enabled = enabled;
       
        if (leftRayInteractor != null)
            leftRayInteractor.enabled = enabled;
       
        if (rightRayInteractor != null)
            rightRayInteractor.enabled = enabled;
    }
   
    private void SetControllerModelsActive(bool active)
    {
        if (leftControllerModel != null)
            leftControllerModel.SetActive(active);
       
        if (rightControllerModel != null)
            rightControllerModel.SetActive(active);
    }
   
    private void SetSaberModelsActive(bool active)
    {
        if (leftSaberModel != null)
            leftSaberModel.SetActive(active);
       
        if (rightSaberModel != null)
            rightSaberModel.SetActive(active);
    }
   
    private void SetSabersEnabled(bool enabled)
    {
        if (leftSaber != null)
            leftSaber.enabled = enabled;
       
        if (rightSaber != null)
            rightSaber.enabled = enabled;
    }
   
    // Auto-find components if not assigned in inspector
    private void FindComponentsIfNull()
    {
        if (leftSaber == null)
        {
            BeatSaberSaber[] sabers = FindObjectsOfType<BeatSaberSaber>();
            foreach (var saber in sabers)
            {
                if (saber.saberType == BeatSaberSaber.SaberType.Left)
                    leftSaber = saber;
                else if (saber.saberType == BeatSaberSaber.SaberType.Right)
                    rightSaber = saber;
            }
        }
    }
   
    void OnValidate()
    {
        // Auto-find components in editor
        FindComponentsIfNull();
    }
}


