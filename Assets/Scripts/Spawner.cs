using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject zombiePrefab;
    public float spawnRate = 1.0f;  // Initial spawn rate
    public float zombieSpeed = 1.0f; // Initial zombie speed

    private List<GameObject> activeZombies = new List<GameObject>(); // Track active zombies

    void Start()
    {
        StartCoroutine(SpawnZombies());
    }

    IEnumerator SpawnZombies()
    {
        while (true)
        {
            if (spawnRate > 0)
            {
                GameObject newZombie = Instantiate(zombiePrefab, transform.position, Quaternion.identity);
                newZombie.GetComponent<Zombie>().SetSpeed(zombieSpeed); // Set the initial speed of the zombie
                activeZombies.Add(newZombie); // Add the new zombie to the list

                yield return new WaitForSeconds(1.0f / spawnRate);
            }
            else
            {
                yield return null; // Wait for the next frame if spawn rate is zero
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
