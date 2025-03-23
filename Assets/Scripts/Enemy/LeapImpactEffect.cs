using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapImpactEffect : MonoBehaviour
{
    // Add serialized fields to adjust in inspector
    [Header("Damage Settings")]
    [SerializeField] private float impactDamage = 15f;       // Default damage value
    [SerializeField] private float playerHitMultiplier = 1.5f; // Multiplier when hitting player directly
    
    // Original fields
    private float damage;
    private GameObject sender;
    private float lifetime = 3f;
    private bool damageApplied = false;
    
    [Header("Area Effect Settings")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private bool showRadiusGizmo = true;
    [SerializeField] private bool scaleParticlesToRadius = true; // Add this option
    
    // Tags that the effect can detect for different reactions
    public string[] groundTags = new string[] { "Ground", "Terrain", "Floor", "Environment" };
    
    // Layer mask for objects that can be damaged (e.g. player)
    public LayerMask damageLayers;
    
    // Add this method to automatically scale particle systems on start
    private void Start()
    {
        if (scaleParticlesToRadius)
        {
            ScaleParticleSystemsToRadius();
        }
    }
    
    // Add this new method to scale particle systems
    private void ScaleParticleSystemsToRadius()
    {
        // Get all particle systems (including children)
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        
        foreach (ParticleSystem ps in particleSystems)
        {
            // Scale shape module
            var shape = ps.shape;
            if (shape.enabled)
            {
                // Scale shape radius to match damage radius
                if (shape.shapeType == ParticleSystemShapeType.Sphere || 
                    shape.shapeType == ParticleSystemShapeType.Hemisphere ||
                    shape.shapeType == ParticleSystemShapeType.Circle)
                {
                    shape.radius = radius * 0.5f; // Half the radius works best visually
                }
                else if (shape.shapeType == ParticleSystemShapeType.Box || 
                         shape.shapeType == ParticleSystemShapeType.Cone)
                {
                    // For box/cone shapes, scale all dimensions
                    shape.scale = new Vector3(radius, radius, radius);
                }
            }
            
            // Scale start speed based on radius
            var main = ps.main;
            if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
            {
                main.startSpeed = main.startSpeed.constant * (radius / 2.5f);
            }
            else if (main.startSpeed.mode == ParticleSystemCurveMode.TwoConstants)
            {
                float scaleFactor = radius / 2.5f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    main.startSpeed.constantMin * scaleFactor,
                    main.startSpeed.constantMax * scaleFactor
                );
            }
            
            // Scale lifetime if needed to ensure particles reach the edge
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                // Increase lifetime proportionally to radius
                main.startLifetime = Mathf.Max(main.startLifetime.constant, radius / main.startSpeed.constant);
            }
            
            // Scale starting size based on radius
            if (main.startSize.mode == ParticleSystemCurveMode.Constant)
            {
                main.startSize = main.startSize.constant * (radius / 2.5f);
            }
            else if (main.startSize.mode == ParticleSystemCurveMode.TwoConstants)
            {
                float scaleFactor = radius / 2.5f;
                main.startSize = new ParticleSystem.MinMaxCurve(
                    main.startSize.constantMin * scaleFactor,
                    main.startSize.constantMax * scaleFactor
                );
            }
        }
    }
    
    // Modified Initialize method that doesn't require damage from caller
    public void Initialize(GameObject senderObject, bool isPlayerDirectHit = false, float effectLifetime = 3f)
    {
        // Calculate damage based on whether it's a direct player hit
        damage = isPlayerDirectHit ? impactDamage * playerHitMultiplier : impactDamage;
        sender = senderObject;
        lifetime = effectLifetime;
        
        // Start the cleanup coroutine
        StartCoroutine(CleanupAfterLifetime());
        
        // Check for collisions immediately in a radius
        CheckForCollisions();
        
        // Scale particle effects to match radius
        if (scaleParticlesToRadius)
        {
            ScaleParticleSystemsToRadius();
        }
    }
    
    // Legacy method for backward compatibility
    public void Initialize(float damageAmount, GameObject senderObject, float effectLifetime = 3f)
    {
        // This lets existing code still work, but we override with our custom damage
        damage = damageAmount;
        sender = senderObject;
        lifetime = effectLifetime;
        
        StartCoroutine(CleanupAfterLifetime());
        CheckForCollisions();
    }
    
    private void CheckForCollisions()
    {
        // Apply damage in a radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, damageLayers);
        
        foreach (var hitCollider in hitColliders)
        {
            // Skip if it's the sender
            if (hitCollider.gameObject == sender) continue;
            
            // Apply damage to player
            PlayerPerformance playerPerf = hitCollider.GetComponent<PlayerPerformance>();
            if (playerPerf != null && !damageApplied)
            {
                playerPerf.TakeDamage(damage, sender);
                sender.SetIndicator();
                damageApplied = true;
                Debug.Log($"Impact effect dealt {damage} damage");
            }
            
            // React based on the tag of the hit object
            foreach (string groundTag in groundTags)
            {
                if (hitCollider.CompareTag(groundTag))
                {
                    // Modify particle effect for ground hit
                    ParticleSystem ps = GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.startColor = new Color(0.8f, 0.6f, 0.2f); // Earthy color for ground
                    }
                    break;
                }
            }
        }
    }
    
    private IEnumerator CleanupAfterLifetime()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
    
    // Visualize the damage radius in editor
    private void OnDrawGizmosSelected()
    {
        if (showRadiusGizmo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
