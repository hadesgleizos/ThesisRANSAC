using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage

public class GAManager : MonoBehaviour
{
    public PlayerPerformance playerPerformance; // Reference to PlayerPerformance script
    public Spawner spawner; // Reference to Spawner script

    // GA parameters
    private List<Individual> population; // List of individuals in the population
    public int populationSize = 10; // Number of individuals in the population
    public float mutationRate = 0.1f; // Mutation rate for GA
    public int generations = 0; // Track the number of generations

    // Difficulty parameters
    private float minSpawnRate = 0.5f;
    private float maxSpawnRate = 2.5f;
    private float minSpeed = 1.0f;
    private float maxSpeed = 1.5f;
    private float currentSpawnRate;
    private float currentSpeed;

    // Performance tracking variables
    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;

    private void Start()
    {
        InitializePopulation(); // Initialize GA population
        StartCoroutine(EvolvePopulation()); // Start evolving the population
    }

    private void InitializePopulation()
    {
        population = new List<Individual>();
        for (int i = 0; i < populationSize; i++)
        {
            Individual newIndividual = new Individual
            {
                spawnRate = UnityEngine.Random.Range(minSpawnRate, maxSpawnRate),
                speed = UnityEngine.Random.Range(minSpeed, maxSpeed)
            };
            population.Add(newIndividual);
        }
    }

    private IEnumerator EvolvePopulation()
    {
        while (true)
        {
            stopwatch.Restart(); // Start or reset the stopwatch
            long memoryBefore = GC.GetTotalMemory(false); // Measure memory usage before running the algorithm

            generations++;
            List<Individual> newPopulation = new List<Individual>();

            // Selection: Choose the best individuals based on fitness
            population.Sort((a, b) => a.fitness.CompareTo(b.fitness)); // Sort by fitness
            for (int i = 0; i < populationSize / 2; i++)
            {
                newPopulation.Add(population[i]); // Keep the top half
            }

            // Crossover: Generate offspring
            for (int i = 0; i < populationSize / 2; i++)
            {
                Individual parent1 = newPopulation[UnityEngine.Random.Range(0, newPopulation.Count)];
                Individual parent2 = newPopulation[UnityEngine.Random.Range(0, newPopulation.Count)];
                Individual offspring = Crossover(parent1, parent2);
                newPopulation.Add(offspring);
            }

            // Mutation: Apply random changes
            foreach (Individual individual in newPopulation)
            {
                if (UnityEngine.Random.value < mutationRate)
                {
                    Mutate(individual);
                }
            }

            population = newPopulation; // Replace old population with the new one
            EvaluatePopulation(); // Evaluate the fitness of the new population

            stopwatch.Stop(); // Stop the stopwatch
            long memoryAfter = GC.GetTotalMemory(false); // Measure memory usage after running the algorithm

            // Calculate time and memory used
            float elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            long memoryUsed = memoryAfter - memoryBefore;

            // Update the total time, memory, and run count
            totalExecutionTime += elapsedMilliseconds;
            totalMemoryUsage += memoryUsed;
            runCount++;

            // Calculate the rolling averages
            float averageTime = totalExecutionTime / runCount;
            long averageMemory = totalMemoryUsage / runCount;

            // Log the rolling averages
            UnityEngine.Debug.Log("Rolling Average Execution Time: " + averageTime + " ms");
            UnityEngine.Debug.Log("Rolling Average Memory Usage: " + averageMemory + " bytes");

            // Calculate and log aggressiveness
            float aggressiveness = CalculateAggressiveness();
            UnityEngine.Debug.Log("Aggressiveness Score: " + aggressiveness);

            yield return new WaitForSeconds(1f); // Wait for a second before the next generation
        }
    }

    private float CalculateAggressiveness()
    {
        // Get the current best individual's parameters
        Individual bestIndividual = population[0];
        currentSpawnRate = bestIndividual.spawnRate;
        currentSpeed = bestIndividual.speed;

        // Normalize the spawnRate and speed
        float normalizedSpawnRate = (currentSpawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate);
        float normalizedSpeed = (currentSpeed - minSpeed) / (maxSpeed - minSpeed);

        // Aggressiveness score as an average of the normalized values
        return (normalizedSpawnRate + normalizedSpeed) / 2;
    }

    private Individual Crossover(Individual parent1, Individual parent2)
    {
        // Simple crossover: Average the parent's values
        Individual offspring = new Individual
        {
            spawnRate = (parent1.spawnRate + parent2.spawnRate) / 2,
            speed = (parent1.speed + parent2.speed) / 2
        };
        return offspring;
    }

    private void Mutate(Individual individual)
    {
        // Apply small random changes to the individual's properties
        individual.spawnRate = Mathf.Clamp(individual.spawnRate + UnityEngine.Random.Range(-0.1f, 0.1f), minSpawnRate, maxSpawnRate);
        individual.speed = Mathf.Clamp(individual.speed + UnityEngine.Random.Range(-0.1f, 0.1f), minSpeed, maxSpeed);
    }

    private void EvaluatePopulation()
    {
        foreach (Individual individual in population)
        {
            // Evaluate fitness based on player performance
            float killRate = playerPerformance.GetKillRate();
            float healthPercentage = playerPerformance.GetHealth() / 100f;
            individual.fitness = killRate + healthPercentage; // Simple fitness function, adjust as needed
        }

        // Apply the best individual's values to the game
        Individual bestIndividual = population[0];
        spawner.UpdateSpawnRate(bestIndividual.spawnRate);
        spawner.SetAllZombieSpeeds(bestIndividual.speed);
    }
}

// Individual class representing a solution
public class Individual
{
    public float spawnRate;
    public float speed;
    public float fitness;
}
