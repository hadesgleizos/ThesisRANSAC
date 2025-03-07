using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public float healAmount = 20f; // Amount of health the pack restores

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("HealthPack collided with: " + other.name);

        // Get all child GameObjects
        Transform[] children = other.gameObject.GetComponentsInChildren<Transform>();
        
        foreach (Transform child in children)
        {
            // Check if any child has the Player tag
            if (child.CompareTag("Player"))
            {
                Debug.Log($"Found Player tagged object: {child.name}");
                PlayerPerformance player = child.GetComponent<PlayerPerformance>();
                
                if (player != null)
                {
                    Debug.Log($"Found PlayerPerformance on {child.name}");
                    player.Heal(healAmount);
                    Destroy(gameObject);
                    return;
                }
            }
        }
        
        Debug.LogWarning($"No Player tagged object with PlayerPerformance component found in {other.gameObject.name} or its children");
    }
}
