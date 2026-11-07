
using UnityEngine;
using Unity.Netcode;

public class CoOpZombieHealth : NetworkBehaviour
{
    [Header("Zombie Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int killReward = 100;

    [Header("Kill Rewards")]
    [SerializeField] private float healthReward = 15f;
    [SerializeField] private int ammoReward = 3;

    private NetworkVariable<float> currentHealth =
        new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> isDead =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

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

    public void TakeDamage(float damage, ulong attackerClientId)
    {
        if (!IsServer || isDead.Value || damage <= 0f)
            return;

        currentHealth.Value = Mathf.Max(
            0f,
            currentHealth.Value - damage);

        Debug.Log(
            $"{gameObject.name}: " +
            $"{currentHealth.Value}/{maxHealth} HP");

        if (currentHealth.Value <= 0f)
            Die(attackerClientId);
    }

    private void Die(ulong attackerClientId)
    {
        if (!IsServer || isDead.Value)
            return;

        isDead.Value = true;

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.RegisterCoOpZombieKilled(
                killReward);
        }

        Debug.Log(
            $"{gameObject.name} eliminated by client " +
            $"{attackerClientId}. Reward: {killReward}");

        // Reward only the player who delivered the killing shot.
        ClientRpcParams target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { attackerClientId }
            }
        };

        GiveKillerRewardClientRpc(
            healthReward,
            ammoReward,
            target);

        NetworkObject.Despawn(true);
    }

    [ClientRpc]
    private void GiveKillerRewardClientRpc(
        float healthAmount,
        int ammoAmount,
        ClientRpcParams clientRpcParams = default)
    {
        // This RPC is sent only to the client who got the kill.
        CoOpPistolRaycast pistol =
            CoOpPistolRaycast.LocalInstance;

        if (pistol != null)
        {
            pistol.AddAmmoReward(ammoAmount);
        }

        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsSortMode.None);

        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            NetworkObject playerNetworkObject =
                player.GetComponent<NetworkObject>();

            if (playerNetworkObject != null &&
                playerNetworkObject.IsOwner)
            {
                player.Heal(healthAmount);
                break;
            }
        }

        Debug.Log(
            $"CO-OP Kill Reward: +{healthAmount} HP, " +
            $"+{ammoAmount} reserve bullets.");
    }
}
