
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class CoOpPistolRaycast : NetworkBehaviour
{
    public static CoOpPistolRaycast LocalInstance { get; private set; }

    [Header("Pistol Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float shotsPerSecond = 6f;

    [Header("Ammunition")]
    [SerializeField] private int magazineCapacity = 12;
    [SerializeField] private int startingReserveAmmo = 24;
    [SerializeField] private int maxReserveAmmo = 150;
    [SerializeField] private float reloadDuration = 1.5f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private ParticleSystem muzzleFlash;

    private int currentAmmo;
    private int reserveAmmo;
    private int lastPreparedWave;

    private float nextFireTime;
    private bool isReloading;
    private bool initialized;
    private bool emergencyAmmoUsed;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    private static readonly int FireHash =
        Animator.StringToHash("Fire");

    private static readonly int ReloadHash =
        Animator.StringToHash("Reload");

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        LocalInstance = this;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);

        magazineCapacity = Mathf.Max(1, magazineCapacity);
        maxReserveAmmo = Mathf.Max(150, maxReserveAmmo);

        currentAmmo = magazineCapacity;

        reserveAmmo = Mathf.Clamp(
            startingReserveAmmo, 0, maxReserveAmmo);

        initialized = true;

        Debug.Log(
            $"CO-OP Pistol Ready: {currentAmmo}/{reserveAmmo}");
    }

    public override void OnNetworkDespawn()
    {
        if (LocalInstance == this)
            LocalInstance = null;
    }

    private void Update()
    {
        if (!IsOwner || !initialized)
            return;

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

        Debug.Log(
            $"CO-OP Wave {wave}: Ammo prepared. " +
            $"Target={bufferedShots}, Current={currentAmmo + reserveAmmo}"
        );
    }

    public void Fire()
    {
        if (!IsOwner || !initialized || isReloading)
            return;

        if (Time.time < nextFireTime)
            return;

        if (currentAmmo <= 0)
        {
            if (reserveAmmo <= 0 && !emergencyAmmoUsed)
            {
                reserveAmmo = magazineCapacity;
                emergencyAmmoUsed = true;

                Debug.LogWarning("CO-OP emergency magazine granted.");
            }

            ReloadWeapon();
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("CO-OP player camera is missing.");
            return;
        }

        nextFireTime = Time.time +
            1f / Mathf.Max(0.1f, shotsPerSecond);

        currentAmmo--;

        if (weaponAnimator != null)
        {
            weaponAnimator.ResetTrigger(ReloadHash);
            weaponAnimator.SetTrigger(FireHash);
        }

        if (muzzleFlash != null)
            muzzleFlash.Play(true);

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            CoOpZombieHealth zombieHealth =
                hit.collider.GetComponentInParent<CoOpZombieHealth>();

            if (zombieHealth != null)
            {
                NetworkObject zombieNetworkObject =
                    zombieHealth.GetComponent<NetworkObject>();

                if (zombieNetworkObject != null &&
                    zombieNetworkObject.IsSpawned)
                {
                    RequestZombieDamageServerRpc(
                        zombieNetworkObject.NetworkObjectId,
                        damage);
                }
            }
        }

        Debug.Log(
            $"CO-OP Ammo: {currentAmmo}/{reserveAmmo}");
    }

    public void ReloadWeapon()
    {
        if (!IsOwner || !initialized || isReloading)
            return;

        if (currentAmmo >= magazineCapacity ||
            reserveAmmo <= 0)
            return;

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;

        if (weaponAnimator != null)
        {
            weaponAnimator.ResetTrigger(FireHash);
            weaponAnimator.SetTrigger(ReloadHash);
        }

        yield return new WaitForSeconds(reloadDuration);

        int needed = magazineCapacity - currentAmmo;

        int amountToLoad = Mathf.Min(
            needed, reserveAmmo);

        currentAmmo += amountToLoad;
        reserveAmmo -= amountToLoad;

        isReloading = false;

        Debug.Log(
            $"CO-OP Reload Complete: {currentAmmo}/{reserveAmmo}");
    }

    public void AddAmmoReward(int amount)
    {
        if (!IsOwner || amount <= 0)
            return;

        reserveAmmo = Mathf.Min(
            reserveAmmo + amount,
            maxReserveAmmo);

        Debug.Log(
            $"CO-OP Ammo Reward: +{amount}. " +
            $"Current: {currentAmmo}/{reserveAmmo}");
    }

    [ServerRpc]
    private void RequestZombieDamageServerRpc(
        ulong zombieNetworkObjectId,
        float damageAmount,
        ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
            zombieNetworkObjectId,
            out NetworkObject zombie))
            return;

        CoOpZombieHealth health =
            zombie.GetComponent<CoOpZombieHealth>();

        if (health != null)
        {
            ulong attackerClientId =
                rpcParams.Receive.SenderClientId;

            health.TakeDamage(
                damageAmount,
                attackerClientId);
        }
    }
}
