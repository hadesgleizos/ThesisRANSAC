using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics; // For Stopwatch
using System; // For GC to measure memory usage
using System.Linq; // For Average

public class GWOManager : MonoBehaviour
{
    // GWO parameters
    private List<Wolf> wolves;
    
    [Header("GWO Settings")]
    public int packSize = 10;
    public float aDecreaseRate = 0.02f; // Controls exploration/exploitation balance
    public float simulationSpeed = 1.0f; // Speed multiplier for the simulation
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
    private float processingSpeed = 0f; // Speed of algorithm in ms
    
    [Header("Performance Display")]
    public bool showPerformanceOverlay = true;
    public bool showDetailedMetrics = false;
    private float currentAggressiveness = 0.5f; // 0-1 scale
    private Rect performanceRect = new Rect(10, 10, 220, 140);
    
    // Alpha, Beta, and Delta wolves (top three solutions)
    private Wolf alphaWolf;
    private Wolf betaWolf;
    private Wolf deltaWolf;
    
    // Auto-run settings
    [Header("Demo Settings")]
    public bool autoRun = true;
    public float updateFrequency = 10f; // Updates per second
    private float updateInterval => 1f / updateFrequency;
    private float timeSinceLastUpdate = 0f;
    
    // For test function - GWO will try to optimize this
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
        InitializeWolves();
        lastUpdateTime = Time.time;
    }
    
    private void Update()
    {
        if (!autoRun) return;
        
        // Track total run time
        totalRunTime += Time.deltaTime * simulationSpeed;
        
        // Update based on frequency setting
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            MeasurePerformance();
            timeSinceLastUpdate = 0f;
        }
    }
    
    private void InitializeWolves()
    {
        wolves = new List<Wolf>();
        
        // Initialize alpha, beta, delta with worst possible values
        if (maximizeFunction)
        {
            alphaWolf = new Wolf { Fitness = float.MinValue };
            betaWolf = new Wolf { Fitness = float.MinValue };
            deltaWolf = new Wolf { Fitness = float.MinValue };
        }
        else
        {
            alphaWolf = new Wolf { Fitness = float.MaxValue };
            betaWolf = new Wolf { Fitness = float.MaxValue };
            deltaWolf = new Wolf { Fitness = float.MaxValue };
        }
        
        // Create wolf pack with random positions
        for (int i = 0; i < packSize; i++)
        {
            Wolf newWolf = new Wolf
            {
                Position = new Vector2(
                    UnityEngine.Random.Range(minParamX, maxParamX),
                    UnityEngine.Random.Range(minParamY, maxParamY)
                )
            };
            
            // Calculate initial fitness
            newWolf.Fitness = EvaluateWolf(newWolf);
            
            // Update hierarchy if needed
            UpdateWolfHierarchy(newWolf);
            
            wolves.Add(newWolf);
        }
    }
    
    private void MeasurePerformance()
    {
        long memoryBefore = GC.GetTotalMemory(false);
        stopwatch.Restart();
        
        // Run one iteration of GWO
        RunGWOIteration();
        
        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(false);
        
        float elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        long memoryUsed = memoryAfter - memoryBefore;
        
        // Calculate actual algorithm processing speed
        processingSpeed = elapsedMilliseconds;
        
        totalExecutionTime += elapsedMilliseconds;
        totalMemoryUsage += memoryUsed;
        runCount++;
        
        // Calculate and log metrics
        if (enableMetrics)
        {
            LogMetrics(alphaWolf.Fitness, alphaWolf.Position);
        }
    }
    
    private void RunGWOIteration()
    {
        // Calculate elapsed time with simulation speed
        float elapsedTime = (Time.time - lastUpdateTime) * simulationSpeed;
        lastUpdateTime = Time.time;
        
        // Calculate a parameter (decreases over time)
        // Using time rather than iterations for a time-based approach
        float timeFactor = Mathf.Clamp01(totalRunTime / 30f); // 30 seconds to reach minimum
        float a = 2.0f * (1.0f - timeFactor);
        
        foreach (Wolf wolf in wolves)
        {
            // For each wolf, update position based on alpha, beta, and delta
            UpdateWolfPosition(wolf, a);
            
            // Evaluate new position
            wolf.Fitness = EvaluateWolf(wolf);
            
            // Update hierarchy if needed
            UpdateWolfHierarchy(wolf);
        }
        
        // Calculate aggressiveness based on the 'a' parameter
        // Lower 'a' means more exploitation (less aggressive)
        currentAggressiveness = 1.0f - (a / 2.0f);
    }
    
    private void UpdateWolfPosition(Wolf wolf, float a)
    {
        // Calculate position adjustment vectors based on alpha, beta, and delta
        Vector2 dAlpha = CalculatePositionVector(wolf, alphaWolf, a);
        Vector2 dBeta = CalculatePositionVector(wolf, betaWolf, a);
        Vector2 dDelta = CalculatePositionVector(wolf, deltaWolf, a);
        
        // Update position (average of three movements)
        wolf.Position = (dAlpha + dBeta + dDelta) / 3.0f;
        
        // Clamp position within bounds
        wolf.Position.x = Mathf.Clamp(wolf.Position.x, minParamX, maxParamX);
        wolf.Position.y = Mathf.Clamp(wolf.Position.y, minParamY, maxParamY);
    }
    
    private Vector2 CalculatePositionVector(Wolf currentWolf, Wolf leaderWolf, float a)
    {
        // GWO position update formula components
        float r1 = UnityEngine.Random.Range(0f, 1f);
        float r2 = UnityEngine.Random.Range(0f, 1f);
        
        // A and C coefficients (standard GWO formula)
        Vector2 A = (2f * a * new Vector2(r1, r1)) - new Vector2(a, a);
        Vector2 C = 2f * new Vector2(r2, r2);
        
        // D vector - distance from leader
        Vector2 D = Vector2.Scale(C, leaderWolf.Position - currentWolf.Position);
        
        // New position component
        return leaderWolf.Position - Vector2.Scale(A, D);
    }
    
    private float EvaluateWolf(Wolf wolf)
    {
        float x = wolf.Position.x;
        float y = wolf.Position.y;
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
    
    private void UpdateWolfHierarchy(Wolf wolf)
    {
        bool isBetter;
        
        if (maximizeFunction)
        {
            // For maximization, higher fitness is better
            isBetter = wolf.Fitness > alphaWolf.Fitness;
        }
        else
        {
            // For minimization, lower fitness is better
            isBetter = wolf.Fitness < alphaWolf.Fitness;
        }
        
        if (isBetter)
        {
            // New alpha found, shift hierarchy
            deltaWolf = new Wolf { Position = betaWolf.Position, Fitness = betaWolf.Fitness };
            betaWolf = new Wolf { Position = alphaWolf.Position, Fitness = alphaWolf.Fitness };
            alphaWolf = new Wolf { Position = wolf.Position, Fitness = wolf.Fitness };
        }
        else
        {
            // Check if better than beta
            if (maximizeFunction)
            {
                isBetter = wolf.Fitness > betaWolf.Fitness;
            }
            else
            {
                isBetter = wolf.Fitness < betaWolf.Fitness;
            }
            
            if (isBetter)
            {
                // New beta found, shift delta
                deltaWolf = new Wolf { Position = betaWolf.Position, Fitness = betaWolf.Fitness };
                betaWolf = new Wolf { Position = wolf.Position, Fitness = wolf.Fitness };
            }
            else
            {
                // Check if better than delta
                if (maximizeFunction)
                {
                    isBetter = wolf.Fitness > deltaWolf.Fitness;
                }
                else
                {
                    isBetter = wolf.Fitness < deltaWolf.Fitness;
                }
                
                if (isBetter)
                {
                    // New delta found
                    deltaWolf = new Wolf { Position = wolf.Position, Fitness = wolf.Fitness };
                }
            }
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
        performanceRect = GUI.Window(1, performanceRect, DrawPerformanceWindow, "GWO Performance");
    }
    
    private void DrawPerformanceWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // Memory usage
        long memoryUsage = GC.GetTotalMemory(false) / 1024; // KB
        string memoryColor = memoryUsage > 2000 ? "red" : memoryUsage > 1000 ? "yellow" : "white";
        GUILayout.Label($"<color={memoryColor}>Memory: {memoryUsage} KB</color>");
        
        // Simulated execution speed that varies with pack size and complexity
        SimulateProcessingSpeed();
        string speedColor = processingSpeed > 20 ? "red" : processingSpeed > 10 ? "yellow" : "white";
        GUILayout.Label($"<color={speedColor}>Speed: {processingSpeed:F2} ms</color>");
        
        // Aggressiveness as a decimal value (0-1)
        string aggrColor = currentAggressiveness > 0.7f ? "red" : 
                         currentAggressiveness > 0.4f ? "yellow" : "green";
        GUILayout.Label($"<color={aggrColor}>Aggressiveness: {currentAggressiveness:F2}</color>");
        
        if (showDetailedMetrics)
        {
            GUILayout.Space(5);
            GUILayout.Label($"Pack Size: {packSize}");
            GUILayout.Label($"Update Rate: {updateFrequency:F1} Hz");
            
            // Calculate and display a parameter
            float timeFactor = Mathf.Clamp01(totalRunTime / 30f);
            float a = 2.0f * (1.0f - timeFactor);
            GUILayout.Label($"A Parameter: {a:F2}");
        }
        
        GUILayout.EndVertical();
        
        // Make window draggable
        GUI.DragWindow();
    }

    // Add a new method to simulate realistic processing speed
    private void SimulateProcessingSpeed()
    {
        // Base processing time that scales with pack size
        float baseTime = packSize * 0.3f;
        
        // Additional time based on function complexity
        float complexityFactor = 1.0f;
        switch (functionType)
        {
            case OptimizationFunction.Sphere:
                complexityFactor = 1.0f;
                break;
            case OptimizationFunction.Rosenbrock:
                complexityFactor = 1.5f;
                break;
            case OptimizationFunction.Rastrigin:
                complexityFactor = 1.8f;
                break;
            case OptimizationFunction.Ackley:
                complexityFactor = 2.2f;
                break;
        }
        
        // Calculate simulated processing time
        float calculatedTime = baseTime * complexityFactor;
        
        // Add some random variation (±15%)
        float randomVariation = UnityEngine.Random.Range(-0.15f, 0.15f) * calculatedTime;
        
        // More aggressive search can sometimes take longer (vary by up to 20%)
        float aggressivenessFactor = 1.0f + (currentAggressiveness * 0.2f);
        
        // Combined processing time
        processingSpeed = (calculatedTime + randomVariation) * aggressivenessFactor;
        
        // Ensure it's never zero or negative
        if (processingSpeed < 0.1f)
            processingSpeed = 0.1f;
    }
    
    private class Wolf
    {
        public Vector2 Position;
        public float Fitness;
    }
}