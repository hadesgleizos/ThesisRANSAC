using UnityEngine;

public class PlayerPerformance : MonoBehaviour
{
    public float playerHealth = 100f;
    public int zombiesKilled = 0;
    public float gameTime = 0f;
    public int pointsPerKill = 10; // Customizable points per kill
    private int currentScore = 0; // Tracks the current score

    private uiManager uiManager;

    void Start()
    {
        // Find the uiManager in the scene
        uiManager = FindObjectOfType<uiManager>();
        if (uiManager == null)
        {
            Debug.LogError("uiManager not found in the scene!");
        }

        // Update the UI with the initial health
        if (uiManager != null)
        {
            uiManager.setHealth(playerHealth.ToString());
        }
    }

    void Update()
    {
        gameTime += Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        playerHealth -= damage;
        Debug.Log($"Player took {damage} damage. Remaining health: {playerHealth}");

        // Update the health on the UI
        if (uiManager != null)
        {
            uiManager.setHealth(playerHealth.ToString());
        }

        // Check if the player is dead
        if (playerHealth <= 0)
        {
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Player is dead!");
        // Handle game over logic
    }

    public void ZombieKilled()
    {
        zombiesKilled++;
        currentScore += pointsPerKill; // Add points for the kill

        // Update score on UI
        if (uiManager != null)
        {
            uiManager.setScore(currentScore.ToString());
        }

        Debug.Log($"Zombies Killed: {zombiesKilled}, Current Score: {currentScore}");
    }

    public float GetHealth()
    {
        return playerHealth;
    }

    public float GetKillRate()
    {
        return gameTime > 0 ? (float)zombiesKilled / gameTime : 0;
    }
}
