using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionCorrector : MonoBehaviour
{
    [Header("Collision Prevention Settings")]
    [SerializeField] private float correctionDistance = 0.3f;
    [SerializeField] private LayerMask wallLayers = -1; // Default to all layers
    [SerializeField] private bool visualizeRays = false;

    [Header("Advanced Settings")]
    [SerializeField] private int raycastCount = 12;
    [SerializeField] private float teleportThreshold = 0.5f;

    [Header("Zombie Attack Protection")]
    [SerializeField] private bool preventZombiePushing = true;
    [SerializeField] private string zombieTag = "Zombie";
    [SerializeField] private bool makeZombiesPassThrough = true;

    [Header("Emergency Measures")]
    [SerializeField] private bool enableEmergencyTeleport = true;
    [SerializeField] private float emergencyEscapeDistance = 0.3f;
    [SerializeField] private float stuckTimeThreshold = 0.5f;
    [SerializeField] private bool restorePositionWhenStuck = true;
    
    // Keep track of safe positions
    private Vector3 lastSafePosition;
    private float timeSinceLastSafePosition = 0f;
    private float safePositionUpdateInterval = 0.2f;
    
    private CharacterController characterController;
    private bool isBeingPushed = false;
    private Vector3 pushDirection;
    private float lastPushTime;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private bool isStuck = false;
    private HashSet<Collider> ignoredColliders = new HashSet<Collider>();
    
    // Position history for detecting clipping
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private int positionHistoryLength = 5;
    private float lastUpdateTime = 0f;

    [Header("Anti-Cheese Settings")]
    [SerializeField] private bool preventStandingOnZombies = true;
    [SerializeField] private float zombieTopCheckRadius = 0.5f;
    [SerializeField] private float maxStandingOnZombieTime = 0.5f;
    private float standingOnZombieTimer = 0f;
    private bool isOnTopOfZombie = false;
    private Vector3 lastGroundedPosition;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            Debug.LogError("CollisionCorrector requires a CharacterController component on the player!");
            this.enabled = false;
            return;
        }
        
        // Initialize position tracking
        lastPosition = transform.position;
        lastSafePosition = transform.position;
        
        // Initialize position history
        for (int i = 0; i < positionHistoryLength; i++)
        {
            positionHistory.Enqueue(transform.position);
        }
    }
    
    void Update()
    {
        // Record position history at a fixed interval
        if (Time.time - lastUpdateTime > 0.05f)
        {
            lastUpdateTime = Time.time;
            positionHistory.Dequeue();
            positionHistory.Enqueue(transform.position);
        }
        
        // Update the last safe position periodically when not stuck or pushed
        timeSinceLastSafePosition += Time.deltaTime;
        if (timeSinceLastSafePosition > safePositionUpdateInterval && !isBeingPushed && !isStuck)
        {
            // Only update safe position if not too close to a wall
            if (!IsNearWall(0.3f))
            {
                lastSafePosition = transform.position;
                timeSinceLastSafePosition = 0f;
                
                // If we're on ground and not on zombies, update lastGroundedPosition
                if (characterController.isGrounded && !isOnTopOfZombie)
                {
                    lastGroundedPosition = transform.position;
                }
            }
        }
        
        // Check and prevent wall penetration
        CheckAndPreventWallClipping();
        
        // Check if player is stuck
        CheckIfStuck();
        
        // Handle zombie standing timer
        if (isOnTopOfZombie)
        {
            standingOnZombieTimer += Time.deltaTime;
            if (standingOnZombieTimer > maxStandingOnZombieTime)
            {
                ForcePlayerOffZombies();
            }
        }
        else
        {
            standingOnZombieTimer = 0f;
        }
        
        // Check if player is standing on zombies
        if (preventStandingOnZombies)
        {
            CheckIfStandingOnZombies();
        }
        
        // Check for clipping through walls
        CheckForTeleportation();
    }
    
    // Check if player is currently standing on top of any zombies
    private void CheckIfStandingOnZombies()
    {
        isOnTopOfZombie = false;
        
        // Cast a short ray downward to detect what's below
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position, 
            zombieTopCheckRadius, 
            Vector3.down, 
            0.5f
        );
        
        foreach (RaycastHit hit in hits)
        {
            // Check if we hit a zombie
            if (hit.collider.CompareTag(zombieTag) || hit.transform.root.CompareTag(zombieTag))
            {
                isOnTopOfZombie = true;
                
                // Get the horizontal direction from zombie to player
                Vector3 slideDirection = transform.position - hit.transform.position;
                slideDirection.y = 0;
                
                if (slideDirection.magnitude > 0.01f)
                {
                    slideDirection.Normalize();
                    // Move the player in the slide direction
                    transform.position += slideDirection * Time.deltaTime * 2f;
                }
                else
                {
                    // If directly on top, apply random horizontal movement
                    Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                    transform.position += randomDir * Time.deltaTime * 2f;
                }
                break;
            }
        }
    }
    
    // Force the player off zombies after standing on them too long
    private void ForcePlayerOffZombies()
    {
        if (lastGroundedPosition != Vector3.zero)
        {
            // Teleport back to last grounded position
            transform.position = lastGroundedPosition + Vector3.up * 0.5f;
            Debug.Log("Anti-cheese protection: Teleported player back to ground");
        }
        
        // Reset timer
        standingOnZombieTimer = 0f;
    }
    
    // Check if sudden teleportation occurred (might indicate clipping through walls)
    private void CheckForTeleportation()
    {
        if (positionHistory.Count < 2) return;
        
        Vector3 oldestPosition = positionHistory.Peek();
        Vector3 currentPosition = transform.position;
        
        float distance = Vector3.Distance(oldestPosition, currentPosition);
        float expectedMaxDistance = 2.0f; // Maximum reasonable distance 
        
        if (distance > expectedMaxDistance)
        {
            // We may have clipped through a wall - check if there's a wall between old and new position
            RaycastHit hit;
            Vector3 direction = (currentPosition - oldestPosition).normalized;
            if (Physics.Raycast(oldestPosition, direction, out hit, distance, wallLayers))
            {
                // There is a wall between our positions, we likely clipped through
                Debug.LogWarning("Detected potential wall clip-through! Restoring position.");
                transform.position = lastSafePosition;
            }
        }
    }
    
    // Check if the player is near a wall
    private bool IsNearWall(float checkDist)
    {
        Vector3 center = transform.position + characterController.center;
        
        for (int i = 0; i < 8; i++) // Check in 8 directions
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            if (Physics.Raycast(center, direction, checkDist, wallLayers))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Check if player is stuck against a wall
    private void CheckIfStuck()
    {
        // Calculate movement since last frame
        float movementMagnitude = Vector3.Distance(transform.position, lastPosition);
        
        // Only trigger stuck if player is trying to move but can't
        bool isInputActive = Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        
        if (isInputActive && movementMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            
            // We're stuck for too long
            if (stuckTimer > stuckTimeThreshold)
            {
                isStuck = true;
                
                // Either try emergency escape or restore to last safe position
                if (enableEmergencyTeleport)
                {
                    if (restorePositionWhenStuck)
                    {
                        // Teleport back to last safe position
                        transform.position = lastSafePosition;
                        Debug.Log("Restored to safe position!");
                    }
                    else
                    {
                        // Try to escape in a safe direction
                        AttemptEmergencyEscape();
                    }
                }
            }
        }
        else
        {
            stuckTimer = 0f;
            isStuck = false;
        }
        
        lastPosition = transform.position;
    }
    
    // Emergency teleport to escape when completely stuck
    private void AttemptEmergencyEscape()
    {
        // Find a safe direction to escape
        Vector3 escapeDirection = FindSafeEscapeDirection();
        if (escapeDirection != Vector3.zero)
        {
            // Teleport slightly in that direction
            transform.position += escapeDirection * emergencyEscapeDistance;
            Debug.Log("Emergency escape activated!");
        }
    }
    
    // Find a safe direction to escape
    private Vector3 FindSafeEscapeDirection()
    {
        Vector3 bestDirection = Vector3.zero;
        float bestDistance = 0f;
        
        Vector3 center = transform.position + characterController.center;
        
        for (int i = 0; i < 36; i++) // Check many directions
        {
            float angle = i * 10f; // Every 10 degrees
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            RaycastHit hit;
            if (!Physics.Raycast(center, direction, out hit, 1f, wallLayers))
            {
                // This direction is open, so it's a potential escape route
                return direction;
            }
            else if (hit.distance > bestDistance)
            {
                // This might be the best direction so far
                bestDistance = hit.distance;
                bestDirection = direction;
            }
        }
        
        // Return the best direction even if it's not completely open
        return bestDirection;
    }
    
    private void CheckAndPreventWallClipping()
    {
        Vector3 center = transform.position + characterController.center;
        
        // Calculate ray length based on player size
        float rayLength = correctionDistance + characterController.radius;
        
        // Cast rays in multiple directions to detect walls
        for (int i = 0; i < raycastCount; i++)
        {
            // Calculate evenly distributed directions around the player
            float angle = i * (360f / raycastCount);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            // Cast the ray to check for walls
            RaycastHit hit;
            if (Physics.Raycast(center, direction, out hit, rayLength, wallLayers))
            {
                // Debug visualization
                if (visualizeRays)
                {
                    Debug.DrawRay(center, direction * rayLength, Color.red, 0.1f);
                    Debug.DrawRay(hit.point, hit.normal, Color.yellow, 0.1f);
                }
                
                // Calculate how much the player is penetrating the wall
                float penetrationDepth = rayLength - hit.distance;
                
                // Apply a corrective position change
                if (penetrationDepth > 0)
                {
                    // Move the character away from the wall
                    transform.position += hit.normal * penetrationDepth;
                    
                    // If we're deeply penetrating, teleport away from the wall
                    if (penetrationDepth > rayLength * teleportThreshold)
                    {
                        transform.position += hit.normal * 0.15f;
                    }
                }
            }
            else if (visualizeRays)
            {
                // Debug visualization for rays that don't hit
                Debug.DrawRay(center, direction * rayLength, Color.green, 0.1f);
            }
        }
    }
    
    // Handle controller collisions with zombies
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!preventZombiePushing) return;
        
        bool isZombie = hit.gameObject.CompareTag(zombieTag) || 
                        hit.transform.root.CompareTag(zombieTag);
        
        if (isZombie && makeZombiesPassThrough)
        {
            // Ignore collision with zombie's collider
            Physics.IgnoreCollision(characterController, hit.collider, true);
            ignoredColliders.Add(hit.collider);
            
            // Re-enable collision after a short delay
            StartCoroutine(ReenableCollision(hit.collider));
        }
    }
    
    private IEnumerator ReenableCollision(Collider zombieCollider)
    {
        // Wait a short time
        yield return new WaitForSeconds(0.5f);
        
        // Re-enable collision if objects still exist
        if (zombieCollider != null && gameObject != null && characterController != null)
        {
            Physics.IgnoreCollision(characterController, zombieCollider, false);
            ignoredColliders.Remove(zombieCollider);
        }
    }
    
    private void OnDisable()
    {
        foreach (var collider in ignoredColliders)
        {
            if (collider != null && gameObject != null && characterController != null)
            {
                Physics.IgnoreCollision(characterController, collider, false);
            }
        }
        ignoredColliders.Clear();
    }
}
