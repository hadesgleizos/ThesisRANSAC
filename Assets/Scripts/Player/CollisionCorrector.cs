using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionCorrector : MonoBehaviour
{
    [Header("Collision Prevention Settings")]
    [SerializeField] private float correctionForce = 10f;
    [SerializeField] private float checkDistance = 0.3f;
    [SerializeField] private LayerMask wallLayers = -1; // Default to all layers
    [SerializeField] private bool visualizeRays = false;

    [Header("Advanced Settings")]
    [SerializeField] private int raycastCount = 12; // Increased from 8 to 12 for better coverage
    [SerializeField] private float clipPreventionMultiplier = 3f; // Increased from 2 to 3
    [SerializeField] private float cornerDetectionAngle = 120f; // Angle to detect corner cases
    [SerializeField] private float teleportThreshold = 0.5f; // When to teleport the player away from walls

    [Header("Zombie Attack Protection")]
    [SerializeField] private bool preventZombiePushing = true;
    [SerializeField] private string zombieTag = "Zombie";
    [SerializeField] private bool makeZombiesPassThrough = true; // Complete passthrough option
    [SerializeField] private bool absolutePhysicsImmunity = true; // NEW: Complete physics immunity from zombies

    [Header("Emergency Measures")]
    [SerializeField] private bool enableEmergencyTeleport = true;
    [SerializeField] private float emergencyEscapeDistance = 0.3f;
    [SerializeField] private float stuckTimeThreshold = 0.5f; // Time to detect being stuck
    [SerializeField] private bool restorePositionWhenStuck = true; // NEW: Restore position when stuck
    
    // NEW: Keep track of safe positions
    private Vector3 lastSafePosition;
    private float timeSinceLastSafePosition = 0f;
    private float safePositionUpdateInterval = 0.2f;
    
    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private bool isBeingPushed = false;
    private Vector3 pushDirection;
    private float lastPushTime;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private bool isStuck = false;
    private HashSet<Collider> ignoredColliders = new HashSet<Collider>();
    
    // NEW: Add CMF Mover compatibility if present
    private CMF.Mover moverComponent;
    private bool usingCMFMover = false;
    
    // NEW: Position history for detecting sudden teleportation through walls
    private Queue<Vector3> positionHistory = new Queue<Vector3>();
    private int positionHistoryLength = 5;
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        
        if (rb == null)
        {
            Debug.LogError("CollisionCorrector requires a Rigidbody component on the player!");
        }
        
        if (playerCollider == null)
        {
            // Try to find any collider if capsule collider isn't available
            playerCollider = GetComponent<CapsuleCollider>();
            if (playerCollider == null)
            {
                Debug.LogWarning("No CapsuleCollider found. CollisionCorrector works best with a CapsuleCollider.");
            }
        }
        
        // NEW: Try to get CMF Mover component
        moverComponent = GetComponent<CMF.Mover>();
        usingCMFMover = (moverComponent != null);
        if (usingCMFMover)
        {
            Debug.Log("CMF Mover found - enabling compatibility mode");
            
            // Set the appropriate CMF Mover settings for better collision prevention
            moverComponent.SetColliderThickness(1.2f);
            moverComponent.SetStepHeightRatio(0.1f);
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
            }
        }
    }
    
    void FixedUpdate()
    {
        // Track if we're being pushed hard by external forces
        TrackPushingForces();
        
        // NEW: Immediately cancel ALL forces from zombies if the absolute immunity is enabled
        if (preventZombiePushing && absolutePhysicsImmunity && isBeingPushed)
        {
            CancelZombiePushingForces();
        }
        
        // Cast rays to detect and prevent wall penetration
        CheckAndPreventWallClipping();
        
        // Check if player is stuck
        CheckIfStuck();
        
        // NEW: Check for sudden position changes that might indicate clipping through walls
        CheckForTeleportation();
    }
    
    // NEW: Check if sudden teleportation occurred (might indicate clipping through walls)
    private void CheckForTeleportation()
    {
        if (positionHistory.Count < 2) return;
        
        Vector3 oldestPosition = positionHistory.Peek();
        Vector3 currentPosition = transform.position;
        
        float distance = Vector3.Distance(oldestPosition, currentPosition);
        float expectedMaxDistance = 2.0f; // Maximum reasonable distance to travel in our history window
        
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
                rb.velocity = Vector3.zero;
            }
        }
    }
    
    // NEW: Check if the player is near a wall
    private bool IsNearWall(float checkDist)
    {
        Vector3 center = GetPlayerCenter();
        
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
    
    // NEW: Completely cancel zombie pushing forces
    private void CancelZombiePushingForces()
    {
        // Only keep vertical velocity to not interfere with jumping/falling
        float verticalVelocity = rb.velocity.y;
        
        // Zero out the current velocity
        rb.velocity = new Vector3(0f, verticalVelocity, 0f);
        
        // Also clear any angular velocity
        rb.angularVelocity = Vector3.zero;
    }
    
    private void TrackPushingForces()
    {
        // If we're being pushed with significant force (like from zombies)
        if (rb.velocity.magnitude > 2.0f)
        {
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            
            // If this is a new push or significantly different direction
            if (!isBeingPushed || Vector3.Angle(horizontalVelocity, pushDirection) > 45f)
            {
                isBeingPushed = true;
                pushDirection = horizontalVelocity.normalized;
                lastPushTime = Time.time;
            }
        }
        else if (isBeingPushed && Time.time - lastPushTime > 0.5f)
        {
            // We're no longer being pushed
            isBeingPushed = false;
        }
    }
    
    // Check if player is stuck against a wall
    private void CheckIfStuck()
    {
        // Calculate movement since last frame
        float movementMagnitude = Vector3.Distance(transform.position, lastPosition);
        
        // If we're being pushed but haven't moved much
        if (isBeingPushed && movementMagnitude < 0.01f && rb.velocity.magnitude > 0.5f)
        {
            stuckTimer += Time.fixedDeltaTime;
            
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
                
                // Clear any velocity to prevent continued pushing
                rb.velocity = Vector3.zero;
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
        // Cast rays in many directions to find open space
        Vector3 bestDirection = Vector3.zero;
        float bestDistance = 0f;
        
        Vector3 center = GetPlayerCenter();
        
        for (int i = 0; i < 36; i++) // Check many more directions
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
    
    private Vector3 GetPlayerCenter()
    {
        Vector3 center = transform.position;
        if (playerCollider != null)
        {
            center = transform.position + playerCollider.center;
        }
        return center;
    }
    
    private void CheckAndPreventWallClipping()
    {
        Vector3 center = GetPlayerCenter();
        
        // Calculate ray length based on player size
        float rayLength = checkDistance;
        if (playerCollider != null)
        {
            rayLength += playerCollider.radius;
        }
        
        // Store closest wall hits to detect corners
        List<RaycastHit> wallHits = new List<RaycastHit>();
        List<Vector3> hitDirections = new List<Vector3>();
        
        // Cast rays in multiple directions to detect walls
        for (int i = 0; i < raycastCount; i++)
        {
            // Calculate evenly distributed directions around the player
            float angle = i * (360f / raycastCount);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            // If we're being pushed, prioritize checking in the push direction
            float currentRayLength = rayLength;
            if (isBeingPushed)
            {
                float angleToPush = Vector3.Angle(direction, pushDirection);
                if (angleToPush < 90f)
                {
                    // Longer rays in the direction we're being pushed
                    currentRayLength *= 1.5f;
                }
            }
            
            // Cast the ray to check for walls
            RaycastHit hit;
            if (Physics.Raycast(center, direction, out hit, currentRayLength, wallLayers))
            {
                // Debug visualization
                if (visualizeRays)
                {
                    Debug.DrawRay(center, direction * currentRayLength, Color.red, 0.1f);
                    Debug.DrawRay(hit.point, hit.normal, Color.yellow, 0.1f);
                }
                
                // Store this hit for corner detection
                wallHits.Add(hit);
                hitDirections.Add(direction);
                
                // Calculate how much the player is penetrating the wall
                float penetrationDepth = currentRayLength - hit.distance;
                
                // Apply a corrective force proportional to penetration depth
                if (penetrationDepth > 0)
                {
                    // Apply much stronger correction when in a corner or being pushed
                    float multiplier = isBeingPushed ? clipPreventionMultiplier : 1f;
                    
                    // Scale correction force by penetration depth
                    float forceMagnitude = correctionForce * penetrationDepth * multiplier;
                    
                    // Apply the force away from the wall
                    rb.AddForce(hit.normal * forceMagnitude, ForceMode.Impulse);
                    
                    // If we're deeply penetrating, teleport away from the wall
                    if (penetrationDepth > currentRayLength * teleportThreshold)
                    {
                        transform.position += hit.normal * 0.15f;
                        
                        // NEW: Reset velocity when deeply penetrating a wall
                        rb.velocity = Vector3.zero;
                    }
                }
            }
            else if (visualizeRays)
            {
                // Debug visualization for rays that don't hit
                Debug.DrawRay(center, direction * currentRayLength, Color.green, 0.1f);
            }
        }
        
        // Check for corner case (multiple close walls)
        if (wallHits.Count >= 2)
        {
            CheckForCornerCase(wallHits, hitDirections);
        }
    }
    
    // Detect and handle corner cases
    private void CheckForCornerCase(List<RaycastHit> hits, List<Vector3> directions)
    {
        bool isInCorner = false;
        Vector3 escapeDirection = Vector3.zero;
        
        // Check angles between hit directions to detect corners
        for (int i = 0; i < hits.Count; i++)
        {
            for (int j = i + 1; j < hits.Count; j++)
            {
                float angle = Vector3.Angle(directions[i], directions[j]);
                
                // If the angle between walls is large enough, we're in a corner
                if (angle > cornerDetectionAngle)
                {
                    isInCorner = true;
                    
                    // Calculate an escape direction (average of the two normals)
                    Vector3 cornerEscape = (hits[i].normal + hits[j].normal).normalized;
                    escapeDirection += cornerEscape;
                }
            }
        }
        
        if (isInCorner && isBeingPushed)
        {
            if (escapeDirection != Vector3.zero)
            {
                // Apply a strong escape force and/or position adjustment
                rb.AddForce(escapeDirection * correctionForce * 2f, ForceMode.Impulse);
                
                if (enableEmergencyTeleport)
                {
                    transform.position += escapeDirection * 0.1f;
                    
                    // Also cancel velocity when in a corner being pushed
                    rb.velocity = Vector3.zero;
                }
            }
        }
    }
    
    // Handle continuous collisions with walls
    void OnCollisionStay(Collision collision)
    {
        // NEW: If this is a zombie collision and we should ignore it completely
        if (preventZombiePushing && 
            (collision.gameObject.CompareTag(zombieTag) || 
             collision.transform.root.CompareTag(zombieTag)))
        {
            if (makeZombiesPassThrough)
            {
                // Make all zombie parts pass through player
                foreach (var contact in collision.contacts)
                {
                    if (contact.otherCollider != null)
                    {
                        Physics.IgnoreCollision(GetComponent<Collider>(), contact.otherCollider, true);
                        ignoredColliders.Add(contact.otherCollider);
                    }
                }
                
                // Cancel all velocity if using absolute immunity
                if (absolutePhysicsImmunity)
                {
                    CancelZombiePushingForces();
                }
            }
            return;
        }
        
        // If we're colliding with something while being pushed
        if (isBeingPushed || isStuck)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                // If the contact normal is mostly horizontal (wall)
                if (Mathf.Abs(contact.normal.y) < 0.5f)
                {
                    // Apply a stronger force away from the wall
                    rb.AddForce(contact.normal * correctionForce * 1.5f, ForceMode.Impulse);
                    
                    if (visualizeRays)
                    {
                        Debug.DrawRay(contact.point, contact.normal * 0.5f, Color.blue, 0.2f);
                    }
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!preventZombiePushing) return;
        
        // Check if we're colliding with a zombie (directly or via parent)
        bool isZombie = collision.gameObject.CompareTag(zombieTag) || 
                         collision.transform.root.CompareTag(zombieTag);
        
        if (isZombie)
        {
            // Handle zombie collisions
            HandleZombieCollision(collision);
            
            // NEW: Immediately cancel all velocity if using absolute immunity
            if (absolutePhysicsImmunity)
            {
                CancelZombiePushingForces();
            }
        }
    }
    
    private void HandleZombieCollision(Collision collision)
    {
        // Get all colliders on this zombie, including child objects
        Collider[] zombieColliders = collision.gameObject.GetComponentsInChildren<Collider>();
        
        if (makeZombiesPassThrough)
        {
            // Make all zombie colliders pass through player
            Collider playerCollider = GetComponent<Collider>();
            foreach (Collider zombieCollider in zombieColliders)
            {
                if (zombieCollider != null && playerCollider != null)
                {
                    Physics.IgnoreCollision(zombieCollider, playerCollider, true);
                    ignoredColliders.Add(zombieCollider);
                }
            }
            
            // Cancel any momentum from the zombie
            Vector3 zombieDirection = (transform.position - collision.transform.position).normalized;
            rb.velocity = Vector3.ProjectOnPlane(rb.velocity, zombieDirection);
        }
        else
        {
            // Apply the original logic for partial push prevention
            Rigidbody zombieRb = collision.gameObject.GetComponent<Rigidbody>();
            if (zombieRb != null)
            {
                // Make the collision not affect the player's physics
                Collider playerCollider = GetComponent<Collider>();
                Physics.IgnoreCollision(collision.collider, playerCollider, true);
                
                // Remember this collider
                ignoredColliders.Add(collision.collider);
                
                // Re-enable collision after a short delay (so damage can still register)
                StartCoroutine(ReenableCollision(collision.collider));
            }
        }
    }

    private IEnumerator ReenableCollision(Collider zombieCollider)
    {
        // Wait a short time (enough for attack to complete but not too long)
        yield return new WaitForSeconds(0.5f);
        
        // Re-enable collision if objects still exist
        if (zombieCollider != null && gameObject != null && GetComponent<Collider>() != null)
        {
            Physics.IgnoreCollision(zombieCollider, GetComponent<Collider>(), false);
            ignoredColliders.Remove(zombieCollider);
        }
    }
    
    // Clean up ignored collisions when object is destroyed or disabled
    private void OnDisable()
    {
        foreach (var collider in ignoredColliders)
        {
            if (collider != null && gameObject != null && GetComponent<Collider>() != null)
            {
                Physics.IgnoreCollision(collider, GetComponent<Collider>(), false);
            }
        }
        ignoredColliders.Clear();
    }
}
