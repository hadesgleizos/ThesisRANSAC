using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenuHighScore : MonoBehaviour
{
    public TextMeshProUGUI[] stageHighScoreTexts; // Array of TextMeshProUGUI for multiple stages
    public string[] stageNames; // Names of stages to display scores for

    void Start()
    {
        UpdateHighScoreDisplay();
    }

    public void UpdateHighScoreDisplay()
    {
        // Make sure arrays are properly set up
        if (stageHighScoreTexts == null || stageNames == null || 
            stageHighScoreTexts.Length == 0 || stageNames.Length == 0)
        {
            //Debug.LogWarning("High score display not properly configured.");
            return;
        }

        // Display high scores for each stage
        for (int i = 0; i < stageHighScoreTexts.Length && i < stageNames.Length; i++)
        {
            if (stageHighScoreTexts[i] != null)
            {
                int highScore = ScoreManager.Instance.GetHighScore(stageNames[i]);
                stageHighScoreTexts[i].text = $"Score:{highScore}";
            }
        }
    }
}
