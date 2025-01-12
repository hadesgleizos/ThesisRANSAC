using UnityEngine;

public class ArmPositionController : MonoBehaviour
{
    public Transform cameraTransform; // Assign the camera transform in the Inspector

    void Update()
    {
        // Lock the arm's rotation to the camera's horizontal rotation only
        Vector3 fixedRotation = new Vector3(0, cameraTransform.eulerAngles.y, 0);
        transform.eulerAngles = fixedRotation;
    }
}
