using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    public Weapon weaponScript;                // Reference to the Weapon script
    public WeaponStats[] weaponStatsArray;     // Array of WeaponStats for each weapon
    public GameObject[] weaponPrefabs;         // Array of rig-attached weapon prefabs
    public GameObject[] fakeGunPrefabs;        // Array of fake gun prefabs for each weapon
    public GameObject arms;                    // Reference to the arms GameObject

    public bool[] weaponsOwned;                // Tracks which weapons the player has
    public int currentWeaponIndex = 0;         // Current weapon index

    private Dictionary<int, int> ammoCounts = new Dictionary<int, int>(); // To store magazine ammo counts for each weapon
    private Dictionary<int, int> totalAmmoCounts = new Dictionary<int, int>(); // To store total ammo counts for each weapon

    void Start()
    {
        // Initialize the weapons owned array
        weaponsOwned = new bool[weaponStatsArray.Length];

        // Ensure all weapons and arms are disabled at the start
        DisableAllWeapons();
        if (arms != null)
        {
            arms.SetActive(false);
        }

        // Initialize ammo counts for each weapon
        for (int i = 0; i < weaponStatsArray.Length; i++)
        {
            ammoCounts[i] = 0; // Default magazine ammo to 0
            totalAmmoCounts[i] = weaponStatsArray[i].totalAmmo; // Set total ammo from WeaponStats
        }
    }

    void Update()
    {
        // Prevent switching if arms are not active or no weapon is equipped
        if (arms == null || !arms.activeSelf)
        {
            return;
        }

        // Count how many weapons the player owns
        int ownedWeaponsCount = weaponsOwned.Count(owned => owned);

        // Only allow switching if player has more than one weapon
        if (ownedWeaponsCount > 1)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && weaponsOwned[0]) EquipWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) && weaponsOwned[1]) EquipWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) && weaponsOwned[2]) EquipWeapon(2);
        }
    }

public void EquipWeapon(int index)
{
    if (index >= 0 && index < weaponStatsArray.Length && index < weaponPrefabs.Length)
    {
        // Save the current weapon's ammo counts before switching
        if (weaponScript != null)
        {
            ammoCounts[currentWeaponIndex] = weaponScript.GetBulletsLeft();
            totalAmmoCounts[currentWeaponIndex] = weaponScript.GetTotalAmmo();
        }

        Debug.Log($"Equipping weapon at index: {index}");
        currentWeaponIndex = index;

        // Enable the correct weapon prefab and disable others
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            if (weaponPrefabs[i] != null)
            {
                weaponPrefabs[i].SetActive(i == index);
            }
        }

        // Update the weapon script with the selected weapon stats
        if (weaponScript != null)
        {
            weaponScript.enabled = true;
            weaponScript.currentWeaponStats = weaponStatsArray[index];
            
            // Set ammo counts
            if (ammoCounts.ContainsKey(index))
            {
                weaponScript.SetBulletsLeft(ammoCounts[index]);
                weaponScript.SetTotalAmmo(totalAmmoCounts[index]);
            }
            else
            {
                // This is a newly acquired weapon, initialize with default values
                weaponScript.SetBulletsLeft(weaponStatsArray[index].magazineSize);
                weaponScript.SetTotalAmmo(weaponStatsArray[index].totalAmmo);
            }

            // Apply weapon stats (animations, etc) without affecting ammo
            weaponScript.ApplyWeaponStats();
        }
    }
    else
    {
        Debug.LogWarning($"Invalid weapon index: {index}. Unable to equip weapon.");
    }
}
    public void DisableAllWeapons()
    {
        foreach (GameObject weapon in weaponPrefabs)
        {
            if (weapon != null)
            {
                weapon.SetActive(false);
            }
        }

        // Clear the weapon stats in the Weapon script
        if (weaponScript != null)
        {
            weaponScript.currentWeaponStats = null;
            weaponScript.enabled = false;
        }

        currentWeaponIndex = 0;
    }

    public GameObject GetFakeGunPrefab()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < fakeGunPrefabs.Length)
        {
            return fakeGunPrefabs[currentWeaponIndex];
        }
        Debug.LogWarning($"No fake gun prefab found for index {currentWeaponIndex}");
        return null;
    }

public void AddWeapon(int index)
{
    if (index >= 0 && index < weaponsOwned.Length)
    {
        // Mark the weapon as owned
        weaponsOwned[index] = true;
        Debug.Log($"Added weapon {index}. Weapon ownership status:");

        // Initialize ammo counts for the new weapon if not already initialized
        if (!ammoCounts.ContainsKey(index))
        {
            ammoCounts[index] = weaponStatsArray[index].magazineSize; // Initial magazine size
        }
        if (!totalAmmoCounts.ContainsKey(index))
        {
            totalAmmoCounts[index] = weaponStatsArray[index].totalAmmo; // Initial total ammo
        }

        // Print status of all weapons
        for (int i = 0; i < weaponsOwned.Length; i++)
        {
            Debug.Log($"Weapon {i}: {(weaponsOwned[i] ? "Owned" : "Not Owned")}");
        }

        // Automatically equip the weapon when picked up
        EquipWeapon(index);
    }
    else
    {
        Debug.LogError($"Attempted to add weapon with invalid index: {index}");
    }
}


    public void RemoveWeapon(int index)
    {
        if (index >= 0 && index < weaponsOwned.Length)
        {
            weaponsOwned[index] = false;
            Debug.Log($"Removed weapon at index: {index}");

            // If we removed the current weapon, switch to another owned weapon if possible
            if (currentWeaponIndex == index)
            {
                for (int i = 0; i < weaponsOwned.Length; i++)
                {
                    if (weaponsOwned[i])
                    {
                        EquipWeapon(i);
                        return;
                    }
                }

                DisableAllWeapons();

                if (arms != null)
                {
                    arms.SetActive(false);
                }
            }
        }
    }

    public bool HasAnyWeapons()
    {
        return weaponsOwned.Any(owned => owned);
    }

    public GameObject GetCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponPrefabs.Length)
        {
            return weaponPrefabs[currentWeaponIndex];
        }
        return null;
    }
}
