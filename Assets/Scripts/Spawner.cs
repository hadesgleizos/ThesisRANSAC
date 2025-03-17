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
    public float spawnRate = 0.1f;
    public float zombieSpeed = 0.2f;

    [Header("Wave Settings")]
    public int totalWaves = 5;               // Total number of waves
    public float waveDuration = 30f;         // Duration of each wave in seconds
    public float cooldownDuration = 10f;     // Cooldown duration between waves

    [Header("Spawn Points")]
    public List<GameObject> spawnPoints = new List<GameObject>(); // List of spawn points

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
            // Start new wave
            currentWave++;
            spawning = true;
            inCooldown = false;
            
            // Update wave display
            if (WaveText != null)
            {
                WaveText.text = $"Wave: {currentWave}/{totalWaves}";
            }

            // Capture metrics at wave start
            waveStartKillCount = totalKillCount;
            waveStartTime = totalElapsedTime;

            OnWaveStart?.Invoke(currentWave);
            Debug.Log($"Wave {currentWave} started!");

            // Spawn zombies during wave duration
            StartCoroutine(SpawnZombies());
            yield return new WaitForSeconds(waveDuration);

            // End current wave
            spawning = false;
            OnWaveEnd?.Invoke(currentWave);
            Debug.Log($"Wave {currentWave} ended!");

            // Start cooldown period and despawn all zombies
            if (currentWave < totalWaves) // Only show cooldown if not the last wave
            {
                inCooldown = true;
                cooldownTimeRemaining = cooldownDuration;
                DespawnAllZombies(); // Add this line to despawn zombies
                yield return new WaitForSeconds(cooldownDuration);
                inCooldown = false;
            }
        }

        Debug.Log("All waves completed!");
        ShowScoreScreen();
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

        int randomIndex = Random.Range(0, zombieTypes.Count);
        return zombieTypes[randomIndex].zombiePrefab;
    }

    private IEnumerator SpawnZombies()
    {
        while (spawning)
        {
            if (spawnRate > 0 && spawnPoints.Count > 0)
            {
                // Get random zombie type
                GameObject zombiePrefab = GetRandomZombiePrefab();
                if (zombiePrefab != null)
                {
                    // Spawn zombie at the next spawn point
                    Vector3 spawnPosition = spawnPoints[currentSpawnIndex].transform.position;
                    GameObject newZombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                    newZombie.GetComponent<Zombie>().SetSpeed(zombieSpeed);
                    activeZombies.Add(newZombie);

                    // Cycle through spawn points
                    currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Count;
                }

                yield return new WaitForSeconds(1.0f / spawnRate);
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
                float timeRemaining = waveDuration - (totalElapsedTime - waveStartTime);
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                TimerText.text = $"{minutes:00}:{seconds:00}";
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
        zombieSpeed = newSpeed;
        foreach (GameObject zombie in activeZombies)
        {
            if (zombie != null)
            {
                zombie.GetComponent<Zombie>().SetSpeed(newSpeed);
            }
        }
        Debug.Log($"All Zombie Speeds Updated to: {newSpeed}");
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

    public float GetCurrentZombieSpeed()
    {
        return zombieSpeed;
    }

    public int GetActiveSpawnerCount()
    {
        return spawnPoints.Count;
    }

    private void DespawnAllZombies()
    {
        foreach (GameObject zombie in activeZombies.ToList())
        {
            if (zombie != null)
            {
                Destroy(zombie);
            }
        }
        activeZombies.Clear();
    }
}
