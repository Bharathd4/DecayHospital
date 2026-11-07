
using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Zombie Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int killReward = 100;

    [Header("Kill Rewards")]
    [SerializeField] private float healthReward = 15f;
    [SerializeField] private int ammoReward = 3;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public int KillReward => killReward;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
    }

    public void ConfigureStats(float health, int reward)
    {
        maxHealth = Mathf.Max(1f, health);
        killReward = Mathf.Max(0, reward);
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        currentHealth = Mathf.Max(
            0f, currentHealth - damage);

        Debug.Log(
            $"{gameObject.name}: {currentHealth}/{maxHealth} HP");

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.RegisterZombieKilled(killReward);
        }

        if (PistolRaycast.Instance != null)
        {
            PistolRaycast.Instance.AddAmmoReward(ammoReward);
        }

        PlayerHealth playerHealth =
            FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.Heal(healthReward);
        }

        Debug.Log(
            $"{gameObject.name} eliminated! " +
            $"+{healthReward} HP, +{ammoReward} bullets.");

        Destroy(gameObject);
    }
}
