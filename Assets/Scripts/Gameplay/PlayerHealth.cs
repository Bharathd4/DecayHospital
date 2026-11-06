
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

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        Debug.Log($"Player Health: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
            Die();
    }
    public void RestoreFullHealth()
    {
        if (IsDead)
            return;

        CurrentHealth = maxHealth;

        Debug.Log("Player health restored: " + CurrentHealth);
    }
    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        FPSMovement movement = GetComponent<FPSMovement>();
        if (movement != null)
            movement.enabled = false;

        // Stop the pistol attached to the Camera.
        PistolRaycast pistol = GetComponentInChildren<PistolRaycast>();
        if (pistol != null)
            pistol.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("GAME OVER - Player died.");
    }
}
