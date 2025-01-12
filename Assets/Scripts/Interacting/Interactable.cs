using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    //message displayed to the player when looking at an interactable
    public string promptMessage;
    
    public void BaseInteract(){
        Interact();
    }
    protected virtual void Interact(){
        //this method is meant to be overwritten by subclasses
    }
}
