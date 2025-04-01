using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Add this line for SceneManager

using TMPro;

public class uiManager : MonoBehaviour
{
    public TextMeshProUGUI health, ammo, score;
    public TextMeshProUGUI highScoreText; // Add this field for displaying high score
    public GameObject[] weaponIndicator = new GameObject[2];

    private Color defaultColor; // Default health text color
    private string currentLevel; // Keep track of current level

    // Start is called before the first frame update
    private void Start()
    {
        // Parse the custom color (#B5F165) and set it as the default
        if (ColorUtility.TryParseHtmlString("#B5F165", out defaultColor))
        {
            health.color = defaultColor;
        }

        // Look for stage scenes - they typically start with "Stage"
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name.StartsWith("Stage"))
            {
                currentLevel = scene.name;
                //Debug.Log($"Found stage scene: {currentLevel}");
                break;
            }
        }
        
        // If no stage scene was found, fall back to active scene
        if (string.IsNullOrEmpty(currentLevel))
        {
            currentLevel = SceneManager.GetActiveScene().name;
            //Debug.Log($"No stage scene found, using active scene: {currentLevel}");
        }
        
        // List all loaded scenes for debugging
        //Debug.Log("All loaded scenes:");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            //Debug.Log($"Scene {i}: {scene.name} (active: {scene.isLoaded})");
        }
        
        // Display high score if we have the text component
        if (highScoreText != null)
        {
            UpdateHighScoreDisplay();
        }
    }

    public void setHealth(string i)
    {
        // Add the "+" symbol or health icon representation
        health.text = $"+{i}";
        UpdateHealthColor(float.Parse(i));
    }

    public void setAmmo(string i) { ammo.text = i; }
    
    public void setScore(string i) 
    { 
        score.text = i;
        
        // Check if this is a new high score and save it
        int currentScore = int.Parse(i);
        //Debug.Log($"Setting score to {currentScore} for level {currentLevel}");
        ScoreManager.Instance.SaveHighScore(currentLevel, currentScore);
        UpdateHighScoreDisplay();
    }

    private void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            int highScore = ScoreManager.Instance.GetHighScore(currentLevel);
            highScoreText.text = $"HIGH SCORE: {highScore}";
        }
    }

    public void setWeaponToDisplay(int e)
    {
        for (int i = 0; i < weaponIndicator.Length; i++)
        {
            weaponIndicator[i].SetActive(false);
        }
        for (int i = 0; i < weaponIndicator.Length; i++)
        {
            if (i == e) weaponIndicator[i].SetActive(true);
        }
    }

    private void UpdateHealthColor(float currentHealth)
    {
        float maxHealth = 100f; // Assuming 100 is the max health
        float healthPercentage = (currentHealth / maxHealth) * 100;

        if (healthPercentage <= 30)
        {
            health.color = Color.red; // Red color for health <= 30%
        }
        else if (healthPercentage <= 60)
        {
            health.color = new Color(1f, 0.65f, 0f); // Orange color for health <= 60%
        }
        else
        {
            health.color = defaultColor; // Default custom color for health > 60%
        }
    }

    public void ToggleUI(bool isActive)
    {
        health.gameObject.SetActive(isActive);
        ammo.gameObject.SetActive(isActive);
        score.gameObject.SetActive(isActive);
        if (highScoreText != null)
        {
            highScoreText.gameObject.SetActive(isActive);
        }

        foreach (var weapon in weaponIndicator)
        {
            weapon.SetActive(false); // Ensure all weapon indicators are initially disabled
        }
    }

    public void UpdateWeaponUI(int weaponIndex)
    {
        for (int i = 0; i < weaponIndicator.Length; i++)
        {
            weaponIndicator[i].SetActive(i == weaponIndex);
        }
    }

    public void SetCurrentLevel(string levelName)
    {
        currentLevel = levelName;
        UpdateHighScoreDisplay();
    }
}
