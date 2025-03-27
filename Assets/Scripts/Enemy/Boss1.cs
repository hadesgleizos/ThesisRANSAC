using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static takeDamage;

public class Boss1 : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;
    private Spawner spawner;
    private PlayerPerformance playerPerformance;
    private float updateInterval = 1.0f;
    private Animator animator;
    private bool canAttack = true;
    private bool isDead = false;
    private bool isAttacking = false;

    [Header("Boss Stats")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 3f;

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;
    public float acceleration = 8f;
    public float stoppingDistance = 1.5f;
    public float baseSpeed = 5f;          // Base movement speed for the boss
    public float minSpeed = 3f;           // Minimum speed when player is struggling
    public float maxSpeed = 7f;           // Maximum speed when player is doing well
    [SerializeField] private float speedAdjustmentRate = 0.2f;  // How quickly speed adjusts

    [Header("Sound Settings")]
    public float idleSoundInterval = 8f;
    public float idleSoundVolume = 1f;
    public float attackSoundVolume = 1f;
    public float deathSoundVolume = 1f;
    private float nextIdleSoundTime;

    [Header("Debug Visualization")]
    public bool showAttackRange = true;
    public Color attackRangeColor = Color.red;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            //Debug.LogError("Animator component missing from boss!");
        }
        
        // Set the same tag and layer as zombies
        gameObject.tag = "Zombie";
        gameObject.layer = LayerMask.NameToLayer("Zombie");
        
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (agent != null)
        {
            agent.speed = baseSpeed;
            agent.angularSpeed = 120;
            agent.acceleration = acceleration;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = 1f;            // Larger radius for boss
            agent.height = 3f;            // Taller height for boss
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.autoRepath = true;
            agent.autoBraking = true;
            agent.stoppingDistance = 0.5f;
            
            //Debug.Log($"Initial boss speed set to: {agent.speed}");
        }

        playerPerformance = FindObjectOfType<PlayerPerformance>();
        if (playerPerformance == null)
        {
            //Debug.LogWarning("PlayerPerformance not found in scene!");
        }
        
        nextIdleSoundTime = Time.time + Random.Range(0f, idleSoundInterval);
        StartCoroutine(PlayIdleSoundsRoutine());
        StartCoroutine(UpdateSpeedRoutine());
    }

    void Update()
    {
        if (isDead || isAttacking) return;

        if (player != null && agent != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

            if (distanceToPlayer > agent.stoppingDistance)
            {
                agent.SetDestination(player.position);
            }

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
                SoundManager.Instance.PlayRandomBossSound(
                    SoundManager.Instance.bossIdleSounds,
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

        if (!isDead && player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            PlayerPerformance playerPerformance = player.GetComponent<PlayerPerformance>();
            if (playerPerformance != null)
            {
                playerPerformance.TakeDamage(damage, gameObject);
                gameObject.SetIndicator();
                //Debug.Log($"Boss dealt {damage} damage to the player.");
            }
        }

        SoundManager.Instance.PlayRandomBossSound(
            SoundManager.Instance.bossAttackSounds,
            attackSoundVolume
        );
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackCooldown);

        if (agent && agent.isOnNavMesh && !isDead)
        {
            agent.isStopped = false;
        }

        canAttack = true;
        isAttacking = false;
    }

    private IEnumerator UpdateSpeedRoutine()
    {
        while (true)
        {
            if (playerPerformance != null)
            {
                // Get player's health and convert it to percentage
                float healthPercentage = playerPerformance.GetHealth() / 100f;
                
                // Calculate target speed based on player's health (higher health = faster boss)
                float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, healthPercentage);
                
                // Smoothly adjust current speed
                float currentSpeed = agent.speed;
                float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedAdjustmentRate);
                
                SetSpeed(newSpeed);
                //Debug.Log($"[Boss] Updated speed to: {newSpeed:F2} based on player health: {healthPercentage:P0}");
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    public void SetSpeed(float newSpeed)
    {
        if (agent != null)
        {
            agent.speed = newSpeed;
            agent.acceleration = acceleration * (newSpeed / 3.5f);
            //Debug.Log($"Boss speed set to: {newSpeed}");
        }
    }

    public void TakeDamage(float damageAmount, CollisionType hitLocation)
    {
        if (isDead) return;

        float multiplier = 1f;
        switch (hitLocation)
        {
            case CollisionType.HEAD:
                multiplier = 2f;  // Match zombie's headshot multiplier
                break;
            case CollisionType.ARMS:
                multiplier = 0.5f;  // Match zombie's limb multiplier
                break;
            case CollisionType.BODY:
                multiplier = 1f;
                break;
        }

        float totalDamage = damageAmount * multiplier;
        health -= totalDamage;

        // Trigger hit animation
        if (animator != null)
        {
            animator.SetTrigger("OnHit");
        }

        // Add debug logging
        //Debug.Log($"Boss hit! Location: {hitLocation}, Damage: {totalDamage}, Health remaining: {health}");

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        SoundManager.Instance.PlayRandomBossSound(
            SoundManager.Instance.bossDeathSounds,
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
            playerPerformance.BossKilled(); // Add points for boss kill
        }

        // Notify spawner that boss is defeated
        Spawner.Instance.BossDefeated();
        
        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmos()
    {
        if (showAttackRange)
        {
            Gizmos.color = attackRangeColor;
            
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            
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
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, forward * attackRange);
        }
    }
}
