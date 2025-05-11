using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoicelineInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 2f;
    public KeyCode interactionKey = KeyCode.E;
    public string interactionTag = "MainCamera";
    public bool triggerOnce = true;
    
    [Header("Voiceline")]
    public string voicelineId;
    
    [Header("UI")]
    public GameObject interactionPrompt;
    
    private bool hasInteracted = false;
    private bool playerInRange = false;
    
    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(interactionTag) && (!hasInteracted || !triggerOnce))
        {
            playerInRange = true;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(interactionTag))
        {
            playerInRange = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
    
    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey) && (!hasInteracted || !triggerOnce))
        {
            PlayVoiceline();
        }
    }
    
    public void PlayVoiceline()
    {
        if (hasInteracted && triggerOnce) return;
        
        if (!string.IsNullOrEmpty(voicelineId) && Voicelines.Instance != null)
        {
            Voicelines.Instance.PlayVoiceline(voicelineId);
            
            if (triggerOnce)
            {
                hasInteracted = true;
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }
    }
    
    // Add a public method to reset the interactable
    public void Reset()
    {
        hasInteracted = false;
        if (interactionPrompt != null && playerInRange)
        {
            interactionPrompt.SetActive(true);
        }
    }
    
    // Visual indicator in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
