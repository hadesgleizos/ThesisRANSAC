using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage

public class PSOManager : MonoBehaviour
{
    public PlayerPerformance playerPerformance; // Reference to PlayerPerformance script
    public Spawner spawner; // Reference to Spawner script

    // PSO parameters
    private List<Particle> particles; // List of particles in the swarm
    public int swarmSize = 10; // Number of particles in the swarm
    public float inertiaWeight = 0.5f; // Inertia weight for PSO
    public float cognitiveWeight = 1.0f; // Cognitive weight for PSO
    public float socialWeight = 1.0f; // Social weight for PSO

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
        InitializeParticles(); // Initialize PSO particles
        StartCoroutine(EvaluatePlayerPerformance()); // Start evaluating player performance periodically
    }

    private void InitializeParticles()
    {
        particles = new List<Particle>();
        for (int i = 0; i < swarmSize; i++)
        {
            Particle newParticle = new Particle
            {
                Position = new Vector2(UnityEngine.Random.Range(minSpawnRate, maxSpawnRate), UnityEngine.Random.Range(minSpeed, maxSpeed)),
                BestPosition = new Vector2(UnityEngine.Random.Range(minSpawnRate, maxSpawnRate), UnityEngine.Random.Range(minSpeed, maxSpeed)),
                Velocity = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f))
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

        // Run PSO algorithm
        UpdateParticles(killRate, healthPercentage);
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

    private void AdjustDifficulty()
    {
        Vector2 bestParticle = GetGlobalBestPosition();
        
        float currentSpawnRate = spawner.GetCurrentSpawnRate();
        float newSpawnRate = Mathf.Lerp(currentSpawnRate, bestParticle.x, 0.2f);
        spawner.UpdateSpawnRate(newSpawnRate);

        float currentSpeed = spawner.GetCurrentZombieSpeed();
        float targetSpeed = bestParticle.y;

        float killRate = playerPerformance.GetKillRate();
        if (killRate < 0.1f)
        {
            targetSpeed = Mathf.Lerp(currentSpeed, currentSpeed - 0.1f, 0.5f);
        }

        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 0.1f);
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

    private void UpdateParticles(float killRate, float healthPercentage)
    {
        foreach (Particle particle in particles)
        {
            particle.Velocity = inertiaWeight * particle.Velocity +
                                cognitiveWeight * UnityEngine.Random.Range(0f, 1f) * (particle.BestPosition - particle.Position) +
                                socialWeight * UnityEngine.Random.Range(0f, 1f) * (GetGlobalBestPosition() - particle.Position);

            particle.Position += particle.Velocity;

            particle.Position.x = Mathf.Clamp(particle.Position.x, minSpawnRate, maxSpawnRate);
            particle.Position.y = Mathf.Clamp(particle.Position.y, minSpeed, maxSpeed);

            float currentFitness = EvaluateParticle(particle, killRate, healthPercentage);
            float bestFitness = EvaluateParticle(new Particle { Position = particle.BestPosition }, killRate, healthPercentage);

            if (currentFitness > bestFitness)
            {
                particle.BestPosition = particle.Position;
            }
        }
    }

    private float EvaluateParticle(Particle particle, float killRate, float healthPercentage)
    {
        float spawnRate = particle.Position.x;
        float speed = particle.Position.y;

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

    private Vector2 GetGlobalBestPosition()
    {
        Vector2 bestPosition = Vector2.zero;
        float bestFitness = float.MinValue;

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

    // Particle class to represent each individual in the swarm
    private class Particle
    {
        public Vector2 Position; // Current position of the particle
        public Vector2 BestPosition; // Best position found by the particle
        public Vector2 Velocity; // Current velocity of the particle
    }
}
