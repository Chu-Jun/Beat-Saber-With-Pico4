using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glowing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Renderer renderer = GameObject.Find("Cube-Point-Bottom-With-Glow").GetComponent<Renderer>();
        Material mat = renderer.material;

        Color glowColor = new Color(0f, 0.75f, 1f, 1f); // Bright blue
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", glowColor * 5f); // Multiply for intensity
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
