using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AcidPool : MonoBehaviour
{
    [Header("Expansion Settings")]
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float expansionDuration = 1f;
    [SerializeField] private float lifeDuration = 5f;
    [SerializeField] private ParticleSystem acidParticles; // Add this line

    [Header("Damage Settings")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private float damageTickRate = 0.5f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private LayerMask damageableLayers;

    private float currentScale = 0f;
    private float expansionStartTime;
    private float nextDamageTime;
    private GameObject sender;

    public void SetSender(GameObject spitter)
    {
        sender = spitter;
    }

    private void Start()
    {
        expansionStartTime = Time.time;
        transform.localScale = Vector3.zero;
        nextDamageTime = Time.time;

        // Ignore collisions with enemy layer
        Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);

        // Destroy after lifetime
        Destroy(gameObject, lifeDuration);
    }

    private void Update()
    {
        // Handle expansion
        float expansionProgress = (Time.time - expansionStartTime) / expansionDuration;
        currentScale = Mathf.Lerp(0, maxScale, expansionProgress);
        
        // Update both transform and particle system scale
        transform.localScale = new Vector3(currentScale, 0.1f, currentScale);
        
        // Update particle system size to match the hitbox
        if (acidParticles != null)
        {
            var main = acidParticles.main;
            main.startSize = radius * currentScale * 2f; // Multiply by 2 since radius is half the total size
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
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius * currentScale, damageableLayers);
        
        foreach (var hitCollider in hitColliders)
        {
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

            if (playerPerformance != null)
            {
                float tickDamage = damagePerSecond * damageTickRate;
                playerPerformance.TakeDamage(tickDamage, sender);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the damage radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius * currentScale);
    }
}
