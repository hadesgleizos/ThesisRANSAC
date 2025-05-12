using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Renamed from EventTrigger to CustomEventTrigger to avoid collision with Unity's EventTrigger
public class CustomEventTrigger : MonoBehaviour
{
    [Header("Event Settings")]
    public int eventIndex = 0;
    public bool triggerOnce = true;
    public bool requireKeyPress = true;
    public KeyCode triggerKey = KeyCode.E;
    
    [Header("UI")]
    public GameObject interactionPrompt;
    
    // Optional custom spawn points for this specific event trigger
    [Header("Custom Spawn Points")]
    public List<Transform> spawnPoints;
    
    [Header("Detection Settings")]
    public string playerTag = "MainCamera"; // Make this configurable
    
    [Header("Voiceline")]
    public bool playVoiceline = false;
    public string voicelineId = "";
    public bool playVoicelineOnTriggerExit = false;
    public string exitVoicelineId = "";
    
    [Header("Debug")]
    public bool debugMode = false; // Add toggle for debug mode
    
    private bool playerInTrigger = false;
    private bool hasTriggered = false;
    private Coroutine activeCoroutine = null;
    
    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Stop any existing coroutine to prevent duplicates
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        
        // Only start the keep-alive check if in debug mode
        if (debugMode)
        {
            activeCoroutine = StartCoroutine(MonitorColliderState());
        }
    }
    
    private void OnDisable()
    {
        // Clean up: stop the coroutine when disabled
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
        
        // Log only in debug mode to avoid console spam
        if (debugMode)
        {
            Debug.Log($"Trigger {gameObject.name} was disabled");
        }
    }

    // Change to a monitoring-only approach instead of auto-fixing
    private IEnumerator MonitorColliderState()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            // Just log issues rather than auto-fixing them
            Collider col = GetComponent<Collider>();
            if (col != null && !col.enabled && debugMode)
            {
                Debug.Log($"Note: Collider on {gameObject.name} is disabled");
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && (!triggerOnce || !hasTriggered))
        {
            playerInTrigger = true;
            if (debugMode) Debug.Log($"Player entered trigger: {gameObject.name}");
            
            // Play voiceline if configured
            if (playVoiceline && !string.IsNullOrEmpty(voicelineId) && Voicelines.Instance != null)
            {
                Voicelines.Instance.PlayVoiceline(voicelineId);
                if (debugMode) Debug.Log($"Playing enter voiceline: {voicelineId}");
            }
            
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
        if (other.CompareTag(playerTag))
        {
            playerInTrigger = false;
            if (debugMode) Debug.Log($"Player exited trigger: {gameObject.name}");
            
            // Play exit voiceline if configured
            if (playVoicelineOnTriggerExit && !string.IsNullOrEmpty(exitVoicelineId) && Voicelines.Instance != null)
            {
                Voicelines.Instance.PlayVoiceline(exitVoicelineId);
                if (debugMode) Debug.Log($"Playing exit voiceline: {exitVoicelineId}");
            }
            
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
            if (debugMode) Debug.Log($"Triggering event {eventIndex} from {gameObject.name}");
            
            // Pass this transform as the trigger location and optionally spawn points
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                // Call the overload that accepts custom spawn points
                Spawner.Instance.StartEvent(eventIndex, transform, spawnPoints);
            }
            else
            {
                // Call the simplified version without custom spawn points
                Spawner.Instance.StartEvent(eventIndex, transform, null);
            }
            
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
    
    // Add a public method to reset the trigger
    public void ResetTrigger()
    {
        hasTriggered = false;
        if (interactionPrompt != null && playerInTrigger && requireKeyPress)
        {
            interactionPrompt.SetActive(true);
        }
    }
}