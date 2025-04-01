using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    public Slider sensitivitySlider;

    void Start()
    {
        // Make sure ScoreManager is initialized
        //Debug.Log("Initializing ScoreManager from MainMenuManager");
        var scoreManager = ScoreManager.Instance;
        
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 1f;
            sensitivitySlider.maxValue = 100f;
            float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 30f);
            sensitivitySlider.value = savedSensitivity;
            sensitivitySlider.onValueChanged.AddListener(UpdateSensitivity);
        }
    }

    private void UpdateSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    // Call this from a button OnClick() or anywhere you want to start the game.
    public void StartGame()
    {
        // First, load the BaseScene in Single mode to replace the current (Menu) scene.
        SceneManager.LoadScene("BaseScene", LoadSceneMode.Single);

        // Then, load Stage1 Additively on top of the BaseScene.
        SceneManager.LoadScene("Stage 1", LoadSceneMode.Additive);
        
        // Set the stage scene as the active scene
        StartCoroutine(SetStageAsActive("Stage 1"));
    }

    private IEnumerator SetStageAsActive(string stageName)
    {
        // Wait for the scene to be fully loaded
        yield return new WaitForSeconds(0.1f);
        
        // Get the scene and set it as active
        Scene stageScene = SceneManager.GetSceneByName(stageName);
        if (stageScene.IsValid())
        {
            SceneManager.SetActiveScene(stageScene);
            //Debug.Log($"Set active scene to: {stageName}");
        }
        else
        {
            //Debug.LogError($"Could not set {stageName} as active - scene not found");
        }
    }
}
