
using UnityEngine;
using Unity.Netcode;

public class CoOpPistolRaycast : NetworkBehaviour
{
    [Header("Pistol Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float shotsPerSecond = 4f;

    [Header("Weapon Animation")]
    [SerializeField] private Animator weaponAnimator;
    private static readonly int FireHash =
        Animator.StringToHash("Fire");

    [SerializeField] private ParticleSystem muzzleFlash;

    private float nextFireTime;

    private void Update()
    {
        if (!IsOwner)
            return;

        // PC left-click shooting
        if (Input.GetMouseButtonDown(0))
            Fire();
    }

    // Called by PC or mobile Fire button
    public void Fire()
    {
        if (!IsOwner)
            return;

        TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time +
                       1f / Mathf.Max(0.1f, shotsPerSecond);

        if (weaponAnimator != null)
            weaponAnimator.SetTrigger(FireHash);

        Camera cam = GetComponentInChildren<Camera>();

        if (muzzleFlash != null)
            muzzleFlash.Play(true);

        if (cam == null)
            return;

        Ray ray = cam.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            NetworkObject zombie =
                hit.collider.GetComponentInParent<NetworkObject>();

            if (zombie != null)
            {
                RequestZombieDamageServerRpc(
                    zombie.NetworkObjectId,
                    damage
                );
            }
        }
    }

    [ServerRpc]
    private void RequestZombieDamageServerRpc(
        ulong zombieNetworkObjectId,
        float damageAmount)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
            zombieNetworkObjectId, out NetworkObject zombie))
            return;

        CoOpZombieHealth health =
            zombie.GetComponent<CoOpZombieHealth>();

        if (health != null)
            health.TakeDamage(damageAmount);
    }
}
