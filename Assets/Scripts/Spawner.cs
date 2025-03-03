using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // Import TextMeshPro for UI
using UnityEngine.SceneManagement;  // For restarting the game

public class Spawner : MonoBehaviour
{
    public static Spawner Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject ScoreScreen;  // Reference to the Score Screen UI
    public TMP_Text ScorePoints;    // Reference to the Score text

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
    public GameObject zombiePrefab;
    public float spawnRate = 0.1f;   // Initial spawn rate
    public float zombieSpeed = 0.2f; // Initial zombie speed

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
    public string baseSceneName = "BaseScene";
    public string stageSceneName = "Stage 2";

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

            // Wait for cooldown before next wave
            yield return new WaitForSeconds(cooldownDuration);
        }

        Debug.Log("All waves completed!");
        ShowScoreScreen(); // Display final score
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

    // UPDATED RESTART LOGIC
    public void RestartGame()
    {
        Time.timeScale = 1;
        // Load BaseScene in Single mode (which unloads all current scenes)
        SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);

        // Then load the chosen stage scene additively
        SceneManager.LoadScene(stageSceneName, LoadSceneMode.Additive);
    }

    private IEnumerator SpawnZombies()
    {
        while (spawning)
        {
            if (spawnRate > 0 && spawnPoints.Count > 0)
            {
                // Spawn zombie at the next spawn point
                Vector3 spawnPosition = spawnPoints[currentSpawnIndex].transform.position;
                GameObject newZombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                newZombie.GetComponent<Zombie>().SetSpeed(zombieSpeed);
                activeZombies.Add(newZombie);

                // Cycle through spawn points
                currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Count;

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
}
