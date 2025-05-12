using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwoColliderInteractable : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 3f; // Adjust this to set interaction distance
    [SerializeField] private VoicelineInteractable voicelineInteractable;
    
    private SphereCollider triggerCollider;
    
    void Start()
    {
        // Make sure we have a reference to the VoicelineInteractable component
        if (voicelineInteractable == null)
            voicelineInteractable = GetComponent<VoicelineInteractable>();
            
        // Create a larger trigger collider for interaction
        GameObject triggerObject = new GameObject("InteractionTrigger");
        triggerObject.transform.parent = this.transform;
        triggerObject.transform.localPosition = Vector3.zero;
        
        triggerCollider = triggerObject.AddComponent<SphereCollider>();
        triggerCollider.radius = interactionRadius;
        triggerCollider.isTrigger = true;
        
        // Add a trigger handler component
        InteractionTrigger trigger = triggerObject.AddComponent<InteractionTrigger>();
        trigger.Initialize(voicelineInteractable);
    }
}

public class InteractionTrigger : MonoBehaviour
{
    private VoicelineInteractable targetInteractable;
    
    public void Initialize(VoicelineInteractable interactable)
    {
        targetInteractable = interactable;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera") || other.CompareTag("Player"))
        {
            // When player enters range, enable the interactable's prompt
            if (targetInteractable != null)
            {
                targetInteractable.EnablePrompt(true);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera") || other.CompareTag("Player"))
        {
            // When player exits range, disable the interactable's prompt
            if (targetInteractable != null)
            {
                targetInteractable.EnablePrompt(false);
            }
        }
    }
}
