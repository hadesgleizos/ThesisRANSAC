using UnityEngine;
using TMPro;

public class PersistentUIManager : MonoBehaviour
{
    public static PersistentUIManager Instance { get; private set; }

    [Header("Required UI Elements")]
    public GameObject subtitlePanel;
    public TMP_Text subtitleText;
    public GameObject pointerObject;
    public GameObject onScreenPointerHub;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            
            // Keep this GameObject alive between scene changes
            DontDestroyOnLoad(gameObject);
            
            // Keep critical UI elements alive
            if (subtitlePanel != null)
                DontDestroyOnLoad(subtitlePanel);
                
            if (subtitleText != null && subtitleText.gameObject != subtitlePanel)
                DontDestroyOnLoad(subtitleText.gameObject);
                
            if (pointerObject != null)
                DontDestroyOnLoad(pointerObject);
                
            if (onScreenPointerHub != null)
                DontDestroyOnLoad(onScreenPointerHub);
                
            // Initialize UI elements
            InitializeUI();
        }
        else
        {
            // If an instance already exists, destroy this duplicate
            Destroy(gameObject);
        }
    }

    private void InitializeUI()
    {
        // Make sure UI elements are properly set up
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
        
        // If we have Voicelines singleton, update its references
        if (Voicelines.Instance != null)
        {
            Voicelines.Instance.SetAllReferences(
                subtitleText,
                subtitlePanel
            );
        }
    }
    
    // Call this method after scene changes to ensure references are maintained
    public void RebuildReferences()
    {
        if (Voicelines.Instance != null)
        {
            Voicelines.Instance.SetAllReferences(
                subtitleText,
                subtitlePanel
            );
        }
        
        // Additional reference rebuilding can be done here
    }
}