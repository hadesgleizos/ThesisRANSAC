using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage
using System.Linq; // For Average

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

    [Header("Performance Metrics")]
    public bool enableMetrics = true;
    private List<float> fitnessHistory = new List<float>();
    private List<Vector2> parameterHistory = new List<Vector2>();

    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;

    private int previousKillCount = 0; // Track the player's previous kill count
    private bool isWaveActive = false; // Changed to false by default
    private bool isPSOPaused = false;

    [Header("Wave Info")]
    private int currentWave = 0;
    private int totalWaves;

    private void Start()
    {
        // Subscribe to wave events
        Spawner.OnWaveStart += OnWaveStart;
        Spawner.OnWaveEnd += OnWaveEnd;

        // Get total waves from spawner
        if (spawner != null)
        {
            totalWaves = spawner.totalWaves;
        }
        else
        {
            UnityEngine.Debug.LogError("Spawner reference not set in PSOManager!");
        }

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
        isPSOPaused = false;
        currentWave = waveNumber;
        UpdatePSOParameters();
        UnityEngine.Debug.Log($"[PSOManager] Wave {waveNumber} started. PSO calculations resumed.");
    }

    private void OnWaveEnd(int waveNumber)
    {
        isWaveActive = false;
        isPSOPaused = true;
        UnityEngine.Debug.Log($"[PSOManager] Wave {waveNumber} ended. PSO calculations paused.");
        
        // Reset evaluation timer to ensure fresh start on next wave
        evaluationTimer = 0f;
        previousKillCount = 0;
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
            // Only evaluate if wave is active and PSO is not paused
            if (isWaveActive && !isPSOPaused && !spawner.IsInCooldown())
            {
                evaluationTimer += Time.deltaTime;

                if (evaluationTimer >= evaluationInterval)
                {
                    evaluationTimer = 0f;

                    int currentKillCount = spawner.GetWaveKillCount();
                    float killRate = (currentKillCount - previousKillCount) / Mathf.Max(1f, evaluationInterval);
                    previousKillCount = currentKillCount;

                    float healthPercentage = playerPerformance.GetHealth() / 100f;

                    MeasurePerformance(killRate, healthPercentage);
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
        newSpeed = Mathf.Lerp(currentSpeed, bestSpeed * 0.5f, bigDropFactor); // Reduce speed more aggressively
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
        // Issue: No consideration of wave progress or current wave number
        Vector2 globalBestPos = GetGlobalBestPosition();

        foreach (Particle particle in particles)
        {
            // Standard PSO velocity update
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

            LogMetrics(currentFitness, particle.Position);
        }
    }

private float EvaluateParticle(Particle particle, float killRate, float healthPct)
{
    float spawnRate = particle.Position.x;
    float speed = particle.Position.y;

    // Get number of active spawners
    int spawnerCount = spawner.GetActiveSpawnerCount();
    
    // Base kill rate per spawner (kills expected per second per spawner)
    float baseKillRatePerSpawner = 0.3f; // Lowered from 0.05f for better balance
    
    // Calculate expected kill rate based on number of spawners and current parameters
    float expectedKillRate = baseKillRatePerSpawner * spawnerCount * 
                           (spawnRate / maxSpawnRate) * 
                           (speed / maxSpeed);

    // If no kills when zombies are available
    if (killRate <= 0 && spawnRate > minSpawnRate)
    {
        return 0.1f;
    }

    // Calculate scores
    float killRateScore = 1f - Mathf.Pow(Mathf.Abs(killRate - expectedKillRate), 2);
    float healthScore = Mathf.Clamp01(healthPct);

    // Final fitness calculation
    float fitness = (killRateScore * 0.7f) + (healthScore * 0.3f);

    UnityEngine.Debug.Log($"PSO Evaluation - Spawners: {spawnerCount}, " +
                         $"Expected KillRate: {expectedKillRate:F2}, " +
                         $"Actual KillRate: {killRate:F2}, " +
                         $"SpawnRate: {spawnRate:F2}, Speed: {speed:F2}, " +
                         $"Fitness: {fitness:F2}");

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

    private void LogMetrics(float fitness, Vector2 parameters)
    {
        if (!enableMetrics) return;
        
        fitnessHistory.Add(fitness);
        parameterHistory.Add(parameters);
        
        if (fitnessHistory.Count > 100)
        {
            float avgFitness = fitnessHistory.Average();
            float parameterVariance = CalculateParameterVariance();
            UnityEngine.Debug.Log($"PSO Metrics - Avg Fitness: {avgFitness:F3}, Parameter Variance: {parameterVariance:F3}");
            fitnessHistory.Clear();
            parameterHistory.Clear();
        }
    }

    private void UpdatePSOParameters()
    {
        // Adapt weights based on wave progression
        float progress = (float)currentWave / totalWaves;
        
        inertiaWeight = Mathf.Lerp(0.7f, 0.4f, progress);
        cognitiveWeight = Mathf.Lerp(1.5f, 0.8f, progress);
        socialWeight = Mathf.Lerp(0.8f, 1.5f, progress);
    }

    private float CalculateParameterVariance()
    {
        if (parameterHistory.Count == 0) return 0f;

        Vector2 mean = Vector2.zero;
        foreach (Vector2 param in parameterHistory)
        {
            mean += param;
        }
        mean /= parameterHistory.Count;

        float variance = 0f;
        foreach (Vector2 param in parameterHistory)
        {
            variance += (param - mean).sqrMagnitude;
        }
        return variance / parameterHistory.Count;
    }

    private class Particle
    {
        public Vector2 Position;
        public Vector2 BestPosition;
        public Vector2 Velocity;
    }
}
