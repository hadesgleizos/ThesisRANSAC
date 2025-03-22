using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static takeDamage;

public class Spitter : MonoBehaviour
{
    private NavMeshAgent agent; // Reference to the NavMeshAgent
    private Transform player; // Reference to the player's transform
    private Spawner spawner; // Reference to the Spawner script
    private PlayerPerformance playerPerformance;
    private float updateInterval = 1.0f; // How often to update speed in seconds
    private Animator animator;
    public float attackRange = 12f;        // Range at which spitter can attack (longer than zombie)
    public float attackCooldown = 3f;     // Time between attacks (longer for balance)
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttacking = false;     // Flag to track if spitter is attacking
    public float health = 75f;            // Less health than regular zombies

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;
    public float acceleration = 8f;
    public float stoppingDistance = 8f;  // Keep more distance than regular zombies

    [Header("Sound Settings")]
    public float idleSoundInterval = 5f;
    public float idleSoundVolume = 1f;
    public float attackSoundVolume = 1f;
    public float deathSoundVolume = 1f;
    private float nextIdleSoundTime;

    [Header("Debug Visualization")]
    public bool showAttackRange = true;
    public Color attackRangeColor = Color.green;  // Different color to distinguish from zombies

    [Header("Projectile Settings")]
    public GameObject acidProjectilePrefab;     // Reference to your existing AcidProjectile prefab
    public Transform projectileSpawnPoint;      // Empty GameObject to position where acid comes from
    public float projectileSpeed = 15f;         // Speed of the projectile

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("Animator component missing from spitter!");
        }
        
        player = GameObject.FindGameObjectWithTag("Player").transform;
        spawner = FindObjectOfType<Spawner>();

        if (agent != null && spawner != null)
        {
            float currentSpeed = spawner.GetCurrentZombieSpeed();
            SetSpeed(currentSpeed); // Slightly slower than regular zombies
            
            // Set other NavMeshAgent parameters
            agent.angularSpeed = 120;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = 0.5f;
            agent.height = 2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoRepath = true;
            agent.autoBraking = true;
            
            Debug.Log($"[Spitter {gameObject.GetInstanceID()}] Initialized with speed: {agent.speed}");
        }
        
        playerPerformance = FindObjectOfType<PlayerPerformance>();
        if (playerPerformance == null)
        {
            Debug.LogWarning("PlayerPerformance not found in scene!");
        }
        
        nextIdleSoundTime = Time.time + Random.Range(0f, idleSoundInterval);
        StartCoroutine(PlayIdleSoundsRoutine());
    }

    void Update()
    {
        if (isDead || isAttacking) return; // If dead or attacking, don't move

        if (player != null && agent != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Smoothly rotate towards the player
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

            // Set destination only if distance is significant
            if (distanceToPlayer > agent.stoppingDistance)
            {
                agent.SetDestination(player.position);
            }

            // Update animator if you have movement animations
            if (animator != null)
            {
                animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);
            }

            // Only start attack sequence if in range and can attack
            if (distanceToPlayer <= attackRange && canAttack)
            {
                StartCoroutine(AttackPlayer());
            }
        }
    }

    private IEnumerator PlayIdleSoundsRoutine()
    {
        while (!isDead)
        {
            if (Time.time >= nextIdleSoundTime)
            {
                SoundManager.Instance.PlayRandomSpitterSound(
                    SoundManager.Instance.spitterIdleSounds,
                    idleSoundVolume
                );
                nextIdleSoundTime = Time.time + idleSoundInterval + Random.Range(-1f, 1f);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator AttackPlayer()
    {
        if (!agent || !agent.isOnNavMesh) yield break;

        isAttacking = true;
        canAttack = false; 
        agent.isStopped = true;

        // Turn to face the player before shooting
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // Keep rotation on horizontal plane only
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        
        // Smoothly rotate to face the player before attacking
        float rotationTime = 0;
        float maxRotationTime = 1.0f; // Increased max time to spend rotating for visibility
        
        Debug.Log($"Spitter {gameObject.GetInstanceID()} starting rotation toward player");
        
        while (rotationTime < maxRotationTime)
        {
            if (isDead) yield break; // Exit if died while turning
            
            // Calculate current angle difference to target
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
            
            // If close enough to target rotation, break early
            if (angleDifference < 5f)
            {
                Debug.Log($"Spitter {gameObject.GetInstanceID()} rotation complete, angle to target: {angleDifference}");
                break;
            }
            
            // Get fresh target rotation in case player moved
            directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;
            targetRotation = Quaternion.LookRotation(directionToPlayer);
            
            // Smoothly rotate toward target, but faster than normal movement
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed * 3f // Triple rotation speed during attack prep
            );
            
            rotationTime += Time.deltaTime;
            yield return null;
        }
        
        // Small delay after rotation completes
        yield return new WaitForSeconds(0.2f);

        // Play attack sound and animation
        SoundManager.Instance.PlayRandomSpitterSound(
            SoundManager.Instance.spitterAttackSounds,
            attackSoundVolume
        );
        
        Debug.Log($"Spitter {gameObject.GetInstanceID()} playing attack animation");
        animator.SetTrigger("Attack");
        
        // Wait a moment before spawning projectile (time it with animation)
        yield return new WaitForSeconds(0.5f);
        
        // Shoot acid projectile
        if (!isDead && acidProjectilePrefab != null && projectileSpawnPoint != null)
        {
            // Get an updated target position since the player might have moved during our attack animation
            Vector3 targetPosition = player.position;
            
            // Get updated direction from spawn point to player
            Vector3 direction = (targetPosition - projectileSpawnPoint.position).normalized;
            
            // Instantiate the acid projectile
            GameObject projectile = Instantiate(
                acidProjectilePrefab, 
                projectileSpawnPoint.position, 
                Quaternion.LookRotation(direction)
            );
            
            // Add force to the projectile
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = direction * projectileSpeed;
            }
            
            // Set the spitter as the sender for damage tracking
            AcidProjectile acidProjectile = projectile.GetComponent<AcidProjectile>();
            if (acidProjectile != null)
            {
                acidProjectile.SetSender(gameObject);
            }
            
            Debug.Log($"Spitter {gameObject.GetInstanceID()} fired projectile at player");
        }

        // Wait for attack cooldown (minus the time we already spent in animation)
        yield return new WaitForSeconds(attackCooldown - 0.7f); // Adjusted for the rotation and animation delay 

        if (agent && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
        }

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
        SoundManager.Instance.PlayRandomSpitterSound(
            SoundManager.Instance.spitterDeathSounds,
            deathSoundVolume
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
            Debug.Log($"[Spitter {gameObject.GetInstanceID()}] Speed changed from {oldSpeed:F2} to {newSpeed:F2}");
        }
    }

    private void OnDrawGizmos()
    {
        if (showAttackRange)
        {
            Gizmos.color = attackRangeColor;
            
            // Draw a horizontal circle at spitter's position
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
}