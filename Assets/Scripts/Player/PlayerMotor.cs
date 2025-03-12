using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    public bool isGrounded;
    public float gravity = -9.8f;
    public float speed = 5f;
    public float jumpHeight = 3f;

    private float footstepTimer = 0f;
    public float footstepInterval = 0.5f; // Time between footsteps

    public Transform raycastOrigin; // GameObject or Transform as raycast origin

    private Vector2 movementInput; // Cache the movement input

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        movementInput = GetMovementInput();

        // Play footstep sound only if the player is grounded and moving
        if (isGrounded && movementInput.magnitude > 0.1f)
        {
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
    }

    public void ProcessMove(Vector2 input)
    {
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        // Get the camera's forward and right vectors, but ignore Y component
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction relative to camera
        Vector3 desiredMove = right * moveDirection.x + forward * moveDirection.z;

        // Apply movement
        controller.Move(desiredMove * speed * Time.deltaTime);

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        controller.Move(playerVelocity * Time.deltaTime);
    }

    public Vector2 GetMovementInput()
    {
        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    public void Jump()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }

private void PlayFootstepSound()
{
    if (raycastOrigin == null)
    {
        return;
    }

    RaycastHit hit;
    Vector3 rayOrigin = raycastOrigin.position; // Use the specified raycast origin

    if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 1f))
    {
        string groundTag = hit.collider.tag;

        // Play the footstep sound directly using SoundManager
        SoundManager.Instance.PlayFootstep(groundTag);
    }
}


    private void OnDrawGizmos()
    {
        if (raycastOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(raycastOrigin.position, raycastOrigin.position + Vector3.down * 1f);
        }
    }
}
