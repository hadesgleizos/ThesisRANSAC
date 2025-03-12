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

    public float groundedGravity = -2f;
    public float gravityMultiplier = 2f;
    public float maxFallSpeed = -20f;

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
        // Handle grounded state and gravity
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = groundedGravity;
        }
        else
        {
            // Apply stronger gravity when falling
            float currentGravity = gravity * (playerVelocity.y < 0 ? gravityMultiplier : 1f);
            playerVelocity.y += currentGravity * Time.deltaTime;
            
            // Clamp fall speed
            playerVelocity.y = Mathf.Max(playerVelocity.y, maxFallSpeed);
        }

        // Handle horizontal movement
        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMove = right * moveDirection.x + forward * moveDirection.z;
        
        // Apply movements separately to maintain better control
        controller.Move(desiredMove * speed * Time.deltaTime);
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
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            // Reset the grounded state to prevent double jumps
            isGrounded = false;
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
