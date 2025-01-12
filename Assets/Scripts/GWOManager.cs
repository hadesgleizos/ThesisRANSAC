using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage

public class GWOManager : MonoBehaviour
{
    public PlayerPerformance playerPerformance; // Reference to PlayerPerformance script
    public Spawner spawner; // Reference to Spawner script

    // GWO parameters
    private List<Wolf> wolves; // List of wolves in the pack
    public int packSize = 10; // Number of wolves in the pack

    private float evaluationInterval = 20f; // Evaluation interval for player performance
    private float evaluationTimer = 0f; // Timer for evaluation interval

    // Difficulty parameters
    private float minSpawnRate = 0.5f;
    private float maxSpawnRate = 2.5f;
    private float minSpeed = 1.0f;
    private float maxSpeed = 2.0f;
    private float currentSpawnRate;
    private float currentSpeed;

    // Performance tracking variables
    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;

    private void Start()
    {
        InitializeWolves(); // Initialize GWO wolves
        StartCoroutine(EvaluatePlayerPerformance()); // Start evaluating player performance periodically
    }

    private void InitializeWolves()
    {
        wolves = new List<Wolf>();
        for (int i = 0; i < packSize; i++)
        {
            Wolf newWolf = new Wolf
            {
                Position = new Vector2(UnityEngine.Random.Range(minSpawnRate, maxSpawnRate), UnityEngine.Random.Range(minSpeed, maxSpeed))
            };
            wolves.Add(newWolf);
        }
    }

    private IEnumerator EvaluatePlayerPerformance()
    {
        while (true)
        {
            evaluationTimer += Time.deltaTime;
            if (evaluationTimer >= evaluationInterval)
            {
                evaluationTimer = 0f;
                float killRate = playerPerformance.GetKillRate(); // Get the player's kill rate
                float healthPercentage = playerPerformance.GetHealth() / 100f; // Get player's health percentage

                MeasurePerformance(killRate, healthPercentage); // Measure and log performance
            }
            yield return null; // Wait for the next frame
        }
    }

    private void MeasurePerformance(float killRate, float healthPercentage)
    {
        long memoryBefore = System.GC.GetTotalMemory(false);
        stopwatch.Restart();

        // Run GWO algorithm
        UpdateWolves(killRate, healthPercentage);
        AdjustDifficulty();

        stopwatch.Stop();
        long memoryAfter = System.GC.GetTotalMemory(false);

        float elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        long memoryUsed = memoryAfter - memoryBefore;

        totalExecutionTime += elapsedMilliseconds;
        totalMemoryUsage += memoryUsed;
        runCount++;

        float averageTime = totalExecutionTime / runCount;
        long averageMemory = totalMemoryUsage / runCount;

        UnityEngine.Debug.Log("Rolling Average Execution Time: " + averageTime + " ms");
        UnityEngine.Debug.Log("Rolling Average Memory Usage: " + averageMemory + " bytes");
    }

    private void UpdateWolves(float killRate, float healthPercentage)
    {
        // Sort wolves based on their fitness
        wolves.Sort((a, b) => EvaluateWolf(b, killRate, healthPercentage).CompareTo(EvaluateWolf(a, killRate, healthPercentage)));

        // Alpha, Beta, and Delta wolves
        Wolf alpha = wolves[0];
        Wolf beta = wolves[1];
        Wolf delta = wolves[2];

        // Update positions of all wolves
        foreach (Wolf wolf in wolves)
        {
            Vector2 newAlphaPosition = alpha.Position + RandomVector() * Vector2.Distance(alpha.Position, wolf.Position);
            Vector2 newBetaPosition = beta.Position + RandomVector() * Vector2.Distance(beta.Position, wolf.Position);
            Vector2 newDeltaPosition = delta.Position + RandomVector() * Vector2.Distance(delta.Position, wolf.Position);

            // Calculate the new position
            wolf.Position = (newAlphaPosition + newBetaPosition + newDeltaPosition) / 3f;

            // Clamp the values to avoid extreme parameters
            wolf.Position.x = Mathf.Clamp(wolf.Position.x, minSpawnRate, maxSpawnRate);
            wolf.Position.y = Mathf.Clamp(wolf.Position.y, minSpeed, maxSpeed);
        }
    }

    private void AdjustDifficulty()
    {
        Vector2 alphaPosition = wolves[0].Position; // Alpha wolf's position

        // Smoothly adjust spawn rate
        float currentSpawnRate = spawner.GetCurrentSpawnRate();
        float newSpawnRate = Mathf.Lerp(currentSpawnRate, alphaPosition.x, 0.2f);
        spawner.UpdateSpawnRate(newSpawnRate);

        // Smoothly adjust zombie speed
        float currentSpeed = spawner.GetCurrentZombieSpeed();
        float targetSpeed = alphaPosition.y;

        // Limit speed increase when kill rate is very low
        if (playerPerformance.GetKillRate() < 0.1f)
        {
            targetSpeed = Mathf.Min(targetSpeed, currentSpeed + 0.1f);
        }

        // Apply smoothing
        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 0.3f);

        // Clamp the speed to a more reasonable range
        newSpeed = Mathf.Clamp(newSpeed, 1.0f, 1.5f);
        spawner.SetAllZombieSpeeds(newSpeed);

        // Calculate and log aggressiveness
        float aggressiveness = CalculateAggressiveness(newSpawnRate, newSpeed);
        UnityEngine.Debug.Log("Aggressiveness Score: " + aggressiveness);
    }

    // Method to calculate aggressiveness
    private float CalculateAggressiveness(float spawnRate, float speed)
    {
        float normalizedSpawnRate = (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate);
        float normalizedSpeed = (speed - minSpeed) / (maxSpeed - minSpeed);
        return (normalizedSpawnRate + normalizedSpeed) / 2;
    }

    private float EvaluateWolf(Wolf wolf, float killRate, float healthPercentage)
    {
        float spawnRate = wolf.Position.x;
        float speed = wolf.Position.y;

        float desiredKillRate = Mathf.Lerp(0.2f, 0.8f, (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate));
        float killRateDifference = Mathf.Abs(killRate - desiredKillRate);
        float healthFactor = 1f - healthPercentage;
        float fitness = (1f - killRateDifference) * (1f + healthFactor);

        if (killRate <= 0 || (healthPercentage >= 0.9f && killRate < 0.1f))
        {
            fitness *= 0.5f;
        }
        if (speed > 1.3f && killRate < 0.2f)
        {
            fitness *= 0.8f;
        }

        return fitness;
    }

    private Vector2 RandomVector()
    {
        return new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
    }

    // Wolf class to represent each individual in the pack
    private class Wolf
    {
        public Vector2 Position; // Current position of the wolf
    }
}
