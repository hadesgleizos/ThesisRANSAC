using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingPacks : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;
    public Vector3 rotationAxis = Vector3.up;

    [Header("Floating Settings")]
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 1f;
    
    private Vector3 startPosition;
    private float sinTime;
    private Rigidbody rb;

    void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        
        // Configure Rigidbody for kinematic motion
        if (rb != null)
        {
            rb.isKinematic = true;  // Prevent physics forces from affecting the object
            rb.useGravity = false;  // Disable gravity
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth out motion
        }
    }

    void Update()
    {
        // Rotate the object
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);

        // Calculate floating movement using sine wave
        sinTime += Time.deltaTime * floatFrequency;
        float yOffset = Mathf.Sin(sinTime) * floatAmplitude;
        
        // Apply floating movement
        transform.position = startPosition + Vector3.up * yOffset;
    }
}
