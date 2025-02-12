using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class Resolutions : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resolutionDropDown; // Assign in the Inspector!

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private int currentResolutionIndex = 0;

    [HideInInspector] 
    public Resolution resolution;

    private void Start()
    {
        // If "ScreenMode" doesn't exist, set a default. 
        //   3 = Windowed, 2 = MaximizedWindow, 1 = ExclusiveFullScreen, 0 = FullScreenWindow
        // Change this default to 3 if you really want standard windowed mode:
        if (!PlayerPrefs.HasKey("ScreenMode"))
        {
            PlayerPrefs.SetInt("ScreenMode", 2); 
            PlayerPrefs.Save();
        }

        // Gather all resolutions from the system
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        // Clear any existing options in the dropdown
        resolutionDropDown.ClearOptions();

        // Populate unique resolutions (avoid duplicates of same width x height)
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution r = resolutions[i];
            if (!filteredResolutions.Any(x => x.width == r.width && x.height == r.height))
            {
                filteredResolutions.Add(r);
            }
        }

        // Build a list of displayable option strings
        List<string> options = new List<string>();
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            Resolution r = filteredResolutions[i];
            string option = r.width + " x " + r.height;
            options.Add(option);

            // If this resolution matches the current screen size, store the index
            if (r.width == Screen.width && r.height == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        // Add those resolution options to the dropdown UI
        resolutionDropDown.AddOptions(options);
        resolutionDropDown.value = currentResolutionIndex;
        resolutionDropDown.RefreshShownValue();

        // Check if the previous resolution was flagged as "bugged"
        if (PlayerPrefs.GetInt("BuggedResolution", 0) == 1)
        {
            Debug.LogWarning("Detected bugged resolution! Resetting to safe default (1280x720, Windowed).");
            PlayerPrefs.SetInt("ScreenMode", 2); // or 3 if you want true Windowed
            PlayerPrefs.SetInt("BuggedResolution", 0);
            PlayerPrefs.Save();
            ResetToSafeResolution();
        }

        // IMPORTANT: Wire up the dropdown to call SetResolution when the user changes it
        resolutionDropDown.onValueChanged.AddListener(SetResolution);
    }

    /// <summary>
    /// Called whenever the user picks a new resolution in the dropdown.
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        resolution = filteredResolutions[resolutionIndex];
        
        // Retrieve the desired screen mode from PlayerPrefs 
        FullScreenMode selectedMode = (FullScreenMode)PlayerPrefs.GetInt("ScreenMode");
        
        Debug.Log($"Setting resolution to {resolution.width}x{resolution.height} in mode {selectedMode}");

        try
        {
            // Apply the new resolution + "isFullScreen" based on whether it's Windowed
            bool isFullscreen = (selectedMode != FullScreenMode.Windowed);
            Screen.SetResolution(resolution.width, resolution.height, isFullscreen);

            // Then explicitly set the full screen mode
            Screen.fullScreenMode = selectedMode;

            Debug.Log($"Resolution applied successfully: {Screen.width}x{Screen.height}");
            Debug.Log($"Current fullscreen mode: {Screen.fullScreenMode}");

            // Mark resolution as valid (not bugged)
            PlayerPrefs.SetInt("BuggedResolution", 0);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Resolution change failed! {e.Message}");
            // Mark resolution as bugged and reset
            PlayerPrefs.SetInt("BuggedResolution", 1);
            PlayerPrefs.Save();
            ResetToSafeResolution();
        }
    }

    /// <summary>
    /// Resets to a guaranteed-safe resolution (1280x720 windowed).
    /// </summary>
    private void ResetToSafeResolution()
    {
        Debug.LogWarning("Resetting to 1280x720 (Windowed) to avoid game crash.");

        Screen.SetResolution(1280, 720, false);
        Screen.fullScreenMode = FullScreenMode.Windowed;

        // Store our fallback as mode = 3 (Windowed)
        PlayerPrefs.SetInt("ScreenMode", 3);
        PlayerPrefs.SetInt("BuggedResolution", 0);
        PlayerPrefs.Save();
    }
}
