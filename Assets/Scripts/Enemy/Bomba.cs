using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static takeDamage;

public class Bomba : MonoBehaviour
{
    private NavMeshAgent agent; // Reference to the NavMeshAgent
    private Transform player; // Reference to the player's transform
    private Spawner spawner; // Reference to the Spawner script
    private PlayerPerformance playerPerformance;
    private float updateInterval = 1.0f; // How often to update speed in seconds
    private Animator animator;
    public float damage = 20f;            // Damage dealt to player
    public float explosionRange = 5f;     // Range of explosion effect
    public float explosionDelay = 1.5f;   // Delay before explosion after detection
    private bool isDetonating = false;
    private bool isDead = false;
    public float health = 100f;

    [Header("Explosion Settings")]
    public float detectionRange = 3f;     // Range at which to start detonation sequence
    public GameObject explosionEffectPrefab; // Particle system prefab for explosion

    [Header("Movement Settings")]
    public float rotationSpeed = 10f;
    public float acceleration = 8f;
    public float stoppingDistance = 1.5f;

    [Header("Sound Settings")]
    public float idleSoundInterval = 5f;
    public float idleSoundVolume = 1f;
    public float explosionSoundVolume = 1f;
    public float deathSoundVolume = 1f;
    private float nextIdleSoundTime;

    [Header("Debug Visualization")]
    public bool showDetectionRange = true;
    public Color detectionRangeColor = Color.yellow;
    public bool showExplosionRange = true;
    public Color explosionRangeColor = Color.red;

    [Header("Effect Positioning")]
    public Transform explosionOrigin; // Optional - if null, will use this transform
    public Vector3 explosionOffset = new Vector3(0, 1f, 0); // Default offset from center

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            //Debug.LogError("Animator component missing from Bomba!");
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
            
            //Debug.Log($"[Bomba {gameObject.GetInstanceID()}] Initialized with speed: {currentSpeed}");
        }
        
        playerPerformance = FindObjectOfType<PlayerPerformance>();
        if (playerPerformance == null)
        {
            //Debug.LogWarning("PlayerPerformance not found in scene!");
        }
        
        nextIdleSoundTime = Time.time + Random.Range(0f, idleSoundInterval);
        StartCoroutine(PlayIdleSoundsRoutine());
    }

    void Update()
    {
        if (isDead || isDetonating) 
        {
            // Ensure the agent stays stopped during detonation
            if (isDetonating && agent != null && agent.enabled)
            {
                agent.isStopped = true;
            }
            return; 
        }

        // Rest of the method remains unchanged
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

            if (distanceToPlayer <= detectionRange && !isDetonating)
            {
                StartCoroutine(DetonateSequence());
            }
        }
    }

    // Update PlayIdleSoundsRoutine in your Bomba class
    private IEnumerator PlayIdleSoundsRoutine()
    {
        while (!isDead)
        {
            if (Time.time >= nextIdleSoundTime)
            {
                // Use Bomba-specific sounds with zombie sounds as fallback
                if (SoundManager.Instance.bombaIdleSounds != null && 
                    SoundManager.Instance.bombaIdleSounds.Length > 0)
                {
                    SoundManager.Instance.PlayRandomBombaSound(
                        SoundManager.Instance.bombaIdleSounds,
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

    // Update DetonateSequence to use Bomba-specific detection sounds
    private IEnumerator DetonateSequence()
    {
        if (!agent || !agent.isOnNavMesh || isDetonating || isDead) yield break;

        isDetonating = true;
        agent.isStopped = true;

        // Reset any existing animation triggers that might interfere
        if (animator != null)
        {
            animator.ResetTrigger("OnHit");
            
            // Set a boolean parameter to block transitions in Animator
            animator.SetBool("IsExploding", true);
            
            // Trigger the detonation animation
            animator.SetTrigger("Detonate");
        }

        // Play warning sound using Bomba-specific sounds
        if (SoundManager.Instance.bombaDetectionSounds != null && 
            SoundManager.Instance.bombaDetectionSounds.Length > 0)
        {
            SoundManager.Instance.PlayRandomBombaSound(
                SoundManager.Instance.bombaDetectionSounds,
                explosionSoundVolume
            );
        }
        else
        {
            SoundManager.Instance.PlayRandomZombieSound(
                SoundManager.Instance.zombieAttackSounds,
                explosionSoundVolume
            );
        }

        // Wait for explosion delay
        yield return new WaitForSeconds(explosionDelay);

        // Check if we're still detonating (wasn't destroyed during delay)
        if (isDetonating && !isDead)
        {
            // Explode!
            Explode();
        }
    }

    // Update Explode method to use Bomba-specific explosion sounds
    private void Explode()
    {
        // Determine explosion position
        Vector3 explosionPosition;
        
        if (explosionOrigin != null)
        {
            explosionPosition = explosionOrigin.position;
        }
        else
        {
            explosionPosition = transform.position + explosionOffset;
        }

        // Apply damage to player if in range
        if (player != null && playerPerformance != null)
        {
            float distanceToPlayer = Vector3.Distance(explosionPosition, player.position);
            if (distanceToPlayer <= explosionRange)
            {
                // Apply damage directly
                playerPerformance.TakeDamage(damage, gameObject);
                //Debug.Log($"Bomba explosion dealt {damage} damage to player at distance {distanceToPlayer}");
            }
        }

        // Instantiate explosion particle effect
        if (explosionEffectPrefab != null)
        {
            GameObject explosionObj = Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
            BombaExplosionEffect explosionEffect = explosionObj.GetComponent<BombaExplosionEffect>();
            
            if (explosionEffect == null)
            {
                explosionEffect = explosionObj.AddComponent<BombaExplosionEffect>();
            }
            
            if (explosionEffect != null)
            {
                explosionEffect.Initialize(gameObject, damage, explosionRange);
            }
            else
            {
                //Debug.LogWarning("BombaExplosionEffect component not found on explosionEffectPrefab");
            }
        }

        // Play explosion sound using Bomba-specific sounds
        if (SoundManager.Instance.bombaExplosionSounds != null && 
            SoundManager.Instance.bombaExplosionSounds.Length > 0)
        {
            SoundManager.Instance.PlayRandomBombaSound(
                SoundManager.Instance.bombaExplosionSounds,
                explosionSoundVolume * 1.5f  // Make explosion sound louder
            );
        }
        else
        {
            SoundManager.Instance.PlayRandomZombieSound(
                SoundManager.Instance.zombieDeathSounds,
                explosionSoundVolume
            );
        }

        // Die immediately after explosion
        Die(true); // Pass true to destroy instantly
    }

    // Update Die method to use Bomba-specific death sounds if not exploding
    private void Die(bool instantDestroy = false)
    {
        isDead = true;
        isDetonating = false; // Reset this flag
        
        // Only play death sound if we're not exploding (otherwise the explosion sound plays)
        if (!instantDestroy && SoundManager.Instance.bombaDeathSounds != null && 
            SoundManager.Instance.bombaDeathSounds.Length > 0)
        {
            SoundManager.Instance.PlayRandomBombaSound(
                SoundManager.Instance.bombaDeathSounds,
                deathSoundVolume
            );
        }

        // Rest of the method remains the same
        if (animator != null)
        {
            // Reset animation parameters
            animator.SetBool("IsExploding", false);
            animator.ResetTrigger("Detonate");
            animator.ResetTrigger("OnHit");
        }

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

        if (playerPerformance != null)
        {
            playerPerformance.ZombieKilled(); // Update kills counter
        }

        // Hide the model immediately
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // Destroy immediately or after delay
        if (instantDestroy)
        {
            Destroy(gameObject); // Destroy immediately
        }
        else
        {
            Destroy(gameObject, 2f); // Default behavior - destroy after 2 seconds
        }
    }

    public void TakeDamage(float damageAmount, CollisionType hitLocation)
    {
        if (isDead || isDetonating) return;  // Add isDetonating check

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

        // Trigger hit animation only if not detonating
        if (animator != null && !isDetonating)
        {
            animator.SetTrigger("OnHit");
        }

        if (health <= 0 && !isDead)
        {
            // If low health, explode immediately
            if (!isDetonating)
            {
                StartCoroutine(DetonateSequence());
            }
        }
    }

    public void SetSpeed(float newSpeed)
    {
        if (agent != null)
        {
            float oldSpeed = agent.speed;
            agent.speed = newSpeed;
            agent.acceleration = acceleration * (newSpeed / 3.5f);
            //Debug.Log($"[Bomba {gameObject.GetInstanceID()}] Speed changed from {oldSpeed:F2} to {newSpeed:F2}");
        }
    }

    private void OnDrawGizmos()
    {
        // Get explosion position for visualization
        Vector3 explosionPosition;
        if (explosionOrigin != null)
        {
            explosionPosition = explosionOrigin.position;
        }
        else
        {
            explosionPosition = transform.position + explosionOffset;
        }

        // Draw detection range
        if (showDetectionRange)
        {
            Gizmos.color = detectionRangeColor;
            DrawCircle(transform.position, detectionRange);
        }
        
        // Draw explosion range
        if (showExplosionRange)
        {
            Gizmos.color = explosionRangeColor;
            DrawCircle(explosionPosition, explosionRange);
        }
        
        // Draw a small sphere at explosion origin
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(explosionPosition, 0.2f);
    }
    
    private void DrawCircle(Vector3 position, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep;
            float angle2 = (i + 1) * angleStep;
            
            Vector3 point1 = position + new Vector3(
                Mathf.Sin(angle1 * Mathf.Deg2Rad) * radius,
                0.1f,
                Mathf.Cos(angle1 * Mathf.Deg2Rad) * radius
            );
            
            Vector3 point2 = position + new Vector3(
                Mathf.Sin(angle2 * Mathf.Deg2Rad) * radius,
                0.1f,
                Mathf.Cos(angle2 * Mathf.Deg2Rad) * radius
            );
            
            Gizmos.DrawLine(point1, point2);
        }
    }
}
