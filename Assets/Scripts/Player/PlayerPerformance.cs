using UnityEngine;
using static bl_DamageCallback;   // Ensure the namespace matches package if necessary

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
            Debug.LogWarning("uiManager not found in the scene. Health and score updates will be disabled.");
        }

        // Update the UI with the initial health
        if (uiManager != null)
        {
            uiManager.setHealth(((int)playerHealth).ToString());
        }
    }

    void Update()
    {
        gameTime += Time.deltaTime;
    }

public void TakeDamage(float damage, GameObject attacker)
{
    playerHealth = Mathf.Max(playerHealth - damage, 0);
    Debug.Log($"Player took {damage} damage. Remaining health: {playerHealth}");

    if (uiManager != null)
    {
        uiManager.setHealth(((int)playerHealth).ToString());
    }

    // Create the bl_DamageInfo struct
    bl_DamageInfo info = new bl_DamageInfo(damage);
    info.Sender = attacker;

    // Trigger Damage HUD event
    bl_DamageDelegate.OnDamageEvent(info);

    // Handle death condition
    if (playerHealth <= 0)
    {
        HandlePlayerDeath();
    }
}

private void HandlePlayerDeath()
{
    Debug.Log("Player is dead!");
    enabled = false;
    bl_DamageDelegate.OnDie(); // Show Death HUD
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

        // Notify Spawner of the kill
        Spawner.Instance?.IncrementKillCount();

        Debug.Log($"Zombies Killed: {zombiesKilled}, Current Score: {currentScore}");
    }

    public float GetHealth()
    {
        return playerHealth;
    }
    
    public int GetScore()
{
    return currentScore;
}


    public float GetKillRate()
    {
        if (gameTime < 1f) return 0; // Avoid calculation for very small game times
        return (float)zombiesKilled / gameTime;
    }
}
