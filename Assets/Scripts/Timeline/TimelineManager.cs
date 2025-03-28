using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineManager : MonoBehaviour
{
    [SerializeField] private PlayableDirector timelineDirector;
    [SerializeField] private string gameSceneName = "Stage 1";
    [SerializeField] private string cutsceneSceneName = "WakingUp";
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeOutDuration = 1f;

    private void Start()
    {
        if (timelineDirector != null)
        {
            timelineDirector.played += Director_Played;
            timelineDirector.stopped += Director_Stopped;
            SetupFade();
            timelineDirector.Play();
        }
    }

    private void SetupFade()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f; // Start fully black
            StartCoroutine(FadeIn()); // Add initial fade in
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }
    }

    private void Director_Played(PlayableDirector obj)
    {
        //Debug.Log("Cutscene started");
    }

    private void Director_Stopped(PlayableDirector obj)
    {
        //Debug.Log("Cutscene finished");
        StartCoroutine(TransitionToGameScene());
    }

    private IEnumerator TransitionToGameScene()
    {
        if (fadeCanvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeOutDuration);
                yield return null;
            }
        }

        // Load Stage 1 additively
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
        
        // Unload the cutscene scene
        SceneManager.UnloadSceneAsync(cutsceneSceneName);
    }

    private void OnDestroy()
    {
        if (timelineDirector != null)
        {
            timelineDirector.played -= Director_Played;
            timelineDirector.stopped -= Director_Stopped;
        }
    }
}
