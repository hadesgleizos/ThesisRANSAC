using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Call this from a button OnClick() or anywhere you want to start the game.
    public void StartGame()
    {
        // First, load the BaseScene in Single mode to replace the current (Menu) scene.
        SceneManager.LoadScene("BaseScene", LoadSceneMode.Single);

        // Then, load Stage1 Additively on top of the BaseScene.
        SceneManager.LoadScene("Stage 1", LoadSceneMode.Additive);
    }
}
