using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class bl_HudDamageManager : MonoBehaviour {

    [Header("Settings")]
    [Range(0,10)]
    [SerializeField]private float DelayFade = 0.25f;
    [Range(0.01f,5)]
    [SerializeField]private float FadeSpeed = 0.4f;
    [Range(0.1f,0.9f)]
    [SerializeField]private float MinAlpha = 0.4f;
    [SerializeField]private AnimationCurve CurveFade;
    [SerializeField]private bool AnimateHealthInfo = true;
    [SerializeField]private Color MaxHealthColor;
    [SerializeField]private Color MinHealthColor;

    [Header("Shake")]
    [SerializeField]private bool useShake = true;
    public Transform ShakeObject = null;
    private Vector3 originPosition;
    private Quaternion originRotation;
    [Range(0.001f, 0.01f)]
    [SerializeField]private float ShakeDecay = 0.002f;
    [Range(0.01f, 0.2f)]
    [SerializeField]private float ShakeIntensity = 0.02f;
    [Range(0.01f, 0.5f)]
    [SerializeField]private float ShakeAmount = 0.2f;
    private float shakeIntensity;

    [Header("References")]
    [SerializeField]private CanvasGroup m_canvasGroup;
    [SerializeField]private Slider HealthSlider = null;
    [SerializeField]private Text HealthText = null;
    [SerializeField]private GameObject DeathHUD;
    [SerializeField]private GameObject HealthInfo;

    [Header("Scene Names")]
    [SerializeField] private string baseSceneName = "BaseScene";
    [SerializeField] private string restartSceneName = "Stage 2";

    [Header("Death")]
    [SerializeField] private Animator deathHUDAnimator;
    [SerializeField] private float deathFreezeDelay = 0.2f; // Add this field
    private bool isDead = false;
    private Coroutine freezeCoroutine;

    private float Alpha = 0;
    private float Health = 100;
    private float MaxHealth = 100;
    private float NextDelay = 0;
    private int HealthValue;

    /// <summary>
    /// 
    /// </summary>
    void Start()
    {
        if (HealthSlider != null)
        {
            HealthSlider.maxValue = MaxHealth;
            HealthSlider.value = Health;
        }
        if (!AnimateHealthInfo) { HealthInfo.GetComponent<Animator>().enabled = false; }
        HealthValue = (int)Health;
        originPosition = ShakeObject.localPosition;
        originRotation = ShakeObject.localRotation;
    }

    /// <summary>
    /// Register all callbacks 
    /// </summary>
    void OnEnable()
    {
        bl_DamageDelegate.OnDamageEvent += OnDamage;
        bl_DamageDelegate.OnDieEvent += OnDie;
    }

    /// <summary>
    /// UnRegister all callbacks 
    /// </summary>
    void OnDisable()
    {
        bl_DamageDelegate.OnDamageEvent -= OnDamage;
        bl_DamageDelegate.OnDieEvent -= OnDie;
    }

    /// <summary>
    /// This is called by event delegate
    /// sure to call when player receive the damage.
    /// </summary>
    void OnDamage(bl_DamageInfo info)
    {
        if (info.Damage == 0) // This is a healing event
        {
            // Get the current health from PlayerPerformance
            GameObject player = info.Sender;
            PlayerPerformance playerPerf = player.GetComponent<PlayerPerformance>();
            if (playerPerf != null)
            {
                Health = playerPerf.GetHealth();
                // Don't update Alpha for healing
                if (AnimateHealthInfo) { HealthInfoControll(); }
            }
        }
        else // This is a damage event
        {
            Health -= info.Damage;
            //Calculate the difference in health for apply to the alpha
            Alpha = (MaxHealth - Health) / 100;
            //Ensure that alpha is never less than the minimum allowed
            Alpha = Mathf.Clamp(Alpha, MinAlpha, 1);
            //Update delay
            NextDelay = Time.time + DelayFade;
            if (AnimateHealthInfo) { HealthInfoControll(); }
            if (useShake && ShakeObject != null) 
            { 
                StopAllCoroutines(); 
                StartCoroutine(Shake()); 
            }
        }
    }

    /// <summary>
    /// Call by event delegate (OnDie)
    /// send the event when the player die.
    /// see the example bl_DamageCallbak as reference of usage.
    /// </summary>
    private void OnDie()
    {
        if (isDead) return;
        isDead = true;

        // Show death HUD before freezing
        DeathHUD.SetActive(true);

        // Disable pause menu functionality first
        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.enabled = false;
        }

        // Show cursor during death (changed from hiding it)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Pause all audio sources just like pause menu
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            if (source.isPlaying)
            {
                source.Pause();
            }
        }

        // Set up Death HUD animation to work with frozen time
        if (deathHUDAnimator != null)
        {
            deathHUDAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            deathHUDAnimator.Rebind();
            deathHUDAnimator.Update(0f);
            deathHUDAnimator.Play("DeathHUDAnimation", 0, 0f);
        }

        // Configure all UI elements in death HUD
        foreach (var canvas in DeathHUD.GetComponentsInChildren<CanvasGroup>(true))
        {
            canvas.interactable = true;
            canvas.blocksRaycasts = true;
            canvas.alpha = 1f;
        }

        DisablePlayerInput();

        if (freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);
        freezeCoroutine = StartCoroutine(FreezeGameWithDelay());
    }

    private IEnumerator FreezeGameWithDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(deathFreezeDelay);
        
        // Freeze the game
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Disables player input components when dead
    /// </summary>
    private void DisablePlayerInput()
    {
        // Find the player GameObject (assuming it has a "Player" tag)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Disable character controller if present
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
                characterController.enabled = false;

            // Disable rigidbody if present
            var rigidbody = player.GetComponent<Rigidbody>();
            if (rigidbody != null)
                rigidbody.isKinematic = true;

            // Disable any custom player input script
            var playerInput = player.GetComponent<MonoBehaviour>();
            if (playerInput != null)
                playerInput.enabled = false;
        }
    }

    /// <summary>
    /// If the health default or maxHealth default of player no equal to 100
    /// then call this for update with this and keep it synchronized at the start
    /// NOTE: Call this in start / awake from your 'player health' script
    /// </summary>
    /// <param name="_health"></param>
    /// <param name="_maxHealth"></param>
    public void SetUp(float _health,float _maxHealth)
    {
        Health = _health;
        MaxHealth = _maxHealth;

        if (HealthSlider != null)
        {
            HealthSlider.maxValue = MaxHealth;
            HealthSlider.value = Health;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void FixedUpdate()
    {
        //Apply fade effect to HUD.
        FadeRedScreen();
        HealthHUDControll();
    }

    /// <summary>
    /// 
    /// </summary>
    void FadeRedScreen()
    {
        if (m_canvasGroup.alpha != Alpha)
        {
            if (Time.time > NextDelay && Alpha > 0)
            {
                Alpha = Mathf.Lerp(Alpha, 0, Time.deltaTime);
                Alpha = CurveFade.Evaluate(Alpha);
            }
            m_canvasGroup.alpha = Mathf.Lerp(m_canvasGroup.alpha, Alpha, Time.deltaTime * FadeSpeed);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void HealthHUDControll()
    {
        if(HealthSlider != null)
        {
            Image fillImage = HealthSlider.fillRect.GetComponent<Image>();
            HealthSlider.value = Mathf.Lerp(HealthSlider.value, Health, 7 * Time.deltaTime);
            fillImage.color = Color.Lerp(MinHealthColor, fillImage.color, HealthSlider.value / MaxHealth);
        }
        if(HealthText != null)
        {
            // Remove lerp for health text to show accurate value immediately
            HealthValue = (int)Health;
            HealthText.text = (Health > 0) ? HealthValue.ToString() : "Dead";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    void HealthInfoControll(float value = 0)
    {
        if (HealthInfo == null)
            return;

        Animator a = HealthInfo.GetComponent<Animator>();
        a.Play("HealthInfoHit", 0, 0);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerator Shake()
    {
        shakeIntensity = ShakeIntensity;
        while (shakeIntensity > 0)
        {
            ShakeObject.localPosition = originPosition + Random.insideUnitSphere * shakeIntensity;
            ShakeObject.localRotation = new Quaternion(
                originRotation.x + Random.Range(-shakeIntensity, shakeIntensity) * ShakeAmount,
                originRotation.y + Random.Range(-shakeIntensity, shakeIntensity) * ShakeAmount,
                originRotation.z + Random.Range(-shakeIntensity, shakeIntensity) * ShakeAmount,
                originRotation.w + Random.Range(-shakeIntensity, shakeIntensity) * ShakeAmount);
            shakeIntensity -= ShakeDecay;
            yield return false;
        }
        ShakeObject.localPosition = originPosition;
        ShakeObject.localRotation = originRotation;
    }

    /// <summary>
    /// Simple restart 
    /// this is not requiered to use in your project.
    /// </summary>
    public void Restart()
    {
        // Set timescale first to prevent any unwanted updates
        Time.timeScale = 1f;

        // Stop all audio sources instead of unpausing them
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }

        isDead = false;

        // Re-enable pause menu before restarting
        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.enabled = true;
        }

        // Load scenes
        SceneManager.LoadScene(baseSceneName, LoadSceneMode.Single);
        SceneManager.LoadScene(restartSceneName, LoadSceneMode.Additive);
    }

    public float BloodFadeSpeed 
    {
        get
        {
           return FadeSpeed;
        }
        set
        {
            FadeSpeed = value;
        }
    }

    public float FadeDelay
    {
        get
        {
            return DelayFade;
        }
        set
        {
            DelayFade = value;
        }
    }
}