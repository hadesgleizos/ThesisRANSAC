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
}
