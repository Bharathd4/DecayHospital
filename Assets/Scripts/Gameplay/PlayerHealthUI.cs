
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;

    private void OnGUI()
    {
        if (playerHealth == null)
            return;

        GUIStyle healthStyle = new GUIStyle(GUI.skin.label);
        healthStyle.fontSize = 24;
        healthStyle.fontStyle = FontStyle.Bold;
        healthStyle.normal.textColor = Color.white;

        GUI.Label(
            new Rect(20, 20, 300, 40),
            "HEALTH: " +
            Mathf.CeilToInt(playerHealth.CurrentHealth),
            healthStyle
        );

        if (!playerHealth.IsDead)
            return;

        // Dark overlay.
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 42;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.red;

        GUI.Label(
            new Rect(0, Screen.height * 0.25f,
                     Screen.width, 70),
            "GAME OVER",
            titleStyle
        );

        float buttonWidth = 220f;
        float buttonHeight = 55f;

        Rect restartRect = new Rect(
            (Screen.width - buttonWidth) / 2f,
            Screen.height * 0.55f,
            buttonWidth,
            buttonHeight
        );

        if (GUI.Button(restartRect, "RESTART SURVIVAL"))
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
