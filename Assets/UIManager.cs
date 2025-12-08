using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Reference to INaturalistMapController
    private INaturalistMapController inatController;

    void Awake()
    {
        // Find the INaturalistMapController in the scene
        inatController = FindObjectOfType<INaturalistMapController>();
        if (inatController == null)
        {
            Debug.LogWarning("UIManager: INaturalistMapController not found in scene.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // ...existing code...
    }
}
