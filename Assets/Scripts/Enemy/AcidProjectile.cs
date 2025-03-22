using UnityEngine;

public class AcidProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 20f;
    [SerializeField] private float splashRadius = 3f;
    [SerializeField] private GameObject acidPoolPrefab;
    [SerializeField] private LayerMask damageableLayers;
    private GameObject sender;

    private void Start()
    {
        // Ignore collisions with enemy layer
        Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
    }

    public void SetSender(GameObject spitter)
    {
        sender = spitter;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Don't spawn acid pool if hitting an enemy
        if (!collision.gameObject.CompareTag("Enemy"))
        {
            // Check if the collision surface has one of the valid tags
            if (collision.gameObject.CompareTag("Dirt") || 
                collision.gameObject.CompareTag("Metal") || 
                collision.gameObject.CompareTag("Wood") || 
                collision.gameObject.CompareTag("Grass") || 
                collision.gameObject.CompareTag("Concrete"))
            {
                if (acidPoolPrefab != null)
                {
                    // Spawn slightly above the hit point to prevent clipping
                    Vector3 spawnPos = collision.contacts[0].point + Vector3.up * 0.1f;
                    GameObject acidPool = Instantiate(acidPoolPrefab, spawnPos, Quaternion.identity);
                    
                    // Keep acid pool flat regardless of surface normal
                    acidPool.transform.rotation = Quaternion.identity;
                    Debug.Log($"Spawned acid pool at position: {spawnPos} on {collision.gameObject.tag} surface");

                    var poolScript = acidPool.GetComponent<AcidPool>();
                    if (poolScript != null)
                    {
                        poolScript.SetSender(sender);
                    }
                }
            }

            DealSplashDamage();
            Destroy(gameObject);
        }
    }

    private void DealSplashDamage()
    {
        Debug.Log($"Attempting to deal splash damage at position: {transform.position}");
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, splashRadius, damageableLayers);
        Debug.Log($"Found {hitColliders.Length} objects in splash radius");

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
                Debug.Log($"Found PlayerPerformance component, dealing {damage} damage");
                playerPerformance.TakeDamage(damage, sender);
                if (sender != null)
                {
                    sender.SetIndicator();
                }
                Debug.Log($"Successfully dealt {damage} damage to player");
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