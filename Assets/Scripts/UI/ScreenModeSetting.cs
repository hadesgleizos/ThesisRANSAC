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

        int val = PlayerPrefs.GetInt("ScreenMode", 2); // Default to Windowed mode if not set
        ScreenModeDropDown.value = val;
        ScreenModeDropDown.RefreshShownValue();
    }

    public void SetScreenMode(int index)
    {
        PlayerPrefs.SetInt("ScreenMode", index);
        PlayerPrefs.Save(); // Save the preference

        if (index == 0) // Borderless Fullscreen (was 4K full before)
        {
            Debug.Log("Setting Borderless Fullscreen");
            Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, true);
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else if (index == 1) // Borderless Windowed
        {
            Debug.Log("Setting Borderless");
            Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, true);
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow; // Change Exclusive to Borderless
        }
        else if (index == 2) // Windowed Mode (Fixed)
        {
            Debug.Log("Setting Windowed Mode");
            Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, false);
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }

        Debug.Log($"New Screen Mode: {Screen.fullScreenMode}, Resolution: {Screen.width}x{Screen.height}");
    }
}
