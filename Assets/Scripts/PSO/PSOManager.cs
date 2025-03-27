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
    
    [Header("Fitness Evaluation Parameters")]
    public float baseKillRatePerSpawner = 0.3f; // Previously hardcoded as 0.3f
    public float noKillsPenaltyFitness = 0.1f;  // Previously hardcoded as 0.1f
    public float killRateWeight = 0.7f;         // Previously hardcoded as 0.7f
    public float healthWeight = 0.3f;           // Previously hardcoded as 0.3f
    
    [Header("PSO Adaptation Parameters")]
    public float initialInertiaWeight = 0.7f;    // Previously hardcoded as 0.7f
    public float finalInertiaWeight = 0.4f;      // Previously hardcoded as 0.4f
    public float initialCognitiveWeight = 1.5f;  // Previously hardcoded as 1.5f
    public float finalCognitiveWeight = 0.8f;    // Previously hardcoded as 0.8f
    public float initialSocialWeight = 0.8f;     // Previously hardcoded as 0.8f
    public float finalSocialWeight = 1.5f;       // Previously hardcoded as 1.5f
    
    [Header("Performance Metrics")]
    public bool enableMetrics = true;
    public int metricsHistoryLimit = 100;       // Previously hardcoded as 100
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

    [SerializeField] 
    private PSODisplay psoDisplay;

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

    // Use the same struggle definition as in EvaluateParticle
    float expectedKillRate = baseKillRatePerSpawner * spawner.GetActiveSpawnerCount() * 
                           (currentSpawnRate / maxSpawnRate) * 
                           (currentSpeed / maxSpeed);
    float performanceRatio = killRate / Mathf.Max(0.01f, expectedKillRate);
    bool isStruggling = (healthPercentage < lowHealthThreshold || performanceRatio < 0.85f);

    float newSpawnRate;
    float newSpeed;

    if (isStruggling)
    {
        // Use a more aggressive drop when struggling
        newSpawnRate = Mathf.Lerp(currentSpawnRate, minSpawnRate + (maxSpawnRate - minSpawnRate) * 0.3f, bigDropFactor);
        newSpeed = Mathf.Lerp(currentSpeed, minSpeed + (maxSpeed - minSpeed) * 0.3f, bigDropFactor);
    }
    else
    {
        // When not struggling, consider applying best position more aggressively
        // Increase adjustment factor based on how well the player is doing
        float adjustmentFactor = smallUpFactor;
        if (performanceRatio > 1.2f) {
            // Player is doing very well, increase adjustment speed
            adjustmentFactor = smallUpFactor * 2f;
        }
        
        newSpawnRate = Mathf.Lerp(currentSpawnRate, bestSpawnRate, adjustmentFactor);
        newSpeed = Mathf.Lerp(currentSpeed, bestSpeed, adjustmentFactor);
    }

    // Clamp values to ensure they stay within bounds
    newSpawnRate = Mathf.Clamp(newSpawnRate, minSpawnRate, maxSpawnRate);
    newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);

    // Apply adjustments
    if (spawner != null) {
        spawner.UpdateSpawnRate(newSpawnRate);
        spawner.SetAllZombieSpeeds(newSpeed);
    }

    UnityEngine.Debug.Log($"[PSOManager] Adjustments - Spawn Rate: {newSpawnRate:F3}, Speed: {newSpeed:F3}, " +
                         $"Global Best: ({bestSpawnRate:F2}, {bestSpeed:F2}), " +
                         $"Performance Ratio: {performanceRatio:F2}, " +
                         $"Struggling: {isStruggling}");
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
    
    // Calculate expected kill rate based on number of spawners and current parameters
    float expectedKillRate = baseKillRatePerSpawner * spawnerCount * 
                           (spawnRate / maxSpawnRate) * 
                           (speed / maxSpeed);

    // If no kills when zombies are available
    if (killRate <= 0 && spawnRate > minSpawnRate)
    {
        return noKillsPenaltyFitness;
    }

    // Calculate the performance ratio (how player is doing relative to expectation)
    float performanceRatio = killRate / Mathf.Max(0.01f, expectedKillRate);
    
    // More clear struggling detection - consider struggling if achieving less than 85% of expected
    bool playerStruggling = (healthPct < lowHealthThreshold || performanceRatio < 0.85f);
    
    float killRateScore;
    if (playerStruggling) {
        // When struggling, reward lower spawn rates and speeds
        // The lower the parameters, the higher the score
        killRateScore = 1.0f - ((spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate)) * 
                              ((speed - minSpeed) / (maxSpeed - minSpeed));
    } else {
        // When doing well, reward parameters that challenge the player appropriately
        // Score highest when kill rate is near expected (not too easy, not too hard)
        killRateScore = 1.0f - Mathf.Pow(Mathf.Abs(killRate - expectedKillRate), 2);
        
        // Bonus for challenging parameters when player is doing well
        if (performanceRatio > 1.1f) {  // Player doing better than expected
            // Encourage higher parameters
            killRateScore *= 0.8f + 0.2f * ((spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate)) * 
                                    ((speed - minSpeed) / (maxSpeed - minSpeed));
        }
    }
    
    float healthScore = Mathf.Clamp01(healthPct);

    // Final fitness calculation
    float fitness = (killRateScore * killRateWeight) + (healthScore * healthWeight);

    UnityEngine.Debug.Log($"PSO Evaluation - Spawners: {spawnerCount}, " +
                         $"Expected KillRate: {expectedKillRate:F2}, " +
                         $"Actual KillRate: {killRate:F2}, " +
                         $"Performance Ratio: {performanceRatio:F2}, " +
                         $"SpawnRate: {spawnRate:F2}, Speed: {speed:F2}, " +
                         $"Struggling: {playerStruggling}, " +
                         $"Fitness: {fitness:F2}");

    if (psoDisplay != null)
    {
        psoDisplay.UpdateDisplayData(
            spawnerCount,
            expectedKillRate,
            killRate,
            performanceRatio,
            spawnRate,
            speed,
            playerStruggling,
            fitness
        );
    }

    return fitness;
}


private Vector2 GetGlobalBestPosition()
{
    Vector2 bestPosition = Vector2.zero;
    float bestFitness = float.MinValue;

    float currentKillRate = playerPerformance.GetKillRate();
    float currentHealthPct = playerPerformance.GetHealth() / 100f;
    
    // Cache player state
    bool isStruggling = (currentHealthPct < lowHealthThreshold || 
                        currentKillRate < lowKillRateThreshold);

    foreach (Particle particle in particles)
    {
        // Get current fitness 
        float fitness = EvaluateParticle(particle, currentKillRate, currentHealthPct);
        
        // If player is struggling, favor lower parameters
        if (isStruggling)
        {
            // Apply a bias toward lower parameters when struggling
            float spawnRateNormalized = (particle.Position.x - minSpawnRate) / (maxSpawnRate - minSpawnRate);
            float speedNormalized = (particle.Position.y - minSpeed) / (maxSpeed - minSpeed);
            
            // Invert the score - lower parameters get higher fitness when struggling
            float parameterPenalty = (spawnRateNormalized + speedNormalized) / 2.0f;
            fitness *= (1.0f - parameterPenalty * 0.5f);
        }
        
        if (fitness > bestFitness)
        {
            bestFitness = fitness;
            bestPosition = particle.Position;
        }
    }
    
    UnityEngine.Debug.Log($"PSO Global Best - Position: ({bestPosition.x:F2}, {bestPosition.y:F2}), " +
                         $"Player Struggling: {isStruggling}");
    
    return bestPosition;
}

    private void LogMetrics(float fitness, Vector2 parameters)
    {
        if (!enableMetrics) return;
        
        fitnessHistory.Add(fitness);
        parameterHistory.Add(parameters);
        
        if (fitnessHistory.Count > metricsHistoryLimit)
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
        
        inertiaWeight = Mathf.Lerp(initialInertiaWeight, finalInertiaWeight, progress);
        cognitiveWeight = Mathf.Lerp(initialCognitiveWeight, finalCognitiveWeight, progress);
        socialWeight = Mathf.Lerp(initialSocialWeight, finalSocialWeight, progress);
        
        UnityEngine.Debug.Log($"PSO Parameters Updated - Wave: {currentWave}/{totalWaves}, " +
                             $"Inertia: {inertiaWeight:F2}, " +
                             $"Cognitive: {cognitiveWeight:F2}, " +
                             $"Social: {socialWeight:F2}");
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
