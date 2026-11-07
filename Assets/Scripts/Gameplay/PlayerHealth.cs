
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player Health")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(
            0f, CurrentHealth - damage);

        Debug.Log(
            $"Player Health: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float previousHealth = CurrentHealth;

        CurrentHealth = Mathf.Min(
            maxHealth,
            CurrentHealth + amount);

        Debug.Log(
            $"Player healed: +{CurrentHealth - previousHealth} HP. " +
            $"Health: {CurrentHealth}/{maxHealth}");
    }

    public void RestoreFullHealth()
    {
        if (IsDead)
            return;

        CurrentHealth = maxHealth;

        Debug.Log(
            "Player health restored: " + CurrentHealth);
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        FPSMovement movement =
            GetComponent<FPSMovement>();

        if (movement != null)
            movement.enabled = false;

        PistolRaycast pistol =
            GetComponentInChildren<PistolRaycast>();

        if (pistol != null)
            pistol.enabled = false;

        CoOpPistolRaycast coOpPistol =
            GetComponentInChildren<CoOpPistolRaycast>();

        if (coOpPistol != null)
            coOpPistol.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("GAME OVER - Player died.");
    }
}
