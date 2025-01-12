using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public float spawnRate = 0.1f;  // Initial spawn rate
    public float zombieSpeed = 0.2f; // Initial zombie speed

    public List<GameObject> spawnPoints = new List<GameObject>(); // New list for spawn points
    private List<GameObject> activeZombies = new List<GameObject>(); // Track active zombies
    private int currentSpawnIndex = 0; // Track which spawn point to use next


    void Start()
    {
        StartCoroutine(SpawnZombies());
    }

    IEnumerator SpawnZombies()
    {
        while (true)
        {
            if (spawnRate > 0 && spawnPoints.Count > 0) // Check if we have spawn points
            {
                // Get the next spawn point position
                Vector3 spawnPosition = spawnPoints[currentSpawnIndex].transform.position;
                
                // Create zombie at the spawn point
                GameObject newZombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                newZombie.GetComponent<Zombie>().SetSpeed(zombieSpeed);
                activeZombies.Add(newZombie);

                // Update spawn index for next spawn, wrapping around to 0
                currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Count;

                yield return new WaitForSeconds(1.0f / spawnRate);
            }
            else
            {
                yield return null;
            }
        }
    }

    public void SetAllZombieSpeeds(float newSpeed)
    {
        zombieSpeed = newSpeed; // Update the local speed variable
        foreach (GameObject zombie in activeZombies)
        {
            if (zombie != null) // Check if the zombie still exists
            {
                zombie.GetComponent<Zombie>().SetSpeed(newSpeed); // Update each zombie's speed
            }
        }
        Debug.Log($"All Zombie Speeds Updated to: {newSpeed}"); // Debug log for speed update
    }

    public void RemoveZombie(GameObject zombie)
    {
        if (activeZombies.Contains(zombie))
        {
            activeZombies.Remove(zombie);
        }
    }

    // Method to update spawn rate from PSOManager
    public void UpdateSpawnRate(float newSpawnRate)
    {
        spawnRate = newSpawnRate;
        Debug.Log($"Spawn Rate Updated: {spawnRate}"); // Debug log for spawn rate
    }

    // New method to get current spawn rate
    public float GetCurrentSpawnRate()
    {
        return spawnRate;
    }

    // New method to get current zombie speed
    public float GetCurrentZombieSpeed()
    {
        return zombieSpeed;
    }
}
