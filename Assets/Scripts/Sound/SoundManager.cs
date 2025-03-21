using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio; // Add this line

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioMixer masterMixer; // Add this line
    public static SoundManager Instance { get; private set; }

    // Audio Sources
    public AudioSource generalAudioSource;
    public AudioSource musicAudioSource;
    public AudioSource ambienceAudioSource;
    public AudioSource zombieAudioSource;
    public AudioSource bossAudioSource;  // Add this line for boss sounds

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

    // Zombie Sounds
    public AudioClip[] zombieIdleSounds;
    public AudioClip[] zombieAttackSounds;
    public AudioClip[] zombieDeathSounds;

    // Boss Sounds
    public AudioClip[] bossIdleSounds;
    public AudioClip[] bossAttackSounds;
    public AudioClip[] bossDeathSounds;

    // Add this with other audio clips
    [Header("Boss Music")]
    public AudioClip bossMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            SetupAudioSources();
        }
    }

    private void SetupAudioSources()
    {
        // Get all AudioSource components on this GameObject
        AudioSource[] audioSources = GetComponents<AudioSource>();
        
        // Set the output of each AudioSource to the master mixer
        if (masterMixer != null)
        {
            foreach (AudioSource source in audioSources)
            {
                source.outputAudioMixerGroup = masterMixer.FindMatchingGroups("Master")[0];
            }
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

    // Modify the PlayRandomZombieSound method
    public void PlayRandomZombieSound(AudioClip[] clips, float volumeMultiplier = 1f)
    {
        if (clips != null && clips.Length > 0 && zombieAudioSource != null)
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            zombieAudioSource.PlayOneShot(randomClip, volumeMultiplier);
        }
    }

    // Add this new method
    public void SetZombieVolume(float volume)
    {
        if (zombieAudioSource != null)
        {
            zombieAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    // Add this new method for boss sounds
    public void PlayRandomBossSound(AudioClip[] clips, float volumeMultiplier = 1f)
    {
        if (clips != null && clips.Length > 0 && bossAudioSource != null)
        {
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            bossAudioSource.PlayOneShot(randomClip, volumeMultiplier);
        }
    }

    public void SetBossVolume(float volume)
    {
        if (bossAudioSource != null)
        {
            bossAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void PlayBossMusic()
    {
        if (bossMusic != null && musicAudioSource != null)
        {
            StopMusic(); // Stop current music
            musicAudioSource.clip = bossMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }
    }
}
