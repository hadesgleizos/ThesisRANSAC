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

    private float evaluationInterval = 2f;
    private float evaluationTimer = 0f;

    // Difficulty parameters
    [Header("Difficulty Bounds")]
    public float minSpawnRate = 0.5f;
    public float maxSpawnRate = 1f;
    public float minSpeed = 0.3f;
    public float maxSpeed = 1.0f;

    [Header("Decrease/Increase Factors")]
    [Tooltip("If the player is struggling, how aggressively do we Lerp downward toward a lower difficulty? (0..1)")]
    public float bigDropFactor = 0.7f;  // Large step downward
    [Tooltip("If the player is doing okay, how quickly do we Lerp upward toward higher difficulty? (0..1)")]
    public float smallUpFactor = 0.1f;  // Small step upward

    [Header("Struggle Thresholds")]
    [Tooltip("If player health below this fraction => 'struggling'")]
    public float lowHealthThreshold = 0.5f;
    [Tooltip("If kill rate below this fraction => 'struggling'")]
    public float lowKillRateThreshold = 0.2f;

    // Performance tracking
    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;

    private void Start()
    {
        InitializeParticles();
        StartCoroutine(EvaluatePlayerPerformance());
    }

    private void InitializeParticles()
    {
        particles = new List<Particle>();
        for (int i = 0; i < swarmSize; i++)
        {
            Particle newParticle = new Particle
            {
                // X => spawnRate, Y => speed
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
        while (true)
        {
            evaluationTimer += Time.deltaTime;
            if (evaluationTimer >= evaluationInterval)
            {
                evaluationTimer = 0f;

                float killRate = playerPerformance.GetKillRate();
                UnityEngine.Debug.Log($"[PSOManager] Current killRate = {killRate:F3}");

                float healthPercentage = playerPerformance.GetHealth() / 100f;

                MeasurePerformance(killRate, healthPercentage);
            }
            yield return null;
        }
    }

    private void MeasurePerformance(float killRate, float healthPercentage)
    {
        long memoryBefore = GC.GetTotalMemory(false);
        stopwatch.Restart();

        // Run PSO steps
        UpdateParticles(killRate, healthPercentage);
        AdjustDifficulty();

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
        UnityEngine.Debug.Log($"Rolling Average Memory Usage:  {averageMemory} bytes");
    }

    private void AdjustDifficulty()
    {
        // PSO's best guess
        Vector2 bestParticle = GetGlobalBestPosition();

        // Current difficulty
        float currentSpawnRate = spawner.GetCurrentSpawnRate();
        float currentSpeed = spawner.GetCurrentZombieSpeed();

        // The "ideal" from the swarm's perspective
        float bestSpawnRate = bestParticle.x;
        float bestSpeed = bestParticle.y;

        // Check if the player is struggling
        float killRate = playerPerformance.GetKillRate();
        float healthPct = playerPerformance.GetHealth() / 100f;
        bool isStruggling = (healthPct < lowHealthThreshold || killRate < lowKillRateThreshold);

        float newSpawnRate;
        float newSpeed;

        if (isStruggling)
        {
            if (bestSpawnRate < currentSpawnRate)
            {
                newSpawnRate = Mathf.Lerp(currentSpawnRate, bestSpawnRate, bigDropFactor);
            }
            else
            {
                float middlePoint = (currentSpawnRate + minSpawnRate) * 0.5f;
                newSpawnRate = Mathf.Lerp(currentSpawnRate, middlePoint, bigDropFactor);
            }

            if (bestSpeed < currentSpeed)
            {
                newSpeed = Mathf.Lerp(currentSpeed, bestSpeed, bigDropFactor);
            }
            else
            {
                float middleSpeed = (currentSpeed + minSpeed) * 0.5f;
                newSpeed = Mathf.Lerp(currentSpeed, middleSpeed, bigDropFactor);
            }
        }
        else
        {
            // Not struggling => small step toward best
            newSpawnRate = Mathf.Lerp(currentSpawnRate, bestSpawnRate, smallUpFactor);
            newSpeed = Mathf.Lerp(currentSpeed, bestSpeed, smallUpFactor);
        }

        newSpawnRate = Mathf.Clamp(newSpawnRate, minSpawnRate, 1.0f);
        newSpeed = Mathf.Clamp(newSpeed, minSpeed, 1.0f);

        spawner.UpdateSpawnRate(newSpawnRate);
        spawner.SetAllZombieSpeeds(newSpeed);

        float aggressiveness = CalculateAggressiveness(newSpawnRate, newSpeed);
        UnityEngine.Debug.Log($"Aggressiveness Score: {aggressiveness:F3}");
    }

    private float CalculateAggressiveness(float spawnRate, float speed)
    {
        float normalizedSpawnRate = (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate);
        float normalizedSpeed = (speed - minSpeed) / (maxSpeed - minSpeed);
        return (normalizedSpawnRate + normalizedSpeed) / 2f;
    }

    private void UpdateParticles(float killRate, float healthPercentage)
    {
        Vector2 globalBestPos = GetGlobalBestPosition();

        foreach (Particle particle in particles)
        {
            // PSO velocity update
            particle.Velocity = inertiaWeight * particle.Velocity
                + cognitiveWeight * UnityEngine.Random.Range(0f, 1f) * (particle.BestPosition - particle.Position)
                + socialWeight    * UnityEngine.Random.Range(0f, 1f) * (globalBestPos - particle.Position);

            particle.Position += particle.Velocity;

            // Clamp
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

        // Example "desiredKillRate" formula
        float desiredKillRate = Mathf.Lerp(
            0.1f,
            0.8f,
            (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate)
        );

        float killRateDiff = Mathf.Abs(killRate - desiredKillRate);
        float healthFactor = 1f - healthPct;

        float fitness = (1f - killRateDiff) * (1f + healthFactor);

        // Additional penalty if struggling
        if (healthPct < lowHealthThreshold || killRate < lowKillRateThreshold)
        {
            fitness *= 0.5f;
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

    // Particle class
    private class Particle
    {
        public Vector2 Position;
        public Vector2 BestPosition;
        public Vector2 Velocity;
    }
}
