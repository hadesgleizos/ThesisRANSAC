using UnityEngine;
using System;


public class GunPickup : MonoBehaviour
{
    public Transform gunHolder;            // Parent object holding all rig-attached guns
    public GameObject arms;                // Player's arms GameObject (to hide/show when unarmed)
    public float throwForce = 10f;         // Force applied when throwing the fake gun

    private GameObject currentGun;         // Currently equipped gun (rig-attached)
    private WeaponManager weaponManager;   // Reference to the WeaponManager

    void Start()
    {
        weaponManager = GetComponent<WeaponManager>();
        if (weaponManager == null)
        {
            //Debug.LogError("WeaponManager not found on the player!");
            return;
        }

        // Ensure everything is disabled at start
        if (weaponManager.weaponScript != null)
        {
            weaponManager.weaponScript.enabled = false;
            weaponManager.weaponScript.currentWeaponStats = null;
        }

        // Hide arms at start
        if (arms != null)
        {
            arms.SetActive(false);
        }

        // Clear ammo display
        if (AmmoManager.Instance != null && AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "";
        }
    }

    void Update()
    {
        // Detect when the player presses the pickup button (e.g., "E")
        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit hit;
            // Cast a ray forward to detect pickupable fake guns
            if (Physics.Raycast(transform.position, transform.forward, out hit, 5f))
            {
                if (hit.collider.CompareTag("Pickup"))
                {
                    PickupGun(hit.collider.gameObject);
                }
            }
        }

        // Detect when the player presses the throw button (e.g., "G")
        if (Input.GetKeyDown(KeyCode.G))
        {
            ThrowGun();
        }
    }

void PickupGun(GameObject fakeGun)
{
    //Debug.Log("Attempting to pick up gun: " + fakeGun.name);

    // Destroy the pickup object immediately
    Destroy(fakeGun);  // Changed from SetActive(false) to Destroy()

    // Find the corresponding rig-attached gun prefab using the WeaponManager
    for (int i = 0; i < weaponManager.weaponPrefabs.Length; i++)
    {
        if (weaponManager.weaponPrefabs[i].name.Equals(fakeGun.name.Replace("(Clone)", "").Trim(), StringComparison.OrdinalIgnoreCase))
        {
            // Activate the rig-attached gun
            currentGun = weaponManager.weaponPrefabs[i];
            
            // Add the weapon to owned weapons and equip it
            weaponManager.AddWeapon(i);

            // Show the arms since the player is now armed
            if (arms != null)
            {
                arms.SetActive(true);
            }

            return;
        }
    }
}



void ThrowGun()
{
    if (currentGun == null) return;

    // Get the currently EQUIPPED weapon index (not just the first owned weapon)
    int currentIndex = weaponManager.currentWeaponIndex;
    
    //Debug.Log($"Throwing weapon at index: {currentIndex}");
    
    GameObject fakeGunPrefab = weaponManager.fakeGunPrefabs[currentIndex];
    if (fakeGunPrefab != null)
    {
        // Create and throw the fake gun
        GameObject thrownGun = Instantiate(fakeGunPrefab, transform.position + transform.forward, transform.rotation);
        Rigidbody rb = thrownGun.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(transform.forward * throwForce, ForceMode.Impulse);
        }

        // Remove the weapon from inventory
        weaponManager.RemoveWeapon(currentIndex);
        
        // Update currentGun reference based on what weapon is now equipped
        if (weaponManager.HasAnyWeapons())
        {
            currentGun = weaponManager.GetCurrentWeapon();
        }
        else
        {
            currentGun = null;
            if (arms != null)
            {
                arms.SetActive(false);
            }
        }
    }
}




}
