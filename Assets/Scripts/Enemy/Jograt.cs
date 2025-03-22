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

    [Header("Leap Settings")]
    public float leapRange = 8f;             // Max distance to trigger leap
    public float leapCooldown = 5f;          // Time between leaps
    public float leapDamage = 30f;           // Damage dealt on successful leap
    public float leapForce = 12f;            // Force of the leap
    public float leapHeight = 3f;            // Height of the leap
    private bool canLeap = true;             // Flag for leap cooldown
    private bool isLeaping = false;          // Flag for when currently leaping

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
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

    // Update method with improved leap decision making
    void Update()
    {
        if (isDead) return; // If dead, don't do anything
        
        if (isLeaping || isAttacking) return; // If currently leaping or attacking, skip update
        
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
            // Only leap when player is DEFINITELY out of attack range but within leap range
            else if (distanceToPlayer > attackRange + 0.5f && distanceToPlayer <= leapRange && canLeap && !isLeaping)
            {
                // Add a buffer zone of 0.5 units to be extra safe
                // Use leap to close distance when player is further away
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
                SoundManager.Instance.PlayRandomZombieSound(
                    SoundManager.Instance.zombieIdleSounds
                );
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

        // Play attack sound and animation
        SoundManager.Instance.PlayRandomZombieSound(
            SoundManager.Instance.zombieAttackSounds
        );

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

        // Play death sound
        SoundManager.Instance.PlayRandomZombieSound(
            SoundManager.Instance.zombieDeathSounds
        );

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
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
        if (showAttackRange)
        {
            Gizmos.color = attackRangeColor;
            
            // Draw a horizontal circle at zombie's position
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            
            // Draw main circle
            int segments = 32;
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;
                
                Vector3 point1 = position + new Vector3(
                    Mathf.Sin(angle1 * Mathf.Deg2Rad) * attackRange,
                    0f,
                    Mathf.Cos(angle1 * Mathf.Deg2Rad) * attackRange
                );
                
                Vector3 point2 = position + new Vector3(
                    Mathf.Sin(angle2 * Mathf.Deg2Rad) * attackRange,
                    0f,
                    Mathf.Cos(angle2 * Mathf.Deg2Rad) * attackRange
                );
                
                Gizmos.DrawLine(point1, point2);
            }
            
            // Draw forward direction indicator
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, forward * attackRange);
        }
    }

    // Modify the start of the LeapAtPlayer method
    private IEnumerator LeapAtPlayer()
    {
        if (!agent || !agent.isOnNavMesh) 
        {
            isLeaping = false; // Reset flag if we can't leap
            yield break;
        }
        
        // CRITICAL: Double-check the player distance one more time
        // to ensure we really should be leaping
        float currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (currentDistanceToPlayer <= attackRange + 0.5f)
        {
            Debug.Log("Leap canceled because player is now within attack range");
            isLeaping = false;
            canLeap = true;
            yield break; // Exit without leaping
        }

        canLeap = false;
        isAttacking = true; // Prevent normal attacks during leap

        Debug.Log($"Jograt is leaping at the player! Distance: {currentDistanceToPlayer}");

        // Disable NavMeshAgent during the leap
        if (agent)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // Play leap sound
        SoundManager.Instance.PlayRandomZombieSound(
            SoundManager.Instance.zombieAttackSounds,
            attackSoundVolume * 1.2f
        );

        // Play leap animation
        if (animator != null)
        {
            animator.SetTrigger("Leap");
        }

        // Calculate leap trajectory without using Rigidbody
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = player.position;
        Vector3 directionToPlayer = (targetPosition - startPosition).normalized;
        
        // Calculate the landing position (slightly beyond the player to create momentum)
        float leapDistance = Vector3.Distance(startPosition, targetPosition);
        if (leapDistance > 3f) // Don't overshoot if very close
        {
            targetPosition = player.position + directionToPlayer * 1.5f; // Land slightly past the player
        }
        
        float leapDuration = 1.8f; // Time to complete the leap
        float elapsedTime = 0f;
        
        // Track if we've hit the player
        bool hasHitPlayer = false;

        // Start our manual leap loop
        while (elapsedTime < leapDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / leapDuration; // 0 to 1
            
            // Create a parabolic path using sine for height
            float height = Mathf.Sin(normalizedTime * Mathf.PI) * leapHeight;
            
            // Calculate the current position along the path
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, normalizedTime);
            newPosition.y = startPosition.y + height; // Add height based on parabola
            
            // Move the jograt
            transform.position = newPosition;
            
            // Rotate towards direction of travel
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
            
            // Check for collision with player if we haven't hit them yet
            if (!hasHitPlayer)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer < 1.5f) // Close enough to count as a hit
                {
                    // Apply leap damage
                    PlayerPerformance playerPerformance = player.GetComponent<PlayerPerformance>();
                    if (playerPerformance != null)
                    {
                        playerPerformance.TakeDamage(leapDamage, gameObject);
                        gameObject.SetIndicator();
                        Debug.Log($"Jograt dealt {leapDamage} leap damage to the player.");
                        hasHitPlayer = true;
                    }
                }
            }
            
            yield return null;
        }

        // Make sure we end up on a valid position on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            // Adjust position to be on NavMesh
            transform.position = hit.position;
        }
        
        // Wait a moment after landing before re-enabling NavMeshAgent
        yield return new WaitForSeconds(0.2f);
        
        // Re-enable NavMeshAgent
        if (agent && !isDead)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // Reset flags after a brief delay
        yield return new WaitForSeconds(0.3f);
        isLeaping = false;
        isAttacking = false;

        // Start leap cooldown
        yield return new WaitForSeconds(leapCooldown);
        canLeap = true;
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
}
