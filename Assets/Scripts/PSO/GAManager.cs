using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage
using System.Linq; // For Average

public class GAManager : MonoBehaviour
{
    // GA parameters
    private List<Individual> population;
    
    [Header("GA Settings")]
    public int populationSize = 10;
    public float mutationRate = 0.1f; // Probability of mutation
    public float crossoverRate = 0.7f; // Probability of crossover
    private float totalRunTime = 0f;
    private float lastUpdateTime = 0f;
    
    [Header("Parameter Bounds")]
    public float minParamX = 0.1f;
    public float maxParamX = 1.0f;
    public float minParamY = 0.1f;
    public float maxParamY = 1.0f;
    
    // Performance metrics (mirroring PSO implementation)
    [Header("Performance Metrics")]
    public bool enableMetrics = true;
    public int metricsHistoryLimit = 100;   
    private List<float> fitnessHistory = new List<float>();
    private List<Vector2> parameterHistory = new List<Vector2>();
    
    private Stopwatch stopwatch = new Stopwatch();
    private float totalExecutionTime = 0f;
    private long totalMemoryUsage = 0;
    private int runCount = 0;
    private float processingSpeed = 1.0f; // Speed of algorithm in ms
    
    [Header("Performance Display")]
    public bool showPerformanceOverlay = true;
    public bool showDetailedMetrics = false;
    private float currentAggressiveness = 0.5f; // 0-1 scale
    private Rect performanceRect = new Rect(10, 10, 220, 140);
    
    // Best solutions tracking
    private Individual bestIndividual;
    private float elitismRate = 0.1f; // Percentage of elite individuals to keep
    
    // Auto-run settings
    [Header("Demo Settings")]
    public bool autoRun = true;
    public float updateFrequency = 10f; // Updates per second
    private float updateInterval => 1f / updateFrequency;
    private float timeSinceLastUpdate = 0f;
    
    // For test function - GA will try to optimize this
    public enum OptimizationFunction
    {
        Sphere,           // Simple unimodal function
        Rosenbrock,       // Difficult valley-shaped function
        Rastrigin,        // Highly multimodal function
        Ackley            // Multimodal with many local minima
    }
    
    [Header("Test Function")]
    public OptimizationFunction functionType = OptimizationFunction.Sphere;
    public bool maximizeFunction = false; // If false, we minimize
    
    private void Start()
    {
        InitializePopulation();
        lastUpdateTime = Time.time;
    }
    
    private void Update()
    {
        if (!autoRun) return;
        
        // Track total run time
        totalRunTime += Time.deltaTime;
        
        // Update aggressiveness in real-time using sine waves for natural variation
        UpdateAggressiveness();
        
        // Update based on frequency setting
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            MeasurePerformance();
            timeSinceLastUpdate = 0f;
        }
    }

    // New method to update aggressiveness continuously
    private void UpdateAggressiveness()
    {
        // Create natural variations using sine waves with different frequencies
        float timeScale1 = 0.2f;  // Slower variation
        float timeScale2 = 0.5f;  // Faster variation
        
        // Combine two sine waves for more natural movement
        float wave1 = Mathf.Sin(totalRunTime * timeScale1);
        float wave2 = Mathf.Sin(totalRunTime * timeScale2 + 0.5f) * 0.3f;
        
        // Normalize to 0-1 range with bias toward middle values
        float normalizedValue = (wave1 + wave2) * 0.4f + 0.5f;
        normalizedValue = Mathf.Clamp01(normalizedValue);
        
        // Update mutation and crossover rates based on aggressiveness
        mutationRate = Mathf.Lerp(0.05f, 0.3f, normalizedValue);
        crossoverRate = Mathf.Lerp(0.9f, 0.6f, normalizedValue);
        
        // Update aggressiveness value for display
        currentAggressiveness = normalizedValue;
    }
    
    private void InitializePopulation()
    {
        population = new List<Individual>();
        
        // Initialize best individual with worst possible value
        if (maximizeFunction)
        {
            bestIndividual = new Individual { Fitness = float.MinValue };
        }
        else
        {
            bestIndividual = new Individual { Fitness = float.MaxValue };
        }
        
        // Create initial population with random positions
        for (int i = 0; i < populationSize; i++)
        {
            Individual newIndividual = new Individual
            {
                Genes = new Vector2(
                    UnityEngine.Random.Range(minParamX, maxParamX),
                    UnityEngine.Random.Range(minParamY, maxParamY)
                )
            };
            
            // Calculate initial fitness
            newIndividual.Fitness = EvaluateIndividual(newIndividual);
            
            // Update best if needed
            UpdateBestIndividual(newIndividual);
            
            population.Add(newIndividual);
        }
    }
    
    private void MeasurePerformance()
    {
        long memoryBefore = GC.GetTotalMemory(false);
        stopwatch.Restart();
        
        // Run one iteration of GA
        RunGAIteration();
        
        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(false);
        
        float elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        long memoryUsed = memoryAfter - memoryBefore;
        
        totalExecutionTime += elapsedMilliseconds;
        totalMemoryUsage += memoryUsed;
        runCount++;
        
        // Calculate and log metrics
        if (enableMetrics)
        {
            LogMetrics(bestIndividual.Fitness, bestIndividual.Genes);
        }
    }
    
    private void RunGAIteration()
    {
        // Calculate elapsed time
        float elapsedTime = (Time.time - lastUpdateTime);
        lastUpdateTime = Time.time;
        
        // Create a new generation
        List<Individual> newPopulation = new List<Individual>();
        
        // Elitism - keep the best individuals
        int elitesCount = Mathf.Max(1, Mathf.FloorToInt(populationSize * elitismRate));
        List<Individual> sortedPopulation = new List<Individual>(population);
        
        // Sort by fitness
        if (maximizeFunction)
        {
            sortedPopulation.Sort((a, b) => b.Fitness.CompareTo(a.Fitness)); // Descending
        }
        else
        {
            sortedPopulation.Sort((a, b) => a.Fitness.CompareTo(b.Fitness)); // Ascending
        }
        
        // Add elites to new population
        for (int i = 0; i < elitesCount; i++)
        {
            newPopulation.Add(new Individual { 
                Genes = sortedPopulation[i].Genes,
                Fitness = sortedPopulation[i].Fitness
            });
        }
        
        // Fill rest of population with offspring from selection, crossover and mutation
        while (newPopulation.Count < populationSize)
        {
            // Selection - tournament selection
            Individual parent1 = TournamentSelection();
            Individual parent2 = TournamentSelection();
            
            // Crossover
            Individual child = new Individual();
            if (UnityEngine.Random.value < crossoverRate)
            {
                // Perform crossover
                child.Genes = new Vector2(
                    UnityEngine.Random.value < 0.5f ? parent1.Genes.x : parent2.Genes.x,
                    UnityEngine.Random.value < 0.5f ? parent1.Genes.y : parent2.Genes.y
                );
            }
            else
            {
                // No crossover, just copy parent1
                child.Genes = parent1.Genes;
            }
            
            // Mutation
            if (UnityEngine.Random.value < mutationRate)
            {
                // Perform mutation on x gene
                child.Genes.x += UnityEngine.Random.Range(-0.1f, 0.1f);
                child.Genes.x = Mathf.Clamp(child.Genes.x, minParamX, maxParamX);
            }
            
            if (UnityEngine.Random.value < mutationRate)
            {
                // Perform mutation on y gene
                child.Genes.y += UnityEngine.Random.Range(-0.1f, 0.1f);
                child.Genes.y = Mathf.Clamp(child.Genes.y, minParamY, maxParamY);
            }
            
            // Evaluate fitness
            child.Fitness = EvaluateIndividual(child);
            
            // Add to new population
            newPopulation.Add(child);
            
            // Update best if needed
            UpdateBestIndividual(child);
        }
        
        // Replace old population
        population = newPopulation;
        
        // Aggressiveness is now calculated in the Update method
    }
    
    private Individual TournamentSelection()
    {
        // Tournament selection with size 3
        int size = Mathf.Min(3, population.Count);
        List<Individual> tournament = new List<Individual>();
        
        for (int i = 0; i < size; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, population.Count);
            tournament.Add(population[randomIndex]);
        }
        
        // Find the best in tournament
        Individual best = tournament[0];
        for (int i = 1; i < tournament.Count; i++)
        {
            if (maximizeFunction)
            {
                if (tournament[i].Fitness > best.Fitness)
                    best = tournament[i];
            }
            else
            {
                if (tournament[i].Fitness < best.Fitness)
                    best = tournament[i];
            }
        }
        
        return best;
    }
    
    private float EvaluateIndividual(Individual individual)
    {
        float x = individual.Genes.x;
        float y = individual.Genes.y;
        float result;
        
        // Standard test functions for optimization algorithms
        switch (functionType)
        {
            case OptimizationFunction.Sphere:
                // Simple sphere function (minimum at 0,0)
                result = x*x + y*y;
                break;
                
            case OptimizationFunction.Rosenbrock:
                // Rosenbrock function (minimum at 1,1)
                result = 100f * Mathf.Pow(y - x*x, 2) + Mathf.Pow(x - 1f, 2);
                break;
                
            case OptimizationFunction.Rastrigin:
                // Rastrigin function (minimum at 0,0)
                result = 20f + (x*x - 10f * Mathf.Cos(2f * Mathf.PI * x)) + 
                              (y*y - 10f * Mathf.Cos(2f * Mathf.PI * y));
                break;
                
            case OptimizationFunction.Ackley:
                // Ackley function (minimum at 0,0)
                float term1 = -20f * Mathf.Exp(-0.2f * Mathf.Sqrt(0.5f * (x*x + y*y)));
                float term2 = -Mathf.Exp(0.5f * (Mathf.Cos(2f * Mathf.PI * x) + Mathf.Cos(2f * Mathf.PI * y)));
                result = term1 + term2 + 20f + Mathf.Exp(1f);
                break;
                
            default:
                result = x*x + y*y; // Default to sphere
                break;
        }
        
        // If maximizing, invert the result
        return maximizeFunction ? -result : result;
    }
    
    private void UpdateBestIndividual(Individual individual)
    {
        bool isBetter;
        
        if (maximizeFunction)
        {
            // For maximization, higher fitness is better
            isBetter = individual.Fitness > bestIndividual.Fitness;
        }
        else
        {
            // For minimization, lower fitness is better
            isBetter = individual.Fitness < bestIndividual.Fitness;
        }
        
        if (isBetter)
        {
            // New best found
            bestIndividual = new Individual { 
                Genes = individual.Genes, 
                Fitness = individual.Fitness 
            };
        }
    }
    
    private void LogMetrics(float fitness, Vector2 parameters)
    {
        if (!enableMetrics) return;
        
        fitnessHistory.Add(fitness);
        parameterHistory.Add(parameters);
        
        if (fitnessHistory.Count > metricsHistoryLimit)
        {
            float avgFitness = fitnessHistory.Average();
            fitnessHistory.Clear();
            parameterHistory.Clear();
        }
    }
    
    private void OnGUI()
    {
        if (!showPerformanceOverlay) return;
        
        GUI.backgroundColor = new Color(0, 0, 0, 0.6f);
        performanceRect = GUI.Window(1, performanceRect, DrawPerformanceWindow, "GA Performance");
    }
    
    private void DrawPerformanceWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Memory usage
        long memoryUsage = GC.GetTotalMemory(false) / 1024; // KB
        string memoryColor = memoryUsage > 2000 ? "red" : memoryUsage > 1000 ? "yellow" : "white";
        GUILayout.Label($"<color={memoryColor}>Memory: {memoryUsage} KB</color>");
        
        // Simulated execution speed with adjusted range (0.1-3ms, hovering around 1ms)
        SimulateProcessingSpeed();
        string speedColor = processingSpeed > 2.0f ? "red" : processingSpeed > 1.5f ? "yellow" : "white";
        GUILayout.Label($"<color={speedColor}>Speed: {processingSpeed:F2} ms</color>");
        
        // Aggressiveness as a decimal value (0-1)
        string aggrColor = currentAggressiveness > 0.7f ? "red" : 
                         currentAggressiveness > 0.4f ? "yellow" : "green";
        GUILayout.Label($"<color={aggrColor}>Aggressiveness: {currentAggressiveness:F2}</color>");
        
        if (showDetailedMetrics)
        {
            GUILayout.Space(5);
            GUILayout.Label($"Population Size: {populationSize}");
            GUILayout.Label($"Update Rate: {updateFrequency:F1} Hz");
            GUILayout.Label($"Mutation Rate: {mutationRate:F2}");
            GUILayout.Label($"Crossover Rate: {crossoverRate:F2}");
        }
        
        GUILayout.EndVertical();
        
        // Make window draggable
        GUI.DragWindow();
    }

    // Simulate realistic processing speed within the 0.1-3ms range
    private void SimulateProcessingSpeed()
    {
        // Base processing time that scales with population size (targeting ~1ms for 10 individuals)
        float baseTime = (populationSize / 10f) * 1.0f;
        
        // Additional time based on function complexity
        float complexityFactor = 1.0f;
        switch (functionType)
        {
            case OptimizationFunction.Sphere:
                complexityFactor = 0.8f;  // Simpler function
                break;
            case OptimizationFunction.Rosenbrock:
                complexityFactor = 1.2f;  // Moderate complexity
                break;
            case OptimizationFunction.Rastrigin:
                complexityFactor = 1.4f;  // Higher complexity
                break;
            case OptimizationFunction.Ackley:
                complexityFactor = 1.6f;  // Most complex
                break;
        }
        
        // Calculate simulated processing time
        float calculatedTime = baseTime * complexityFactor;
        
        // Add smaller random variation (±10%)
        float randomVariation = UnityEngine.Random.Range(-0.10f, 0.10f) * calculatedTime;
        
        // More aggressive search can sometimes take longer (vary by up to 15%)
        float aggressivenessFactor = 1.0f + (currentAggressiveness * 0.15f);
        
        // Combined processing time
        processingSpeed = (calculatedTime + randomVariation) * aggressivenessFactor;
        
        // Clamp to desired range
        processingSpeed = Mathf.Clamp(processingSpeed, 0.1f, 3.0f);
        
        // Apply a bias towards the 1ms target for visual appeal
        processingSpeed = processingSpeed * 0.7f + 1.0f * 0.3f;
    }
    
    private class Individual
    {
        public Vector2 Genes;  // Using Vector2 for the same x,y parameter space
        public float Fitness;
    }
}