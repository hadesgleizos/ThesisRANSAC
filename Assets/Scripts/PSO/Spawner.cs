using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // Import TextMeshPro for UI
using UnityEngine.SceneManagement;  // For restarting the game
using System.Linq; // Import Linq for ToList()
using UnityEngine.Events; // Import for UnityEvent

public enum SpawnEventType
{
    Standard,       // Regular waves with cooldown and no boss
    BossEvent,      // Existing boss fight behavior
    Ambush,         // Quick waves with more enemies
    TimedChallenge, // Time-limited waves with rewards
    Custom          // For specialized events
}

[System.Serializable]
public class ZombieType
{
    public GameObject zombiePrefab;
    public string zombieName;  // For debugging/identification
    [Range(0, 100)]
    public float spawnChance = 10;  // Default 10% spawn chance
}

[System.Serializable]
public class SpawnEvent
{
    public string eventName = "Default Event";
    public SpawnEventType eventType = SpawnEventType.Standard;
    public int eventWaves = 3;
    public float eventWaveDuration = 30f;
    public float eventCooldown = 10f;
    public bool spawnBossAtEnd = false;
    public bool endGameOnCompletion = false;
    public bool startRegularWavesAfterCompletion = false;
    
    [Header("Voicelines")]
    public string startVoicelineId = "";
    public bool useVoicelineSequenceOnComplete = false;
    [Tooltip("Use this for a single voiceline on completion")]
    public string completeVoicelineId = "";
    [Tooltip("Or use this for a sequence of voicelines with actions")]
    public string completeSequenceId = "";
    public string[] waveStartVoicelineIds; // Array for each wave start
    public string[] waveEndVoicelineIds;   // Array for each wave end
    
    public UnityEvent onEventStart;
    public UnityEvent onEventComplete;
}

public class Spawner : MonoBehaviour
{
    public static Spawner Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject ScoreScreen;  // Reference to the Score Screen UI
    public TMP_Text ScorePoints;    // Reference to the Score text
    public TMP_Text WaveText;      // Reference to display current wave
    public TMP_Text TimerText;     // Reference to display timer

    // NEW: Reference to PlayerPerformance
    public PlayerPerformance playerPerformance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Zombie Settings")]
    public List<ZombieType> zombieTypes = new List<ZombieType>();  // Replace zombiePrefab
    public float baseZombieSpeed = 3.0f;     // Add this line - base speed
    [SerializeField] private float currentZombieSpeed;  // Add this line - current speed from PSO
    public float spawnRate = 0.1f;
    public bool debugSpeedChanges = true;

    [Header("Wave Settings")]
    public int totalWaves = 5;               // Total number of waves
    public float waveDuration = 30f;         // Duration of each wave in seconds
    public float cooldownDuration = 10f;     // Cooldown duration between waves

    [Header("Spawn Points")]
    public List<GameObject> spawnPoints = new List<GameObject>(); // List of spawn points

    [Header("Boss Settings")]
    public GameObject bossPrefab;
    public float bossSpawnDelay = 2f;
    private GameObject currentBoss;

    private List<GameObject> activeZombies = new List<GameObject>(); // Track active zombies
    private int currentSpawnIndex = 0;       // Track which spawn point to use next
    private int currentWave = 0;             // Current wave index
    private bool spawning = false;           // Whether the spawner is actively spawning

    // Cumulative metrics
    public int totalKillCount { get; private set; } = 0;      // Total kills across all waves
    public float totalElapsedTime { get; private set; } = 0f; // Total time elapsed across all waves

    // Wave-specific metrics (calculated dynamically)
    private int waveStartKillCount = 0;      // Kill count at the start of the current wave
    private float waveStartTime = 0f;        // Time at the start of the current wave

    // Events to notify other systems (e.g., PSOManager)
    public delegate void WaveEvent(int waveNumber);
    public static event WaveEvent OnWaveStart;
    public static event WaveEvent OnWaveEnd;

    // NEW: Expose these so they're set in Inspector
    [Header("Scene Names")]
public string baseSceneName = "BaseScene";
public string restartSceneName = "Stage 2";  // Used by RestartGame
public string nextStageSceneName = "Stage 3"; // Used by NextStage

    private bool inCooldown = false; // Add this with other private variables
    private float cooldownTimeRemaining = 0f; // Add this with other private variables

    // Add these variables for deterministic spawning
    private float nextSpawnTime = 0f;
    private float spawnInterval = 2f; // Will be calculated from spawnRate

    // NEW: Event system variables
    [Header("Event System")]
    public List<SpawnEvent> configuredEvents = new List<SpawnEvent>();
    private SpawnEvent currentEvent;
    private bool eventActive = false;

    [Header("Spawner Settings")]
public bool startRegularWavesAutomatically = false; // Set this to false in Inspector

    [Header("Voiceline UI References")]
// These references would be set in the inspector
public TMPro.TMP_Text subtitleTextReference;
public GameObject subtitlePanelReference;

    private void Start()
    {
        // Initialize lists, references, and other setup
        
        // Only start regular wave system if configured to do so
        if (startRegularWavesAutomatically)
        {
            StartCoroutine(WaveSystem());
        }

        // Call this method when level starts or restarts
        InitializeVoicelineSystem();
    }

    // Call this method when level starts or restarts
private void InitializeVoicelineSystem()
{
    if (Voicelines.Instance != null)
    {
        // Try to find references if they're not set
        if (subtitleTextReference == null || subtitlePanelReference == null)
        {
            FindVoicelineUIReferences();
        }
        
        // Set up the voiceline references
        if (subtitleTextReference != null && subtitlePanelReference != null)
        {
            Voicelines.Instance.SetAllReferences(
                subtitleTextReference,
                subtitlePanelReference
            );
            Debug.Log("Voiceline references initialized by Spawner");
        }
        else
        {
            Debug.LogWarning("Missing UI references for Voicelines system!");
        }
    }
    else
    {
        Debug.LogWarning("Voicelines instance not found!");
    }
}

    private IEnumerator WaveSystem()
    {
        while (currentWave < totalWaves)
        {
            currentWave++;
            spawning = true;
            inCooldown = false;
            
            if (WaveText != null)
            {
                WaveText.text = $"Wave: {currentWave}/{totalWaves}";
            }

            waveStartKillCount = totalKillCount;
            waveStartTime = totalElapsedTime;

            OnWaveStart?.Invoke(currentWave);
            //Debug.Log($"Wave {currentWave} started!");

            // Only spawn regular zombies if it's not the final wave
            if (currentWave < totalWaves)
            {
                StartCoroutine(SpawnZombies());
                yield return new WaitForSeconds(waveDuration);
                spawning = false;
                
                inCooldown = true;
                cooldownTimeRemaining = cooldownDuration;
                DespawnAllZombies();
                yield return new WaitForSeconds(cooldownDuration);
                inCooldown = false;
            }
            // Final wave - spawn boss
            else
            {
                if (WaveText != null)
                {
                    WaveText.text = "BOSS WAVE";
                }
                
                yield return new WaitForSeconds(bossSpawnDelay);
                SpawnBoss();
                
                // Wait until boss is defeated
                while (currentBoss != null)
                {
                    if (TimerText != null)
                    {
                        TimerText.text = "Defeat the Boss!";
                    }
                    yield return new WaitForSeconds(0.5f);
                }
                spawning = false;
            }

            OnWaveEnd?.Invoke(currentWave);
            //Debug.Log($"Wave {currentWave} ended!");
        }

        //Debug.Log("All waves completed!");
    }

    private void ShowScoreScreen()
    {
        if (ScoreScreen != null)
        {
            ScoreScreen.SetActive(true); // Show the Score Screen
        }

        if (ScorePoints != null)
        {
            // NEW: Show PlayerPerformance score if available
            if (playerPerformance != null)
            {
                ScorePoints.text = $"Score: {playerPerformance.GetScore()}";
            }
            else
            {
                // Fallback to spawner kill count if no PlayerPerformance reference
                ScorePoints.text = $"Score: {totalKillCount}";
            }
        }
        Time.timeScale = 0; // Pause the game
    }

public void RestartGame()
{
    // Reset time scale to normal
    Time.timeScale = 1;
    
    // Reset all game state variables
    ResetGameState();
    
    // Use Unity's LoadScene to create a clean restart
    if (restartSceneName != "Tutorial") 
    {
        // For multi-scene setup (base + stage)
        SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);
        
        // Use a coroutine to load the second scene after the first is loaded
        StartCoroutine(LoadSecondSceneAfterDelay(restartSceneName));
    }
    else 
    {
        // For single scene setup (simpler case)
        SceneManager.LoadScene(restartSceneName, LoadSceneMode.Single);
    }
}

// Helper method to load second scene with a slight delay
private IEnumerator LoadSecondSceneAfterDelay(string sceneName)
{
    // Wait for end of frame to ensure first scene is fully loaded
    yield return new WaitForEndOfFrame();
    
    // Load the second scene additively
    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    
    // Set this scene as active after loading
    yield return null;
    Scene sceneToActivate = SceneManager.GetSceneByName(sceneName);
    if (sceneToActivate.IsValid())
    {
        SceneManager.SetActiveScene(sceneToActivate);
    }
}

// Helper method to reset all game state variables
private void ResetGameState()
{
    // Reset all persistent state variables
    currentWave = 0;
    totalKillCount = 0;
    totalElapsedTime = 0f;
    waveStartKillCount = 0;
    waveStartTime = 0f;
    inCooldown = false;
    spawning = false;
    cooldownTimeRemaining = 0f;
    nextSpawnTime = 0f;
    eventActive = false;
    currentEvent = null;
    
    // Clear lists
    activeZombies.Clear();
    
    // Reset boss state
    if (currentBoss != null)
    {
        Destroy(currentBoss);
        currentBoss = null;
    }
    
    // Reset UI elements
    if (WaveText != null)
    {
        WaveText.text = "";
    }
    
    if (TimerText != null)
    {
        TimerText.text = "";
    }
    
    if (ScoreScreen != null)
    {
        ScoreScreen.SetActive(false);
    }
    
    // Reset any static state in other classes (if applicable)
    // This is important for ensuring a truly clean restart
    
    // Additionally, you might want to reset any static state in other classes
    // For example, if PlayerPerformance has static variables:
    // Reset the spawn rate and zombie speed to initial values
    spawnRate = 0.1f; // Use your default value
    currentZombieSpeed = baseZombieSpeed;
    
    // Reset the Voicelines system
    if (Voicelines.Instance != null)
    {
        Voicelines.Instance.Reset();
    }
    
    Debug.Log("Game state reset for clean restart");
}

public void NextStage()
{
    // Reset time scale to normal
    Time.timeScale = 1;
    
    // Reset all game state variables
    ResetGameState();
    
    // Load the base scene first
    SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);
    
    // Load the next stage scene additively with a slight delay
    StartCoroutine(LoadSecondSceneAfterDelay(nextStageSceneName));
}

// Add this coroutine to wait for scene loading before initializing
private IEnumerator InitializeVoicelinesAfterSceneLoad()
{
    // Wait for scene to be fully loaded - two frames is usually enough
    yield return null;
    yield return null;
    
    // Find UI references again
    FindVoicelineUIReferences();
    
    // Now initialize the voiceline system
    InitializeVoicelineSystem();
}

// Add this method to find references in the newly loaded scene
private void FindVoicelineUIReferences()
{
    // Try to find the subtitle text by tag or name
    if (subtitleTextReference == null)
    {
        GameObject subtitleTextObj = GameObject.FindWithTag("SubtitleText");
        if (subtitleTextObj != null)
        {
            subtitleTextReference = subtitleTextObj.GetComponent<TMPro.TMP_Text>();
        }
        else
        {
            // Try finding by name as fallback
            subtitleTextObj = GameObject.Find("SubtitleText");
            if (subtitleTextObj != null)
            {
                subtitleTextReference = subtitleTextObj.GetComponent<TMPro.TMP_Text>();
            }
        }
    }
    
    // Try to find the subtitle panel by tag or name
    if (subtitlePanelReference == null)
    {
        subtitlePanelReference = GameObject.FindWithTag("SubtitlePanel");
        if (subtitlePanelReference == null)
        {
            // Try finding by name as fallback
            subtitlePanelReference = GameObject.Find("SubtitlePanel");
        }
    }
    
    if (subtitleTextReference == null || subtitlePanelReference == null)
    {
        Debug.LogWarning("Could not find subtitle UI references in the scene!");
    }
}

    private GameObject GetRandomZombiePrefab()
    {
        if (zombieTypes.Count == 0)
        {
            //Debug.LogError("No zombie types defined!");
            return null;
        }

        // Calculate the sum of all spawn chances
        float totalChance = 0;
        foreach (var zombieType in zombieTypes)
        {
            totalChance += zombieType.spawnChance;
        }

        // If all chances are 0, use equal probability
        if (totalChance <= 0)
        {
            int randomIndex = Random.Range(0, zombieTypes.Count);
            return zombieTypes[randomIndex].zombiePrefab;
        }

        // Get a random value between 0 and the total chance
        float randomValue = Random.Range(0, totalChance);
        
        // Find which zombie type this random value corresponds to
        float currentSum = 0;
        foreach (var zombieType in zombieTypes)
        {
            currentSum += zombieType.spawnChance;
            if (randomValue <= currentSum)
            {
                return zombieType.zombiePrefab;
            }
        }

        // Fallback (should never happen unless there's a calculation error)
        return zombieTypes[0].zombiePrefab;
    }

    private IEnumerator SpawnZombies(Transform preferredSpawnPoint = null, List<Transform> customSpawnPoints = null)
    {
        // Initialize nextSpawnTime when spawning starts
        nextSpawnTime = Time.time;
        // Calculate initial spawn interval
        spawnInterval = 1f / spawnRate;
        
        // Get the wave duration for the current context (event or regular)
        float currentWaveDuration = eventActive ? currentEvent.eventWaveDuration : waveDuration;
        float waveEndTime = Time.time + currentWaveDuration;
        
        while (spawning && Time.time < waveEndTime)
        {
            if (spawnRate > 0)
            {
                // Check if it's time to spawn
                if (Time.time >= nextSpawnTime)
                {
                    GameObject zombiePrefab = GetRandomZombiePrefab();
                    if (zombiePrefab != null)
                    {
                        // Determine spawn position
                        Vector3 spawnPosition;
                        
                        // Priority 1: Use custom spawn points if provided
                        if (customSpawnPoints != null && customSpawnPoints.Count > 0)
                        {
                            // Get a random spawn point from the custom points
                            int randomIndex = Random.Range(0, customSpawnPoints.Count);
                            Transform spawnPoint = customSpawnPoints[randomIndex];
                            spawnPosition = spawnPoint.position;
                            
                            // Add a small random offset for variety
                            spawnPosition += new Vector3(
                                Random.Range(-1f, 1f), 
                                0, 
                                Random.Range(-1f, 1f)
                            ) * 1.5f;
                        }
                        // Priority 2: Use preferred spawn point (trigger location)
                        else if (preferredSpawnPoint != null)
                        {
                            // Use the trigger location with a random offset for variety
                            spawnPosition = preferredSpawnPoint.position + Random.insideUnitSphere * 5f;
                            spawnPosition.y = preferredSpawnPoint.position.y; // Keep same Y level
                        }
                        // Priority 3: Use built-in spawn points
                        else if (spawnPoints.Count > 0)
                        {
                            // Use existing spawn points
                            int spawnIndex = currentSpawnIndex % spawnPoints.Count;
                            GameObject spawnPoint = spawnPoints[spawnIndex];
                            currentSpawnIndex++;
                            spawnPosition = spawnPoint.transform.position;
                        }
                        else
                        {
                            // Fallback if no spawn points available
                            Debug.LogWarning("No spawn points available!");
                            yield return null;
                            continue;
                        }

                        // Spawn the zombie at the selected position
                        GameObject spawnedZombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                        
                        // Set zombie speed to the current speed
                        if (spawnedZombie.GetComponent<Zombie>() != null)
                        {
                            spawnedZombie.GetComponent<Zombie>().SetSpeed(currentZombieSpeed);
                        }
                        else if (spawnedZombie.GetComponent<Spitter>() != null)
                        {
                            spawnedZombie.GetComponent<Spitter>().SetSpeed(currentZombieSpeed * 0.9f);
                        }
                        else if (spawnedZombie.GetComponent<Jograt>() != null)
                        {
                            spawnedZombie.GetComponent<Jograt>().SetSpeed(currentZombieSpeed);
                        }
                        else if (spawnedZombie.GetComponent<Bomba>() != null)
                        {
                            spawnedZombie.GetComponent<Bomba>().SetSpeed(currentZombieSpeed);
                        }
                        
                        // Add to the active zombies list
                        activeZombies.Add(spawnedZombie);
                    }
                    
                    // Update next spawn time
                    nextSpawnTime = Time.time + spawnInterval;
                }
            }
            
            yield return null;
        }
    }

    private void Update()
    {
        // Increment cumulative time if currently spawning
        if (spawning)
        {
            totalElapsedTime += Time.deltaTime;
            
            // Update timer display during wave
            if (TimerText != null)
            {
                if (currentWave == totalWaves && currentBoss != null)
                {
                    // Show "Defeat the Boss!" text during boss fight
                    TimerText.text = "Defeat the Boss!";
                }
                else
                {
                    // Show regular timer for normal waves
                    float currentDuration = eventActive ? currentEvent.eventWaveDuration : waveDuration;
                    float timeRemaining = currentDuration - (totalElapsedTime - waveStartTime);
                    int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                    TimerText.text = $"{minutes:00}:{seconds:00}";
                }
            }
        }
        else if (inCooldown)
        {
            // Update cooldown timer
            cooldownTimeRemaining -= Time.deltaTime;
            
            // Check if we're in an event AND we haven't reached the final wave yet
            bool isInEventCooldown = eventActive && currentWave < currentEvent.eventWaves;
            
            // Check if we're in regular waves AND haven't reached the final wave yet
            bool isInRegularCooldown = !eventActive && currentWave < totalWaves;
            
            if (TimerText != null)
            {
                int seconds = Mathf.CeilToInt(cooldownTimeRemaining);
                
                // Only show "Get Ready" if we're in a valid cooldown between waves
                if (isInEventCooldown || isInRegularCooldown)
                {
                    TimerText.text = $"Get Ready in: {seconds}";
                }
                else
                {
                    // Clear the timer if we're not in a valid cooldown
                    TimerText.text = "";
                }
            }
            
            // Update wave text to show next wave ONLY if we're between valid waves
            if (WaveText != null)
            {
                if (isInEventCooldown)
                {
                    WaveText.text = $"Preparing {currentEvent.eventName}: {currentWave + 1}/{currentEvent.eventWaves}";
                }
                else if (isInRegularCooldown)
                {
                    WaveText.text = $"Preparing Wave {currentWave + 1}/{totalWaves}";
                }
            }
        }
    }

    public void IncrementKillCount()
    {
        totalKillCount++;
        //Debug.Log($"Zombie killed! Total kills: {totalKillCount}");
    }

    public int GetWaveKillCount()
    {
        return totalKillCount - waveStartKillCount;
    }

    public float GetWaveElapsedTime()
    {
        return totalElapsedTime - waveStartTime;
    }

    public void SetAllZombieSpeeds(float newSpeed)
    {
        float oldSpeed = currentZombieSpeed;
        currentZombieSpeed = newSpeed;

        var updatedEnemies = 0;
        foreach (GameObject zombie in activeZombies.ToList())
        {
            if (zombie != null)
            {
                // Check for regular Zombie
                var zombieComponent = zombie.GetComponent<Zombie>();
                if (zombieComponent != null)
                {
                    zombieComponent.SetSpeed(currentZombieSpeed);
                    updatedEnemies++;
                }
                
                // Check for Spitter
                var spitterComponent = zombie.GetComponent<Spitter>();
                if (spitterComponent != null)
                {
                    spitterComponent.SetSpeed(currentZombieSpeed * 0.9f); // Keep them slightly slower
                    updatedEnemies++;
                }
                
                // Check for Jograt
                var jogratComponent = zombie.GetComponent<Jograt>();
                if (jogratComponent != null)
                {
                    jogratComponent.SetSpeed(currentZombieSpeed);
                    updatedEnemies++;
                }
                
                // Add check for Bomba
                var bombaComponent = zombie.GetComponent<Bomba>();
                if (bombaComponent != null)
                {
                    bombaComponent.SetSpeed(currentZombieSpeed);
                    updatedEnemies++;
                }
            }
        }

        if (debugSpeedChanges)
        {
            //Debug.Log($"[Spawner] Speed Change - Old: {oldSpeed:F2}, New: {currentZombieSpeed:F2}, Updated {updatedEnemies} enemies");
        }
    }

    public float GetCurrentZombieSpeed()
    {
        return currentZombieSpeed;
    }

    public void RemoveZombie(GameObject zombie)
    {
        if (activeZombies.Contains(zombie))
        {
            activeZombies.Remove(zombie);
        }
    }

    public void UpdateSpawnRate(float newSpawnRate)
    {
        float oldRate = spawnRate;
        spawnRate = newSpawnRate;
        
        // Calculate new interval
        float newInterval = 1f / spawnRate;
        
        // Only update the spawn interval if significantly different (over 10%)
        if (Mathf.Abs(newInterval - spawnInterval) / spawnInterval > 0.1f)
        {
            // Log before change
            //Debug.Log($"Spawn Rate Updated: {spawnRate:F2} (changing interval from {spawnInterval:F2}s to {newInterval:F2}s)");
            
            // Update the spawn interval
            spawnInterval = newInterval;
            
            // Optionally, adjust the next spawn time based on the new interval
            // This makes changes apply more smoothly
            if (nextSpawnTime > Time.time)
            {
                float remainingTimeRatio = (nextSpawnTime - Time.time) / spawnInterval;
                nextSpawnTime = Time.time + (remainingTimeRatio * newInterval);
            }
        }
    }

    public float GetCurrentSpawnRate()
    {
        return spawnRate;
    }

    public int GetActiveSpawnerCount()
    {
        return spawnPoints.Count;
    }

    private void DespawnAllZombies()
    {
        foreach (GameObject zombie in activeZombies.ToList())
        {
            if (zombie != null && zombie != currentBoss)
            {
                Destroy(zombie);
            }
        }
        activeZombies.RemoveAll(z => z != currentBoss);
    }

    private void SpawnBoss()
    {
        if (spawnPoints.Count > 0)
        {
            // Check if SoundManager instance exists before playing music
            if (SoundManager.Instance != null)
            {
                //Debug.Log("Playing boss music...");
                SoundManager.Instance.PlayBossMusic();
            }
            else
            {
                //Debug.LogWarning("SoundManager.Instance is null! Cannot play boss music.");
            }
            
            int randomSpawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnPosition = spawnPoints[randomSpawnIndex].transform.position;
            currentBoss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
            
            Boss1 bossComponent = currentBoss.GetComponent<Boss1>();
            if (bossComponent != null)
            {
                bossComponent.SetSpeed(currentZombieSpeed);
            }
            
            activeZombies.Add(currentBoss);
            //Debug.Log("Final Boss spawned!");
        }
    }

    // Add method to handle boss defeat
    public void BossDefeated()
    {
        if (currentBoss != null)
        {
            RemoveZombie(currentBoss);
            currentBoss = null;
            //Debug.Log("Boss defeated!");
            
            // Stop the boss music
            if (SoundManager.Instance != null)
            {
                //Debug.Log("Stopping boss music...");
                SoundManager.Instance.StopMusic();
            }
            
            // Disable timer text
            if (TimerText != null)
            {
                TimerText.gameObject.SetActive(false);
            }
            
            StartCoroutine(ShowScoreScreenDelayed(5f));
        }
    }

    private IEnumerator ShowScoreScreenDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowScoreScreen();
    }

    public bool IsInCooldown()
    {
        return inCooldown;
    }

    // Add this to your Spawner class
public void StartEvent(int eventIndex, Transform triggerLocation, List<Transform> customSpawnPoints = null)
{
    // Don't start an event if one is already running
    if (eventActive)
    {
        Debug.Log("Cannot start new event: An event is already in progress");
        return;
    }
    
    // Check if event index is valid
    if (eventIndex < 0 || eventIndex >= configuredEvents.Count)
    {
        Debug.LogError($"Invalid event index: {eventIndex}");
        return;
    }
    
    currentEvent = configuredEvents[eventIndex];
    Debug.Log($"Starting event: {currentEvent.eventName}");
    
    // Call the event's start callback
    currentEvent.onEventStart?.Invoke();
    
    // Reset counters for the new event
    currentWave = 0;
    waveStartKillCount = totalKillCount;
    waveStartTime = totalElapsedTime;
    
    // Start the event wave system with both parameters
    eventActive = true;
    StartCoroutine(EventWaveSystem(triggerLocation, customSpawnPoints));
}

private IEnumerator EventWaveSystem(Transform triggerLocation, List<Transform> customSpawnPoints = null)
{
    // Store original spawn points
    List<GameObject> originalSpawnPoints = null;
    
    // If we have custom spawn points, temporarily replace the default ones
    if (customSpawnPoints != null && customSpawnPoints.Count > 0)
    {
        originalSpawnPoints = new List<GameObject>(spawnPoints);
        spawnPoints.Clear();
        
        // Convert Transform to GameObject for the spawner's system
        foreach (Transform spawnPoint in customSpawnPoints)
        {
            GameObject spawnPointObj = spawnPoint.gameObject;
            spawnPoints.Add(spawnPointObj);
        }
    }

    while (currentWave < currentEvent.eventWaves)
    {
        currentWave++;
        spawning = true;
        inCooldown = false;
        
        // Update UI
        if (WaveText != null)
        {
            WaveText.text = $"{currentEvent.eventName}: {currentWave}/{currentEvent.eventWaves}";
        }

        waveStartKillCount = totalKillCount;
        waveStartTime = totalElapsedTime;

        OnWaveStart?.Invoke(currentWave);
        Debug.Log($"Event wave {currentWave} started with duration: {currentEvent.eventWaveDuration}s");

        // Start spawning and pass BOTH the trigger location AND custom spawn points
        StartCoroutine(SpawnZombies(triggerLocation, customSpawnPoints));
        
        // Wait for the specified event wave duration
        float waveEndTime = Time.time + currentEvent.eventWaveDuration;
        while (Time.time < waveEndTime && spawning)
        {
            // Update timer display
            if (TimerText != null)
            {
                float timeRemaining = waveEndTime - Time.time;
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                TimerText.text = $"{minutes:00}:{seconds:00}";
            }
            yield return null;
        }
        
        spawning = false;
        
        // Last wave completed, show event complete message immediately
        if (currentWave >= currentEvent.eventWaves)  // <-- FIXED COMPARISON
        {
            // Skip the cooldown for the last wave and show completion immediately
            inCooldown = false;
            DespawnAllZombies();
            
            // Update UI to show Event Complete right away
            if (WaveText != null)
            {
                WaveText.text = "Event Complete";
            }
            
            if (TimerText != null)
            {
                TimerText.text = "";
            }
            
            OnWaveEnd?.Invoke(currentWave);
            Debug.Log($"Final event wave {currentWave} ended!");
            break; // Exit the loop to skip cooldown on final wave
        }
        else
        {
            // Normal cooldown between event waves
            inCooldown = true;
            cooldownTimeRemaining = currentEvent.eventCooldown;
            DespawnAllZombies();
            yield return new WaitForSeconds(currentEvent.eventCooldown);
            inCooldown = false;

            OnWaveEnd?.Invoke(currentWave);
            Debug.Log($"Event wave {currentWave} ended!");
        }
    }
    
    // Make sure to restore original spawn points if we changed them
    if (originalSpawnPoints != null)
    {
        spawnPoints = originalSpawnPoints;
    }

    // Call the event's complete callback
    currentEvent.onEventComplete?.Invoke();
    
    // IMPORTANT: Set these flags BEFORE handling UI
    eventActive = false;
    inCooldown = false;
    spawning = false; // Make sure spawning is also set to false

    if (currentEvent.endGameOnCompletion)
    {
        // End the game
        ShowScoreScreen();
    }
    else 
    {
        // UI is already showing "Event Complete" from the break above
        // Just wait for display duration then clear
        yield return new WaitForSeconds(3f);
        
        // IMPORTANT: Force clear all UI text
        if (WaveText != null)
        {
            WaveText.text = "";
            WaveText.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);
            WaveText.gameObject.SetActive(true);
        }
        
        // Make sure other UI elements are also reset properly
        if (TimerText != null)
        {
            TimerText.text = "";
        }
        
        // Re-null the current event to ensure complete cleanup
        currentEvent = null;
    }
}

    // Add these public methods to your Spawner class

    // Check if an event is currently active
    public bool IsEventActive()
    {
        return eventActive;
    }

    // Get the current event configuration
    public SpawnEvent GetCurrentEvent()
    {
        return currentEvent;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    StartCoroutine(InitializeVoicelinesAfterSceneLoad());
}

private void OnEnable()
{
    // Subscribe to scene loading events
    SceneManager.sceneLoaded += OnSceneLoaded;
}

private void OnDisable()
{
    // Unsubscribe from scene loading events
    SceneManager.sceneLoaded -= OnSceneLoaded;
}

// Public method that can be called from other scripts if needed
public void ReinitializeVoicelines()
{
    FindVoicelineUIReferences();
    InitializeVoicelineSystem();
}
}
