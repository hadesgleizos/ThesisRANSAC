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

        isAttacking = true;
        canAttack = false; 
        agent.isStopped = true;

        // Play attack sound and animation
        SoundManager.Instance.PlayRandomZombieSound(
            SoundManager.Instance.zombieAttackSounds
        );
        animator.SetTrigger("Attack");
        
        // Wait a moment before spawning projectile (time it with animation)
        yield return new WaitForSeconds(0.5f);
        
        // Shoot acid projectile
        if (!isDead && acidProjectilePrefab != null && projectileSpawnPoint != null)
        {
            // Calculate direction to player with slight upward arc
            Vector3 targetPosition = player.position;
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
            
            Debug.Log($"Spitter shot acid projectile at player");
        }

        // Wait for attack cooldown
        yield return new WaitForSeconds(attackCooldown - 0.5f); // Adjusted for the initial wait

        if (agent && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
        }

        canAttack = true;
        isAttacking = false;
    }

    public void TakeDamage(float damageAmount, CollisionType hitLocation)
    {
        if (isDead)
        {
            Debug.Log($"Spitter {gameObject.GetInstanceID()} ignored damage because already dead");
            return;
        }

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

        float actualDamage = damageAmount * multiplier;
        health -= actualDamage;
        Debug.Log($"Spitter {gameObject.GetInstanceID()} took {actualDamage} damage ({damageAmount} x {multiplier}) to {hitLocation}. Health now: {health}");

        // Trigger hit animation
        if (animator != null)
        {
            animator.SetTrigger("OnHit");
        }

        if (health <= 0 && !isDead)
        {
            Debug.Log($"Spitter {gameObject.GetInstanceID()} health reached {health}, calling Die()");
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Spitter {gameObject.GetInstanceID()} Die() method called. Health: {health}");
        
        isDead = true;

        // Play death sound
        SoundManager.Instance.PlayRandomZombieSound(
            SoundManager.Instance.zombieDeathSounds
        );
        Debug.Log($"Spitter {gameObject.GetInstanceID()} death sound played");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
            Debug.Log($"Spitter {gameObject.GetInstanceID()} NavMeshAgent disabled");
        }
        else
        {
            Debug.LogWarning($"Spitter {gameObject.GetInstanceID()} NavMeshAgent was null in Die()");
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log($"Spitter {gameObject.GetInstanceID()} Rigidbody set to kinematic");
        }
        else
        {
            Debug.LogWarning($"Spitter {gameObject.GetInstanceID()} has no Rigidbody component");
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        Debug.Log($"Spitter {gameObject.GetInstanceID()} disabling {colliders.Length} colliders");
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Death");
            Debug.Log($"Spitter {gameObject.GetInstanceID()} Death animation triggered");
        }
        else
        {
            Debug.LogWarning($"Spitter {gameObject.GetInstanceID()} animator was null in Die()");
        }

        if (playerPerformance != null)
        {
            playerPerformance.ZombieKilled(); // Already correctly updating kills
            Debug.Log($"Spitter {gameObject.GetInstanceID()} added to kill count");
        }
        else
        {
            Debug.LogWarning($"Spitter {gameObject.GetInstanceID()} playerPerformance was null in Die()");
        }

        Debug.Log($"Spitter {gameObject.GetInstanceID()} scheduled for destruction in 3 seconds");
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
}
