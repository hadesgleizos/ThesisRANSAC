using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidPool : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float expansionDuration = 1f;
    [SerializeField] private float lifeDuration = 5f;
    [SerializeField] private float fallSpeed = 5f; // Speed at which acid falls
    [SerializeField] private string[] groundTags = { "Floor", "Ground", "Terrain" }; // Tags the acid will stick to

    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem acidParticles;

    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageTickRate = 0.5f;
    [SerializeField] private LayerMask damageableLayers;

    private float expansionStartTime;
    private float nextDamageTime;
    private GameObject sender;
    private Dictionary<GameObject, float> lastDamageTime;
    private bool hasLanded = false;
    private Vector3 targetPosition;

    public void SetSender(GameObject spitter)
    {
        sender = spitter;
    }

    private void Start()
    {
        lastDamageTime = new Dictionary<GameObject, float>();
        expansionStartTime = Time.time;
        nextDamageTime = Time.time;
        targetPosition = transform.position;
        
        // Set up particle system for collision
        var collision = acidParticles.collision;
        collision.enabled = true;
        collision.sendCollisionMessages = true;
        collision.collidesWith = damageableLayers;

        // Set initial particle system shape scale to final size for damage calculation
        var shape = acidParticles.shape;
        shape.scale = Vector3.one; // Set to full size immediately for damage area
        
        // Create a separate MaterialPropertyBlock for visual scaling
        var renderer = acidParticles.GetComponent<ParticleSystemRenderer>();
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetVector("_Scale", Vector3.zero); // Start visual scale at zero
        renderer.SetPropertyBlock(propBlock);

        // Ignore collisions with enemy layer
        Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);

        // Start checking for ground
        StartCoroutine(CheckForGround());

        // Destroy after lifetime
        Destroy(gameObject, lifeDuration);
    }

    private IEnumerator CheckForGround()
    {
        while (!hasLanded)
        {
            // Cast a ray downward to find ground
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f))
            {
                // Check if the hit object has one of the ground tags
                bool isGround = false;
                foreach (string tag in groundTags)
                {
                    if (hit.collider.CompareTag(tag))
                    {
                        isGround = true;
                        break;
                    }
                }

                // If it's ground, stick to it
                if (isGround)
                {
                    // Debug.Log($"Acid pool landed on {hit.collider.gameObject.name} with tag {hit.collider.tag}");
                    targetPosition = hit.point + (Vector3.up * 0.3f); // Adjusted to 0.1f to prevent clipping
                    hasLanded = true;

                    // Set the correct orientation
                    transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // Match the transform stats from the screenshot
                }
            }

            yield return null;
        }
    }

    private void Update()
    {
        // Move the acid pool down if it hasn't landed
        if (!hasLanded)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
        else if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            // Smoothly move to the ground target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, 5f * Time.deltaTime);
        }

        // Handle only visual expansion
        float expansionProgress = (Time.time - expansionStartTime) / expansionDuration;
        expansionProgress = Mathf.Clamp01(expansionProgress);

        // Update only the visual scale
        if (acidParticles != null)
        {
            var renderer = acidParticles.GetComponent<ParticleSystemRenderer>();
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetVector("_Scale", Vector3.Lerp(Vector3.zero, Vector3.one, expansionProgress));
            renderer.SetPropertyBlock(propBlock);
        }

        // Handle damage ticks
        if (Time.time >= nextDamageTime)
        {
            DealDamage();
            nextDamageTime = Time.time + damageTickRate;
        }
    }

    private void DealDamage()
    {
        // Use particle system's shape size for damage radius
        float damageRadius = acidParticles.shape.scale.x / 2f;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, damageRadius, damageableLayers);
        
        foreach (var hitCollider in hitColliders)
        {
            PlayerPerformance playerPerformance = hitCollider.GetComponent<PlayerPerformance>();
            if (playerPerformance == null)
            {
                playerPerformance = hitCollider.GetComponentInChildren<PlayerPerformance>();
            }
            if (playerPerformance == null)
            {
                playerPerformance = hitCollider.GetComponentInParent<PlayerPerformance>();
            }

            if (playerPerformance != null)
            {
                float tickDamage = damagePerSecond * damageTickRate;
                playerPerformance.TakeDamage(tickDamage, sender);
            }
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        PlayerPerformance playerPerformance = other.GetComponent<PlayerPerformance>();
        if (playerPerformance == null)
        {
            playerPerformance = other.GetComponentInChildren<PlayerPerformance>();
        }
        if (playerPerformance == null)
        {
            playerPerformance = other.GetComponentInParent<PlayerPerformance>();
        }

        if (playerPerformance != null)
        {
            // Check if enough time has passed since last damage
            if (!lastDamageTime.ContainsKey(other) || 
                Time.time - lastDamageTime[other] >= damageTickRate)
            {
                float tickDamage = damagePerSecond * damageTickRate;
                playerPerformance.TakeDamage(tickDamage, sender);
                lastDamageTime[other] = Time.time;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the damage radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, acidParticles.shape.scale.x / 2f);
    }
}
