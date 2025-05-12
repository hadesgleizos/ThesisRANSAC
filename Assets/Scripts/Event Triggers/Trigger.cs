using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class Trigger : MonoBehaviour
{
    [SerializeField] bool destroyOnTriggerEnter;
    [SerializeField] bool destroyOnTriggerExit;
    [SerializeField] string tagFilter = "Player";
    [SerializeField] UnityEvent onTriggerEnter;
    [SerializeField] UnityEvent onTriggerExit;
    [SerializeField] private AudioClip soundEffect;
    
    // Add event spawning capability
    [Header("Event Spawning")]
    [SerializeField] bool startEventOnTrigger;
    [SerializeField] int eventIndex;
    [SerializeField] List<Transform> spawnPoints; // Add this line to provide spawn points

    private SoundManager soundManager;

    void Start(){
        soundManager = FindObjectOfType<SoundManager>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!String.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;
        
        onTriggerEnter.Invoke();

        if (soundManager && soundEffect)
            soundManager.PlaySound(soundEffect);
            
        // Start event if configured - pass the custom spawn points (can be null)
        if (startEventOnTrigger && Spawner.Instance != null)
        {
            Spawner.Instance.StartEvent(eventIndex, transform, spawnPoints);
        }
        
        if (destroyOnTriggerEnter)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!String.IsNullOrEmpty(tagFilter) && !other.gameObject.CompareTag(tagFilter)) return;
        onTriggerExit.Invoke();

        if (destroyOnTriggerExit)
        {
            Destroy(gameObject);
        }
    }
}