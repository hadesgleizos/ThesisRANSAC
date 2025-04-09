using UnityEngine;
using System.Collections;
using TMPro;
public class TutorialController : MonoBehaviour
{
    public GameObject tutorialCanvasGroup;

    public TextMeshProUGUI tutorialText;

    private GameObject MoveContPass;


    private KeyCode[] tutorialKeys = new KeyCode[]
    {
        KeyCode.W,
        KeyCode.A,
        KeyCode.S,
        KeyCode.D,
        KeyCode.Space
    };

    private string[] tutorialMessages = new string[]
    {
        "Press W to move forward!",
        "Press A to move left!",
        "Press S to move backward!",
        "Press D to move right!",
        "Press Space to jump!"
    };

    private int currentIndex = 0;
    private bool isFadingOut = false;

    private void Start()
    {
        MoveContPass = GameObject.FindGameObjectWithTag("MovementPass");
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
                MoveContPass.SetActive(false);
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
