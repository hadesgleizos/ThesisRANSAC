using UnityEngine;

public class Bullet : MonoBehaviour
{
    private bool hasHit = false; // Flag to prevent multiple collisions

private void OnCollisionEnter(Collision objectWeHit)
{
    CreateBulletImpactEffect(objectWeHit);

    // Destroy bullet immediately if it hits something valid
    if (objectWeHit.gameObject.CompareTag("Target") || 
        objectWeHit.gameObject.CompareTag("Zombie") ||
        objectWeHit.gameObject.CompareTag("Dirt") ||
        objectWeHit.gameObject.CompareTag("Metal") ||
        objectWeHit.gameObject.CompareTag("Wood"))
    {
        Destroy(gameObject);
    }
    else
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}


public void CreateBulletImpactEffect(Collision objectWeHit) // Change to public
{
    ContactPoint contact = objectWeHit.contacts[0];
    GameObject effectPrefab;

    // Choose effect based on the object's tag
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

    // Instantiate effect if a valid prefab exists
    if (effectPrefab != null)
    {
        GameObject impactEffect = Instantiate(
            effectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );

        impactEffect.transform.SetParent(objectWeHit.gameObject.transform);
        impactEffect.transform.position += impactEffect.transform.forward / 1000;

        // Destroy the effect after a delay
        Destroy(impactEffect, 5f);
    }
    else
    {
        //Debug.LogWarning($"No impact effect prefab assigned for tag: {objectWeHit.gameObject.tag}");
    }
}

    }

