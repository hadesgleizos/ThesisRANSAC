using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSODisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showOnStart = false;
    public KeyCode toggleKey = KeyCode.F1;
    
    // Add auto-scaling options
    [Header("UI Scaling")]
    public bool autoScale = true;
    [Tooltip("Reference resolution the UI was designed for")]
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    [Range(0.5f, 3.0f)]
    public float uiScale = 1.0f;
    [Range(0.5f, 3.0f)]
    public float minScale = 0.75f;
    [Range(0.5f, 3.0f)]
    public float maxScale = 2.0f;
    
    // Original position/size fields (these will be modified by auto-scaling)
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

    // Add this for key detection debugging
    private bool wasKeyPressed = false;

    // Add this to track the last toggle time to prevent double-toggling
    private float lastToggleTime = 0f;
    private float toggleCooldown = 0.5f; // Minimum seconds between toggles

    // Add these new fields to refresh the display periodically
    private float displayRefreshInterval = 0.5f;
    private float lastRefreshTime = 0f;
    
    // Add this for storing the calculated scale
    private float calculatedScale = 1.0f;
    
    void Start()
    {
        isDisplayVisible = showOnStart;
        
        // Find PSOManager if not assigned
        if (psoManager == null)
        {
            psoManager = FindObjectOfType<PSOManager>();
            if (psoManager == null)
            {
                //Debug.LogError("PSODisplay: No PSOManager found in scene!");
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
        
        // Calculate initial scale based on screen resolution
        CalculateUIScale();
        
        // Initialize styles in Start so they're ready when needed
        InitializeStyles();
        
        //Debug.Log($"PSODisplay initialized. Toggle with {toggleKey}. Initial visibility: {isDisplayVisible}");
    }
    
    void Update()
    {
        // Recalculate scale if needed
        CalculateUIScale();
        
        // Check if we're allowed to toggle based on cooldown
        bool canToggle = (Time.unscaledTime - lastToggleTime) > toggleCooldown;
        
        if (canToggle && Input.GetKeyDown(toggleKey))
        {
            ToggleVisibility();
            lastToggleTime = Time.unscaledTime;
            wasKeyPressed = true; // Set wasKeyPressed to prevent OnGUI from toggling again
        }
        
        // Only refresh displayed values when display is visible
        // This saves performance by not calculating values when they're not being shown
        if (isDisplayVisible && psoManager != null && psoManager.spawner != null)
        {
            if (Time.unscaledTime - lastRefreshTime > displayRefreshInterval)
            {
                // Get current values directly from the spawner
                spawnRate = psoManager.spawner.GetCurrentSpawnRate();
                speed = psoManager.spawner.GetCurrentZombieSpeed();
                
                // The rest of the data will be updated through UpdateDisplayData
                lastRefreshTime = Time.unscaledTime;
            }
        }
    }
    
    // Add this method to calculate UI scale based on screen resolution
    private void CalculateUIScale()
    {
        if (autoScale)
        {
            // Calculate scale based on current resolution vs reference resolution
            float widthRatio = Screen.width / referenceResolution.x;
            float heightRatio = Screen.height / referenceResolution.y;
            
            // Use the smaller ratio to ensure UI fits on screen
            float baseScale = Mathf.Min(widthRatio, heightRatio);
            
            // Clamp the scale between min and max values
            calculatedScale = Mathf.Clamp(baseScale, minScale, maxScale);
        }
        else
        {
            calculatedScale = uiScale; // Use manual scale if auto-scale is disabled
        }
        
        // Update font sizes based on scale
        UpdateFontSizes();
    }
    
    // Update font sizes based on calculated scale
    private void UpdateFontSizes()
    {
        if (textStyle != null)
        {
            textStyle.fontSize = Mathf.RoundToInt(12 * calculatedScale);
            difficultyEasyStyle.fontSize = Mathf.RoundToInt(14 * calculatedScale);
            difficultyNormalStyle.fontSize = Mathf.RoundToInt(14 * calculatedScale);
            difficultyHardStyle.fontSize = Mathf.RoundToInt(14 * calculatedScale);
        }
    }
    
    // Modify the OnGUI method to use scaled positions and sizes
    void OnGUI()
    {
        // Skip all GUI rendering when not visible to save performance
        if (!isDisplayVisible) return;
        
        // Only check for key press in OnGUI if it wasn't already handled in Update
        Event e = Event.current;
        bool canToggle = (Time.unscaledTime - lastToggleTime) > toggleCooldown;
        
        if (canToggle && e.type == EventType.KeyDown && e.keyCode == toggleKey && !wasKeyPressed)
        {
            ToggleVisibility();
            lastToggleTime = Time.unscaledTime;
            wasKeyPressed = true;
            e.Use(); // Mark event as used
        }
        
        if (e.type == EventType.KeyUp && e.keyCode == toggleKey)
        {
            wasKeyPressed = false;
        }
        
        if (!isDisplayVisible) return;
        
        // Calculate scaled window dimensions
        float scaledWidth = windowSize.x * calculatedScale;
        float scaledHeight = windowSize.y * calculatedScale;
        
        // Position can be adjusted based on screen size if needed
        float posX = windowPosition.x;
        float posY = windowPosition.y;
        
        // Draw background with scaled dimensions
        GUI.Box(new Rect(posX, posY, scaledWidth, scaledHeight), "", backgroundStyle);
        
        // Calculate scaled padding for content
        float padding = 10 * calculatedScale;
        float lineHeight = 20 * calculatedScale;
        
        // Draw title
        GUI.Label(new Rect(posX + padding, posY + padding, scaledWidth - (padding * 2), lineHeight), 
            "PSO Evaluation Data", textStyle);
        
        // Draw data
        float yOffset = padding + lineHeight + padding/2;
        
        // Get latest data if PSOManager exists
        if (psoManager != null)
        {
            // Determine difficulty based on current parameters
            string difficultyText = GetDifficultyText();
            GUIStyle difficultyStyle = GetDifficultyStyle();
            
            // Display difficulty indicator
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Difficulty: {difficultyText}", difficultyStyle);
            yOffset += lineHeight + (5 * calculatedScale); // Add a little extra space
            
            // Display parameter ranges for context
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Spawn Rate Range: {minSpawnRate:F2} - {maxSpawnRate:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Speed Range: {minSpeed:F2} - {maxSpeed:F2}", textStyle);
            yOffset += lineHeight + (5 * calculatedScale); // Add extra space
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Spawners: {spawnerCount}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Expected Kill Rate: {expectedKillRate:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Actual Kill Rate: {actualKillRate:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Performance Ratio: {performanceRatio:F2}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Current Spawn Rate: {spawnRate:F2} ({GetSpawnRatePercentage():P0})", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Current Speed: {speed:F2} ({GetSpeedPercentage():P0})", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Struggling: {playerStruggling}", textStyle);
            yOffset += lineHeight;
            
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                $"Fitness: {fitness:F2}", textStyle);
        }
        else
        {
            GUI.Label(new Rect(posX + padding, posY + yOffset, scaledWidth - (padding * 2), lineHeight),
                "PSOManager not found. Data unavailable.", textStyle);
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
        textStyle.fontSize = Mathf.RoundToInt(12 * calculatedScale);
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.wordWrap = true;
        
        // Style for background
        backgroundStyle = new GUIStyle();
        backgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.2f, backgroundAlpha));
        
        // Difficulty styles
        difficultyEasyStyle = new GUIStyle();
        difficultyEasyStyle.normal.textColor = Color.green;
        difficultyEasyStyle.fontSize = Mathf.RoundToInt(14 * calculatedScale);
        difficultyEasyStyle.fontStyle = FontStyle.Bold;
        
        difficultyNormalStyle = new GUIStyle();
        difficultyNormalStyle.normal.textColor = Color.yellow;
        difficultyNormalStyle.fontSize = Mathf.RoundToInt(14 * calculatedScale);
        difficultyNormalStyle.fontStyle = FontStyle.Bold;
        
        difficultyHardStyle = new GUIStyle();
        difficultyHardStyle.normal.textColor = Color.red;
        difficultyHardStyle.fontSize = Mathf.RoundToInt(14 * calculatedScale);
        difficultyHardStyle.fontStyle = FontStyle.Bold;
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
        // Only update data if the display is visible to save performance
        if (!isDisplayVisible)
            return;
            
        spawnerCount = spawners;
        expectedKillRate = expectedKR;
        actualKillRate = actualKR;
        performanceRatio = perfRatio;
        // Override these values with the direct ones from spawner
        if (psoManager != null && psoManager.spawner != null)
        {
            spawnRate = psoManager.spawner.GetCurrentSpawnRate();
            speed = psoManager.spawner.GetCurrentZombieSpeed();
        }
        else
        {
            // Fallback to provided values if spawner not available
            spawnRate = sRate;
            speed = zombieSpeed;
        }
        playerStruggling = struggling;
        fitness = fitScore;
    }
    
    // Determine current difficulty level based on spawn rate and speed
    private string GetDifficultyText()
    {
        // Calculate a weighted difficulty score based on spawn rate and speed
        float spawnRateScore = 0f;
        float speedScore = 0f;
        
        // Evaluate spawn rate difficulty using normalized values (0 to 1)
        float spawnRateNormalized = Mathf.Clamp01((spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate));
        
        // Evaluate speed difficulty using normalized values (0 to 1)
        float speedNormalized = Mathf.Clamp01((speed - minSpeed) / (maxSpeed - minSpeed));
        
        // Calculate a single combined normalized value (0 to 1)
        // Weight speed higher since it has more impact on gameplay difficulty
        float weightedSpeed = 0.5f;
        float weightedSpawnRate = 0.5f;
        float combinedNormalized = (speedNormalized * weightedSpeed) + (spawnRateNormalized * weightedSpawnRate);
        
        // Apply struggling modifier - shifts the difficulty up when player is struggling
        if (playerStruggling)
        {
            combinedNormalized = Mathf.Min(combinedNormalized + 0.2f, 1.0f);
        }
        
        // Convert normalized value to difficulty text
        if (combinedNormalized < 0.33f)
            return "EASY";
        else if (combinedNormalized < 0.66f)
            return "NORMAL";
        else
            return "HARD";
    }

    // Add a new method to get a numeric difficulty value for UI elements
    private float GetDifficultyValue()
    {
        float spawnRateNormalized = Mathf.Clamp01((spawnRate - minSpawnRate) / (maxSpawnRate - minSpawnRate));
        float speedNormalized = Mathf.Clamp01((speed - minSpeed) / (maxSpeed - minSpeed));
        
        float weightedSpeed = 0.7f;
        float weightedSpawnRate = 0.3f;
        float combinedNormalized = (speedNormalized * weightedSpeed) + (spawnRateNormalized * weightedSpawnRate);
        
        if (playerStruggling)
        {
            combinedNormalized = Mathf.Min(combinedNormalized + 0.2f, 1.0f);
        }
        
        return combinedNormalized; // Returns 0-1 value
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

    // Method to toggle visibility
    private void ToggleVisibility()
    {
        isDisplayVisible = !isDisplayVisible;
        //Debug.Log($"PSODisplay visibility toggled to: {isDisplayVisible}");
        
        // Force immediate update of thresholds when toggling on
        if (isDisplayVisible && psoManager != null)
        {
            minSpawnRate = psoManager.minSpawnRate;
            maxSpawnRate = psoManager.maxSpawnRate;
            minSpeed = psoManager.minSpeed;
            maxSpeed = psoManager.maxSpeed;
            UpdateThresholdsFromRanges();
            
            // Force immediate refresh of current values from spawner
            if (psoManager.spawner != null)
            {
                spawnRate = psoManager.spawner.GetCurrentSpawnRate();
                speed = psoManager.spawner.GetCurrentZombieSpeed();
            }
        }
    }
}
