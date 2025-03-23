using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // Import TextMeshPro for UI
using UnityEngine.SceneManagement;  // For restarting the game
using System.Linq; // Import Linq for ToList()

[System.Serializable]
public class ZombieType
{
    public GameObject zombiePrefab;
    public string zombieName;  // For debugging/identification
    [Range(0, 100)]
    public float spawnChance = 10;  // Default 10% spawn chance
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

    private void Start()
    {
        // NEW: If no PlayerPerformance assigned in Inspector, try finding it
        if (playerPerformance == null)
        {
            playerPerformance = FindObjectOfType<PlayerPerformance>();
        }

        if (ScoreScreen != null)
        {
            ScoreScreen.SetActive(false); // Hide the Score Screen at the start
        }

        Time.timeScale = 1; // Ensure the game is running normally at start
        StartCoroutine(WaveSystem());
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
            Debug.Log($"Wave {currentWave} started!");

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
            Debug.Log($"Wave {currentWave} ended!");
        }

        Debug.Log("All waves completed!");
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
    Time.timeScale = 1;
    SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);
    SceneManager.LoadScene(restartSceneName, LoadSceneMode.Additive);
}

public void NextStage()
{
    Time.timeScale = 1;
    SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);
    SceneManager.LoadScene(nextStageSceneName, LoadSceneMode.Additive);
}

    private GameObject GetRandomZombiePrefab()
    {
        if (zombieTypes.Count == 0)
        {
            Debug.LogError("No zombie types defined!");
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

    private IEnumerator SpawnZombies()
    {
        while (spawning)
        {
            if (spawnRate > 0 && spawnPoints.Count > 0)
            {
                // Generate a random value to determine if we spawn this frame
                if (Random.value < spawnRate * Time.deltaTime)
                {
                    GameObject zombiePrefab = GetRandomZombiePrefab();
                    if (zombiePrefab != null)
                    {
                        // Randomly select one of the available spawn points
                        int randomSpawnIndex = Random.Range(0, spawnPoints.Count);
                        Vector3 spawnPosition = spawnPoints[randomSpawnIndex].transform.position;
                        
                        GameObject newZombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                        
                        var zombieComponent = newZombie.GetComponent<Zombie>();
                        if (zombieComponent != null)
                        {
                            zombieComponent.SetSpeed(currentZombieSpeed); // Use current speed from PSO
                            if (debugSpeedChanges)
                            {
                                Debug.Log($"[Spawner] New zombie spawned with speed: {currentZombieSpeed:F2}");
                            }
                        }
                        
                        activeZombies.Add(newZombie);
                    }
                }
                
                // Wait a short time before checking again
                yield return null;
            }
            else
            {
                yield return null;
            }
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
                    float timeRemaining = waveDuration - (totalElapsedTime - waveStartTime);
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
            if (TimerText != null)
            {
                int seconds = Mathf.CeilToInt(cooldownTimeRemaining);
                TimerText.text = $"Get Ready in: {seconds}";
            }
            
            // Update wave text to show next wave
            if (WaveText != null)
            {
                WaveText.text = $"Preparing Wave {currentWave + 1}";
            }
        }
    }

    public void IncrementKillCount()
    {
        totalKillCount++;
        Debug.Log($"Zombie killed! Total kills: {totalKillCount}");
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
                
                // NEW: Check for Jograt
                var jogratComponent = zombie.GetComponent<Jograt>();
                if (jogratComponent != null)
                {
                    jogratComponent.SetSpeed(currentZombieSpeed);
                    updatedEnemies++;
                }
            }
        }

        if (debugSpeedChanges)
        {
            Debug.Log($"[Spawner] Speed Change - Old: {oldSpeed:F2}, New: {currentZombieSpeed:F2}, Updated {updatedEnemies} enemies");
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
        spawnRate = newSpawnRate;
        Debug.Log($"Spawn Rate Updated: {spawnRate}");
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
                Debug.Log("Playing boss music...");
                SoundManager.Instance.PlayBossMusic();
            }
            else
            {
                Debug.LogWarning("SoundManager.Instance is null! Cannot play boss music.");
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
            Debug.Log("Final Boss spawned!");
        }
    }

    // Add method to handle boss defeat
    public void BossDefeated()
    {
        if (currentBoss != null)
        {
            RemoveZombie(currentBoss);
            currentBoss = null;
            Debug.Log("Boss defeated!");
            
            // Stop the boss music
            if (SoundManager.Instance != null)
            {
                Debug.Log("Stopping boss music...");
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
}
