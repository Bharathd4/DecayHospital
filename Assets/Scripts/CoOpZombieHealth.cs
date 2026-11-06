
using UnityEngine;
using Unity.Netcode;

public class CoOpZombieHealth : NetworkBehaviour
{
    [Header("Zombie Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int killReward = 100;

    private NetworkVariable<float> currentHealth =
        new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private NetworkVariable<bool> isDead =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public float CurrentHealth => currentHealth.Value;
    public float MaxHealth => maxHealth;
    public int KillReward => killReward;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }
    }

    public void ConfigureStats(float health, int reward)
    {
        if (!IsServer)
            return;

        maxHealth = Mathf.Max(1f, health);
        killReward = Mathf.Max(0, reward);
        currentHealth.Value = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer || isDead.Value || damage <= 0f)
            return;

        currentHealth.Value = Mathf.Max(
            0f,
            currentHealth.Value - damage
        );

        Debug.Log(
            $"{gameObject.name}: " +
            $"{currentHealth.Value}/{maxHealth} HP"
        );

        if (currentHealth.Value <= 0f)
            Die();
    }

    private void Die()
    {
        if (!IsServer || isDead.Value)
            return;

        isDead.Value = true;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.RegisterCoOpZombieKilled(
                killReward
            );
        }

        Debug.Log(
            $"{gameObject.name} eliminated. Reward: {killReward}"
        );

        NetworkObject.Despawn(true);
    }
}
