using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    public Weapon weaponScript;              // Reference to the Weapon script
    public WeaponStats[] weaponStatsArray;   // Array of WeaponStats for each weapon
    public GameObject[] weaponPrefabs;       // Array of rig-attached weapon prefabs
    public GameObject[] fakeGunPrefabs;      // Array of fake gun prefabs for each weapon
    public GameObject arms;                  // Reference to the arms GameObject

    public bool[] weaponsOwned;              // Tracks whether the weapon is currently in inventory
    private bool[] weaponEverOwned;          // Tracks whether we've EVER owned this weapon at least once

    public int currentWeaponIndex = -1;      // Start with -1 to indicate "no weapon equipped yet"

    // Dictionaries to store per-weapon ammo
    private Dictionary<int, int> ammoCounts = new Dictionary<int, int>();      // Magazine ammo
    private Dictionary<int, int> totalAmmoCounts = new Dictionary<int, int>(); // Total ammo

    void Start()
    {
        // Initialize arrays
        weaponsOwned = new bool[weaponStatsArray.Length];
        weaponEverOwned = new bool[weaponStatsArray.Length];

        // Disable all weapons + arms at game start
        DisableAllWeapons();
        if (arms != null)
        {
            arms.SetActive(false);
        }
        // We do NOT auto-init ammo here. Let AddWeapon() handle it.
    }

    void Update()
    {
        // Prevent switching if arms are not active
        if (arms == null || !arms.activeSelf) return;

        // Count how many weapons the player owns
        int ownedWeaponsCount = weaponsOwned.Count(owned => owned);

        // Only allow switching if player owns more than one weapon
        if (ownedWeaponsCount > 1)
        {
            // For two weapons: 0 = AK, 1 = USP. Add more if needed (Alpha3, etc.).
            if (Input.GetKeyDown(KeyCode.Alpha1) && weaponsOwned[0]) EquipWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) && weaponsOwned[1]) EquipWeapon(1);
        }
    }

    public void EquipWeapon(int index)
    {
        // Validate index
        if (index < 0 || index >= weaponStatsArray.Length || index >= weaponPrefabs.Length)
        {
            Debug.LogWarning($"Invalid weapon index: {index}. Unable to equip.");
            return;
        }

        // Cancel reload on the old weapon, if any
        if (weaponScript != null && currentWeaponIndex >= 0 && weaponsOwned[currentWeaponIndex])
        {
            weaponScript.CancelReloadAndSound();

            // Save old weapon's current ammo
            ammoCounts[currentWeaponIndex] = weaponScript.GetBulletsLeft();
            totalAmmoCounts[currentWeaponIndex] = weaponScript.GetTotalAmmo();
        }

        Debug.Log($"Equipping weapon at index: {index}");
        currentWeaponIndex = index;

        // Enable the chosen weapon prefab, disable all others
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            if (weaponPrefabs[i] != null)
            {
                weaponPrefabs[i].SetActive(i == index);
            }
        }

        // Apply new weapon’s stats
        if (weaponScript != null)
        {
            weaponScript.enabled = true;
            weaponScript.currentWeaponStats = weaponStatsArray[index];

            // Restore ammo if we have it saved
            if (ammoCounts.ContainsKey(index))
            {
                weaponScript.SetBulletsLeft(ammoCounts[index]);
            }
            else
            {
                // If no entry yet, default to the weapon's full magazine
                weaponScript.SetBulletsLeft(weaponStatsArray[index].magazineSize);
            }

            if (totalAmmoCounts.ContainsKey(index))
            {
                weaponScript.SetTotalAmmo(totalAmmoCounts[index]);
            }
            else
            {
                // If no entry yet, default to the weapon's total ammo
                weaponScript.SetTotalAmmo(weaponStatsArray[index].totalAmmo);
            }

            // Apply animations, etc.
            weaponScript.ApplyWeaponStats();

            // ----------------------------------------------------
            // If user is currently holding Mouse0,
            // force the new weapon to wait until the user releases.
            // ----------------------------------------------------
            if (Input.GetKey(KeyCode.Mouse0))
            {
                weaponScript.suppressShootingUntilRelease = true;
            }
            else
            {
                weaponScript.suppressShootingUntilRelease = false;
            }
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

        // Clear weapon stats in the script
        if (weaponScript != null)
        {
            weaponScript.currentWeaponStats = null;
            weaponScript.enabled = false;
        }

        // Clear the UI
        if (AmmoManager.Instance != null && AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "";
        }

        // If you use -1 to represent "no weapon," reset index
        currentWeaponIndex = -1;
    }

    public GameObject GetFakeGunPrefab()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < fakeGunPrefabs.Length)
            return fakeGunPrefabs[currentWeaponIndex];

        Debug.LogWarning($"No fake gun prefab found for index {currentWeaponIndex}");
        return null;
    }

public void AddWeapon(int index)
{
    if (index < 0 || index >= weaponsOwned.Length)
    {
        Debug.LogError($"Attempted to add weapon with invalid index: {index}");
        return;
    }

    // If dictionary doesn't have the key, treat as brand-new pickup
    if (!ammoCounts.ContainsKey(index))
    {
        ammoCounts[index] = weaponStatsArray[index].magazineSize; 
        totalAmmoCounts[index] = weaponStatsArray[index].totalAmmo;
        Debug.Log($"New weapon pickup at index {index}, giving full ammo!");
    }
    else
    {
        Debug.Log($"Weapon {index} re-added with leftover ammo (ammoCounts={ammoCounts[index]}, total={totalAmmoCounts[index]})");
    }

    // Mark as owned
    weaponsOwned[index] = true;
    weaponEverOwned[index] = true;

    // Now equip
    EquipWeapon(index);
}

public void RemoveWeapon(int index)
{
    // If we are removing the *currently equipped* weapon, save its current ammo
    if (index == currentWeaponIndex && weaponScript != null)
    {
        weaponScript.CancelReloadAndSound();
        
        // Store its final ammo in the dictionary
        ammoCounts[currentWeaponIndex] = weaponScript.GetBulletsLeft();
        totalAmmoCounts[currentWeaponIndex] = weaponScript.GetTotalAmmo();
    }

    weaponsOwned[index] = false;
    Debug.Log($"Removed weapon at index: {index}. (Keeping leftover ammo in dictionary!)");

    // If we removed the currently equipped weapon, try switching
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

        // No weapons remain
        DisableAllWeapons();
        if (arms != null)
            arms.SetActive(false);
    }
}


    public bool HasAnyWeapons()
    {
        return weaponsOwned.Any(owned => owned);
    }

    public GameObject GetCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponPrefabs.Length)
            return weaponPrefabs[currentWeaponIndex];

        return null;
    }
}
