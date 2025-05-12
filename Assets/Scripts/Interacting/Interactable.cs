using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    //message displayed to the player when looking at an interactable
    public string promptMessage;
    
    // Added fields for player detection
    protected bool playerInRange = false;
    public string playerTag = "MainCamera"; // Typically the tag on your player camera
    
    // Add these methods for player detection
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            Debug.Log($"Player entered range of {gameObject.name}");
        }
    }
    
    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            Debug.Log($"Player exited range of {gameObject.name}");
        }
    }
    
    // Add this to check for the interaction key
    protected virtual void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"Interaction key pressed while in range of {gameObject.name}");
            BaseInteract();
        }
    }
    
    public void BaseInteract()
    {
        Interact();
    }
    
    protected virtual void Interact()
    {
        // This method is meant to be overwritten by subclasses
        Debug.Log("Base Interact() called - override this in derived classes");
    }
}
