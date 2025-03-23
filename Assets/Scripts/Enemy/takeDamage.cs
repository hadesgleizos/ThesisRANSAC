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

    [SerializeField] private CollisionType bodyPart = CollisionType.BODY;
    private Zombie zombieController;
    private Boss1 bossController;
    private Spitter spitterController;
    private Jograt jogratController; // Add Jograt reference

    void Start()
    {
        zombieController = GetComponentInParent<Zombie>();
        bossController = GetComponentInParent<Boss1>();
        spitterController = GetComponentInParent<Spitter>();
        jogratController = GetComponentInParent<Jograt>(); // Get Jograt component
    }

    public void HIT(float damage, CollisionType damageType)
    {
        Debug.Log($"Hit on {gameObject.name} ({bodyPart}) with damage: {damage}");
        
        if (zombieController != null)
        {
            zombieController.TakeDamage(damage, bodyPart);
        }
        else if (bossController != null)
        {
            bossController.TakeDamage(damage, bodyPart);
            Debug.Log($"Boss hit registered! Damage: {damage}, Part: {bodyPart}");
        }
        else if (spitterController != null)
        {
            spitterController.TakeDamage(damage, bodyPart);
            Debug.Log($"Spitter hit registered! Damage: {damage}, Part: {bodyPart}");
        }
        else if (jogratController != null) // Add check for Jograt
        {
            jogratController.TakeDamage(damage, bodyPart);
            Debug.Log($"Jograt hit registered! Damage: {damage}, Part: {bodyPart}");
        }
        else
        {
            Debug.LogError($"No damage controller found on {gameObject.name}!");
        }
    }
}