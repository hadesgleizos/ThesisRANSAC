using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Spitter : MonoBehaviour
{
    [Header("Components")]
    private Animator animator;
    private NavMeshAgent agent;  // Add NavMeshAgent

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float minDistanceToPlayer = 10f;  // Minimum distance to maintain from player

    [Header("Attack Settings")]
    [SerializeField] private GameObject acidProjectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("Sound Settings")]
    public float idleSoundInterval = 5f;
    public float idleSoundVolume = 1f;
    public float attackSoundVolume = 1f;
    public float deathSoundVolume = 1f;
    private float nextIdleSoundTime;
    private bool isDead = false;

    [Header("Animation")]
    private bool isAttacking = false;

    private Transform player;
    private float nextAttackTime;

    void Start() 
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();  // Get NavMeshAgent reference
        
        // Configure NavMeshAgent
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = minDistanceToPlayer;
        }

        StartCoroutine(PlayIdleSoundsRoutine());
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Update NavMeshAgent destination
        if (agent != null && !isAttacking)
        {
            agent.SetDestination(player.position);
        }
        
        // Update animation parameters
        if (animator != null)
        {
            // Set Speed parameter based on agent's velocity
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", speed);
            animator.SetBool("isAttacking", isAttacking);
        }

        // Attack when in range and cooldown is ready
        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime && !isAttacking)
        {
            SpitAcid();
            nextAttackTime = Time.time + attackCooldown;
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

    void SpitAcid()
    {
        // Add debug checks
        if (acidProjectilePrefab == null)
        {
            Debug.LogError("Acid Projectile Prefab is missing on " + gameObject.name);
            return;
        }
        if (shootPoint == null)
        {
            Debug.LogError("Shoot Point is missing on " + gameObject.name);
            return;
        }

        isAttacking = true;
        
        // Stop moving while attacking
        if (agent != null)
        {
            agent.isStopped = true;
        }
        
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Play attack sound
        SoundManager.Instance.PlayRandomSpitterSound(
            SoundManager.Instance.spitterAttackSounds,
            attackSoundVolume
        );

        // Calculate direction to shoot (predict player movement)
        Vector3 targetPosition = player.position;
        Vector3 direction = (targetPosition - shootPoint.position).normalized;

        Debug.Log($"Spitting acid from {shootPoint.position} towards {direction}");
        // Spawn and shoot projectile
        GameObject projectile = Instantiate(acidProjectilePrefab, shootPoint.position, Quaternion.LookRotation(direction));
        var acidProjectile = projectile.GetComponent<AcidProjectile>();
        if (acidProjectile != null)
        {
            Debug.Log($"Setting sender for acid projectile: {gameObject.name}");
            acidProjectile.SetSender(gameObject);
        }
        
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            projectileRb.velocity = direction * projectileSpeed;
        }

        StartCoroutine(ResetAttackState());
    }

    private IEnumerator ResetAttackState()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        
        // Resume movement after attack
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    public void Die()
    {
        isDead = true;
        
        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }

        // Play death sound
        SoundManager.Instance.PlayRandomSpitterSound(
            SoundManager.Instance.spitterDeathSounds,
            deathSoundVolume
        );

        // Disable components
        var colliders = GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        Destroy(gameObject, 3f);
    }
}
