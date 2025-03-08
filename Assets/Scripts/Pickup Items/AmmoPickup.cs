using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AmmoPickup : MonoBehaviour
{
    public bool refillAllWeapons = false; // Toggle to refill all weapons or just current
    public string weaponIndicesInput = "0,1"; // Comma-separated indices, e.g. "0,1" for AK and USP

    void OnTriggerEnter(Collider other)
    {
        Transform[] children = other.gameObject.GetComponentsInChildren<Transform>();
        
        foreach (Transform child in children)
        {
            if (child.CompareTag("Player"))
            {
                WeaponManager weaponManager = child.GetComponent<WeaponManager>();
                
                if (weaponManager != null && weaponManager.weaponScript != null)
                {
                    if (refillAllWeapons)
                    {
                        RefillAllWeapons(weaponManager);
                    }
                    else
                    {
                        RefillCurrentWeapon(weaponManager);
                    }
                    
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    private int[] GetWeaponIndices()
    {
        return weaponIndicesInput.Split(',')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => int.Parse(s.Trim()))
            .ToArray();
    }

    private void RefillCurrentWeapon(WeaponManager weaponManager)
    {
        int currentIndex = weaponManager.currentWeaponIndex;
        int[] indices = GetWeaponIndices();
        
        if (indices.Contains(currentIndex))
        {
            WeaponStats stats = weaponManager.weaponStatsArray[currentIndex];
            weaponManager.weaponScript.SetTotalAmmo(stats.totalAmmo);
            weaponManager.weaponScript.SetBulletsLeft(stats.magazineSize);
        }
    }

    private void RefillAllWeapons(WeaponManager weaponManager)
    {
        int currentWeapon = weaponManager.currentWeaponIndex;
        int[] indices = GetWeaponIndices();

        foreach (int index in indices)
        {
            if (index >= 0 && index < weaponManager.weaponsOwned.Length && weaponManager.weaponsOwned[index])
            {
                weaponManager.EquipWeapon(index);
                WeaponStats stats = weaponManager.weaponStatsArray[index];
                weaponManager.weaponScript.SetTotalAmmo(stats.totalAmmo);
                weaponManager.weaponScript.SetBulletsLeft(stats.magazineSize);
            }
        }

        // Switch back to original weapon
        weaponManager.EquipWeapon(currentWeapon);
    }
}
