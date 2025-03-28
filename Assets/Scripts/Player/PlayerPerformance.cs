using UnityEngine;
using static bl_DamageCallback;   // Ensure the namespace matches package if necessary

public class PlayerPerformance : MonoBehaviour
{
    public float playerHealth = 100f;
    public int zombiesKilled = 0;
    public float gameTime = 0f;
    public int pointsPerKill = 10; // Points for regular zombie kills
    public int pointsPerBossKill = 100; // Points for boss kills
    private int currentScore = 0; // Tracks the current score
    private int bossesKilled = 0; // Track number of bosses killed
    private bool isWaveActive = false; // Add this to track wave state

    private uiManager uiManager;

    void Start()
    {
        // Find the uiManager in the scene
        uiManager = FindObjectOfType<uiManager>();
        if (uiManager == null)
        {
            //Debug.LogWarning("uiManager not found in the scene. Health and score updates will be disabled.");
        }

        // Update the UI with the initial health
        if (uiManager != null)
        {
            uiManager.setHealth(((int)playerHealth).ToString());
        }

        // Subscribe to wave events
        Spawner.OnWaveStart += OnWaveStart;
        Spawner.OnWaveEnd += OnWaveEnd;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        Spawner.OnWaveStart -= OnWaveStart;
        Spawner.OnWaveEnd -= OnWaveEnd;
    }

    private void OnWaveStart(int waveNumber)
    {
        isWaveActive = true;
        //Debug.Log($"Wave {waveNumber} started. Game time tracking resumed.");
    }

    private void OnWaveEnd(int waveNumber)
    {
        isWaveActive = false;
        //Debug.Log($"Wave {waveNumber} ended. Game time tracking paused.");
    }

    void Update()
    {
        // Only increment game time if wave is active
        if (isWaveActive)
        {
            gameTime += Time.deltaTime;
        }
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        playerHealth = Mathf.Max(playerHealth - damage, 0);
        //Debug.Log($"Player took {damage} damage. Remaining health: {playerHealth}");

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
        //Debug.Log("Player is dead!");
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

        //Debug.Log($"Zombies Killed: {zombiesKilled}, Current Score: {currentScore}");
    }

    // Add new method for boss kills
    public void BossKilled()
    {
        bossesKilled++;
        currentScore += pointsPerBossKill;

        if (uiManager != null)
        {
            uiManager.setScore(currentScore.ToString());
        }

        //Debug.Log($"Boss killed! +{pointsPerBossKill} points! Current Score: {currentScore}");
    }

    // Optional: Add method to get boss kill count
    public int GetBossKillCount()
    {
        return bossesKilled;
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

    public void Heal(float amount)
    {
        playerHealth = Mathf.Min(playerHealth + amount, 100f); // Assuming 100 is the max health
        //Debug.Log($"Player healed by {amount}. Current health: {playerHealth}");

        // Create the bl_DamageInfo struct for healing
        bl_DamageInfo info = new bl_DamageInfo(0); // Use 0 for healing
        info.Sender = gameObject; // Set the sender as this gameObject

        // Trigger Damage HUD event to update the UI
        bl_DamageDelegate.OnDamageEvent(info);
        
        // If at full health, trigger the OnDie event to reset the da
    }
}
