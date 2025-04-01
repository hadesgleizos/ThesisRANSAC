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
        // Remove the Rigidbody code if you don't need it with CharacterController
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
        // Store previous Y velocity to handle jumping properly
        float previousYVelocity = playerVelocity.y;
        
        // Handle grounded state and gravity
        if (isGrounded && previousYVelocity < 0)
        {
            playerVelocity.y = groundedGravity;
        }
        else
        {
            // Apply stronger gravity when falling
            float currentGravity = gravity * (previousYVelocity < 0 ? gravityMultiplier : 1f);
            playerVelocity.y += currentGravity * Time.deltaTime;
            
            // Clamp fall speed
            playerVelocity.y = Mathf.Max(playerVelocity.y, maxFallSpeed);
        }

        // Create a single movement vector for both horizontal and vertical motion
        Vector3 finalMoveVector = Vector3.zero;
        
        // Only add horizontal movement when there's actual input
        if (input.magnitude > 0.1f)
        {
            // Get camera direction vectors and flatten them
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            
            // ZERO OUT the y component completely to ensure movement stays horizontal
            forward.y = 0;
            right.y = 0;
            
            // Normalize to maintain consistent speed
            forward.Normalize();
            right.Normalize();

            // Calculate horizontal movement based on camera orientation
            Vector3 horizontalMove = (right * input.x + forward * input.y).normalized;
            
            // Set horizontal components of final movement
            finalMoveVector.x = horizontalMove.x * speed * Time.deltaTime;
            finalMoveVector.z = horizontalMove.z * speed * Time.deltaTime;
        }
        
        // Add vertical movement (completely separate from camera angle)
        finalMoveVector.y = playerVelocity.y * Time.deltaTime;
        
        // Apply the combined movement in a single call
        controller.Move(finalMoveVector);
        
        // Debug.Log to check movement components
        if (input.magnitude > 0.1f || Mathf.Abs(playerVelocity.y) > 0.1f)
        {
            //Debug.Log($"Movement: H={input.magnitude:F2}, V={playerVelocity.y:F2}, Looking Y={Camera.main.transform.forward.y:F2}");
        }
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
