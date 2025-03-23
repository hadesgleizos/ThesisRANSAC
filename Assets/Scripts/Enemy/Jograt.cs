using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static takeDamage;

public class Jograt : MonoBehaviour
{
    private NavMeshAgent agent; // Reference to the NavMeshAgent
    private Transform player; // Reference to the player's transform
    private Spawner spawner; // Reference to the Spawner script
    private PlayerPerformance playerPerformance;
    private float updateInterval = 1.0f; // How often to update speed in seconds
    private Animator animator;
    public float damage = 20f;            // Damage dealt to player
    public float attackRange = 2f;        // Range at which zombie can attack
    public float attackCooldown = 2f;     // Time between attacks
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttacking = false;     // Flag to track if zombie is attacking
    public float health = 100f;
    private Rigidbody rb; // Add this to your class variables

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;
    public float acceleration = 8f;
    public float stoppingDistance = 1.5f;

    [Header("Sound Settings")]
    public float idleSoundInterval = 5f;
    public float idleSoundVolume = 1f;
    public float attackSoundVolume = 1f;
    public float deathSoundVolume = 1f;
    private float nextIdleSoundTime;

    [Header("Debug Visualization")]
    public bool showAttackRange = true;
    public Color attackRangeColor = Color.red;
    public bool showLeapRange = true;           // Toggle for leap range visualization
    public Color leapRangeColor = Color.yellow;  // Color for leap range
    public bool showMinLeapRange = true;         // NEW: Toggle for minimum leap range
    public Color minLeapRangeColor = Color.cyan; // NEW: Color for minimum leap range

    [Header("Leap Settings")]
    public float leapRange = 8f;             // Max distance to trigger leap
    public float leapMinRange = 4f;          // NEW: Minimum distance to consider leaping
    public float leapCooldown = 5f;          // Time between leaps
    public float leapDamage = 30f;           // Damage dealt on successful leap
    public float leapForce = 90f;            // Force of the leap
    public float leapHeight = 3f;            // Height of the leap
    public float leapDuration = 0.9f;        // Duration of leap
    private bool canLeap = true;             // Flag for leap cooldown
    private bool isLeaping = false;          // Flag for when currently leaping

    [Header("Leap Particle Effects")]
    public GameObject leapTrailEffect;      // Assign in inspector - particle effect for trail
    public GameObject leapImpactEffect;     // Assign in inspector - particle effect for impact
    private ParticleSystem activeTrailEffect;  // Reference to the currently active trail effect

    void Start()
    {
        // Get existing components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        // COMPLETELY REMOVE any existing Rigidbody at start
        Rigidbody existingRb = GetComponent<Rigidbody>();
        if (existingRb != null)
        {
            Destroy(existingRb);
        }
        
        // DON'T create Rigidbody at start - we'll create it only when needed
        rb = null;
        
        // Rest of your Start() method...
        
        if (animator == null)
        {
            Debug.LogError("Animator component missing from zombie!");
        }
        
        player = GameObject.FindGameObjectWithTag("Player").transform;
        spawner = FindObjectOfType<Spawner>();

        if (agent != null && spawner != null)
        {
            float currentSpeed = spawner.GetCurrentZombieSpeed();
            SetSpeed(currentSpeed); // Use SetSpeed instead of directly setting agent.speed
            
            // Set other NavMeshAgent parameters
            agent.angularSpeed = 120;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoRepath = true;
            agent.autoBraking = true;
            
            Debug.Log($"[Zombie {gameObject.GetInstanceID()}] Initialized with speed: {currentSpeed}");
        }
        
        playerPerformance = FindObjectOfType<PlayerPerformance>();
        if (playerPerformance == null)
        {
            Debug.LogWarning("PlayerPerformance not found in scene!");
        }
        
        nextIdleSoundTime = Time.time + Random.Range(0f, idleSoundInterval);
        StartCoroutine(PlayIdleSoundsRoutine());
    }

    // Update method with improved leap decision making including minimum range
    void Update()
    {
        if (isDead) return; // If dead, don't do anything
        
        if (isLeaping || isAttacking) return; // If currently leaping or attacking, skip update
        
        // Safety check - if we're not leaping but somehow still have an active Rigidbody, destroy it
        if (!isLeaping && rb != null)
        {
            Destroy(rb);
            rb = null;
            
            // Make sure NavMeshAgent is enabled
            if (agent != null && !agent.enabled)
            {
                agent.enabled = true;
                agent.isStopped = false;
            }
        }
        
        if (player != null && agent != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // Smoothly rotate towards the player
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            
            // Double-check the distance again to make absolutely sure we make the right decision
            distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // If player is within attack range, prioritize normal attack
            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                // Normal attack when close
                StartCoroutine(AttackPlayer());
            }
            // Only leap when player is outside attack range but within leap range 
            // AND farther than the minimum leap range (to avoid unnecessary short leaps)
            else if (distanceToPlayer > attackRange + 0.5f && 
                    distanceToPlayer >= leapMinRange && 
                    distanceToPlayer <= leapRange && 
                    canLeap && !isLeaping)
            {
                // Use leap to close distance when player is significantly farther away
                isLeaping = true;
                StartCoroutine(LeapAtPlayer());
            }
            else if (distanceToPlayer > agent.stoppingDistance)
            {
                // Otherwise keep moving toward player
                agent.SetDestination(player.position);
                
                // Update animator if you have movement animations
                if (animator != null)
                {
                    animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);
                }
            }
        }
    }

    private IEnumerator PlayIdleSoundsRoutine()
    {
        while (!isDead)
        {
            if (Time.time >= nextIdleSoundTime)
            {
                // Use Jograt-specific sounds, with zombie sounds as fallback
                if (SoundManager.Instance.jogratIdleSounds != null && 
                    SoundManager.Instance.jogratIdleSounds.Length > 0)
                {
                    SoundManager.Instance.PlayRandomJogratSound(
                        SoundManager.Instance.jogratIdleSounds,
                        idleSoundVolume
                    );
                }
                else
                {
                    SoundManager.Instance.PlayRandomZombieSound(
                        SoundManager.Instance.zombieIdleSounds,
                        idleSoundVolume
                    );
                }
                
                nextIdleSoundTime = Time.time + idleSoundInterval + Random.Range(-1f, 1f);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator AttackPlayer()
    {
        if (!agent || !agent.isOnNavMesh) yield break;

        // Set the flag here only
        isAttacking = true;
        canAttack = false; 
        
        // IMPORTANT: First stop movement completely before animation
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        // Make sure we've completely stopped before attacking
        yield return new WaitForSeconds(0.1f);
        
        // Apply damage immediately if in range
        if (!isDead && player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerPerformance playerPerformance = player.GetComponent<PlayerPerformance>();
            if (playerPerformance != null)
            {
                playerPerformance.TakeDamage(damage, gameObject);
                gameObject.SetIndicator();
                Debug.Log($"Zombie dealt {damage} damage to the player.");
            }
        }

        // Play attack sound using Jograt-specific sounds
        if (SoundManager.Instance.jogratAttackSounds != null && 
            SoundManager.Instance.jogratAttackSounds.Length > 0)
        {
            SoundManager.Instance.PlayRandomJogratSound(
                SoundManager.Instance.jogratAttackSounds,
                attackSoundVolume
            );
        }
        else
        {
            SoundManager.Instance.PlayRandomZombieSound(
                SoundManager.Instance.zombieAttackSounds,
                attackSoundVolume
            );
        }

        // Make sure Speed is zero during attack to prevent blending issues
        if (animator != null)
        {
            // Force Speed to zero - this is crucial for proper animation blending
            animator.SetFloat("Speed", 0);
            
            // Wait a frame to ensure the speed parameter is applied
            yield return null;
            
            // Now trigger the attack
            animator.SetTrigger("Attack");
        }

        // Wait for attack animation - make sure this matches the actual animation length
        // You might need to adjust this value based on your animation's length
        yield return new WaitForSeconds(attackCooldown);

        // Make sure we wait for the attack animation to COMPLETELY finish
        // before resuming movement
        if (agent && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
            // Wait one more frame before setting speed to ensure clean transition
            yield return null;
            
            // Update Speed parameter to resume locomotion blending
            if (animator != null)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);
            }
        }

        // Reset flags at the very end
        canAttack = true;
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount, CollisionType hitLocation)
    {
        if (isDead) return;

        float multiplier = 1f;
        switch (hitLocation)
        {
            case CollisionType.HEAD:
                multiplier = 2f;
                break;
            case CollisionType.ARMS:
                multiplier = 0.5f;
                break;
            case CollisionType.BODY:
                multiplier = 1f;
                break;
        }

        health -= damageAmount * multiplier;

        // Trigger hit animation
        if (animator != null)
        {
            animator.SetTrigger("OnHit");
        }

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // Play death sound using Jograt-specific sounds
        if (SoundManager.Instance.jogratDeathSounds != null && 
            SoundManager.Instance.jogratDeathSounds.Length > 0)
        {
            SoundManager.Instance.PlayRandomJogratSound(
                SoundManager.Instance.jogratDeathSounds,
                deathSoundVolume
            );
        }
        else
        {
            SoundManager.Instance.PlayRandomZombieSound(
                SoundManager.Instance.zombieDeathSounds,
                deathSoundVolume
            );
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Safely destroy Rigidbody if it exists
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        if (playerPerformance != null)
        {
            playerPerformance.ZombieKilled(); // Already correctly updating kills
        }

        Destroy(gameObject, 3f);
    }

    public void SetSpeed(float newSpeed)
    {
        if (agent != null)
        {
            float oldSpeed = agent.speed;
            agent.speed = newSpeed;
            agent.acceleration = acceleration * (newSpeed / 3.5f);
            Debug.Log($"[Zombie {gameObject.GetInstanceID()}] Speed changed from {oldSpeed:F2} to {newSpeed:F2}");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw attack range
        if (showAttackRange)
        {
            DrawRangeCircle(attackRange, attackRangeColor);
        }
        
        // Draw minimum leap range
        if (showMinLeapRange)
        {
            DrawRangeCircle(leapMinRange, minLeapRangeColor, true);
        }
        
        // Draw maximum leap range
        if (showLeapRange)
        {
            DrawRangeCircle(leapRange, leapRangeColor, true);
        }
    }

    // Helper method to draw a range circle
    private void DrawRangeCircle(float radius, Color color, bool isDashed = false)
    {
        Gizmos.color = color;
        
        // Draw a horizontal circle at zombie's position
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        
        // Draw main circle
        int segments = isDashed ? 64 : 32;  // More segments for leap range
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            // For dashed lines, only draw every other segment
            if (isDashed && i % 2 == 0) continue;
            
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;
            
            Vector3 point1 = position + new Vector3(
                Mathf.Sin(angle1 * Mathf.Deg2Rad) * radius,
                0.05f,  // Slight y-offset to prevent z-fighting with attack range
                Mathf.Cos(angle1 * Mathf.Deg2Rad) * radius
            );
            
            Vector3 point2 = position + new Vector3(
                Mathf.Sin(angle2 * Mathf.Deg2Rad) * radius,
                0.05f,
                Mathf.Cos(angle2 * Mathf.Deg2Rad) * radius
            );
            
            Gizmos.DrawLine(point1, point2);
        }
        
        // Draw direction indicator for attack range only
        if (!isDashed)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, forward * radius);
        }
    }

    // Use this simplified leap method with Rigidbody
    private IEnumerator LeapAtPlayer()
    {
        if (!agent || !agent.isOnNavMesh) 
        {
            isLeaping = false;
            yield break;
        }
        
        // Double check distance
        float currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (currentDistanceToPlayer <= attackRange + 0.5f)
        {
            Debug.Log("Leap canceled - player within attack range");
            isLeaping = false;
            canLeap = true;
            yield break;
        }

        // Set flags
        canLeap = false;
        isLeaping = true;
        isAttacking = true;

        // Disable NavMeshAgent
        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Play leap sound - special for Jograt
        if (SoundManager.Instance.jogratLeapSounds != null && 
            SoundManager.Instance.jogratLeapSounds.Length > 0)
        {
            SoundManager.Instance.PlayRandomJogratSound(
                SoundManager.Instance.jogratLeapSounds,
                attackSoundVolume * 1.3f  // Slightly louder for the leap
            );
        }
        else
        {
            SoundManager.Instance.PlayRandomZombieSound(
                SoundManager.Instance.zombieAttackSounds,
                attackSoundVolume * 1.2f
            );
        }

        // Setup animation
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            yield return null;
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Leap");
        }

        // Calculate leap direction
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        
        // CREATE Rigidbody only when we need to leap
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = 80;
            rb.drag = 0;
            rb.angularDrag = 10;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        
        // Activate physics for the leap
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Set up collisions
        SetupZombieCollisionIgnore();
        
        // Apply the leap force - using a balanced formula
        float horizontalForce = leapForce;
        float verticalForce = Mathf.Sqrt(2 * Physics.gravity.magnitude * leapHeight);
        Vector3 leapVelocity = directionToPlayer * horizontalForce + Vector3.up * verticalForce;
        
        // Apply velocity directly - no AddForce which can be affected by mass
        rb.velocity = leapVelocity;
        
        // Create leap trail effect
        if (leapTrailEffect != null)
        {
            GameObject trailObj = Instantiate(leapTrailEffect, transform.position, Quaternion.identity);
            activeTrailEffect = trailObj.GetComponent<ParticleSystem>();
            
            // Parent the trail to the Jograt
            trailObj.transform.SetParent(transform);
            
            // Position it slightly behind to create a better trail visual
            trailObj.transform.localPosition = new Vector3(0, 0.5f, -0.5f);
        }
        
        // Main leap physics loop - wait until we're grounded again
        bool hasHitPlayer = false;
        float leapTime = 0f;
        float maxLeapTime = 2.0f; // Safety timeout
        
        while (leapTime < maxLeapTime)
        {
            leapTime += Time.deltaTime;
            
            // Check if we hit the player
            if (!hasHitPlayer)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer < 1.5f)
                {
                    PlayerPerformance playerPerformance = player.GetComponent<PlayerPerformance>();
                    if (playerPerformance != null)
                    {
                        playerPerformance.TakeDamage(leapDamage, gameObject);
                        gameObject.SetIndicator();
                        Debug.Log($"Jograt dealt {leapDamage} leap damage");
                        hasHitPlayer = true;
                        
                        // Create impact effect on player hit
                        if (leapImpactEffect != null)
                        {
                            CreateImpactEffect(player.position, true);
                        }
                    }
                }
            }
            
            // Check if we're on the ground (landed)
            if (leapTime > 0.2f && IsGrounded()) // Only check after initial jump
            {
                // Create impact effect on landing if we haven't hit the player
                if (!hasHitPlayer && leapImpactEffect != null)
                {
                    CreateImpactEffect(transform.position, false);
                }
                break;
            }
            
            yield return null;
        }
        
        // Landed - stop physics and reset
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        
        // Stop the trail effect
        if (activeTrailEffect != null)
        {
            // Stop emitting but let existing particles fade out
            var emission = activeTrailEffect.emission;
            emission.enabled = false;
            
            // Detach from parent so it doesn't follow Jograt anymore
            activeTrailEffect.transform.SetParent(null);
            
            // Destroy after particles fade
            Destroy(activeTrailEffect.gameObject, activeTrailEffect.main.duration + activeTrailEffect.main.startLifetime.constantMax);
            activeTrailEffect = null;
        }
        
        // After landing, COMPLETELY DESTROY the Rigidbody
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }
        
        // Make sure we're on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        
        // Short pause before re-enabling navigation
        yield return new WaitForSeconds(0.2f);
        
        // Re-enable navigation
        if (agent && !isDead)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        
        // Reset flags
        yield return new WaitForSeconds(0.3f);
        isLeaping = false;
        isAttacking = false;
        
        // Cooldown
        yield return new WaitForSeconds(leapCooldown);
        canLeap = true;
    }

    // Helper method to check if grounded
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f);
    }

    // Add this method to check if the leap hits the player
    private IEnumerator CheckLeapHit()
    {
        // Check for a few frames to see if we hit the player
        for (int i = 0; i < 10; i++)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer < 1.5f)  // Close enough to count as a hit
            {
                // Apply leap damage
                PlayerPerformance playerPerformance = player.GetComponent<PlayerPerformance>();
                if (playerPerformance != null)
                {
                    playerPerformance.TakeDamage(leapDamage, gameObject);
                    gameObject.SetIndicator();
                    Debug.Log($"Jograt dealt {leapDamage} leap damage to the player.");
                }
                break;
            }
            
            yield return new WaitForFixedUpdate();
        }
    }

    // Add this new method to handle zombie collision ignoring
    private void SetupZombieCollisionIgnore()
    {
        // First get all colliders on this Jograt
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        
        // Find all objects with the "Zombie" tag
        GameObject[] allZombies = GameObject.FindGameObjectsWithTag("Zombie");
        
        foreach (GameObject zombie in allZombies)
        {
            // Skip self
            if (zombie == gameObject) continue;
            
            // Get all colliders on the other zombie
            Collider[] zombieColliders = zombie.GetComponentsInChildren<Collider>();
            
            // Ignore collisions between all colliders
            foreach (Collider myCol in myColliders)
            {
                foreach (Collider zombieCol in zombieColliders)
                {
                    Physics.IgnoreCollision(myCol, zombieCol, true);
                }
            }
        }
    }

    // Optionally, add this method to dynamically ignore new zombie colliders that spawn
    private void OnTriggerEnter(Collider other)
    {
        // If we hit another zombie during movement, ignore its collision
        if (other.CompareTag("Zombie") && other.gameObject != gameObject)
        {
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            Collider[] zombieColliders = other.gameObject.GetComponentsInChildren<Collider>();
            
            foreach (Collider myCol in myColliders)
            {
                foreach (Collider zombieCol in zombieColliders)
                {
                    Physics.IgnoreCollision(myCol, zombieCol, true);
                }
            }
        }
    }
    
    private void CreateImpactEffect(Vector3 position, bool hitPlayer)
    {
        // Position the effect slightly above ground to ensure visibility
        Vector3 effectPosition = position;
        effectPosition.y += 0.1f;
        
        // Create the impact effect
        GameObject impactObj = Instantiate(leapImpactEffect, effectPosition, Quaternion.identity);
        
        // Set up the effect's properties based on whether it hit a player or ground
        LeapImpactEffect impactEffect = impactObj.GetComponent<LeapImpactEffect>();
        if (impactEffect != null)
        {
            // Use the new Initialize method that doesn't need a damage parameter
            impactEffect.Initialize(gameObject, hitPlayer, 3f);
        }
        else
        {
            // Fallback if no custom script - adjust particle color
            ParticleSystem particleEffect = impactObj.GetComponent<ParticleSystem>();
            if (particleEffect != null)
            {
                var main = particleEffect.main;
                main.startColor = hitPlayer ? Color.red : Color.yellow;
            }
            
            // Just destroy after a few seconds
            Destroy(impactObj, 3f);
        }
    }
}
