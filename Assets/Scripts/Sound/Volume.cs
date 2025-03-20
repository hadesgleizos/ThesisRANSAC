using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class Volume : MonoBehaviour
{
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private Slider volumeSlider;
    
    private const string VOLUME_KEY = "MasterVolume";
    private const float MIN_VOLUME = -80f;

    void Start()
    {
        InitializeVolumeControl();
    }

    private void InitializeVolumeControl()
    {
        if (volumeSlider != null)
        {
            // Set slider range (0-1 for easier user understanding)
            volumeSlider.minValue = 0.0001f; // Avoid log10(0)
            volumeSlider.maxValue = 1f;
            
            // Load saved volume or use default
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
            volumeSlider.value = savedVolume;
            
            // Set initial volume
            if (masterMixer != null)
            {
                SetMixerVolume(savedVolume);
            }
            
            // Add listener for volume changes
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        else
        {
            // Even if there's no slider, still apply the saved volume to the mixer
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
            if (masterMixer != null)
            {
                SetMixerVolume(savedVolume);
            }
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (masterMixer != null)
        {
            SetMixerVolume(value);
        }
        PlayerPrefs.SetFloat(VOLUME_KEY, value);
        PlayerPrefs.Save();
    }

    private void SetMixerVolume(float value)
    {
        // Convert linear volume (0-1) to decibels (-80db to 0db)
        float dB = value > 0 ? Mathf.Log10(value) * 20 : MIN_VOLUME;
        masterMixer.SetFloat("MasterVolume", dB);
    }
}
