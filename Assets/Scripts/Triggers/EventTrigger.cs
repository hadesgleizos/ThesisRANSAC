using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class EventTrigger : MonoBehaviour
{
    [Header("Event Settings")]
    public int eventIndex = 0;
    public bool triggerOnce = true;
    public bool requireKeyPress = true;
    public KeyCode triggerKey = KeyCode.E;
    
    [Header("UI")]
    public GameObject interactionPrompt;
    
    [Header("Custom Spawn Points")]
    public List<Transform> spawnPoints;
    
    [Header("Portal Settings")]
    public bool isPortal = false;
    public float fadeOutDuration = 1.0f;
    public Color fadeColor = Color.black;
    
    [Header("Audio")]
    public AudioClip portalSound;
    [Range(0f, 1f)]
    public float portalSoundVolume = 1f;
    
    [Header("Voiceline")]
    public bool playVoiceline = false;
    public string voicelineId = "";
    public float delayAfterVoiceline = 0.5f;
    
    private bool playerInTrigger = false;
    private bool hasTriggered = false;
    private Canvas fadeCanvas;
    private GameObject fadePanel;
    
    private void Start()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // Create fade canvas if this is a portal
        if (isPortal)
        {
            CreateFadeCanvas();
        }
    }
    
    private void CreateFadeCanvas()
    {
        // Create a canvas that overlays everything for the fade effect
        GameObject canvasObj = new GameObject("FadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Make sure it's in front of everything
        
        // Add a canvas scaler for proper UI scaling
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Create a panel that fills the screen
        fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(fadeCanvas.transform, false);
        
        RectTransform rectTransform = fadePanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero; // Fill the whole canvas
        
        UnityEngine.UI.Image image = fadePanel.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Start fully transparent
        
        // Deactivate it until needed
        fadeCanvas.gameObject.SetActive(false);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera") && (!hasTriggered || !triggerOnce))
        {
            playerInTrigger = true;
            if (interactionPrompt != null && requireKeyPress)
            {
                interactionPrompt.SetActive(true);
            }
            else if (!requireKeyPress)
            {
                TriggerEvent();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }
    }
    
    private void Update()
    {
        if (playerInTrigger && requireKeyPress && Input.GetKeyDown(triggerKey))
        {
            TriggerEvent();
        }
    }
    
    private void TriggerEvent()
    {
        if (hasTriggered && triggerOnce) return;
        
        // Handle different trigger types
        if (isPortal)
        {
            ActivatePortal();
        }
        else if (Spawner.Instance != null)
        {
            // Start event with spawner as before
            Spawner.Instance.StartEvent(eventIndex, transform, spawnPoints);
            
            if (triggerOnce)
            {
                hasTriggered = true;
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("Spawner instance not found!");
        }
    }
    
    private void ActivatePortal()
    {
        hasTriggered = true;
        
        // Hide interaction prompt if it exists
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
        
        // Play portal sound if assigned
        if (portalSound != null)
        {
            AudioSource.PlayClipAtPoint(portalSound, transform.position, portalSoundVolume);
        }
        
        // Check if we need to play a voiceline
        if (playVoiceline && !string.IsNullOrEmpty(voicelineId) && Voicelines.Instance != null)
        {
            Voicelines.Instance.PlayVoiceline(voicelineId);
            StartCoroutine(FadeOutAfterDelay(delayAfterVoiceline));
        }
        else
        {
            StartCoroutine(FadeOutAndShowScore());
        }
    }
    
    private IEnumerator FadeOutAfterDelay(float delay)
    {
        // Wait for specified delay (to let voiceline play)
        yield return new WaitForSeconds(delay);
        
        // Start fade out
        StartCoroutine(FadeOutAndShowScore());
    }
    
    private IEnumerator FadeOutAndShowScore()
    {
        // Activate the fade canvas
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
            UnityEngine.UI.Image image = fadePanel.GetComponent<UnityEngine.UI.Image>();
            
            // Fade from transparent to fadeColor
            float elapsedTime = 0f;
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / fadeOutDuration);
                image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }
            
            // Ensure the fade is complete
            image.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            
            // Wait a bit after complete fade
            yield return new WaitForSeconds(0.2f);
            
            // Show the score screen
            ShowScoreScreen();
            
            // Change the sorting order of the fade canvas to be BEHIND the score UI
            // Assuming score screen UI is using Canvas with sorting order higher than 0
            fadeCanvas.sortingOrder = -1;
        }
        else
        {
            // Just show the score screen if no fade canvas
            ShowScoreScreen();
        }
    }
    
    // Extracted ShowScoreScreen method for better organization
    private void ShowScoreScreen()
    {
        // Use the existing Spawner.ShowScoreScreen method
        if (Spawner.Instance != null)
        {
            // Call the ShowScoreScreen method
            Spawner.Instance.SendMessage("ShowScoreScreen", SendMessageOptions.DontRequireReceiver);
            
            // Find the score screen canvas and ensure it's in front
            if (Spawner.Instance.ScoreScreen != null)
            {
                Canvas scoreCanvas = Spawner.Instance.ScoreScreen.GetComponentInChildren<Canvas>();
                if (scoreCanvas != null)
                {
                    // Make sure score UI is in front of our fade canvas
                    scoreCanvas.sortingOrder = 10; // Higher than our fade canvas's new sorting order
                }
                
                // Show and unlock the cursor when the score screen is displayed
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
    
    // Clean up resources when this object is destroyed
    private void OnDestroy()
    {
        if (fadeCanvas != null)
        {
            Destroy(fadeCanvas.gameObject);
        }
    }
}