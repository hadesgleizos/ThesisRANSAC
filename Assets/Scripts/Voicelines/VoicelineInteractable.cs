using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class VoicelineInteractable : Interactable
{
    [Header("Voiceline Settings")]
    [SerializeField] private string interactVoicelineId;
    [SerializeField] private float delayAfterVoiceline = 0.5f;
    
    [Header("Event Settings")]
    [SerializeField] private bool triggerSpawnerEvent = false;
    [SerializeField] private int eventIndex = 0;
    [SerializeField] private Transform[] customSpawnPoints;
    
    [Header("Progress Bar")]
    [SerializeField] private bool showProgressBar = false;
    [SerializeField] private GameObject progressBarObject;  // Changed from Image to GameObject
    [SerializeField] private bool useProgressBarComponent = true;  // New field to select which type of progress bar to use
    [SerializeField] private Image progressBarImage;  // Keep this for backward compatibility
    [SerializeField] private float progressDuration = 10f;
    [SerializeField] private string completionVoicelineId;
    
    [Header("Completion")]
    [SerializeField] private bool fadeScreenOnCompletion = false;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private bool showScoreScreen = false;
    
    [Header("Narrative Before Completion")]
    [SerializeField] private string[] narrativeVoicelineIds; // Array of voiceline IDs to play in sequence before score screen
    [SerializeField] private float narrativePauseBetween = 0.2f; // Pause between narrative voicelines
    
    [Header("Custom Events")]
    [SerializeField] private UnityEvent onInteractionStart;
    [SerializeField] private UnityEvent onProgressComplete;
    
    [Header("Interaction")]
    [SerializeField] private GameObject promptUI; // Assign your UI prompt in the inspector
    
    private bool interactionInProgress = false;
    private bool progressCompleted = false;
    private bool playerInRange = false;

    protected override void Interact()
    {
        if (interactionInProgress) return;
        
        interactionInProgress = true;
        onInteractionStart?.Invoke();
        
        StartCoroutine(InteractionSequence());
    }
    
    private IEnumerator InteractionSequence()
    {
        // Step 1: Play initial voiceline if specified
        if (!string.IsNullOrEmpty(interactVoicelineId) && Voicelines.Instance != null)
        {
            Voicelines.Instance.PlayVoiceline(interactVoicelineId);
            yield return new WaitForSeconds(delayAfterVoiceline);
        }
        
        // Step 2: Trigger spawner event if specified
        if (triggerSpawnerEvent && Spawner.Instance != null)
        {
            List<Transform> spawnPointsList = null;
            if (customSpawnPoints != null && customSpawnPoints.Length > 0)
            {
                spawnPointsList = new List<Transform>(customSpawnPoints);
            }
            
            Spawner.Instance.StartEvent(eventIndex, transform, spawnPointsList);
        }
        
        // Step 3: Show and fill progress bar if specified
        if (showProgressBar)
        {
            if (progressBarObject != null)
            {
                progressBarObject.SetActive(true);
                
                if (useProgressBarComponent)
                {
                    // Try to get either type of progress bar component
                    ProgressBar standardBar = progressBarObject.GetComponent<ProgressBar>();
                    ProgressBarCircle circleBar = progressBarObject.GetComponent<ProgressBarCircle>();
                    
                    float elapsedTime = 0f;
                    
                    // Progress animation loop
                    while (elapsedTime < progressDuration)
                    {
                        // Check if we're in a cooldown period - if so, wait until it's over
                        if (Spawner.Instance != null && Spawner.Instance.IsInCooldown())
                        {
                            yield return new WaitUntil(() => !Spawner.Instance.IsInCooldown());
                        }
                        
                        // Only increment time when not in cooldown
                        elapsedTime += Time.deltaTime;
                        float progress = (elapsedTime / progressDuration) * 100f; // Convert to 0-100 scale
                        
                        // Update the appropriate progress bar component
                        if (standardBar != null)
                        {
                            standardBar.BarValue = progress;
                        }
                        else if (circleBar != null)
                        {
                            circleBar.BarValue = progress;
                        }
                        
                        yield return null;
                    }
                    
                    // Ensure we hit 100%
                    if (standardBar != null)
                    {
                        standardBar.BarValue = 100;
                    }
                    else if (circleBar != null)
                    {
                        circleBar.BarValue = 100;
                    }
                }
                else if (progressBarImage != null)
                {
                    // Original implementation using just Image fill
                    progressBarImage.fillAmount = 0f;
                    
                    float elapsedTime = 0f;
                    while (elapsedTime < progressDuration)
                    {
                        // Check if we're in a cooldown period - if so, wait until it's over
                        if (Spawner.Instance != null && Spawner.Instance.IsInCooldown())
                        {
                            yield return new WaitUntil(() => !Spawner.Instance.IsInCooldown());
                        }
                        
                        // Only increment time when not in cooldown
                        elapsedTime += Time.deltaTime;
                        progressBarImage.fillAmount = elapsedTime / progressDuration;
                        yield return null;
                    }
                    
                    progressBarImage.fillAmount = 1f;
                }
                
                progressCompleted = true;
                
                // Play completion voiceline if specified
                if (!string.IsNullOrEmpty(completionVoicelineId) && Voicelines.Instance != null)
                {
                    Voicelines.Instance.PlayVoiceline(completionVoicelineId);
                    yield return new WaitForSeconds(delayAfterVoiceline);
                }
                
                // Invoke completion event
                onProgressComplete?.Invoke();
                
                // Hide progress bar after completion
                progressBarObject.SetActive(false);
            }
        }
        
        // Step 4: Fade out and show score screen if specified
        if (fadeScreenOnCompletion && progressCompleted)
        {
            yield return StartCoroutine(FadeOutAndShowScore());
        }
        
        interactionInProgress = false;
    }
    
    private IEnumerator FadeOutAndShowScore()
    {
        // Store the original cursor state to restore later if needed
        CursorLockMode originalLockState = Cursor.lockState;
        bool originalCursorVisible = Cursor.visible;
        
        // Disable player input at the start of the narrative sequence
        DisablePlayerInput();
        
        // Create a fullscreen canvas for fading
        GameObject fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // Lower sorting order so subtitles can appear on top
        
        // Add a black image that covers the screen
        GameObject fadeImage = new GameObject("FadeImage");
        fadeImage.transform.SetParent(fadeCanvas.transform, false);
        Image image = fadeImage.AddComponent<Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0); // Start transparent
        
        // Make the image cover the entire screen
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        // Ensure subtitle UI elements have higher sorting order
        EnsureSubtitlesVisibleDuringFade();
        
        // Fade to black
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }
        
        // Ensure the final frame is fully opaque
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1);
        
        // Play narrative voicelines (if any) while screen is black
        if (narrativeVoicelineIds != null && narrativeVoicelineIds.Length > 0 && Voicelines.Instance != null)
        {
            foreach (string voicelineId in narrativeVoicelineIds)
            {
                if (!string.IsNullOrEmpty(voicelineId))
                {
                    // Make sure subtitles are visible before playing
                    EnsureSubtitlesVisibleDuringFade();
                    
                    // Play each narrative voiceline
                    Voicelines.Instance.PlayVoiceline(voicelineId);
                    
                    // Wait for voiceline to complete
                    AudioClip clip = Voicelines.Instance.GetVoicelineClip(voicelineId);
                    if (clip != null)
                    {
                        // Wait for the clip duration plus a small buffer
                        yield return new WaitForSeconds(clip.length + narrativePauseBetween);
                    }
                    else
                    {
                        // If no clip found, just wait a default time
                        yield return new WaitForSeconds(3f + narrativePauseBetween);
                    }
                }
            }
        }
        
        // Add a small delay after all narrative voicelines
        yield return new WaitForSeconds(0.5f);
        
        // Show score screen if specified
        if (showScoreScreen && Spawner.Instance != null)
        {
            // Using the same approach as EventTrigger for consistency
            ShowScoreScreen(canvas);
            // Note: Player input remains disabled for the score screen
        }
        else
        {
            // If we're not showing the score screen, re-enable player input
            EnablePlayerInput();
            
            // Restore original cursor state only if not showing score screen
            Cursor.lockState = originalLockState;
            Cursor.visible = originalCursorVisible;
        }
    }

    // Add a separate method to handle showing the score screen, matching EventTrigger's approach
    private void ShowScoreScreen(Canvas fadeCanvas)
    {
        // Use the existing Spawner.ShowScoreScreen method
        if (Spawner.Instance != null)
        {
            // Call the ShowScoreScreen method directly instead of using SendMessage
            Spawner.Instance.ShowScoreScreen();
            
            // Find the score screen canvas and ensure it's in front
            if (Spawner.Instance.ScoreScreen != null)
            {
                Canvas scoreCanvas = Spawner.Instance.ScoreScreen.GetComponentInChildren<Canvas>();
                if (scoreCanvas != null)
                {
                    // Make sure score UI is in front of our fade canvas
                    scoreCanvas.sortingOrder = 10; // Higher than our fade canvas's sorting order
                }
                
                // Show and unlock the cursor when the score screen is displayed
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            
            // Change the sorting order of the fade canvas to be BEHIND the score UI
            fadeCanvas.sortingOrder = -1;
            
            // Player input remains disabled for score screen
            // (We don't call EnablePlayerInput() here)
        }
    }

    // Use the base class Update implementation to handle interactions
    protected override void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            BaseInteract();
        }
        
        // Call the base class Update which checks for input and triggers BaseInteract()
        base.Update();
        
        // You can still have additional behaviors here if needed
    }

    public void EnablePrompt(bool enabled)
    {
        playerInRange = enabled;
        if (promptUI != null)
        {
            promptUI.SetActive(enabled);
        }
    }

    // Helper method to ensure subtitle UI elements remain visible during fade
    private void EnsureSubtitlesVisibleDuringFade()
    {
        if (Voicelines.Instance != null)
        {
            // Find subtitle panel and text
            GameObject subtitlePanel = Voicelines.Instance.subtitlePanel;
            TMPro.TMP_Text subtitleText = Voicelines.Instance.subtitleText;
            
            if (subtitlePanel != null)
            {
                // Check if there's a Canvas component on the panel or its parent
                Canvas panelCanvas = subtitlePanel.GetComponentInParent<Canvas>();
                if (panelCanvas != null)
                {
                    // Set a higher sorting order to ensure it's above the fade
                    panelCanvas.sortingOrder = 10;
                    Debug.Log("Set subtitle panel canvas sorting order to 10");
                }
                else
                {
                    // If there's no Canvas already, add one
                    Canvas newCanvas = subtitlePanel.AddComponent<Canvas>();
                    newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    newCanvas.sortingOrder = 10;
                    
                    // Add a GraphicRaycaster to make the canvas functional
                    subtitlePanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    Debug.Log("Added new canvas to subtitle panel with sorting order 10");
                }
            }
            
            // Make sure the text is active and visible
            if (subtitleText != null)
            {
                subtitleText.gameObject.SetActive(true);
                
                // Make sure text has enough contrast against black background
                // Only if text color is too dark
                Color textColor = subtitleText.color;
                if (textColor.r < 0.5f && textColor.g < 0.5f && textColor.b < 0.5f)
                {
                    // Store original color to restore later if needed
                    subtitleText.color = new Color(0.9f, 0.9f, 0.9f, textColor.a);
                    Debug.Log("Adjusted subtitle text color for better visibility on black background");
                }
            }
        }
    }

    // Add these new methods to handle player input
    private void DisablePlayerInput()
    {
        // Disable the player's CharacterController
        CharacterController characterController = FindPlayerComponent<CharacterController>();
        if (characterController != null)
            characterController.enabled = false;
        
        // Disable player movement scripts
        PlayerMotor playerMotor = FindPlayerComponent<PlayerMotor>();
        if (playerMotor != null)
            playerMotor.enabled = false;
        
        // Disable InputManager
        InputManager inputManager = FindPlayerComponent<InputManager>();
        if (inputManager != null)
            inputManager.enabled = false;
        
        // Disable player look script
        PlayerLook playerLook = FindPlayerComponent<PlayerLook>();
        if (playerLook != null)
            playerLook.enabled = false;
        
        // For the legacy movement system, if you still use it
        playerMovement playerMovementScript = FindPlayerComponent<playerMovement>();
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;
        
        Debug.Log("Player input disabled for narrative sequence");
    }

    private void EnablePlayerInput()
    {
        // Only re-enable input if we're not showing the score screen
        if (!showScoreScreen)
        {
            // Re-enable the player's CharacterController
            CharacterController characterController = FindPlayerComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = true;
            
            // Re-enable player movement scripts
            PlayerMotor playerMotor = FindPlayerComponent<PlayerMotor>();
            if (playerMotor != null)
                playerMotor.enabled = true;
            
            // Re-enable InputManager
            InputManager inputManager = FindPlayerComponent<InputManager>();
            if (inputManager != null)
                inputManager.enabled = true;
            
            // Re-enable player look script
            PlayerLook playerLook = FindPlayerComponent<PlayerLook>();
            if (playerLook != null)
                playerLook.enabled = true;
            
            // For the legacy movement system, if you still use it
            playerMovement playerMovementScript = FindPlayerComponent<playerMovement>();
            if (playerMovementScript != null)
                playerMovementScript.enabled = true;
            
            Debug.Log("Player input re-enabled after narrative sequence");
        }
    }

    // Helper method to find components on the player GameObject
    private T FindPlayerComponent<T>() where T : Component
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.GetComponent<T>();
        }
        
        // Try the MainCamera tag as well (since your project uses this for player detection)
        GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
        if (camera != null)
        {
            // Try to get from the camera directly
            T component = camera.GetComponent<T>();
            if (component != null)
                return component;
            
            // If not on camera, try its parent
            if (camera.transform.parent != null)
                return camera.transform.parent.GetComponent<T>();
        }
        
        return null;
    }
}