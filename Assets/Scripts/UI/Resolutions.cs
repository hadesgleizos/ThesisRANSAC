using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Resolutions : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Dropdown resolutionDropDown; // Ensure this is assigned in Unity Inspector!

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private int currentResolutionIndex = 0;
    [HideInInspector] public Resolution resolution;

    private void Start()
    {
        // If ScreenMode doesn't exist, set it to Windowed mode (2)
        if (!PlayerPrefs.HasKey("ScreenMode"))
        {
            PlayerPrefs.SetInt("ScreenMode", 2);
            PlayerPrefs.Save();
        }

        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        resolutionDropDown.ClearOptions();

        // Populate available resolutions (avoiding duplicates)
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

            if (filteredResolutions[i].width == Screen.width && filteredResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropDown.AddOptions(options);
        resolutionDropDown.value = currentResolutionIndex;
        resolutionDropDown.RefreshShownValue();

        // Check if the previous resolution setting caused a bug
        if (PlayerPrefs.GetInt("BuggedResolution", 0) == 1)
        {
            Debug.LogWarning("Detected bugged resolution! Resetting to safe default (1280x720, Windowed).");
            PlayerPrefs.SetInt("ScreenMode", 2); // Windowed
            PlayerPrefs.SetInt("BuggedResolution", 0); // Clear bug flag
            PlayerPrefs.Save();
            ResetToSafeResolution();
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        resolution = filteredResolutions[resolutionIndex];
        FullScreenMode selectedMode = (FullScreenMode)PlayerPrefs.GetInt("ScreenMode");

        Debug.Log($"Setting resolution to {resolution.width}x{resolution.height} in mode {selectedMode}");

        try
        {
            // Apply new settings
            Screen.SetResolution(resolution.width, resolution.height, selectedMode != FullScreenMode.Windowed);
            Screen.fullScreenMode = selectedMode;

            Debug.Log($"Resolution applied successfully: {Screen.width}x{Screen.height}");
            Debug.Log($"Current fullscreen mode: {Screen.fullScreenMode}");

            // Mark resolution as valid
            PlayerPrefs.SetInt("BuggedResolution", 0);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Resolution change failed! {e.Message}");
            PlayerPrefs.SetInt("BuggedResolution", 1); // Mark resolution as bugged
            PlayerPrefs.Save();
            ResetToSafeResolution();
        }
    }

    private void ResetToSafeResolution()
    {
        Debug.LogWarning("Resetting to 1280x720 (Windowed) to avoid game crash.");

        Screen.SetResolution(1280, 720, false);
        Screen.fullScreenMode = FullScreenMode.Windowed;

        PlayerPrefs.SetInt("ScreenMode", 2); // Ensure Windowed mode
        PlayerPrefs.SetInt("BuggedResolution", 0);
        PlayerPrefs.Save();
    }
}
