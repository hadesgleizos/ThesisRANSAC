using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Resolutions : MonoBehaviour
{
    private static Resolutions instance;

    private void Awake()
    {
        // Singleton pattern to ensure only one instance exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private TMPro.TMP_Dropdown resolutionDropDown;
    
    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private int currentResolutionIndex = 0;
    [HideInInspector] public Resolution resolution;
    
    private const string RESOLUTION_WIDTH_KEY = "ResolutionWidth";
    private const string RESOLUTION_HEIGHT_KEY = "ResolutionHeight";
    private const string RESOLUTION_INDEX_KEY = "ResolutionIndex";
    
    private void Start()
    {
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        resolutionDropDown.ClearOptions();

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (!filteredResolutions.Any(x => x.width == resolutions[i].width && x.height == resolutions[i].height))
            {
                filteredResolutions.Add(resolutions[i]);
            }
        }

        List<string> options = new List<string>();
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            string resolutionOption = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
            options.Add(resolutionOption);
            
            // Check if this is the current resolution
            if (filteredResolutions[i].width == Screen.width && filteredResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropDown.AddOptions(options);

        // Load saved resolution if it exists
        LoadSavedResolution();
        
        resolutionDropDown.value = currentResolutionIndex;
        resolutionDropDown.RefreshShownValue();
    }

    private void LoadSavedResolution()
    {
        if (PlayerPrefs.HasKey(RESOLUTION_INDEX_KEY))
        {
            currentResolutionIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY);
            int savedWidth = PlayerPrefs.GetInt(RESOLUTION_WIDTH_KEY);
            int savedHeight = PlayerPrefs.GetInt(RESOLUTION_HEIGHT_KEY);

            // Validate saved resolution
            if (currentResolutionIndex < filteredResolutions.Count)
            {
                Resolution savedResolution = filteredResolutions[currentResolutionIndex];
                if (savedResolution.width == savedWidth && savedResolution.height == savedHeight)
                {
                    StartCoroutine(SafeSetResolution(savedResolution));
                    return;
                }
            }
            
            // If saved resolution is invalid, reset to default
            ResetToDefaultResolution();
        }
        else
        {
            // No saved resolution, use current resolution
            ResetToDefaultResolution();
        }
    }

    private void ResetToDefaultResolution()
    {
        currentResolutionIndex = filteredResolutions.FindIndex(r => 
            r.width == Screen.currentResolution.width && 
            r.height == Screen.currentResolution.height);
            
        if (currentResolutionIndex == -1)
            currentResolutionIndex = filteredResolutions.Count - 1; // Use highest available resolution
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= filteredResolutions.Count)
        {
            //Debug.LogError($"Invalid resolution index: {resolutionIndex}");
            return;
        }

        resolution = filteredResolutions[resolutionIndex];
        
        // Save the resolution settings
        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionIndex);
        PlayerPrefs.SetInt(RESOLUTION_WIDTH_KEY, resolution.width);
        PlayerPrefs.SetInt(RESOLUTION_HEIGHT_KEY, resolution.height);
        PlayerPrefs.Save();
        
        StartCoroutine(SafeSetResolution(resolution));
    }

    private IEnumerator SafeSetResolution(Resolution targetResolution)
    {
        Resolution originalResolution = Screen.currentResolution;
        FullScreenMode originalMode = Screen.fullScreenMode;
        int retryCount = 0;
        const int MAX_RETRIES = 3;

        while (retryCount < MAX_RETRIES)
        {
            bool success = true;
            
            // First set the resolution
            try
            {
                Screen.SetResolution(targetResolution.width, targetResolution.height, Screen.fullScreen);
            }
            catch (System.Exception e)
            {
                //Debug.LogError($"Error changing resolution (Attempt {retryCount + 1}/{MAX_RETRIES}): {e.Message}");
                success = false;
            }

            if (success)
            {
                yield return new WaitForSecondsRealtime(0.1f);

                // Then set the screen mode
                int screenMode = PlayerPrefs.GetInt("ScreenMode", 0);
                try
                {
                    switch (screenMode)
                    {
                        case 0: // Fullscreen
                            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                            break;
                        case 1: // Borderless Window
                            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                            break;
                        case 2: // Windowed
                            Screen.fullScreenMode = FullScreenMode.Windowed;
                            break;
                    }
                }
                catch (System.Exception e)
                {
                    //Debug.LogError($"Error changing screen mode (Attempt {retryCount + 1}/{MAX_RETRIES}): {e.Message}");
                    success = false;
                }
            }

            yield return new WaitForSecondsRealtime(0.5f);

            // Verify both resolution and screen mode
            if (success && 
                Screen.width == targetResolution.width && 
                Screen.height == targetResolution.height &&
                IsCorrectScreenMode(PlayerPrefs.GetInt("ScreenMode", 0)))
            {
                yield break; // Success!
            }

            retryCount++;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // If we get here, all attempts failed
        //Debug.LogWarning("Failed to set resolution after multiple attempts. Reverting to original resolution.");
        Screen.SetResolution(originalResolution.width, originalResolution.height, originalMode != FullScreenMode.Windowed);
        Screen.fullScreenMode = originalMode;

        // Update the UI to reflect the actual resolution
        currentResolutionIndex = filteredResolutions.FindIndex(r => 
            r.width == Screen.width && r.height == Screen.height);
        if (currentResolutionIndex != -1)
        {
            resolutionDropDown.value = currentResolutionIndex;
            resolutionDropDown.RefreshShownValue();
        }
    }

    private bool IsCorrectScreenMode(int expectedMode)
    {
        return expectedMode switch
        {
            0 => Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen,
            1 => Screen.fullScreenMode == FullScreenMode.FullScreenWindow,
            2 => Screen.fullScreenMode == FullScreenMode.Windowed,
            _ => false
        };
    }
}