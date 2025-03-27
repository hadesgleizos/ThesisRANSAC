using UnityEngine;

public class TutorialPopUp : MonoBehaviour
{
    // Assign this in the Inspector to your pop-up UI (e.g., a Panel under a Canvas)
    public GameObject popUp;

    // This ensures the pop-up only appears once
    private bool hasBeenTriggered = false;

    // Called when another Collider enters the trigger
    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player and if we haven't displayed the pop-up before
        if (!hasBeenTriggered && other.CompareTag("MainCamera"))
        {
            //Debug.Log("Test");
            popUp.SetActive(true);
            hasBeenTriggered = true;
        }
    }

      void OnTriggerExit(Collider other)
    {
        if (hasBeenTriggered && other.CompareTag("MainCamera"))
        {
            //Debug.Log("Tutorial completed. Destroying trigger...");
            
            // Destroy the GameObject that has this script (the trigger zone)
            Destroy(gameObject);
        }
    }
}
