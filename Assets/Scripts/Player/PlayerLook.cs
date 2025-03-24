using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    public Slider sensitivitySlider;  // Single slider for both sensitivities
    private float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;
    
    // Maximum allowed delta time to prevent large jumps
    [SerializeField] private float maxAllowedDeltaTime = 0.1f; 
    
    // Smoothing variables
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float smoothingFactor = 10f;
    private Vector2 currentLookVelocity;
    private Vector2 smoothedLookInput;

    void Start()
    {
        // Lock and hide the cursor when the game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Load saved sensitivity or use default value
        xSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 30f);
        ySensitivity = xSensitivity;

        // Initialize sensitivity slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1f;
            sensitivitySlider.maxValue = 100f;
            sensitivitySlider.value = xSensitivity; // Use loaded sensitivity value
            sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
        }

        // Find the invisible wall manager and set the player reference
        InvisibleWallManager wallManager = FindObjectOfType<InvisibleWallManager>();
        if (wallManager != null)
        {
            wallManager.SetPlayerReference(transform);
        }
    }

    private void UpdateSensitivity(float value)
    {
        xSensitivity = value;
        ySensitivity = value;
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    // Process the input to rotate the camera and player
    public void ProcessLook(Vector2 input)
    {
        // Cap deltaTime to prevent jumps during fps drops
        float deltaTime = Mathf.Min(Time.deltaTime, maxAllowedDeltaTime);
        
        // Apply smoothing to the input
        if (useSmoothing)
        {
            smoothedLookInput = Vector2.SmoothDamp(
                smoothedLookInput, 
                input, 
                ref currentLookVelocity, 
                0.1f, 
                Mathf.Infinity, 
                deltaTime * smoothingFactor);
            
            input = smoothedLookInput;
        }
        
        float mouseX = input.x;
        float mouseY = input.y;

        // Calculate camera rotation for both axes with capped deltaTime
        xRotation -= (mouseY * deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        
        // Apply both vertical and horizontal rotation to the camera
        float yRotation = cam.transform.eulerAngles.y + (mouseX * deltaTime) * xSensitivity;
        
        // Apply the rotation with Euler angles
        cam.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }

    public Vector2 GetLookInput()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    void Update()
    {
        // Toggle cursor lock and visibility when Alt is pressed
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            ToggleCursor();
        }
    }

    private void ToggleCursor()
    {
        // Toggle the cursor lock state and visibility
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None; // Unlock the cursor
            Cursor.visible = true; // Show the cursor
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
            Cursor.visible = false; // Hide the cursor
        }
    }
}
