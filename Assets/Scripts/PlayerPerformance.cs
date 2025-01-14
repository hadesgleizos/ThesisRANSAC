using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPerformance : MonoBehaviour
{
    public float playerHealth = 100f;
    public int zombiesKilled = 0;
    public float gameTime = 0f;

    void Update()
    {
        gameTime += Time.deltaTime;
    }

public void TakeDamage(float damage)
{
    playerHealth -= damage;
    Debug.Log($"Player took {damage} damage. Remaining health: {playerHealth}");

    // Check if the player is dead
    if (playerHealth <= 0)
    {
        HandlePlayerDeath();
    }
}

private void HandlePlayerDeath()
{
    Debug.Log("Player is dead!");
    // Add logic for handling player death (e.g., game over screen, respawn, etc.)
}


    // Function to increase zombie kill count
    public void ZombieKilled()
    {
        zombiesKilled++;
        Debug.Log("Zombies Killed: " + zombiesKilled); // Log the number of zombies killed
    }

    public float GetHealth()
    {
        return playerHealth;
    }

    public float GetKillRate()
    {
        // Avoid division by zero
        if (gameTime <= 0)
            return 0;

        return (float)zombiesKilled / gameTime; // Cast zombiesKilled to float for accurate division
    }
}
