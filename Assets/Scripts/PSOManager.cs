using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage
using System.Linq; // Add this line for Average()

public class PSOManager : MonoBehaviour
{
    public PlayerPerformance playerPerformance; // Reference to PlayerPerformance script
    public Spawner spawner;                    // Reference to Spawner script

    // PSO parameters
    private List<Particle> particles;
    [Header("PSO Settings")]
    public int swarmSize = 10;
    public float inertiaWeight = 0.5f;
    public float cognitiveWeight = 1.0f;
    public float socialWeight = 1.0f;

    private float evaluationInterval = 2f; // Initial evaluation interval
    private float evaluationTimer = 0f;

    // Difficulty parameters
    [Header("Difficulty Bounds")]
    public float minSpawnRate = 0.5f;
    public float maxSpawnRate = 1f;
    public float minSpeed = 0.3f;
    public float maxSpeed = 1.0f;

    [Header("Decrease/Increase Factors")]
    public float bigDropFactor = 0.7f;  // Large step downward
    public float smallUpFactor = 0.1f;  // Small step upward

    [Header("Struggle Thresholds")]
    public float lowHealthThreshold = 0.5f;
    public float lowKillRateThreshold = 0.2f;

    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;

    private int previousKillCount = 0; // Track the player's previous kill count
    private bool isWaveActive = true; // Tracks whether a wave is currently active

    [Header("Kill Rate Tracking")]
    private float currentKillRate = 0f;
    private Queue<float> killRateHistory = new Queue<float>();
    public int killRateHistorySize = 10; // Store last 10 measurements
    public bool debugKillRate = true; // Toggle for debug logging

    private void Start()
    {
        // Subscribe to wave events
        Spawner.OnWaveStart += OnWaveStart;
        Spawner.OnWaveEnd += OnWaveEnd;

        InitializeParticles();
        StartCoroutine(EvaluatePlayerPerformance());
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        Spawner.OnWaveStart -= OnWaveStart;
        Spawner.OnWaveEnd -= OnWaveEnd;
    }

    private void OnWaveStart(int waveNumber)
    {
        isWaveActive = true;
        UnityEngine.Debug.Log($"[PSOManager] Wave {waveNumber} started.");
    }

    private void OnWaveEnd(int waveNumber)
    {
        isWaveActive = false;
        UnityEngine.Debug.Log($"[PSOManager] Wave {waveNumber} ended. Pausing performance evaluation.");
    }

    private void InitializeParticles()
    {
        particles = new List<Particle>();
        for (int i = 0; i < swarmSize; i++)
        {
            Particle newParticle = new Particle
            {
                Position = new Vector2(
                    UnityEngine.Random.Range(minSpawnRate, maxSpawnRate),
                    UnityEngine.Random.Range(minSpeed, maxSpeed)
                ),
                BestPosition = new Vector2(
                    UnityEngine.Random.Range(minSpawnRate, maxSpawnRate),
                    UnityEngine.Random.Range(minSpeed, maxSpeed)
                ),
                Velocity = Vector2.ClampMagnitude(
                    new Vector2(
                        UnityEngine.Random.Range(-1f, 1f),
                        UnityEngine.Random.Range(-1f, 1f)
                    ),
                    0.5f // Maximum velocity
                )
            };
            particles.Add(newParticle);
        }
    }

    private IEnumerator EvaluatePlayerPerformance()
    {
        while (true)
        {
            if (isWaveActive)
            {
                evaluationTimer += Time.deltaTime;

                if (evaluationTimer >= evaluationInterval)
                {
                    evaluationTimer = 0f;

                    int currentKillCount = spawner.GetWaveKillCount();
                    UnityEngine.Debug.Log($"[PSOManager] Previous Kill Count: {previousKillCount}, Current Kill Count: {currentKillCount}");
                    
                    currentKillRate = (currentKillCount - previousKillCount) / Mathf.Max(1f, evaluationInterval);
                    previousKillCount = currentKillCount;

                    killRateHistory.Enqueue(currentKillRate);
                    if (killRateHistory.Count > killRateHistorySize)
                    {
                        killRateHistory.Dequeue();
                    }

                    float averageKillRate = killRateHistory.Count > 0 ? killRateHistory.Average() : 0f;

                    UnityEngine.Debug.Log($"[PSOManager] KILL RATE UPDATE ----");
                    UnityEngine.Debug.Log($"[PSOManager] Current Kill Rate: {currentKillRate:F2}/s");
                    UnityEngine.Debug.Log($"[PSOManager] Average Kill Rate: {averageKillRate:F2}/s");
                    UnityEngine.Debug.Log($"[PSOManager] Active Spawners: {spawner.GetActiveSpawnerCount()}");
                    UnityEngine.Debug.Log($"[PSOManager] ----");

                    float healthPercentage = playerPerformance.GetHealth() / 100f;
                    MeasurePerformance(currentKillRate, healthPercentage);
                }
            }
            yield return null;
        }
    }

    private void MeasurePerformance(float killRate, float healthPercentage)
    {
        long memoryBefore = GC.GetTotalMemory(false);
        stopwatch.Restart();

        UpdateParticles(killRate, healthPercentage);
        AdjustDifficulty(killRate, healthPercentage);

        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(false);

        float elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        long memoryUsed = memoryAfter - memoryBefore;

        totalExecutionTime += elapsedMilliseconds;
        totalMemoryUsage += memoryUsed;
        runCount++;

        float averageTime = totalExecutionTime / runCount;
        long averageMemory = totalMemoryUsage / runCount;

        UnityEngine.Debug.Log($"Rolling Average Execution Time: {averageTime} ms");
        UnityEngine.Debug.Log($"Rolling Average Memory Usage: {averageMemory} bytes");
    }

private void AdjustDifficulty(float killRate, float healthPercentage)
{
    Vector2 bestParticle = GetGlobalBestPosition();
    float currentSpawnRate = spawner.GetCurrentSpawnRate();
    float currentSpeed = spawner.GetCurrentZombieSpeed();

    float bestSpawnRate = bestParticle.x;
    float bestSpeed = bestParticle.y;

    bool isStruggling = (healthPercentage < lowHealthThreshold || killRate < lowKillRateThreshold);

    float newSpawnRate;
    float newSpeed;

    if (isStruggling)
    {
        // Aggressively reduce spawn rate and speed
        newSpawnRate = Mathf.Lerp(currentSpawnRate, bestSpawnRate, bigDropFactor);
        newSpeed = Mathf.Lerp(currentSpeed, bestSpeed, bigDropFactor); // Reduce speed more aggressively
    }
    else
    {
        // Gradually increase spawn rate and speed
        newSpawnRate = Mathf.Lerp(currentSpawnRate, bestSpawnRate, smallUpFactor);
        newSpeed = Mathf.Lerp(currentSpeed, bestSpeed, smallUpFactor);
    }

    // Clamp values to ensure they stay within bounds
    newSpawnRate = Mathf.Clamp(newSpawnRate, minSpawnRate, maxSpawnRate);
    newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);

    // Apply adjustments
    spawner.UpdateSpawnRate(newSpawnRate);
    spawner.SetAllZombieSpeeds(newSpeed);

    UnityEngine.Debug.Log($"[PSOManager] Adjustments - Spawn Rate: {newSpawnRate:F3}, Speed: {newSpeed:F3}");
}


    private void UpdateParticles(float killRate, float healthPercentage)
    {
        Vector2 globalBestPos = GetGlobalBestPosition();

        foreach (Particle particle in particles)
        {
            particle.Velocity = inertiaWeight * particle.Velocity
                + cognitiveWeight * UnityEngine.Random.Range(0f, 1f) * (particle.BestPosition - particle.Position)
                + socialWeight * UnityEngine.Random.Range(0f, 1f) * (globalBestPos - particle.Position);

            particle.Position += particle.Velocity;

            particle.Position.x = Mathf.Clamp(particle.Position.x, minSpawnRate, maxSpawnRate);
            particle.Position.y = Mathf.Clamp(particle.Position.y, minSpeed, maxSpeed);

            float currentFitness = EvaluateParticle(particle, killRate, healthPercentage);

            var temp = new Particle { Position = particle.BestPosition };
            float bestFitness = EvaluateParticle(temp, killRate, healthPercentage);

            if (currentFitness > bestFitness)
            {
                particle.BestPosition = particle.Position;
            }
        }
    }

private float EvaluateParticle(Particle particle, float killRate, float healthPct)
{
    float spawnRate = particle.Position.x;
    float speed = particle.Position.y;

    // Get the number of active spawners
    int activeSpawners = spawner.GetActiveSpawnerCount(); // You'll need to add this method to your Spawner script
    
    // Scale the ideal kill rate based on spawn rate, speed, and number of active spawners
    float idealKillRate = Mathf.Lerp(
        0.1f, // Minimum ideal kill rate
        0.8f * activeSpawners, // Maximum ideal kill rate scales with number of spawners
        ((spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate) + 
         (speed - minSpeed) / (maxSpeed - minSpeed)) / 2f
    );

    float killRateDiff = Mathf.Abs(killRate - idealKillRate);
    float healthFactor = 1f - healthPct;

    // Separate evaluation for spawn rate and speed
    float spawnRateFitness = 1f - killRateDiff;
    float speedFitness = healthPct;  // Higher health means speed can be higher

    // Combine both factors
    float fitness = (spawnRateFitness + speedFitness) / 2f;

    // Modified struggle penalty that affects both parameters independently
    if (healthPct < lowHealthThreshold || killRate < lowKillRateThreshold)
    {
        float spawnRatePenalty = spawnRate / maxSpawnRate;
        float speedPenalty = speed / maxSpeed;
        fitness *= (1f - (spawnRatePenalty + speedPenalty) / 2f);
    }

    return fitness;
}


    private Vector2 GetGlobalBestPosition()
    {
        Vector2 bestPosition = Vector2.zero;
        float bestFitness = float.MinValue;

        float currentKillRate = playerPerformance.GetKillRate();
        float currentHealthPct = playerPerformance.GetHealth() / 100f;

        foreach (Particle particle in particles)
        {
            float fitness = EvaluateParticle(particle, currentKillRate, currentHealthPct);
            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                bestPosition = particle.Position;
            }
        }
        return bestPosition;
    }

    private class Particle
    {
        public Vector2 Position;
        public Vector2 BestPosition;
        public Vector2 Velocity;
    }
}
