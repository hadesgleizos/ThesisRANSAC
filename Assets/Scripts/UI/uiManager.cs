using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class uiManager : MonoBehaviour
{
    public TextMeshProUGUI health, ammo, score;
    public GameObject[] weaponIndicator = new GameObject[2];

    private Color defaultColor; // Default health text color

    // Start is called before the first frame update
    private void Start()
    {
        // Parse the custom color (#B5F165) and set it as the default
        if (ColorUtility.TryParseHtmlString("#B5F165", out defaultColor))
        {
            health.color = defaultColor;
        }
        else
        {
            Debug.LogError("Failed to parse color #B5F165. Ensure the code is correct.");
        }
    }

    public void setHealth(string i)
    {
        // Add the "+" symbol or health icon representation
        health.text = $"+{i}";
        UpdateHealthColor(float.Parse(i));
    }

    public void setAmmo(string i) { ammo.text = i; }
    public void setScore(string i) { score.text = i; }

    public void setWeaponToDisplay(int e)
    {
        for (int i = 0; i < weaponIndicator.Length; i++)
        {
            weaponIndicator[i].SetActive(false);
        }
        for (int i = 0; i < weaponIndicator.Length; i++)
        {
            if (i == e) weaponIndicator[i].SetActive(true);
        }
    }

    private void UpdateHealthColor(float currentHealth)
    {
        float maxHealth = 100f; // Assuming 100 is the max health
        float healthPercentage = (currentHealth / maxHealth) * 100;

        if (healthPercentage <= 30)
        {
            health.color = Color.red; // Red color for health <= 30%
        }
        else if (healthPercentage <= 60)
        {
            health.color = new Color(1f, 0.65f, 0f); // Orange color for health <= 60%
        }
        else
        {
            health.color = defaultColor; // Default custom color for health > 60%
        }
    }
    public void ToggleUI(bool isActive)
{
    health.gameObject.SetActive(isActive);
    ammo.gameObject.SetActive(isActive);
    score.gameObject.SetActive(isActive);

    foreach (var weapon in weaponIndicator)
    {
        weapon.SetActive(false); // Ensure all weapon indicators are initially disabled
    }
}

public void UpdateWeaponUI(int weaponIndex)
{
    for (int i = 0; i < weaponIndicator.Length; i++)
    {
        weaponIndicator[i].SetActive(i == weaponIndex);
    }
}

}
