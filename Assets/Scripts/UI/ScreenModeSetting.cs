using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenModeSetting : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Dropdown ScreenModeDropDown; // Ensure this is assigned in Unity Inspector!
    private Resolutions resolutions;

    void Start()
    {
        resolutions = FindObjectOfType<Resolutions>();

        // Check for a previous bugged resolution state
        if (PlayerPrefs.GetInt("BuggedScreenMode", 0) == 1)
        {
            Debug.LogWarning("Bugged screen mode detected! Resetting to safe default (1280x720, Windowed).");
            PlayerPrefs.SetInt("ScreenMode", 2); // Default to Windowed
            PlayerPrefs.SetInt("BuggedScreenMode", 0); // Clear the flag
            PlayerPrefs.Save();
            ResetToSafeScreenMode();
        }

        int val = PlayerPrefs.GetInt("ScreenMode", 2); // Default to Windowed mode
        ScreenModeDropDown.value = val;
        ScreenModeDropDown.RefreshShownValue();
    }

    public void SetScreenMode(int index)
    {
        PlayerPrefs.SetInt("ScreenMode", index);
        PlayerPrefs.Save(); // Save the preference

        try
        {
            if (index == 0) // Borderless Fullscreen
            {
                Debug.Log("Setting Borderless Fullscreen");
                Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, true);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
            else if (index == 1) // Borderless Windowed
            {
                Debug.Log("Setting Borderless Windowed");
                Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, true);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
            else if (index == 2) // Windowed Mode (Fixed)
            {
                Debug.Log("Setting Windowed Mode");
                Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, false);
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }

            Debug.Log($"New Screen Mode: {Screen.fullScreenMode}, Resolution: {Screen.width}x{Screen.height}");
            PlayerPrefs.SetInt("BuggedScreenMode", 0); // Mark as valid
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Screen mode change failed! {e.Message}");
            PlayerPrefs.SetInt("BuggedScreenMode", 1); // Mark screen mode as bugged
            PlayerPrefs.Save();
            ResetToSafeScreenMode();
        }
    }

    private void ResetToSafeScreenMode()
    {
        Debug.LogWarning("Resetting to 1280x720 (Windowed) to avoid game crash.");

        Screen.SetResolution(1280, 720, false);
        Screen.fullScreenMode = FullScreenMode.Windowed;

        PlayerPrefs.SetInt("ScreenMode", 2); // Ensure Windowed mode
        PlayerPrefs.SetInt("BuggedScreenMode", 0);
        PlayerPrefs.Save();
    }
}
