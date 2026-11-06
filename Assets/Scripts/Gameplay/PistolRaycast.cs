
using UnityEngine;

public class PistolRaycast : MonoBehaviour
{
    [Header("Pistol Settings")]
    [SerializeField] public float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] public float shotsPerSecond = 4f;

    [Header("Weapon Animation")]
    [SerializeField] private Animator weaponAnimator;
    private static readonly int FireHash =
        Animator.StringToHash("Fire");

    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Testing")]
    [SerializeField] private bool logMisses = false;

    private float nextFireTime;

    private void Update()
    {
        // PC left-click shooting
        if (Input.GetMouseButtonDown(0))
            Fire();
    }

    // Called by the mobile Fire button
    public void Fire()
    {
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

        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range,
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
                Debug.Log("Pistol hit: " + hit.collider.name +
                          " (not a zombie)");
            }
        }
        else if (logMisses)
        {
            Debug.Log("Pistol missed: no collider in range.");
        }

        if (muzzleFlash != null)
            muzzleFlash.Play(true);
    }
}
