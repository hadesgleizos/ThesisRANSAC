using UnityEngine;
using System.Collections;
using TMPro;

public class FireTutorial : MonoBehaviour
{
    public GameObject tutorialCanvasGroup;

    public TextMeshProUGUI tutorialText;

    private GameObject firePass;

    public bool GunPickUpBool;
    
    private KeyCode[] tutorialKeys = new KeyCode[]
    {
        KeyCode.E,
        KeyCode.Mouse0,
        KeyCode.R,
        KeyCode.G,
        KeyCode.Alpha2,
        KeyCode.Alpha1
    };
    private string[] tutorialMessages = new string[]
    {
        "Press E to pick up the gun!",
        "Press Left Click to shoot!",
        "Press R to reload!",
        "Press G to throw!",
        "Press 1 or 2 to switch weapons!"
    };

    private int currentIndex = 0;
    private bool isFadingOut = false;

    private void Start()
    {
        GunPickUpBool = false;
        firePass = GameObject.FindGameObjectWithTag("FirePass");
        UpdateTutorialMessage();
    }
    private void Update()
    {
        if (isFadingOut) return;
        if (Input.GetKeyDown(tutorialKeys[currentIndex]) && currentIndex >= 1)
        {
            // Move on to the next key
            currentIndex++;

            // If there are more keys left, update the message
            if (currentIndex < tutorialKeys.Length)
            {
                UpdateTutorialMessage();
            }
            else
            {
                firePass.SetActive(false);
                gameObject.SetActive(false);
            }
        }
    }

    public void GunPickUpCue() 
    {
        GunPickUpBool = true;
        currentIndex++;
        if (currentIndex < tutorialKeys.Length)
        {
            UpdateTutorialMessage();
        }
    }

    // Update the tutorial text to match the current step
    private void UpdateTutorialMessage()
    {
        tutorialText.text = tutorialMessages[currentIndex];
    }
}
