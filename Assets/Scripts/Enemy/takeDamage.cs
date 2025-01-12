using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class takeDamage : MonoBehaviour
{
    public enum CollisionType
    {
        HEAD,
        BODY,
        ARMS
    }

    [SerializeField] private CollisionType bodyPart = CollisionType.BODY;  // Default to BODY, can be changed in Inspector
    private Zombie zombieController;

    void Start()
    {
        zombieController = GetComponentInParent<Zombie>();
    }

    public void HIT(float damage, CollisionType damageType)
    {
        Debug.Log($"Hit on {gameObject.name} ({bodyPart}) with damage: {damage}"); // Debug log
        
        if (zombieController != null)
        {
            zombieController.TakeDamage(damage, bodyPart);  // Use this object's bodyPart type
        }
        else
        {
            Debug.LogError($"Zombie controller not found on {gameObject.name}!");
        }
    }
}