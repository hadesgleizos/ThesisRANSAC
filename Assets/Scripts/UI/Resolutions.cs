using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Resolutions : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Dropdown resolutionDropDown;    //check name!

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;

    //private float currentRefreshRate;
    private int currentResolutionIndex = 0;
    [HideInInspector] public Resolution resolution;
    private void Start()
    {
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        resolutionDropDown.ClearOptions();
        //currentRefreshRate = Screen.currentResolution.refreshRate;

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (!filteredResolutions.Any(x => x.width == resolutions[i].width && x.height == resolutions[i].height))  //check if resolution already exists in list
            {
                filteredResolutions.Add(resolutions[i]);  //add resolution to list if it doesn't exist yet
            }
        }

        List<string> options = new List<string>();
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            string resolutionOption = filteredResolutions[i].width + " x " + filteredResolutions[i].height;
            options.Add(resolutionOption);
            if (filteredResolutions[i].width == Screen.width && filteredResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropDown.AddOptions(options);
        resolutionDropDown.value = currentResolutionIndex;
        resolutionDropDown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        resolution = filteredResolutions[resolutionIndex];
        StartCoroutine(SafeSetResolution(resolution));
    }

    private IEnumerator SafeSetResolution(Resolution targetResolution)
    {
        // Store original resolution in case we need to fallback
        Resolution originalResolution = Screen.currentResolution;
        FullScreenMode originalMode = Screen.fullScreenMode;

        try
        {
            // Apply new resolution based on screen mode
            if (PlayerPrefs.GetInt("ScreenMode") == 0)
            {
                // Fullscreen - Using FullScreenWindow instead of ExclusiveFullScreen for better stability
                Screen.SetResolution(targetResolution.width, targetResolution.height, true);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
            else if (PlayerPrefs.GetInt("ScreenMode") == 1)
            {
                // Borderless Window
                Screen.SetResolution(targetResolution.width, targetResolution.height, true);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
            else if (PlayerPrefs.GetInt("ScreenMode") == 2)
            {
                // Windowed
                Screen.SetResolution(targetResolution.width, targetResolution.height, false);
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error changing resolution: {e.Message}");
            Screen.SetResolution(originalResolution.width, originalResolution.height, originalMode != FullScreenMode.Windowed);
            Screen.fullScreenMode = originalMode;
        }

        // Wait a frame to let the resolution change take effect
        yield return new WaitForSecondsRealtime(0.5f);

        // Check if the resolution change was successful
        if (Screen.width != targetResolution.width || Screen.height != targetResolution.height)
        {
            Debug.LogWarning("Resolution change failed, reverting to original resolution");
            Screen.SetResolution(originalResolution.width, originalResolution.height, originalMode != FullScreenMode.Windowed);
            Screen.fullScreenMode = originalMode;
        }
    }
}