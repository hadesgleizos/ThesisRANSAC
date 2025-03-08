using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public float healAmount = 20f; // Amount of health the pack restores

    void OnTriggerEnter(Collider other)
    {
        Transform[] children = other.gameObject.GetComponentsInChildren<Transform>();
        
        foreach (Transform child in children)
        {
            if (child.CompareTag("Player"))
            {
                PlayerPerformance player = child.GetComponent<PlayerPerformance>();
                
                if (player != null)
                {
                    player.Heal(healAmount);
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
