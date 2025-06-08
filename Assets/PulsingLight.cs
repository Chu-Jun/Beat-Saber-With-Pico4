using UnityEngine;

public class PulsingLight : MonoBehaviour
{
    public Light lightComponent;
    public float minIntensity = 1f;
    public float maxIntensity = 4f;
    public float pulseSpeed = 2f;
    
    private float originalIntensity;
    
    void Start()
    {
        if (lightComponent == null)
            lightComponent = GetComponent<Light>();
        originalIntensity = lightComponent.intensity;
    }
    
    void Update()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        lightComponent.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
    }
}