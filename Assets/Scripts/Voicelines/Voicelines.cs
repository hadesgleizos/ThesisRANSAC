using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Voicelines : MonoBehaviour
{
    public static Voicelines Instance { get; private set; }

    [System.Serializable]
    public class VoicelineClip
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [TextArea(1, 3)]
        public string subtitleText;
        public float subtitleDuration = 3f;
        public bool interruptable = false;
        
        // New fields for multi-part subtitles
        public bool useMultiPartSubtitles = false;
        [TextArea(1, 3)]
        public string[] subtitleParts;
        public float[] partDurations; // Duration for each part
        public bool useTypingEffect = false;
        public float typingSpeed = 30f; // Characters per second
        
        // New field for sequence actions
        public UnityEvent onComplete;
    }

    // New class for multiple sequential voicelines
    [System.Serializable]
    public class VoicelineSequence
    {
        public string sequenceId;
        
        [System.Serializable]
        public class SequenceEntry
        {
            public string voicelineId;
            public float delayAfterVoiceline = 0.5f;
            public UnityEvent actionsAfterVoiceline;
        }
        
        public List<SequenceEntry> entries = new List<SequenceEntry>();
        public UnityEvent actionsAfterSequence;
    }
    
    [Header("Voiceline Settings")]
    public List<VoicelineClip> voicelineClips = new List<VoicelineClip>();
    public AudioSource audioSource;
    public TMPro.TMP_Text subtitleText;
    
    [Header("Voiceline Sequences")]
    public List<VoicelineSequence> voicelineSequences = new List<VoicelineSequence>();
    
    [Header("Subtitle Display")]
    public float defaultSubtitleDuration = 3f;
    public GameObject subtitlePanel;

    [Header("Default Voiceline IDs")]
    [Tooltip("Fallback ID if no specific ID is set in the spawner")]
    public string defaultFinalWaveCompleteId = "final_wave_complete";
    [Tooltip("Fallback ID for regular wave start")]
    public string defaultWaveStartId = "";
    [Tooltip("Fallback ID for regular wave end")]
    public string defaultWaveEndId = "";

    private Dictionary<string, VoicelineClip> voicelineDict = new Dictionary<string, VoicelineClip>();
    private Dictionary<string, VoicelineSequence> sequenceDict = new Dictionary<string, VoicelineSequence>();
    private Coroutine currentVoicelineCoroutine;
    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Remove this line:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize dictionaries for faster lookups
        foreach (VoicelineClip clip in voicelineClips)
        {
            if (!string.IsNullOrEmpty(clip.id) && !voicelineDict.ContainsKey(clip.id))
            {
                voicelineDict.Add(clip.id, clip);
            }
        }
        
        foreach (VoicelineSequence sequence in voicelineSequences)
        {
            if (!string.IsNullOrEmpty(sequence.sequenceId) && !sequenceDict.ContainsKey(sequence.sequenceId))
            {
                sequenceDict.Add(sequence.sequenceId, sequence);
            }
        }

        // Initially hide subtitle panel if it exists
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Just use the regular setup method - don't look for PersistentCanvas
        SetupUIReferences();
        
        // Log for debugging
        Debug.Log("Voicelines initialized with local references");
    }

    private void OnEnable()
    {
        // Subscribe to Spawner events
        Spawner.OnWaveStart += HandleWaveStart;
        Spawner.OnWaveEnd += HandleWaveEnd;
        
        // Subscribe to scene loading events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from Spawner events
        Spawner.OnWaveStart -= HandleWaveStart;
        Spawner.OnWaveEnd -= HandleWaveEnd;
        
        // Unsubscribe from scene loading events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // This will be called whenever a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, refreshing voiceline UI references");
        // Give a small delay to ensure all objects are initialized
        StartCoroutine(SetupUIReferencesDelayed());
    }

    private IEnumerator SetupUIReferencesDelayed()
    {
        // Wait for the end of the frame to ensure all objects are initialized
        yield return new WaitForEndOfFrame();
        
        // Re-initialize UI references if they're missing
        SetupUIReferences();
    }

    // Method to set up UI references
    public void SetupUIReferences()
    {
        // Check if we've lost our subtitle text reference
        if (subtitleText == null)
        {
            // Try to find by tag first
            GameObject subtitleTextObj = GameObject.FindWithTag("SubtitleText");
            if (subtitleTextObj != null)
            {
                subtitleText = subtitleTextObj.GetComponent<TMPro.TMP_Text>();
                Debug.Log("Found subtitle text by tag");
            }
            else
            {
                // Try to find by name as fallback
                GameObject foundByName = GameObject.Find("SubtitleText");
                if (foundByName != null)
                {
                    subtitleText = foundByName.GetComponent<TMPro.TMP_Text>();
                    Debug.Log("Found subtitle text by name");
                }
                else
                {
                    Debug.LogWarning("Could not find subtitle text object");
                }
            }
        }
        
        // Check if we've lost our subtitle panel reference
        if (subtitlePanel == null)
        {
            // Try to find by tag first
            GameObject subtitlePanelObj = GameObject.FindWithTag("SubtitlePanel");
            if (subtitlePanelObj != null)
            {
                subtitlePanel = subtitlePanelObj;
                Debug.Log("Found subtitle panel by tag");
            }
            else
            {
                // Try to find by name as fallback
                GameObject foundByName = GameObject.Find("SubtitlePanel");
                if (foundByName != null)
                {
                    subtitlePanel = foundByName;
                    Debug.Log("Found subtitle panel by name");
                }
                else
                {
                    Debug.LogWarning("Could not find subtitle panel object");
                }
            }
        }
        
        // Initialize panel state
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
        
        // If no audio source is assigned, try to get one
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Created new audio source for voicelines");
            }
        }
        
        // Log the current status of references
        Debug.Log($"Voicelines references - Panel: {(subtitlePanel != null ? "Found" : "Missing")}, " +
                  $"Text: {(subtitleText != null ? "Found" : "Missing")}, " +
                  $"Audio: {(audioSource != null ? "Found" : "Missing")}");
    }

    // Handle wave start event
    private void HandleWaveStart(int waveNumber)
    {
        // Only handle if we have a Spawner reference
        if (Spawner.Instance == null) return;

        // Check if there's an active event
        if (Spawner.Instance.IsEventActive())
        {
            // Get the current event
            SpawnEvent currentEvent = Spawner.Instance.GetCurrentEvent();
            if (currentEvent != null)
            {
                // Check if there's a specific voiceline for this wave
                if (currentEvent.waveStartVoicelineIds != null && 
                    waveNumber <= currentEvent.waveStartVoicelineIds.Length && 
                    waveNumber > 0 &&
                    !string.IsNullOrEmpty(currentEvent.waveStartVoicelineIds[waveNumber - 1]))
                {
                    PlayVoiceline(currentEvent.waveStartVoicelineIds[waveNumber - 1]);
                    return;
                }
            }
        }
        else if (!string.IsNullOrEmpty(defaultWaveStartId))
        {
            // If no event-specific ID but we have a default, use it
            PlayVoiceline(defaultWaveStartId);
        }
    }

    // Handle wave end event from Spawner
    private void HandleWaveEnd(int waveNumber)
    {
        // Only handle if we have a Spawner reference
        if (Spawner.Instance == null) return;

        // Check if this is an event wave or regular wave
        if (Spawner.Instance.IsEventActive())
        {
            // Get the current event
            SpawnEvent currentEvent = Spawner.Instance.GetCurrentEvent();
            if (currentEvent != null)
            {
                // Check if this is the last wave of the event
                if (waveNumber == currentEvent.eventWaves)
                {
                    // Check if using sequence or single voiceline
                    if (currentEvent.useVoicelineSequenceOnComplete && 
                        !string.IsNullOrEmpty(currentEvent.completeSequenceId))
                    {
                        PlayVoicelineSequence(currentEvent.completeSequenceId);
                    }
                    else if (!string.IsNullOrEmpty(currentEvent.completeVoicelineId))
                    {
                        // Play the event completion voiceline if specified
                        PlayVoiceline(currentEvent.completeVoicelineId);
                    }
                    return;
                }

                // Check if there's a specific voiceline for this wave
                if (currentEvent.waveEndVoicelineIds != null && 
                    waveNumber <= currentEvent.waveEndVoicelineIds.Length &&
                    waveNumber > 0 &&
                    !string.IsNullOrEmpty(currentEvent.waveEndVoicelineIds[waveNumber - 1]))
                {
                    PlayVoiceline(currentEvent.waveEndVoicelineIds[waveNumber - 1]);
                    return;
                }
            }
        }
        else if (waveNumber == Spawner.Instance.totalWaves)
        {
            // Final regular wave - use the default final wave voiceline
            PlayVoiceline(defaultFinalWaveCompleteId);
        }
        else if (!string.IsNullOrEmpty(defaultWaveEndId))
        {
            // Regular wave end - use the default wave end voiceline if available
            PlayVoiceline(defaultWaveEndId);
        }
    }

    // Play a voiceline by ID
    public void PlayVoiceline(string voicelineId)
    {
        if (string.IsNullOrEmpty(voicelineId))
        {
            // Skip if no ID provided
            return;
        }

        if (voicelineDict.TryGetValue(voicelineId, out VoicelineClip clip))
        {
            if (isPlaying && !clip.interruptable)
            {
                // Don't interrupt the current voiceline unless allowed
                return;
            }

            // Stop any current voiceline
            if (currentVoicelineCoroutine != null)
            {
                StopCoroutine(currentVoicelineCoroutine);
            }

            // Start new voiceline
            currentVoicelineCoroutine = StartCoroutine(PlayVoicelineCoroutine(clip));
        }
        else
        {
            Debug.LogWarning($"Voiceline with ID '{voicelineId}' not found.");
        }
    }

    // Play a multi-part voiceline dynamically
    public void PlayMultiPartVoiceline(string voicelineId, string[] subtitleParts, float[] partDurations = null, bool useTyping = true, float typingSpeed = 30f)
    {
        if (voicelineDict.TryGetValue(voicelineId, out VoicelineClip clip))
        {
            // Create a temporary clone of the clip to modify it
            VoicelineClip tempClip = new VoicelineClip
            {
                id = clip.id,
                clip = clip.clip,
                volume = clip.volume,
                interruptable = clip.interruptable,
                useMultiPartSubtitles = true,
                subtitleParts = subtitleParts,
                partDurations = partDurations,
                useTypingEffect = useTyping,
                typingSpeed = typingSpeed
            };
            
            // Stop any current voiceline if needed
            if (isPlaying && !tempClip.interruptable)
            {
                return;
            }

            if (currentVoicelineCoroutine != null)
            {
                StopCoroutine(currentVoicelineCoroutine);
            }

            // Start new voiceline with our temporary settings
            currentVoicelineCoroutine = StartCoroutine(PlayVoicelineCoroutine(tempClip));
        }
        else
        {
            Debug.LogWarning($"Voiceline with ID '{voicelineId}' not found.");
        }
    }

    // Add method to play a sequence
    public void PlayVoicelineSequence(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId))
        {
            return;
        }

        if (sequenceDict.TryGetValue(sequenceId, out VoicelineSequence sequence))
        {
            StopCurrentVoiceline(); // Stop any currently playing voiceline
            StartCoroutine(PlaySequenceCoroutine(sequence));
        }
        else
        {
            Debug.LogWarning($"Voiceline sequence with ID '{sequenceId}' not found.");
        }
    }
    
    private IEnumerator PlaySequenceCoroutine(VoicelineSequence sequence)
    {
        // Play each voiceline in the sequence
        foreach (var entry in sequence.entries)
        {
            // Play the individual voiceline
            if (!string.IsNullOrEmpty(entry.voicelineId) && voicelineDict.TryGetValue(entry.voicelineId, out VoicelineClip clip))
            {
                // Play the voiceline and wait for it to complete
                yield return PlayVoicelineAndWait(clip);
                
                // Invoke any actions attached to this entry
                entry.actionsAfterVoiceline?.Invoke();
                
                // Wait for specified delay before next voiceline
                if (entry.delayAfterVoiceline > 0)
                {
                    yield return new WaitForSeconds(entry.delayAfterVoiceline);
                }
            }
        }
        
        // Invoke actions after the entire sequence is complete
        sequence.actionsAfterSequence?.Invoke();
    }
    
    // Helper method to play a voiceline and wait for it to complete
    private IEnumerator PlayVoicelineAndWait(VoicelineClip clip)
    {
        isPlaying = true;
        
        // Play audio
        audioSource.clip = clip.clip;
        audioSource.volume = clip.volume;
        audioSource.Play();
        
        // Handle subtitles
        if (subtitleText != null)
        {
            if (subtitlePanel != null)
            {
                subtitlePanel.SetActive(true);
            }
            
            // Use existing logic for multi-part subtitles
            if (clip.useMultiPartSubtitles && clip.subtitleParts != null && clip.subtitleParts.Length > 0)
            {
                // Calculate default duration if audio clip exists
                float totalDuration = clip.clip != null ? clip.clip.length : defaultSubtitleDuration;
                float defaultPartDuration = totalDuration / clip.subtitleParts.Length;
                
                // Display each part in sequence
                for (int i = 0; i < clip.subtitleParts.Length; i++)
                {
                    // Clear previous text
                    subtitleText.text = "";
                    
                    string currentPart = clip.subtitleParts[i];
                    
                    // Determine duration for this part
                    float partDuration;
                    if (clip.partDurations != null && i < clip.partDurations.Length && clip.partDurations[i] > 0)
                    {
                        partDuration = clip.partDurations[i];
                    }
                    else
                    {
                        partDuration = defaultPartDuration;
                    }
                    
                    // Use typing effect if enabled
                    if (clip.useTypingEffect)
                    {
                        yield return StartCoroutine(TypeText(currentPart, clip.typingSpeed));
                        
                        // Wait for the remaining time after typing is complete
                        float typingDuration = currentPart.Length / clip.typingSpeed;
                        float remainingTime = partDuration - typingDuration;
                        if (remainingTime > 0)
                        {
                            yield return new WaitForSeconds(remainingTime);
                        }
                    }
                    else
                    {
                        // Set text immediately
                        subtitleText.text = currentPart;
                        yield return new WaitForSeconds(partDuration);
                    }
                }
                
                // Clear the subtitle text after all parts are shown
                subtitleText.text = "";
            }
            else if (!string.IsNullOrEmpty(clip.subtitleText))
            {
                // Original single subtitle behavior
                if (clip.useTypingEffect)
                {
                    yield return StartCoroutine(TypeText(clip.subtitleText, clip.typingSpeed));
                }
                else
                {
                    subtitleText.text = clip.subtitleText;
                }
                
                // Wait for audio to finish or use specified duration
                float duration = clip.subtitleDuration > 0 ? clip.subtitleDuration : 
                                (clip.clip != null ? clip.clip.length : defaultSubtitleDuration);
                
                yield return new WaitForSeconds(duration);
                
                // Hide subtitle
                subtitleText.text = "";
            }
            
            if (subtitlePanel != null)
            {
                subtitlePanel.SetActive(false);
            }
        }
        else if (clip.clip != null)
        {
            // If no subtitle, just wait for audio to finish
            yield return new WaitForSeconds(clip.clip.length);
        }
        
        // Invoke any actions attached to this clip
        clip.onComplete?.Invoke();
        
        isPlaying = false;
    }
    
    // Modified version of original PlayVoicelineCoroutine to use the shared code
    private IEnumerator PlayVoicelineCoroutine(VoicelineClip clip)
    {
        yield return PlayVoicelineAndWait(clip);
    }

    // Typing effect for text
    private IEnumerator TypeText(string text, float charactersPerSecond)
    {
        subtitleText.text = "";
        
        // Calculate time per character
        float secondsPerChar = 1f / charactersPerSecond;
        
        // Display one character at a time
        for (int i = 0; i < text.Length; i++)
        {
            subtitleText.text += text[i];
            yield return new WaitForSeconds(secondsPerChar);
        }
    }

    // Method to stop current voiceline
    public void StopCurrentVoiceline()
    {
        if (currentVoicelineCoroutine != null)
        {
            StopCoroutine(currentVoicelineCoroutine);
        }
        
        audioSource.Stop();
        
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }
        
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
        
        isPlaying = false;
    }

    // Public methods to reset references
    public void SetSubtitleText(TMPro.TMP_Text text)
    {
        subtitleText = text;
        Debug.Log("Subtitle text reference set manually");
    }

    public void SetSubtitlePanel(GameObject panel)
    {
        subtitlePanel = panel;
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
        Debug.Log("Subtitle panel reference set manually");
    }

    public void SetAudioSource(AudioSource source)
    {
        audioSource = source;
        Debug.Log("Audio source reference set manually");
    }

    // A convenient method to set all references at once
    public void SetAllReferences(TMPro.TMP_Text text, GameObject panel, AudioSource source = null)
    {
        SetSubtitleText(text);
        SetSubtitlePanel(panel);
        if (source != null)
        {
            SetAudioSource(source);
        }
    }

    // Add this method to reset the state of the voicelines system
    public void Reset()
    {
        // Stop any current voiceline playback
        StopCurrentVoiceline();
        
        // Re-establish UI references
        SetupUIReferences();
        
        Debug.Log("Voicelines system reset");
    }

    // Add this method to your Voicelines class
    public AudioClip GetVoicelineClip(string id)
    {
        if (voicelineClips == null) return null;
        
        foreach (VoicelineClip clip in voicelineClips)
        {
            if (clip.id == id)
            {
                return clip.clip;
            }
        }
        
        Debug.LogWarning($"Voiceline with ID '{id}' not found!");
        return null;
    }
}
