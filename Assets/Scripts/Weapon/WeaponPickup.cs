using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public int weaponIndex;            // 0 = AK, 1 = USP, etc.
    public WeaponManager weaponManager; // Drag a reference to the same WeaponManager in your scene

    private void OnTriggerEnter(Collider other)
    {
        // If the player collides with the pickup
        if (other.CompareTag("Player"))
        {
            // Add the weapon to the player's WeaponManager
            weaponManager.AddWeapon(weaponIndex);

            // Destroy the pickup so it can’t be picked up again
            Destroy(gameObject);
        }
    }
}
