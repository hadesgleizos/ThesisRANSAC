using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static takeDamage;

public class Zombie : MonoBehaviour
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

    void Start()
    {
        // Get the NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("Animator component missing from zombie!");
        }
        
        // Find the player by tag (ensure the player has a "Player" tag)
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Find the Spawner in the scene
        spawner = FindObjectOfType<Spawner>(); // Ensure the spawner is accessible

        // Optional: Set an initial speed
        if (agent != null)
        {
            agent.speed = 3.5f; // Example speed, adjust as needed
            Debug.Log($"Initial zombie speed set to: {agent.speed}"); // Debug log
        }
        playerPerformance = FindObjectOfType<PlayerPerformance>();
        if (playerPerformance == null)
        {
            Debug.LogWarning("PlayerPerformance not found in scene!");
        }
        

        // Start coroutine to update speed regularly
        StartCoroutine(UpdateSpeedRoutine());
    }

    void Update()
    {
        if (isDead || isAttacking) return; // If dead or attacking, don't move

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Always set the destination to the player's position for chasing
            agent.SetDestination(player.position);

            if (distanceToPlayer <= attackRange && canAttack)
            {
                StartCoroutine(AttackPlayer());
            }
        }
    }

    private IEnumerator AttackPlayer()
    {
        if (!agent || !agent.isOnNavMesh) yield break;

        isAttacking = true;
        canAttack = false; // Prevent attack spam
        agent.isStopped = true;

        // Trigger attack animation
        animator.SetTrigger("Attack");

        // Wait for the animation to play before dealing damage
        yield return new WaitForSeconds(0.5f);

        // Check if the player is still in range before dealing damage
        if (!isDead && player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            // Deal damage to the player
            PlayerPerformance playerPerformance = player.GetComponent<PlayerPerformance>();
            if (playerPerformance != null)
            {
                playerPerformance.TakeDamage(damage);
                Debug.Log($"Zombie dealt {damage} damage to the player.");
            }
        }

        // Wait for attack cooldown
        yield return new WaitForSeconds(attackCooldown);

        // Resume movement and reset attack state only if agent is still valid
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

        if (health <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

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

    private IEnumerator UpdateSpeedRoutine()
    {
        while (true)
        {
            if (spawner != null)
            {
                // Update the speed based on the spawner's speed
                SetSpeed(spawner.zombieSpeed); // Accessing the speed from the spawner
            }
            yield return new WaitForSeconds(updateInterval); // Wait for the next interval
        }
    }

    public void SetSpeed(float newSpeed)
    {
        // Set the speed of the NavMeshAgent
        if (agent != null)
        {
            agent.speed = newSpeed;
            Debug.Log($"Zombie speed set to: {newSpeed}"); // Debug log
        }
    }
}
