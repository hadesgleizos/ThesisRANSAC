using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public Slider sensitivitySlider;

    void Start()
    {
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
    }
}
