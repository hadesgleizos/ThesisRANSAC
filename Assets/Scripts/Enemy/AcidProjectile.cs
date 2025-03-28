using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private float splashRadius = 3f;
    [SerializeField] private GameObject acidPoolPrefab;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private ParticleSystem projectileParticleSystem; // Particle system for the projectile
    [SerializeField] private ParticleSystem impactParticleSystem;     // Particle system for impact
    [SerializeField] private LayerMask collisionMask;                 // What layers the particles can collide with
    private GameObject sender;
    private bool hasCollided = false;
    private List<ParticleCollisionEvent> collisionEvents;
    
    private void Awake()
    {
        collisionEvents = new List<ParticleCollisionEvent>();
        
        // If we don't have a particle system component, add one
        if (projectileParticleSystem == null)
        {
            projectileParticleSystem = GetComponent<ParticleSystem>();
            if (projectileParticleSystem == null)
            {
                projectileParticleSystem = gameObject.AddComponent<ParticleSystem>();
                
                // Configure basic particle system properties
                var main = projectileParticleSystem.main;
                main.startLifetime = 2f;
                main.startSpeed = 5f;
                main.startSize = 0.3f;
                main.startColor = Color.green;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                
                // Configure collision
                var collision = projectileParticleSystem.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.collidesWith = collisionMask;
                collision.sendCollisionMessages = true;
            }
        }
        
        // Configure the particle system's collision to avoid zombies
        var collisionModule = projectileParticleSystem.collision;
        collisionModule.enabled = true;
        collisionModule.type = ParticleSystemCollisionType.World;
        collisionModule.collidesWith = collisionMask;
        collisionModule.sendCollisionMessages = true;
        
        // Make sure particle system isn't colliding with zombie layers
        int zombieLayer = LayerMask.NameToLayer("Zombie");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        
        // Create a mask that excludes zombie and enemy layers
        int excludeZombiesMask = ~(1 << zombieLayer | 1 << enemyLayer);
        collisionModule.collidesWith = excludeZombiesMask;
    }

    private void Start()
    {
        // Play the particle effect
        if (projectileParticleSystem != null && !projectileParticleSystem.isPlaying)
        {
            projectileParticleSystem.Play();
        }
    }

    public void SetSender(GameObject spitter)
    {
        sender = spitter;
    }

    // This is called when particles collide with something
    private void OnParticleCollision(GameObject other)
    {
        // Skip if we've already handled a collision or if the other object is a zombie
        if (hasCollided || 
            other.CompareTag("Zombie") || 
            other.CompareTag("Enemy") || 
            other.CompareTag("Spitter") ||
            other.layer == LayerMask.NameToLayer("Zombie") || 
            other.layer == LayerMask.NameToLayer("Enemy"))
        {
            return;
        }
        
        // Don't collide with sender
        if (sender != null && (other == sender || other.transform.IsChildOf(sender.transform)))
        {
            return;
        }
        
        // Get collision info
        int numCollisionEvents = projectileParticleSystem.GetCollisionEvents(other, collisionEvents);
        if (numCollisionEvents <= 0) return;
        
        // We only care about the first collision point
        Vector3 impactPosition = collisionEvents[0].intersection;
        Vector3 impactNormal = collisionEvents[0].normal;
        
        // Set collision flag to true to avoid multiple impacts
        hasCollided = true;
        
        // Play impact effect
        if (impactParticleSystem != null)
        {
            ParticleSystem impact = Instantiate(impactParticleSystem, impactPosition, Quaternion.LookRotation(impactNormal));
            impact.Play();
            
            // Destroy the impact particles after they finish playing
            float lifetime = 0;
            foreach (var subSystem in impact.GetComponentsInChildren<ParticleSystem>())
            {
                float subSystemLifetime = subSystem.main.duration + subSystem.main.startLifetime.constantMax;
                if (subSystemLifetime > lifetime)
                {
                    lifetime = subSystemLifetime;
                }
            }
            Destroy(impact.gameObject, lifetime);
        }
        
        // Spawn acid pool
        if (acidPoolPrefab != null)
        {
            // Spawn slightly above the impact point to prevent clipping
            Vector3 spawnPos = impactPosition + Vector3.up * 0.1f;
            GameObject acidPool = Instantiate(acidPoolPrefab, spawnPos, Quaternion.identity);

            // Set the transform values to ensure the acid pool lays flat
            acidPool.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            acidPool.transform.localScale = new Vector3(1f, 1f, 1f);
            //Debug.Log($"Spawned acid pool at position: {spawnPos} with rotation and scale set");

            // Pass the sender to the acid pool
            var poolScript = acidPool.GetComponent<AcidPool>();
            if (poolScript != null)
            {
                poolScript.SetSender(sender);
            }
        }

        // Deal splash damage
        DealSplashDamage(impactPosition);
        
        // Stop the main particle system
        if (projectileParticleSystem != null)
        {
            var main = projectileParticleSystem.main;
            main.loop = false;
            projectileParticleSystem.Stop();
        }
        
        // Destroy the entire projectile after a short delay
        Destroy(gameObject, 0.1f);
    }

    private void DealSplashDamage(Vector3 position)
    {
        //Debug.Log($"Attempting to deal splash damage at position: {position}");
        Collider[] hitColliders = Physics.OverlapSphere(position, splashRadius, damageableLayers);
        //Debug.Log($"Found {hitColliders.Length} objects in splash radius");

        // Track unique players that received damage to prevent multiple hits
        HashSet<PlayerPerformance> damagedPlayers = new HashSet<PlayerPerformance>();

        foreach (var hitCollider in hitColliders)
        {
            // Skip zombies, enemies, and the sender
            if (hitCollider.CompareTag("Zombie") || hitCollider.CompareTag("Enemy") || hitCollider.CompareTag("Spitter"))
                continue;
                
            if (sender != null && (hitCollider.gameObject == sender || hitCollider.transform.IsChildOf(sender.transform)))
                continue;

            // Try to get PlayerPerformance from the hit object or its hierarchy
            PlayerPerformance playerPerformance = hitCollider.GetComponent<PlayerPerformance>();
            if (playerPerformance == null)
            {
                playerPerformance = hitCollider.GetComponentInChildren<PlayerPerformance>();
            }
            if (playerPerformance == null)
            {
                playerPerformance = hitCollider.GetComponentInParent<PlayerPerformance>();
            }

            // Only apply damage if we haven't already damaged this player
            if (playerPerformance != null && !damagedPlayers.Contains(playerPerformance))
            {
                //Debug.Log($"Found PlayerPerformance component, dealing {damage} damage");
                playerPerformance.TakeDamage(damage, sender);
                
                // Add to the set to prevent multiple damage applications
                damagedPlayers.Add(playerPerformance);
                
                //Debug.Log($"Successfully dealt {damage} damage to player");
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw the splash damage radius in green
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
}
