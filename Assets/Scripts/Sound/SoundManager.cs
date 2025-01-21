using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // General Sounds
    public AudioSource generalAudioSource;

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
}
