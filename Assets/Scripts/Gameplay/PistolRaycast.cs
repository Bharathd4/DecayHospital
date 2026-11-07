
using System.Collections;
using UnityEngine;
using TMPro;

public class PistolRaycast : MonoBehaviour
{
    public static PistolRaycast Instance { get; private set; }

    [Header("Pistol Settings")]
    public float damage = 25f;
    [SerializeField] private float range = 100f;
    public float shotsPerSecond = 6f;

    [Header("Ammunition")]
    [SerializeField] private int magazineCapacity = 12;
    [SerializeField] private int startingReserveAmmo = 24;
    [SerializeField] private int maxReserveAmmo = 150;
    [SerializeField] private float reloadDuration = 1.5f;
    [SerializeField] private TMP_Text ammoText;

    [Header("Weapon Animation")]
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Testing")]
    [SerializeField] private bool logMisses = false;

    private static readonly int FireHash =
        Animator.StringToHash("Fire");

    private static readonly int ReloadHash =
        Animator.StringToHash("Reload");

    private int currentAmmo;
    private int reserveAmmo;
    private int lastPreparedWave;

    private float nextFireTime;
    private bool isReloading;
    private bool emergencyAmmoUsed;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple PistolRaycast instances detected.");
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        magazineCapacity = Mathf.Max(1, magazineCapacity);
        maxReserveAmmo = Mathf.Max(150, maxReserveAmmo);

        currentAmmo = magazineCapacity;

        reserveAmmo = Mathf.Clamp(
            startingReserveAmmo, 0, maxReserveAmmo);

        UpdateAmmoUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        PrepareAmmoForNewWave();

        if (Input.GetMouseButtonDown(0))
            Fire();

        if (Input.GetKeyDown(KeyCode.R))
            ReloadWeapon();
    }

    private void PrepareAmmoForNewWave()
    {
        if (WaveManager.Instance == null)
            return;

        int wave = WaveManager.Instance.currentWave;

        if (wave <= 0 || wave == lastPreparedWave)
            return;

        int[] waveCounts = WaveManager.Instance.zombiesPerWave;

        if (waveCounts == null || wave > waveCounts.Length)
            return;

        int zombieCount = waveCounts[wave - 1];

        int normalZombieShots = Mathf.CeilToInt(100f / damage);
        int bossShots = Mathf.CeilToInt(500f / damage);

        int requiredShots;

        if (wave == waveCounts.Length)
        {
            int normalZombies = Mathf.Max(0, zombieCount - 1);

            requiredShots =
                normalZombies * normalZombieShots + bossShots;
        }
        else
        {
            requiredShots = zombieCount * normalZombieShots;
        }

        int bufferedShots = Mathf.CeilToInt(requiredShots * 1.2f);

        int currentTotalAmmo = currentAmmo + reserveAmmo;

        int extraAmmo = Mathf.Max(
            0, bufferedShots - currentTotalAmmo);

        reserveAmmo = Mathf.Min(
            maxReserveAmmo, reserveAmmo + extraAmmo);

        emergencyAmmoUsed = false;
        lastPreparedWave = wave;

        UpdateAmmoUI();

        Debug.Log(
            $"SOLO Wave {wave}: Ammo prepared. " +
            $"Target={bufferedShots}, Current={currentAmmo + reserveAmmo}"
        );
    }

    public void Fire()
    {
        if (isReloading)
            return;

        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
        {
            if (reserveAmmo <= 0 && !emergencyAmmoUsed)
            {
                reserveAmmo = magazineCapacity;
                emergencyAmmoUsed = true;

                Debug.LogWarning("Emergency magazine granted.");
            }

            ReloadWeapon();
            return;
        }

        nextFireTime = Time.time +
            1f / Mathf.Max(0.1f, shotsPerSecond);

        currentAmmo--;
        UpdateAmmoUI();

        if (weaponAnimator != null &&
            weaponAnimator.isActiveAndEnabled)
        {
            weaponAnimator.ResetTrigger(ReloadHash);
            weaponAnimator.SetTrigger(FireHash);
        }

        if (muzzleFlash != null)
            muzzleFlash.Play(true);

        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            ZombieHealth zombie =
                hit.collider.GetComponentInParent<ZombieHealth>();

            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                Debug.Log("Pistol hit zombie: " + hit.collider.name);
            }
            else if (logMisses)
            {
                Debug.Log("Pistol hit: " + hit.collider.name);
            }
        }
        else if (logMisses)
        {
            Debug.Log("Pistol missed.");
        }
    }

    public void ReloadWeapon()
    {
        if (isReloading)
            return;

        if (currentAmmo >= magazineCapacity)
            return;

        if (reserveAmmo <= 0)
            return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        if (weaponAnimator != null &&
            weaponAnimator.isActiveAndEnabled)
        {
            weaponAnimator.ResetTrigger(FireHash);
            weaponAnimator.SetTrigger(ReloadHash);
        }

        UpdateAmmoUI();

        yield return new WaitForSeconds(reloadDuration);

        int neededAmmo = magazineCapacity - currentAmmo;

        int ammoToLoad = Mathf.Min(
            neededAmmo, reserveAmmo);

        currentAmmo += ammoToLoad;
        reserveAmmo -= ammoToLoad;

        isReloading = false;

        UpdateAmmoUI();

        Debug.Log(
            "Reload complete: " + currentAmmo + " / " + reserveAmmo);
    }

    public void AddAmmoReward(int amount)
    {
        if (amount <= 0)
            return;

        int previousAmmo = reserveAmmo;

        reserveAmmo = Mathf.Min(
            reserveAmmo + amount,
            maxReserveAmmo);

        int addedAmmo = reserveAmmo - previousAmmo;

        UpdateAmmoUI();

        Debug.Log(
            "Zombie kill ammunition reward: +" + addedAmmo);
    }

    public void AddAmmoForWave(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo = Mathf.Min(
            reserveAmmo + amount,
            maxReserveAmmo);

        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = isReloading
                ? "RELOADING..."
                : currentAmmo + " / " + reserveAmmo;
        }
    }
}
