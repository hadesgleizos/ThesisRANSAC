using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // Audio Sources
    public AudioSource generalAudioSource;
    public AudioSource musicAudioSource;
    public AudioSource ambienceAudioSource;

    // Weapon Sounds
    public AudioClip pistolShootingSound;
    public AudioClip rifleShootingSound;
    public AudioClip reloadSound;
    public AudioClip emptyMagazineSound;
    public AudioClip horrorImpactSound;

    // Footstep Sounds
    public AudioClip dirtFootstep;
    public AudioClip grassFootstep;
    public AudioClip concreteFootstep;
    public AudioClip metalFootstep;

    // Background Music
    public AudioClip mainMenuMusic;
    public AudioClip[] stageAmbientSounds;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && generalAudioSource != null)
        {
            generalAudioSource.PlayOneShot(clip);
        }
    }

    public void PlayFootstep(string groundType)
    {
        AudioClip clipToPlay = null;

        switch (groundType)
        {
            case "Dirt":
                clipToPlay = dirtFootstep;
                break;
            case "Grass":
                clipToPlay = grassFootstep;
                break;
            case "Concrete":
                clipToPlay = concreteFootstep;
                break;
            case "Metal":
                clipToPlay = metalFootstep;
                break;
            default:
                break;
        }

        if (clipToPlay != null)
        {
            PlaySound(clipToPlay);
        }
    }

    public void PlayMainMenuMusic()
    {
        if (mainMenuMusic != null && musicAudioSource != null)
        {
            musicAudioSource.clip = mainMenuMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }
    }

    public void PlayStageAmbience(int stageIndex)
    {
        if (stageAmbientSounds != null && stageIndex < stageAmbientSounds.Length && ambienceAudioSource != null)
        {
            ambienceAudioSource.clip = stageAmbientSounds[stageIndex];
            ambienceAudioSource.loop = true;
            ambienceAudioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
        }
    }

    public void StopAmbience()
    {
        if (ambienceAudioSource != null)
        {
            ambienceAudioSource.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void SetAmbienceVolume(float volume)
    {
        if (ambienceAudioSource != null)
        {
            ambienceAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
}
