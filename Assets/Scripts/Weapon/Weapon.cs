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
        if (readyToShoot && bulletsLeft > 0 && !isReloading)
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

        // Play empty sound if out of ammo
        if (bulletsLeft <= 0 && Input.GetKeyDown(KeyCode.Mouse0))
        {
            SoundManager.Instance.PlaySound(currentWeaponStats.emptyMagazineSound);
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

        // Muzzle effect
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

        // Animator recoil
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("RECOIL");
        }

        // Calculate bullet direction + spread
        Vector3 shootingDirection = CalculateDirectionAndSpread();

        // Spawn bullet
        GameObject bullet = Instantiate(
            currentWeaponStats.bulletPrefab,
            muzzlePoint.position,
            Quaternion.identity
        );
        bullet.transform.forward = shootingDirection;
        bullet.GetComponent<Rigidbody>().AddForce(
            shootingDirection * currentWeaponStats.bulletVelocity,
            ForceMode.VelocityChange
        );
        Destroy(bullet, currentWeaponStats.bulletLifetime);

        // Optional raycast for immediate hits
        if (Physics.Raycast(muzzlePoint.position, shootingDirection, out RaycastHit hit, 100f))
        {
            Debug.DrawLine(muzzlePoint.position, hit.point, Color.red, 2f);
            Debug.Log($"Hit object: {hit.collider.gameObject.name}");

            takeDamage damageComponent = hit.collider.GetComponent<takeDamage>()
                                         ?? hit.collider.GetComponentInParent<takeDamage>();
            if (damageComponent != null)
            {
                Debug.Log($"Applying damage: {currentWeaponStats.damage}");
                damageComponent.HIT(currentWeaponStats.damage, CollisionType.BODY);
            }
            else
            {
                Debug.Log("No takeDamage component found on hit object");
            }
        }
        else
        {
            Debug.Log("Raycast didn't hit anything");
        }

        // Shooting cooldown
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
            Debug.Log("No ammo left to reload!");
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

        // Play reload sound
        if (currentWeaponStats.reloadSound != null)
        {
            reloadAudioSource.clip = currentWeaponStats.reloadSound;
            reloadAudioSource.Play();
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
