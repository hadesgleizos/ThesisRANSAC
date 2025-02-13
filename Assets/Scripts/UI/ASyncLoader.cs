using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ASyncLoader : MonoBehaviour
{
    [Header("Menu Screens")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject mainMenu;

    [Header("Slider")]
    [SerializeField] private Slider loadingSlider;

    [Header("Scene Settings")]
    [SerializeField] private string baseSceneName = "BaseScene";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadLevelBtn(string levelToLoad)
    {
        mainMenu.SetActive(false);
        loadingScreen.SetActive(true);
        StartCoroutine(LoadLevelAsync(levelToLoad));
    }

    private IEnumerator LoadLevelAsync(string levelToLoad)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(levelToLoad);
        while (!loadOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(loadOperation.progress / 0.9f);
            loadingSlider.value = progressValue;
            yield return null;
        }
    }
    public void LoadBaseThenStageBtn(string stageToLoad)
    {
        mainMenu.SetActive(false);
        loadingScreen.SetActive(true);
        StartCoroutine(LoadBaseThenStageAsync(stageToLoad));
    }

    private IEnumerator LoadBaseThenStageAsync(string stageToLoad)
    {
        // 1) Load the Base Scene in Single mode
        AsyncOperation baseLoadOperation = SceneManager.LoadSceneAsync(baseSceneName, LoadSceneMode.Single);
        while (!baseLoadOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(baseLoadOperation.progress / 0.9f);
            loadingSlider.value = progressValue;
            yield return null;
        }

        loadingSlider.value = 1f;
        loadingSlider.value = 0f;

        // 2) Load the stage scene additively
        AsyncOperation stageLoadOperation = SceneManager.LoadSceneAsync(stageToLoad, LoadSceneMode.Additive);
        while (!stageLoadOperation.isDone)
        {
            float progressValue = Mathf.Clamp01(stageLoadOperation.progress / 0.9f);
            loadingSlider.value = progressValue;
            yield return null;
        }

        loadingSlider.value = 1f;
        loadingScreen.SetActive(false);
    }
}
