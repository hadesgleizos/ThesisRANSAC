using UnityEngine;
using System.Collections;
using TMPro;

public class FireTutorial : MonoBehaviour
{
    public GameObject tutorialCanvasGroup;

    public TextMeshProUGUI tutorialText; 


    private KeyCode[] tutorialKeys = new KeyCode[]
    {
        KeyCode.E,
        KeyCode.Mouse0,
        KeyCode.R
    };
    private string[] tutorialMessages = new string[]
    {
        "Press E to pick up the gun!",
        "Press Left Click to shoot!",
        "Press R to reload!"
    };

    private int currentIndex = 0;
    private bool isFadingOut = false;

    private void Start()
    {

        UpdateTutorialMessage();
    }

    private void Update()
    {
        if (isFadingOut) return;

        if (Input.GetKeyDown(tutorialKeys[currentIndex]))
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
                gameObject.SetActive(false);
            }
        }
    }

    // Update the tutorial text to match the current step
    private void UpdateTutorialMessage()
    {
        tutorialText.text = tutorialMessages[currentIndex];
    }
}
