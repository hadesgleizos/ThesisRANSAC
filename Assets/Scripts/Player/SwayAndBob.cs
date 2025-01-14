using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwayAndBob : MonoBehaviour
{
    public Transform cameraTransform;

    [Header("Sway Settings")]
    public float swayStep = 0.01f;
    public float maxSwayDistance = 0.06f;
    public float swaySmooth = 10f;

    [Header("Sway Rotation Settings")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    public float rotationSmooth = 12f;

    [Header("Bobbing Settings")]
    public float bobSpeed = 8f;
    public float bobAmount = 0.1f;
    public float bobExaggeration = 1f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    public Vector3 travelLimit = Vector3.one * 0.025f;

    [Header("Bob Rotation Settings")]
    public Vector3 rotationMultiplier;

    private Vector3 initialPosition;
    private Vector3 swayPosition;
    private Vector3 bobPosition;
    private Vector3 swayRotation;
    private Vector3 bobRotation;

    private float speedCurve;
    private float timer;
    private PlayerMotor playerMotor;

    void Start()
    {
        initialPosition = transform.localPosition;
        playerMotor = GetComponentInParent<PlayerMotor>();
    }

    void Update()
    {
        ApplySway();
        ApplySwayRotation();
        ApplyBob();
        ApplyBobRotation();

        CompositePositionAndRotation();
    }

    void ApplySway()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector3 swayOffset = new Vector3(-mouseInput.x * swayStep, -mouseInput.y * swayStep, 0);
        swayOffset.x = Mathf.Clamp(swayOffset.x, -maxSwayDistance, maxSwayDistance);
        swayOffset.y = Mathf.Clamp(swayOffset.y, -maxSwayDistance, maxSwayDistance);
        swayPosition = swayOffset;
    }

    void ApplySwayRotation()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector3 swayRot = new Vector3(-mouseInput.y * rotationStep, -mouseInput.x * rotationStep, -mouseInput.x * rotationStep);
        swayRot.x = Mathf.Clamp(swayRot.x, -maxRotationStep, maxRotationStep);
        swayRot.y = Mathf.Clamp(swayRot.y, -maxRotationStep, maxRotationStep);
        swayRotation = swayRot;
    }

    void ApplyBob()
    {
        Vector2 movementInput = playerMotor.GetMovementInput();
        float movementMagnitude = movementInput.magnitude;

        if (movementMagnitude > 0)
        {
            speedCurve += Time.deltaTime * bobSpeed;
            float bobSin = Mathf.Sin(speedCurve);
            float bobCos = Mathf.Cos(speedCurve);

            bobPosition.x = (bobCos * bobLimit.x) - (movementInput.x * travelLimit.x);
            bobPosition.y = bobSin * bobLimit.y;
            bobPosition.z = -(movementInput.y * travelLimit.z);
        }
        else
        {
            speedCurve = 0;
            bobPosition = Vector3.Lerp(bobPosition, Vector3.zero, Time.deltaTime * bobSpeed);
        }
    }

    void ApplyBobRotation()
    {
        Vector2 movementInput = playerMotor.GetMovementInput();

        if (movementInput != Vector2.zero)
        {
            float bobSin = Mathf.Sin(2 * speedCurve);
            float bobCos = Mathf.Cos(speedCurve);

            bobRotation.x = rotationMultiplier.x * bobSin;
            bobRotation.y = rotationMultiplier.y * bobCos;
            bobRotation.z = rotationMultiplier.z * bobCos * movementInput.x;
        }
        else
        {
            bobRotation = Vector3.Lerp(bobRotation, Vector3.zero, Time.deltaTime * rotationSmooth);
        }
    }

    void CompositePositionAndRotation()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + swayPosition + bobPosition, Time.deltaTime * swaySmooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayRotation) * Quaternion.Euler(bobRotation), Time.deltaTime * rotationSmooth);
    }
}
