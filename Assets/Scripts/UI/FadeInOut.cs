using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeOutDuration = 2f;

    private void Awake()
    {
        fadeGroup = GetComponentInChildren<CanvasGroup>();
    }
    private void Start()
    {
        SetupFade();
    }
    private void SetupFade()
    {
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 1f; // Start fully black
            StartCoroutine(FadeOut()); // Add fade in
        }
    }
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            if (fadeGroup.alpha == 0f)
            {
                gameObject.SetActive(false);
            }
            yield return null;
        }
    }
}
