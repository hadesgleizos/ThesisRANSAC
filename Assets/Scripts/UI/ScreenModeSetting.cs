using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenModeSetting : MonoBehaviour
{
    private static ScreenModeSetting instance;

    private void Awake()
    {
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

    [SerializeField] private TMPro.TMP_Dropdown ScreenModeDropDown;    //check name!
    private Resolutions resolutions;
    // Start is called before the first frame update
    void Start()
    {
        resolutions = FindObjectOfType<Resolutions>();
        int val = PlayerPrefs.GetInt("ScreenMode");    //check name!
        ScreenModeDropDown.value = val;    //check name!
    }

    public void SetScreenMode(int index)    //check name!
    {
        PlayerPrefs.SetInt("ScreenMode", index);    //check name!
        if (index == 0)
        {
            // Fullscreen - Using FullScreenWindow for better stability
            Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, true);
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        if (index == 1)
        {
            // Borderless Window
            Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, true);
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        if (index == 2)
        {
            // Windowed
            Screen.SetResolution(resolutions.resolution.width, resolutions.resolution.height, false);
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }
}
