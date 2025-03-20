using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public GameObject pauseMenu;
    public GameObject optionsMenu;
    public static bool isPaused;
    private AudioSource[] allAudioSources;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        pauseMenu.SetActive(false);
        // Cache all audio sources in the scene
        allAudioSources = FindObjectsOfType<AudioSource>();
        // Hide cursor at start
        SetCursorState(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Don't process escape key if component is disabled
        if (!enabled) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        SetCursorState(true);
        
        // Pause all audio sources
        foreach (AudioSource source in allAudioSources)
        {
            if (source.isPlaying)
            {
                source.Pause();
            }
        }
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        SetCursorState(false);

        // Resume all paused audio sources
        foreach (AudioSource source in allAudioSources)
        {
            source.UnPause();
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        isPaused = false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public static bool IsGamePaused()
    {
        return isPaused;
    }

    private void RefreshAudioSources()
    {
        allAudioSources = FindObjectsOfType<AudioSource>();
    }

    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
