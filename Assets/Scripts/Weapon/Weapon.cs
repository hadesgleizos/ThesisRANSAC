using System.Collections;
using UnityEngine;
using static takeDamage; // If your code depends on takeDamage definitions

public class Weapon : MonoBehaviour
{
    public WeaponStats currentWeaponStats; // Weapon configuration
    public Transform muzzlePoint;          // Position where muzzle effect will appear
    public Animator weaponAnimator;        // Reference to the weapon's animator
    public GameObject arms;               // Reference to the player's arms GameObject

    private int bulletsLeft;              // Bullets in the current magazine
    private int totalAmmo;                // Total ammo available for this weapon

    private bool isReloading;
    private bool readyToShoot = true;

    // -----------------------------------------
    // New: This flag tells us to ignore shooting
    // until the mouse is released at least once.
    // -----------------------------------------
    [HideInInspector]
    public bool suppressShootingUntilRelease = false;

    private Coroutine reloadCoroutine;    
    private AudioSource reloadAudioSource;

    private void Start()
    {
        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponent<Animator>();
        }

        reloadAudioSource = gameObject.AddComponent<AudioSource>();
        reloadAudioSource.playOnAwake = false;
        reloadAudioSource.loop = false;

        if (currentWeaponStats != null)
        {
            ApplyWeaponStats();
        }
    }

    private void Update()
    {
        if (currentWeaponStats == null)
        {
            if (AmmoManager.Instance.ammoDisplay != null)
            {
                AmmoManager.Instance.ammoDisplay.text = "";
            }
            return;
        }

        HandleInput();
        UpdateUI();
    }

    // --------------------------------------
    //  Public getters / setters for ammo
    // --------------------------------------
    public int GetBulletsLeft() => bulletsLeft;
    public void SetBulletsLeft(int count)
    {
        bulletsLeft = count;
        UpdateUI();
    }

    public int GetTotalAmmo() => totalAmmo;
    public void SetTotalAmmo(int count)
    {
        totalAmmo = count;
        UpdateUI();
    }

    // --------------------------------------
    //  Main input handling
    // --------------------------------------
    private void HandleInput()
    {
            // First check if game is paused
        if (PauseMenu.IsGamePaused())
        {
            return; // Don't process any input while paused
        }
            // Prevent input if arms are not active
        if (!IsArmsActive())
        {
            return;
        }

        // -----------------------------------------------------
        // If we're suppressing shooting because the user was
        // holding the mouse when we equipped this weapon,
        // ignore it until they release.
        // -----------------------------------------------------
    if (suppressShootingUntilRelease)
    {
        // Wait until the user lets go of Mouse0
        if (!Input.GetKey(KeyCode.Mouse0))
        {
            suppressShootingUntilRelease = false;
        }
        else
        {
            // Still holding the old input, so do nothing
            return;
        }
    }
        // Reload
        if (Input.GetKeyDown(KeyCode.R)
            && bulletsLeft < currentWeaponStats.magazineSize
            && !isReloading
            && totalAmmo > 0)
        {
            Reload();
        }

        // Fire (if we have ammo, not reloading, and readyToShoot)
        if (readyToShoot && !isReloading)
        {
            if (bulletsLeft > 0)
            {
                switch (currentWeaponStats.firingMode)
                {
                    case FiringMode.Single:
                        if (Input.GetKeyDown(KeyCode.Mouse0)) FireWeapon();
                        break;

                    case FiringMode.Burst:
                        if (Input.GetKeyDown(KeyCode.Mouse0)) StartCoroutine(FireBurst());
                        break;

                    case FiringMode.Auto:
                        if (Input.GetKey(KeyCode.Mouse0)) FireWeapon();
                        break;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Mouse0) && totalAmmo > 0)
            {
                // Play empty magazine sound
                SoundManager.Instance.PlaySound(currentWeaponStats.emptyMagazineSound);
                
                // Start reload after a small delay
                StartCoroutine(DelayedReload(0.3f));
            }
            else if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                // Just play empty sound if no total ammo left
                SoundManager.Instance.PlaySound(currentWeaponStats.emptyMagazineSound);
            }
        }
    }

    // --------------------------------------
    //  Weapon switching / resetting
    // --------------------------------------
    public void ResetWeapon()
    {
        if (currentWeaponStats != null)
        {
            if (currentWeaponStats.animatorController != null && weaponAnimator != null)
            {
                weaponAnimator.runtimeAnimatorController = currentWeaponStats.animatorController;
            }

            isReloading = false;
            readyToShoot = true;

            UpdateUI();
            //Debug.Log($"Weapon reset: {currentWeaponStats.weaponName} | Bullets: {bulletsLeft}/{totalAmmo}");
        }
        else
        {
            //Debug.LogError("ResetWeapon called with null WeaponStats!");
        }
    }

    public void PlayDrawAnimation()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("DRAW");
            StartCoroutine(EnableAfterDraw());
        }
    }

    private IEnumerator EnableAfterDraw()
    {
        if (currentWeaponStats != null)
        {
            yield return new WaitForSeconds(1f); // Adjust based on draw animation length
            readyToShoot = true;
        }
    }

    public void ApplyWeaponStats()
    {
        if (currentWeaponStats == null) return;

        if (currentWeaponStats.animatorController != null && weaponAnimator != null)
        {
            weaponAnimator.runtimeAnimatorController = currentWeaponStats.animatorController;
        }

        ResetWeapon();
    }

    // --------------------------------------
    //  Shooting logic
    // --------------------------------------
private void FireWeapon()
{
    bulletsLeft--;
    SoundManager.Instance.PlaySound(currentWeaponStats.shootingSound);

    // Play muzzle effect
    if (currentWeaponStats.muzzleEffectPrefab && muzzlePoint != null)
    {
        ParticleSystem muzzleEffect = Instantiate(
            currentWeaponStats.muzzleEffectPrefab,
            muzzlePoint.position,
            muzzlePoint.rotation
        );
        muzzleEffect.Play();
        Destroy(muzzleEffect.gameObject, 0.2f);
    }

    // Play shooting animation
    if (weaponAnimator != null)
    {
        weaponAnimator.SetTrigger("FIRE");
    }

    // Calculate bullet direction + spread
    Vector3 shootingDirection = CalculateDirectionAndSpread();

    // Raycast for hitscan
    if (Physics.Raycast(muzzlePoint.position, shootingDirection, out RaycastHit hit, 100f))
    {
        Debug.DrawLine(muzzlePoint.position, hit.point, Color.red, 2f);
        //Debug.Log($"Hit object: {hit.collider.gameObject.name} with tag: {hit.collider.gameObject.tag}");

        // Get the parent object to check the tag for main entity (e.g., zombie)
        GameObject hitObject = hit.collider.gameObject;
        GameObject parentObject = hitObject.transform.root.gameObject; // Get the root parent (the whole zombie)

        // Impact effect logic
        GameObject effectPrefab;

        // Check the tag of the parent object instead of the hit object
        switch (parentObject.tag)
        {
            case "Zombie":
                //Debug.Log("Zombie tag detected, applying zombie effect.");
                effectPrefab = GlobalReferences.Instance.zombieImpactEffectPrefab;
                break;
            case "Dirt":
                effectPrefab = GlobalReferences.Instance.dirtImpactEffectPrefab;
                break;
            case "Metal":
                effectPrefab = GlobalReferences.Instance.metalImpactEffectPrefab;
                break;
            case "Wood":
                effectPrefab = GlobalReferences.Instance.woodImpactEffectPrefab;
                break;
            default:
                effectPrefab = GlobalReferences.Instance.bulletImpactEffectPrefab;
                break;
        }

        // Check if the prefab is valid
        if (effectPrefab != null)
        {
            GameObject impactEffect = Instantiate(
                effectPrefab,
                hit.point, // Use hit.point for the impact position
                Quaternion.LookRotation(hit.normal) // Use hit.normal for the rotation
            );

            impactEffect.transform.SetParent(hit.collider.gameObject.transform);
            impactEffect.transform.position += impactEffect.transform.forward / 1000;

            // Destroy the effect after a delay
            Destroy(impactEffect, 5f);
        }
        else
        {
            //Debug.LogWarning($"No impact effect prefab assigned for tag: {hit.collider.gameObject.tag}");
        }

        // Handle damage
        takeDamage damageComponent = hit.collider.GetComponent<takeDamage>()
                                     ?? hit.collider.GetComponentInParent<takeDamage>();
        if (damageComponent != null)
        {
            //Debug.Log($"Applying damage: {currentWeaponStats.damage}");
            damageComponent.HIT(currentWeaponStats.damage, CollisionType.BODY);
        }
        else
        {
            //Debug.Log("No takeDamage component found on hit object");
        }
    }
    else
    {
        //Debug.Log("Raycast didn't hit anything");
    }

    // Set readyToShoot to false and reset after shooting delay
    readyToShoot = false;

    // Synchronize fire rate and animation
    Invoke(nameof(ResetShot), currentWeaponStats.shootingDelay);
}

private IEnumerator DelayedReload(float delay)
{
    yield return new WaitForSeconds(delay);
    if (!isReloading && totalAmmo > 0)  // Double-check conditions
    {
        Reload();
    }
}

    private IEnumerator FireBurst()
    {
        int bulletsFired = 0;
        while (bulletsFired < currentWeaponStats.bulletsPerBurst && bulletsLeft > 0)
        {
            FireWeapon();
            bulletsFired++;
            yield return new WaitForSeconds(currentWeaponStats.shootingDelay);
        }
    }

private void ResetShot()
{
    readyToShoot = true;
    if (weaponAnimator != null)
    {
        weaponAnimator.ResetTrigger("FIRE"); // Reset animation trigger to prepare for next shot
    }
}

    private Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100);
        }

        Vector3 direction = targetPoint - muzzlePoint.position;

        direction += new Vector3(
            Random.Range(-currentWeaponStats.spreadIntensity, currentWeaponStats.spreadIntensity),
            Random.Range(-currentWeaponStats.spreadIntensity, currentWeaponStats.spreadIntensity),
            0f
        );

        return direction.normalized;
    }

    // --------------------------------------
    //  Reload logic (Coroutine Version)
    // --------------------------------------
    public void Reload()
    {
        if (totalAmmo <= 0)
        {
            //Debug.Log("No ammo left to reload!");
            return;
        }

        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }
        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // Play reload sound using SoundManager instead of direct AudioSource
        if (currentWeaponStats.reloadSound != null)
        {
            SoundManager.Instance.PlaySound(currentWeaponStats.reloadSound);
        }

        // Trigger reload animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RELOAD");
        }

        yield return new WaitForSeconds(currentWeaponStats.reloadTime);

        if (!gameObject.activeInHierarchy)
        {
            isReloading = false;
            yield break;
        }

        int ammoNeeded = currentWeaponStats.magazineSize - bulletsLeft;
        if (totalAmmo >= ammoNeeded)
        {
            bulletsLeft += ammoNeeded;
            totalAmmo -= ammoNeeded;
        }
        else
        {
            bulletsLeft += totalAmmo;
            totalAmmo = 0;
        }

        isReloading = false;
        reloadCoroutine = null;
    }

    // --------------------------------------
    //  Cancel reload + sound immediately
    // --------------------------------------
    public void CancelReloadAndSound()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        if (isReloading)
        {
            isReloading = false;
        }

        if (reloadAudioSource != null && reloadAudioSource.isPlaying)
        {
            reloadAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        CancelReloadAndSound();
    }

    // --------------------------------------
    //  UI + Helper
    // --------------------------------------
    private void UpdateUI()
    {
        if (AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{bulletsLeft}/{totalAmmo}";
        }
    }

    private bool IsArmsActive()
    {
        return (arms != null && arms.activeInHierarchy);
    }
}
