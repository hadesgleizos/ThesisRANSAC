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
    private GameObject scoreScreen;

    void Awake()
    {
        scoreScreen = GameObject.FindGameObjectWithTag("ScoreScreen");
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            if (optionsMenu.activeSelf)
            {
                // If options menu is open, return to pause menu
                optionsMenu.SetActive(false);
                pauseMenu.SetActive(true);
            }
            else if (isPaused)
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
        optionsMenu.SetActive(false); // Ensure options menu is closed when pausing
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
        optionsMenu.SetActive(false); // Ensure options menu is also closed
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset pause state when a new scene loads
        isPaused = false;
        Time.timeScale = 1f;

        // Re-cache audio sources as they will be different in the new scene
        RefreshAudioSources();

        // Show cursor in main menu, hide in gameplay scenes
        if (scene.name == "MainMenu" ||  scoreScreen)
        {
            SetCursorState(true); // Show cursor in main menu & score screen
        }
        else
        {
            SetCursorState(false); // Hide cursor in gameplay scenes
        }
    }
}
