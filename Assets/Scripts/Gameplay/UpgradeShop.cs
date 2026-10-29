
using UnityEngine;

public class UpgradeShop : MonoBehaviour
{
    [Header("References")]
    public PistolRaycast pistol;

    [Header("Upgrade Costs")]
    public int damageUpgradeCost = 300;
    public int fireRateUpgradeCost = 300;

    [Header("Upgrade Amounts")]
    public float damageIncrease = 10f;
    public float fireRateIncrease = 1f;

    private bool shopOpen;

    private void Update()
    {
        // Press U to open or close the shop during testing.
        if (Input.GetKeyDown(KeyCode.U))
            shopOpen = !shopOpen;
    }

    private void OnGUI()
    {
        if (WaveManager.Instance == null)
            return;

        GUI.Label(
            new Rect(20, 155, 300, 30),
            "POINTS: " + WaveManager.Instance.points
        );

        if (!shopOpen)
        {
            if (GUI.Button(
                new Rect(20, 190, 220, 40),
                "UPGRADES (U)"
            ))
            {
                shopOpen = true;
            }

            return;
        }

        GUI.Box(
            new Rect(20, 190, 300, 170),
            "WEAPON UPGRADES"
        );

        if (GUI.Button(
            new Rect(35, 225, 270, 45),
            $"Damage +{damageIncrease} ({damageUpgradeCost})"
        ))
        {
            BuyDamageUpgrade();
        }

        if (GUI.Button(
            new Rect(35, 280, 270, 45),
            $"Fire Rate +{fireRateIncrease} ({fireRateUpgradeCost})"
        ))
        {
            BuyFireRateUpgrade();
        }

        if (GUI.Button(
            new Rect(35, 325, 270, 25),
            "CLOSE"
        ))
        {
            shopOpen = false;
        }
    }

    private void BuyDamageUpgrade()
    {
        if (pistol == null)
        {
            Debug.LogWarning("UpgradeShop: Assign the pistol reference.");
            return;
        }

        WaveManager manager = WaveManager.Instance;

        if (manager.points < damageUpgradeCost)
            return;

        manager.points -= damageUpgradeCost;
        pistol.damage += damageIncrease;

        Debug.Log("Pistol damage upgraded to " + pistol.damage);
    }

    private void BuyFireRateUpgrade()
    {
        if (pistol == null)
        {
            Debug.LogWarning("UpgradeShop: Assign the pistol reference.");
            return;
        }

        WaveManager manager = WaveManager.Instance;

        if (manager.points < fireRateUpgradeCost)
            return;

        manager.points -= fireRateUpgradeCost;
        pistol.shotsPerSecond += fireRateIncrease;

        Debug.Log("Pistol fire rate upgraded to " +
                  pistol.shotsPerSecond);
    }
}
