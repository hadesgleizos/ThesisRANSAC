using UnityEngine;

using static takeDamage;

[CreateAssetMenu(fileName = "NewWeaponStats", menuName = "Weapon/Weapon Stats")]
public class WeaponStats : ScriptableObject
{
    // Basic Weapon Stats
    [Header("Weapon Identity")]
       public string weaponName;
    [Header("Damage Settings")]
    public float damage = 10f;
    public CollisionType defaultHitType = CollisionType.BODY;

    //Weapon Configuration
    public float shootingDelay;
    public int bulletsPerBurst;
    public float spreadIntensity;
    public GameObject bulletPrefab;
    public float bulletVelocity;
    public float bulletLifetime;
    public float reloadTime;
    public int magazineSize;


    // Total ammo specific to the weapon
    public int totalAmmo;  // New field for total ammo available to the weapon

    // Firing Mode
    public FiringMode firingMode;

    // Visual Effects
    public ParticleSystem muzzleEffectPrefab;

    // Audio Clips for Weapon Sounds
    public AudioClip shootingSound;
    public AudioClip reloadSound;
    public AudioClip emptyMagazineSound;

    // Animation Clips
    public AnimationClip reloadAnimation;
    public AnimationClip recoilAnimation;

    // Optional Animator Controller
    public RuntimeAnimatorController animatorController;
}

public enum FiringMode
{
    Single,
    Burst,
    Auto
}
