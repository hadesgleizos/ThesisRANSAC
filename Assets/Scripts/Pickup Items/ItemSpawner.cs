using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnItem
    {
        [Tooltip("The item prefab to spawn")]
        public GameObject itemPrefab;
        [Tooltip("List of spawn points for this specific item")]
        public Transform[] spawnPoints;
        [Tooltip("Spawn chance per check (0-1)")]
        [Range(0f, 1f)]
        public float spawnChance = 0.05f;
    }

    [Header("Item Spawn Settings")]
    [Tooltip("Configure items and their spawn points")]
    public SpawnItem[] spawnItems;

    [Header("Spawn Timing")]
    [Tooltip("How often to check for spawning (in seconds)")]
    public float spawnCheckInterval = 0.5f;

    private Coroutine spawnRoutine;
    private bool isSpawning = false;

    private void OnEnable()
    {
        // Subscribe to Spawner's wave events
        Spawner.OnWaveStart += StartSpawning;
        Spawner.OnWaveEnd += StopSpawning;
    }

    private void OnDisable()
    {
        // Unsubscribe from Spawner's wave events
        Spawner.OnWaveStart -= StartSpawning;
        Spawner.OnWaveEnd -= StopSpawning;
    }

    private void StartSpawning(int waveNumber)
    {
        isSpawning = true;
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    private void StopSpawning(int waveNumber)
    {
        isSpawning = false;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnCheckInterval);

        while (isSpawning)
        {
            SpawnItems();
            yield return wait;
        }
    }

    private void SpawnItems()
    {
        foreach (SpawnItem item in spawnItems)
        {
            if (item.itemPrefab == null || item.spawnPoints == null) continue;

            foreach (Transform spawnPoint in item.spawnPoints)
            {
                if (!IsSpawnPointOccupied(spawnPoint))
                {
                    if (Random.value < item.spawnChance)
                    {
                        Instantiate(item.itemPrefab, spawnPoint.position, Quaternion.identity);
                    }
                }
            }
        }
    }

    private bool IsSpawnPointOccupied(Transform spawnPoint)
    {
        Collider[] hitColliders = Physics.OverlapSphere(spawnPoint.position, 0.5f);
        
        foreach (Collider collider in hitColliders)
        {
            // Skip colliders that are triggers AND have the "Trigger" tag
            if (collider.isTrigger && collider.CompareTag("Trigger"))
                continue;
                
            // If we find any non-trigger collider or a trigger without the "Trigger" tag, the spot is occupied
            return true;
        }
        
        // No blocking colliders found
        return false;
    }

    private void OnDrawGizmos()
    {
        if (spawnItems == null) return;

        foreach (SpawnItem item in spawnItems)
        {
            if (item.spawnPoints == null) continue;

            // Different colors for different item types
            Gizmos.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            
            foreach (Transform point in item.spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }
    }
}
