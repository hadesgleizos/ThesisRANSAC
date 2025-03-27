using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSODisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showOnStart = false;
    public KeyCode toggleKey = KeyCode.F1;
    public Vector2 windowPosition = new Vector2(20, 20); // Top left corner by default
    public Vector2 windowSize = new Vector2(400, 200);
    public float backgroundAlpha = 0.7f;
    
    [Header("References")]
    public PSOManager psoManager;
    
    [Header("Difficulty Thresholds")]
    public float easySpawnRateThreshold = 0.65f;  // Below this is considered easy
    public float hardSpawnRateThreshold = 0.85f;  // Above this is considered hard
    public float easySpeedThreshold = 0.5f;       // Below this is considered easy
    public float hardSpeedThreshold = 0.8f;       // Above this is considered hard
    
    // Display state
    private bool isDisplayVisible;
    private GUIStyle textStyle;
    private GUIStyle backgroundStyle;
    private GUIStyle difficultyEasyStyle;
    private GUIStyle difficultyNormalStyle;
    private GUIStyle difficultyHardStyle;
    
    // PSO Data
    private int spawnerCount;
    private float expectedKillRate;
    private float actualKillRate;
    private float performanceRatio;
    private float spawnRate;
    private float speed;
    private bool playerStruggling;
    private float fitness;
    
    // Manager Parameter Ranges
    private float minSpawnRate;
    private float maxSpawnRate;
    private float minSpeed;
    private float maxSpeed;
    
    void Start()
    {
        isDisplayVisible = showOnStart;
        
        // Find PSOManager if not assigned
        if (psoManager == null)
        {
            psoManager = FindObjectOfType<PSOManager>();
            if (psoManager == null)
            {
                Debug.LogError("PSODisplay: No PSOManager found in scene!");
            }
        }
        
        // Get parameter ranges from PSOManager
        if (psoManager != null)
        {
            minSpawnRate = psoManager.minSpawnRate;
            maxSpawnRate = psoManager.maxSpawnRate;
            minSpeed = psoManager.minSpeed;
            maxSpeed = psoManager.maxSpeed;
            
            // Update thresholds based on actual ranges
            UpdateThresholdsFromRanges();
        }
        
        // Initialize styles in Start so they're ready when needed
        InitializeStyles();
    }
    
    void Update()
    {
        // Toggle visibility with F1 key
        if (Input.GetKeyDown(toggleKey))
        {
            isDisplayVisible = !isDisplayVisible;
        }
    }
    
    // Update thresholds based on actual min/max values from PSOManager
    void UpdateThresholdsFromRanges()
    {
        // Calculate thresholds as percentages of the available range
        float spawnRateRange = maxSpawnRate - minSpawnRate;
        float speedRange = maxSpeed - minSpeed;
        
        // Easy: bottom 30% of the range
        easySpawnRateThreshold = minSpawnRate + spawnRateRange * 0.3f;
        easySpeedThreshold = minSpeed + speedRange * 0.3f;
        
        // Hard: top 30% of the range
        hardSpawnRateThreshold = maxSpawnRate - spawnRateRange * 0.3f;
        hardSpeedThreshold = maxSpeed - speedRange * 0.3f;
    }
    
    void InitializeStyles()
    {
        // Style for text
        textStyle = new GUIStyle();
        textStyle.normal.textColor = Color.white;
        textStyle.fontSize = 12;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.wordWrap = true;
        
        // Style for background
        backgroundStyle = new GUIStyle();
        backgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.2f, backgroundAlpha));
        
        // Difficulty styles
        difficultyEasyStyle = new GUIStyle();
        difficultyEasyStyle.normal.textColor = Color.green;
        difficultyEasyStyle.fontSize = 14;
        difficultyEasyStyle.fontStyle = FontStyle.Bold;
        
        difficultyNormalStyle = new GUIStyle();
        difficultyNormalStyle.normal.textColor = Color.yellow;
        difficultyNormalStyle.fontSize = 14;
        difficultyNormalStyle.fontStyle = FontStyle.Bold;
        
        difficultyHardStyle = new GUIStyle();
        difficultyHardStyle.normal.textColor = Color.red;
        difficultyHardStyle.fontSize = 14;
        difficultyHardStyle.fontStyle = FontStyle.Bold;
    }
    
    void OnGUI()
    {
        if (!isDisplayVisible) return;
        
        // Draw background
        GUI.Box(new Rect(windowPosition.x, windowPosition.y, windowSize.x, windowSize.y), "", backgroundStyle);
        
        // Draw title
        GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + 10, windowSize.x - 20, 20), 
            "PSO Evaluation Data", textStyle);
        
        // Draw data
        float yOffset = 35;
        float lineHeight = 20;
        
        // Get latest data if PSOManager exists
        if (psoManager != null)
        {
            // Determine difficulty based on current parameters
            string difficultyText = GetDifficultyText();
            GUIStyle difficultyStyle = GetDifficultyStyle();
            
            // Display difficulty indicator
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Difficulty: {difficultyText}", difficultyStyle);
            yOffset += lineHeight + 5; // Add a little extra space
            
            // Display parameter ranges for context
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Spawn Rate Range: {minSpawnRate:F2} - {maxSpawnRate:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Speed Range: {minSpeed:F2} - {maxSpeed:F2}", textStyle);
            yOffset += lineHeight + 5; // Add extra space
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Spawners: {spawnerCount}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Expected Kill Rate: {expectedKillRate:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Actual Kill Rate: {actualKillRate:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Performance Ratio: {performanceRatio:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Current Spawn Rate: {spawnRate:F2} ({GetSpawnRatePercentage():P0})", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Current Speed: {speed:F2} ({GetSpeedPercentage():P0})", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Struggling: {playerStruggling}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                $"Fitness: {fitness:F2}", textStyle);
        }
        else
        {
            GUI.Label(new Rect(windowPosition.x + 10, windowPosition.y + yOffset, windowSize.x - 20, lineHeight),
                "PSOManager not found. Data unavailable.", textStyle);
        }
    }
    
    // Calculate spawn rate as percentage of total range
    private float GetSpawnRatePercentage()
    {
        return (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate);
    }
    
    // Calculate speed as percentage of total range
    private float GetSpeedPercentage()
    {
        return (speed - minSpeed) / (maxSpeed - minSpeed);
    }
    
    // Method to create a texture for the background
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    // Public method to update the display data
    // This should be called from PSOManager during evaluation
    public void UpdateDisplayData(int spawners, float expectedKR, float actualKR, 
                                 float perfRatio, float sRate, float zombieSpeed, 
                                 bool struggling, float fitScore)
    {
        spawnerCount = spawners;
        expectedKillRate = expectedKR;
        actualKillRate = actualKR;
        performanceRatio = perfRatio;
        spawnRate = sRate;
        speed = zombieSpeed;
        playerStruggling = struggling;
        fitness = fitScore;
    }
    
    // Determine current difficulty level based on spawn rate and speed
    private string GetDifficultyText()
    {
        // Calculate a combined difficulty score based on spawn rate and speed
        float spawnRateScore = 0f;
        float speedScore = 0f;
        
        // Evaluate spawn rate difficulty using normalized values
        float spawnRateNormalized = (spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate);
        if (spawnRateNormalized < 0.3f)
            spawnRateScore = 0f; // Easy
        else if (spawnRateNormalized > 0.7f)
            spawnRateScore = 2f; // Hard
        else
            spawnRateScore = 1f; // Normal
            
        // Evaluate speed difficulty using normalized values
        float speedNormalized = (speed - minSpeed) / (maxSpeed - minSpeed);
        if (speedNormalized < 0.3f)
            speedScore = 0f; // Easy
        else if (speedNormalized > 0.7f)
            speedScore = 2f; // Hard
        else
            speedScore = 1f; // Normal
            
        // Combined difficulty score
        float combinedScore = (spawnRateScore + speedScore) / 2f;
        
        // Apply any struggling modifier
        if (playerStruggling)
            combinedScore = Mathf.Min(combinedScore + 0.5f, 2f); // Make it feel harder when struggling
        
        // Convert to text
        if (combinedScore < 0.5f)
            return "EASY";
        else if (combinedScore < 1.5f)
            return "NORMAL";
        else
            return "HARD";
    }
    
    // Get the appropriate style for the current difficulty
    private GUIStyle GetDifficultyStyle()
    {
        string difficulty = GetDifficultyText();
        
        switch (difficulty)
        {
            case "EASY":
                return difficultyEasyStyle;
            case "NORMAL":
                return difficultyNormalStyle;
            case "HARD":
                return difficultyHardStyle;
            default:
                return textStyle;
        }
    }
}
