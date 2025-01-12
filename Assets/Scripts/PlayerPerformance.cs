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

    // Function to simulate health loss (you can modify this as needed)
    public void TakeDamage(float damage)
    {
        playerHealth -= damage;
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
