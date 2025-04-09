using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class ZombieInfo
{
    public string Name;
    public string Description;
    public Sprite zombIMG;
}
public class DescriptionPopUp : MonoBehaviour
{
    [SerializeField] private GameObject ZombDescPopUp;
    [SerializeField] private TextMeshProUGUI VarName;
    [SerializeField] private TextMeshProUGUI Description;
    [SerializeField] private Image ZombIMG;
    [SerializeField] private Button NextButton;
    [SerializeField] private TextMeshProUGUI NextButtonText;
    [SerializeField] private GameObject PrevButton;
    private bool IndexLast = false;

    [Header("Zombie Info")]
    public ZombieInfo[] zombieInfos = new ZombieInfo[0];
    private int currentIndex = 0;
    void Start()
    {
        UpdateDesc();
        NextButton.onClick.AddListener(Next);
        //Previous Button must start inactive
        PrevButton.SetActive(false);
    }
    private void Update()
    {
        if (ZombDescPopUp.activeSelf)
        {
            Time.timeScale = 0f;
            SetCursorState(true);
        } 
    }
    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
    void UpdateDesc()
    {
        VarName.text = zombieInfos[currentIndex].Name;
        Description.text = zombieInfos[currentIndex].Description;
        ZombIMG.sprite = zombieInfos[currentIndex].zombIMG;
    }
    public void Next() 
    {
        if (IndexLast)
        {
            Continue();
        }
        else
        {
            if (currentIndex < zombieInfos.Length - 1)
            {
                currentIndex++;
                PrevButton.SetActive(true);
                UpdateDesc();
            }
            UpdateButton();
        }
    }
    void UpdateButton()
    {
        if (currentIndex == zombieInfos.Length - 1)
        {
            NextButtonText.text = "Continue";
            IndexLast = true;
        }
        else
        {
            NextButtonText.text = "Next";
            IndexLast = false;
        }
    }
    public void Continue() 
    {
        ZombDescPopUp.SetActive(false);
        SetCursorState(false);
        Time.timeScale = 1f;
    }

    public void Previous()
    {
        currentIndex--;
        for (int i = 0; i < zombieInfos.Length; i++)
        {
            UpdateDesc();
            PrevButton.SetActive(true);
            if (currentIndex <= 0)
            {
                currentIndex = 0;
                PrevButton.SetActive(false);
            }
            else if (currentIndex < zombieInfos.Length - 1) UpdateButton();
        }
    }
}
