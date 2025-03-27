using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombaExplosionEffect : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float explosionDamage = 20f;
    [SerializeField] private float damageRadius = 5f;
    [SerializeField] private float minDamagePercent = 0.3f; // Minimum damage at edge of radius (as percentage)
    
    [Header("Visual Settings")]
    [SerializeField] private bool showRadiusGizmo = true;
    [SerializeField] private bool scaleParticlesToRadius = true;
    
    private GameObject sender;
    private float lifetime = 3f;
    private bool damageApplied = false;
    
    // Layer mask for objects that can be damaged
    public LayerMask damageLayers;
    
    private void Start()
    {
        if (scaleParticlesToRadius)
        {
            ScaleParticleSystemsToRadius();
        }
        
        // Apply damage immediately on start
        ApplyExplosionDamage();
    }
    
    public void Initialize(GameObject senderObject, float damage, float radius, float effectLifetime = 3f)
    {
        sender = senderObject;
        explosionDamage = damage;
        damageRadius = radius;
        lifetime = effectLifetime;
        
        StartCoroutine(CleanupAfterLifetime());
        
        // Note: Don't call ApplyExplosionDamage here - it will be called in Start
        // This avoids potential timing issues if Initialize is called after Start
        
        if (scaleParticlesToRadius)
        {
            ScaleParticleSystemsToRadius();
        }
    }
    
    private void ApplyExplosionDamage()
    {
        // Find player directly instead of relying on collision detection
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && !damageApplied)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= damageRadius)
            {
                // Calculate damage based on distance from explosion center
                float damagePercent = Mathf.Lerp(1f, minDamagePercent, distance / damageRadius);
                float actualDamage = explosionDamage * damagePercent;
                
                PlayerPerformance playerPerf = player.GetComponent<PlayerPerformance>();
                if (playerPerf != null)
                {
                    playerPerf.TakeDamage(actualDamage, sender);
                    
                    // Call SetIndicator if it exists as an extension method
                    if (sender != null)
                    {
                        // Try using reflection to find the SetIndicator method
                        try
                        {
                            sender.SendMessage("SetIndicator", SendMessageOptions.DontRequireReceiver);
                        }
                        catch (System.Exception)
                        {
                            // Silently ignore if the method doesn't exist
                        }
                    }
                    
                    damageApplied = true;
                    //Debug.Log($"Explosion dealt {actualDamage} damage (at distance: {distance:F2})");
                }
            }
        }
        
        // Optional: Apply damage to other destructible objects here
        // (Similar to your LeapImpactEffect logic)
    }
    
    private void ScaleParticleSystemsToRadius()
    {
        // Your existing particle scaling code
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        
        foreach (ParticleSystem ps in particleSystems)
        {
            // Scale shape module
            var shape = ps.shape;
            if (shape.enabled)
            {
                if (shape.shapeType == ParticleSystemShapeType.Sphere || 
                    shape.shapeType == ParticleSystemShapeType.Hemisphere ||
                    shape.shapeType == ParticleSystemShapeType.Circle)
                {
                    shape.radius = damageRadius * 0.5f;
                }
                else if (shape.shapeType == ParticleSystemShapeType.Box || 
                         shape.shapeType == ParticleSystemShapeType.Cone)
                {
                    shape.scale = new Vector3(damageRadius, damageRadius, damageRadius);
                }
            }
            
            // Other particle scaling code...
        }
    }
    
    private IEnumerator CleanupAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (showRadiusGizmo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
    
    // For debugging purposes - make sure the explosion effect is working
    private void OnDrawGizmos()
    {
        // Draw small indicator of explosion center in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
}
