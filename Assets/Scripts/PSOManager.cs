using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage

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

    // Performance tracking
    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;

    private bool isWaveActive = false; // Tracks whether a wave is active

    // Smoothing adjustments
    private float smoothedKillRate = 0f;
    private const float smoothingFactor = 0.1f; // Adjust smoothing strength (0.1 for faster, 0.01 for slower)

    private void Start()
    {
        // Listen to wave events from Spawner
        Spawner.OnWaveStart += OnWaveStart;
        Spawner.OnWaveEnd += OnWaveEnd;

        InitializeParticles();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        Spawner.OnWaveStart -= OnWaveStart;
        Spawner.OnWaveEnd -= OnWaveEnd;
    }

private void OnWaveStart(int waveNumber)
{
    isWaveActive = true;
    evaluationTimer = 0f;

    UnityEngine.Debug.Log($"[PSOManager] Wave {waveNumber} started. Continuing with previous difficulty settings.");

    // No reset here; previous adjustments are carried over
    StartCoroutine(EvaluatePlayerPerformance());
}

private void OnWaveEnd(int waveNumber)
{
    isWaveActive = false;

    UnityEngine.Debug.Log($"[PSOManager] Wave {waveNumber} ended. Stopping real-time adjustments.");

    // Stop real-time adjustments at the end of the wave
    StopCoroutine(EvaluatePlayerPerformance());
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
                Velocity = new Vector2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                )
            };
            particles.Add(newParticle);
        }
    }

    private IEnumerator EvaluatePlayerPerformance()
    {
        while (isWaveActive) // Continuously evaluate performance during the active wave
        {
            evaluationTimer += Time.deltaTime;

            if (evaluationTimer >= evaluationInterval)
            {
                evaluationTimer = 0f;

                // Calculate wave-specific metrics
                float waveKillRate = spawner.GetWaveKillCount() / Mathf.Max(1f, spawner.GetWaveElapsedTime());
                float healthPercentage = playerPerformance.GetHealth() / 100f;

                // Smooth the kill rate
                smoothedKillRate = Mathf.Lerp(smoothedKillRate, waveKillRate, smoothingFactor);

                // Adjust evaluation interval dynamically
                AdjustEvaluationInterval(smoothedKillRate);

                UnityEngine.Debug.Log($"[PSOManager] Smoothed Kill Rate: {smoothedKillRate:F3}, Health: {healthPercentage:P1}, Eval Interval: {evaluationInterval:F2}s");

                // Adjust difficulty dynamically during the wave
                AdjustWaveDifficulty(smoothedKillRate, healthPercentage);
            }

            yield return null; // Continue evaluating in real-time
        }
    }

    private void AdjustEvaluationInterval(float waveKillRate)
    {
        // Faster adjustments when kill rate is low, slower when stable
        evaluationInterval = Mathf.Lerp(0.5f, 2f, Mathf.Clamp01(waveKillRate / 0.5f)); // Adjust range as needed
    }

    private void AdjustWaveDifficulty(float waveKillRate, float healthPercentage)
    {
        // Best position calculated by PSO
        Vector2 bestParticle = GetGlobalBestPosition();
        float currentSpawnRate = spawner.GetCurrentSpawnRate();
        float currentSpeed = spawner.GetCurrentZombieSpeed();

        // Determine if the player is struggling or excelling
        bool isStruggling = healthPercentage < lowHealthThreshold || waveKillRate < lowKillRateThreshold;

        float newSpawnRate, newSpeed;

        if (isStruggling)
        {
            // Reduce difficulty dynamically during the wave
            newSpawnRate = Mathf.Lerp(currentSpawnRate, bestParticle.x, bigDropFactor);
            newSpeed = Mathf.Lerp(currentSpeed, bestParticle.y, bigDropFactor);
        }
        else
        {
            // Gradually increase difficulty dynamically during the wave
            newSpawnRate = Mathf.Lerp(currentSpawnRate, bestParticle.x, smallUpFactor);
            newSpeed = Mathf.Lerp(currentSpeed, bestParticle.y, smallUpFactor);
        }

        // Clamp new values to valid ranges
        newSpawnRate = Mathf.Clamp(newSpawnRate, minSpawnRate, maxSpawnRate);
        newSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);

        // Apply adjustments dynamically during the wave
        spawner.UpdateSpawnRate(newSpawnRate);
        spawner.SetAllZombieSpeeds(newSpeed);

        UnityEngine.Debug.Log($"[PSOManager] Adjustments - Spawn Rate: {newSpawnRate:F3}, Speed: {newSpeed:F3}");
    }

    private void UpdateParticles(float killRate, float healthPercentage)
    {
        Vector2 globalBest = GetGlobalBestPosition();
        foreach (Particle particle in particles)
        {
            particle.Velocity = inertiaWeight * particle.Velocity
                                + cognitiveWeight * UnityEngine.Random.Range(0f, 1f) * (particle.BestPosition - particle.Position)
                                + socialWeight * UnityEngine.Random.Range(0f, 1f) * (globalBest - particle.Position);

            particle.Position += particle.Velocity;

            particle.Position.x = Mathf.Clamp(particle.Position.x, minSpawnRate, maxSpawnRate);
            particle.Position.y = Mathf.Clamp(particle.Position.y, minSpeed, maxSpeed);

            float fitness = EvaluateParticle(particle, killRate, healthPercentage);
            if (fitness > EvaluateParticle(new Particle { Position = particle.BestPosition }, killRate, healthPercentage))
            {
                particle.BestPosition = particle.Position;
            }
        }
    }

    private float EvaluateParticle(Particle particle, float killRate, float healthPct)
    {
        float spawnRate = particle.Position.x;
        float speed = particle.Position.y;

        float desiredKillRate = Mathf.Lerp(0.1f, 0.8f, (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate));
        float killRateDiff = Mathf.Abs(killRate - desiredKillRate);
        float healthFactor = 1f - healthPct;

        float fitness = (1f - killRateDiff) * (1f + healthFactor);
        if (healthPct < lowHealthThreshold || killRate < lowKillRateThreshold) fitness *= 0.5f;
        return fitness;
    }

    private Vector2 GetGlobalBestPosition()
    {
        float bestFitness = float.MinValue;
        Vector2 bestPosition = Vector2.zero;

        foreach (Particle particle in particles)
        {
            float fitness = EvaluateParticle(particle, playerPerformance.GetKillRate(), playerPerformance.GetHealth() / 100f);
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
