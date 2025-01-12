using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision objectWeHit)
    {
        CreateBulletImpactEffect(objectWeHit);
        
        // Only destroy bullet if it hits something valid
        if(objectWeHit.gameObject.CompareTag("Target") || 
           objectWeHit.gameObject.CompareTag("Zombie") ||
           objectWeHit.gameObject.CompareTag("Dirt") ||
           objectWeHit.gameObject.CompareTag("Metal") ||
           objectWeHit.gameObject.CompareTag("Wood"))
        {
            Destroy(gameObject);
        }
    }

    void CreateBulletImpactEffect(Collision objectWeHit)
    {
        ContactPoint contact = objectWeHit.contacts[0];
        GameObject effectPrefab;

        // Choose which effect to use based on what we hit
        switch (objectWeHit.gameObject.tag)
        {
            case "Zombie":
                effectPrefab = GlobalReferences.Instance.zombieImpactEffectPrefab;
                break;
            case "Dirt":
                effectPrefab = GlobalReferences.Instance.dirtImpactEffectPrefab;
                break;
            case "Metal":
                effectPrefab = GlobalReferences.Instance.metalImpactEffectPrefab;
                break;
            case "Wood":
                effectPrefab = GlobalReferences.Instance.woodImpactEffectPrefab;
                break;
            default:
                effectPrefab = GlobalReferences.Instance.bulletImpactEffectPrefab;
                break;
        }

        // Only create effect if we have a valid prefab
        if (effectPrefab != null)
        {
            // Create the effect
            GameObject hole = Instantiate(
                effectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );

            hole.transform.SetParent(objectWeHit.gameObject.transform);
            hole.transform.position += hole.transform.forward / 1000;

            // Optional: Destroy the impact effect after some time
            Destroy(hole, 5f);  // Adjust time as needed
        }
        else
        {
            Debug.LogWarning($"No impact effect prefab assigned for tag: {objectWeHit.gameObject.tag}");
        }
    }
}