using UnityEngine;
using System.Collections.Generic;

public class EventTrigger : MonoBehaviour
{
    [Header("Event Settings")]
    public int eventIndex = 0;
    public bool triggerOnce = true;
    public bool requireKeyPress = true;
    public KeyCode triggerKey = KeyCode.E;
    
    [Header("UI")]
    public GameObject interactionPrompt;
    
    // Add custom spawn points to match the CustomEventTrigger
    [Header("Custom Spawn Points")]
    public List<Transform> spawnPoints;
    
    private bool playerInTrigger = false;
    private bool hasTriggered = false;
    
    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            playerInTrigger = true;
            if (interactionPrompt != null && requireKeyPress)
            {
                interactionPrompt.SetActive(true);
            }
            else if (!requireKeyPress)
            {
                TriggerEvent();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
    
    private void Update()
    {
        if (playerInTrigger && requireKeyPress && Input.GetKeyDown(triggerKey))
        {
            TriggerEvent();
        }
    }
    
    private void TriggerEvent()
    {
        if (hasTriggered && triggerOnce) return;
        
        if (Spawner.Instance != null)
        {
            // Update to pass spawnPoints as the third parameter
            Spawner.Instance.StartEvent(eventIndex, transform, spawnPoints);
            
            if (triggerOnce)
            {
                hasTriggered = true;
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("Spawner instance not found!");
        }
    }
}