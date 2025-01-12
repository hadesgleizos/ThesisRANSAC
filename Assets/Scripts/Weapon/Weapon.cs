using System.Collections;
using UnityEngine;

using static takeDamage;
public class Weapon : MonoBehaviour
{
    public WeaponStats currentWeaponStats; // Weapon configuration
    public Transform muzzlePoint;           // The position where the muzzle effect will appear
    public Animator weaponAnimator;        // Reference to the weapon's animator
    public GameObject arms;                // Reference to the player's arms GameObject

    private int bulletsLeft;               // Bullets in the current magazine
    private int totalAmmo;                 // Total ammo available for this weapon
    private bool isReloading, allowReset = true, readyToShoot = true;
    private AnimatorOverrideController overrideController;

    private void Start()
    {
        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponent<Animator>();
        }

        if (weaponAnimator != null)
        {
            overrideController = new AnimatorOverrideController(weaponAnimator.runtimeAnimatorController);
            weaponAnimator.runtimeAnimatorController = overrideController;
        }

        // Initialize weapon stats immediately if they exist
        if (currentWeaponStats != null)
        {
            ApplyWeaponStats();
        }
    }

    private void Update()
    {
        // Only check for null weaponStats, remove enabled check since WeaponManager handles that
        if (currentWeaponStats == null)
        {
            // Clear the UI when no weapon is equipped
            if (AmmoManager.Instance.ammoDisplay != null)
            {
                AmmoManager.Instance.ammoDisplay.text = "";
            }
            return;
        }

        HandleInput();
        UpdateUI();
    }

    public int GetBulletsLeft()
    {
        return bulletsLeft;
    }

    public void SetBulletsLeft(int count)
    {
        bulletsLeft = count;
        UpdateUI();
    }

    public int GetTotalAmmo()
    {
        return totalAmmo;
    }

    public void SetTotalAmmo(int count)
    {
        totalAmmo = count;
        UpdateUI(); // Ensure the UI reflects the updated value
    }

    private void HandleInput()
    {
        // Prevent input if the arms are inactive
        if (!IsArmsActive())
        {
            return;
        }

        // Reload when R is pressed, only if there's ammo to reload and not in the middle of a reload
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < currentWeaponStats.magazineSize && !isReloading && totalAmmo > 0)
        {
            Reload();
        }

        // Handle firing based on firing mode
        if (readyToShoot && bulletsLeft > 0 && !isReloading)
        {
            if (currentWeaponStats.firingMode == FiringMode.Single && Input.GetKeyDown(KeyCode.Mouse0))
            {
                FireWeapon();
            }
            else if (currentWeaponStats.firingMode == FiringMode.Burst && Input.GetKeyDown(KeyCode.Mouse0))
            {
                StartCoroutine(FireBurst());
            }
            else if (currentWeaponStats.firingMode == FiringMode.Auto && Input.GetKey(KeyCode.Mouse0))
            {
                FireWeapon();
            }
        }

        // Play empty magazine sound if out of ammo
        if (bulletsLeft <= 0 && Input.GetKeyDown(KeyCode.Mouse0))
        {
            SoundManager.Instance.PlaySound(currentWeaponStats.emptyMagazineSound);
        }
    }

public void ResetWeapon()
{
    if (currentWeaponStats != null)
    {
        // Assign the Animator Controller dynamically
        if (currentWeaponStats.animatorController != null && weaponAnimator != null)
        {
            weaponAnimator.runtimeAnimatorController = currentWeaponStats.animatorController;
        }

        // Initialize ammo only if it hasn't been set
        if (totalAmmo == 0)
        {
            totalAmmo = currentWeaponStats.totalAmmo;
        }
        if (bulletsLeft == 0)
        {
            bulletsLeft = currentWeaponStats.magazineSize;
        }

        // Reset state variables
        isReloading = false;
        readyToShoot = true;

        // Update UI
        UpdateUI();

        Debug.Log($"Weapon reset: {currentWeaponStats.weaponName} | Bullets: {bulletsLeft}/{totalAmmo}");
    }
    else
    {
        Debug.LogError("ResetWeapon called with null WeaponStats!");
    }
}


    public void PlayDrawAnimation()
    {
        if (weaponAnimator != null)
        {
            // Trigger the draw animation
            weaponAnimator.SetTrigger("DRAW");

            // Delay weapon functionality until the animation finishes
            StartCoroutine(EnableAfterDraw());
        }
    }

    private IEnumerator EnableAfterDraw()
    {
        if (currentWeaponStats != null)
        {
            // Wait for the draw animation length (replace 0.5f with actual animation time)
            yield return new WaitForSeconds(0.5f);

            // Enable weapon functionality
            readyToShoot = true;
        }
    }

    public void ApplyWeaponStats()
    {
        if (currentWeaponStats == null) return;

        // Assign the Animator Controller dynamically
        if (currentWeaponStats.animatorController != null && weaponAnimator != null)
        {
            weaponAnimator.runtimeAnimatorController = currentWeaponStats.animatorController;
        }

        ResetWeapon(); // Ensure weapon stats are fully initialized
    }

    private void FireWeapon()
    {
        bulletsLeft--;

        // Play shooting sound
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

        // Trigger RECOIL animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RECOIL");
        }

        // Calculate shooting direction with spread
        Vector3 shootingDirection = CalculateDirectionAndSpread();

        // Instantiate the bullet
        GameObject bullet = Instantiate(currentWeaponStats.bulletPrefab, muzzlePoint.position, Quaternion.identity);
        bullet.transform.forward = shootingDirection; // Apply spread direction to the bullet
        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * currentWeaponStats.bulletVelocity, ForceMode.VelocityChange);

        Destroy(bullet, currentWeaponStats.bulletLifetime);

        // Perform raycast for immediate hit detection
        RaycastHit hit;
    if (Physics.Raycast(muzzlePoint.position, shootingDirection, out hit, 100f))
    {
        Debug.DrawLine(muzzlePoint.position, hit.point, Color.red, 2f); // Visual debug line
        Debug.Log($"Hit object: {hit.collider.gameObject.name}"); // Debug log

        // Try to get takeDamage component from the hit object or its parent
        takeDamage damageComponent = hit.collider.GetComponent<takeDamage>();
        if (damageComponent == null)
        {
            damageComponent = hit.collider.GetComponentInParent<takeDamage>();
        }

        if (damageComponent != null)
        {
            Debug.Log($"Applying damage: {currentWeaponStats.damage}"); // Debug log
            damageComponent.HIT(currentWeaponStats.damage, CollisionType.BODY); // Default to body shots
        }
        else
        {
            Debug.Log("No takeDamage component found on hit object"); // Debug log
        }
    }
    else
    {
        Debug.Log("Raycast didn't hit anything"); // Debug log
    }

        // Reset shooting cooldown
        readyToShoot = false;
        Invoke(nameof(ResetShot), currentWeaponStats.shootingDelay);
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

    private void Reload()
    {
        if (totalAmmo <= 0)
        {
            Debug.Log("No ammo left to reload!");
            return;
        }

        isReloading = true;

        // Play reload sound via SoundManager
        SoundManager.Instance.PlaySound(currentWeaponStats.reloadSound);

        // Trigger RELOAD animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RELOAD");
        }

        // Complete reload after delay
        Invoke(nameof(CompleteReload), currentWeaponStats.reloadTime);
    }

    private void CompleteReload()
    {
        if (currentWeaponStats == null)
        {
            Debug.LogWarning("CompleteReload called with no active WeaponStats!");
            return;
        }

        int ammoNeeded = currentWeaponStats.magazineSize - bulletsLeft;

        // If there's enough total ammo to refill the magazine
        if (totalAmmo >= ammoNeeded)
        {
            bulletsLeft += ammoNeeded;
            totalAmmo -= ammoNeeded; // Subtract the ammo used
        }
        else
        {
            bulletsLeft += totalAmmo;
            totalAmmo = 0; // All available ammo used up
        }

        isReloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
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
            0
        );

        return direction.normalized;
    }

    private void UpdateUI()
    {
        if (AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{bulletsLeft}/{totalAmmo}";
        }
    }

    private bool IsArmsActive()
    {
        return arms != null && arms.activeInHierarchy;
    }
}
