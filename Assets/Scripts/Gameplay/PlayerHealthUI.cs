
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

        // Dark background overlay.
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );
        GUI.color = Color.white;

        WaveManager manager = WaveManager.Instance;

        int finalScore = manager != null ? manager.points : 0;
        int totalKills = manager != null ? manager.totalKills : 0;
        int completedWaves = manager != null
            ? manager.completedWaves
            : 0;

        // GAME OVER title.
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 46;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.red;

        GUI.Label(
            new Rect(0, Screen.height * 0.16f,
                     Screen.width, 70),
            "GAME OVER",
            titleStyle
        );

        // Scorecard.
        GUIStyle scoreStyle = new GUIStyle(GUI.skin.label);
        scoreStyle.fontSize = 25;
        scoreStyle.fontStyle = FontStyle.Bold;
        scoreStyle.alignment = TextAnchor.MiddleCenter;
        scoreStyle.normal.textColor = Color.white;

        float startY = Screen.height * 0.32f;
        float spacing = 45f;

        GUI.Label(
            new Rect(0, startY, Screen.width, 40),
            "FINAL SCORE: " + finalScore,
            scoreStyle
        );

        GUI.Label(
            new Rect(0, startY + spacing, Screen.width, 40),
            "TOTAL KILLS: " + totalKills,
            scoreStyle
        );

        GUI.Label(
            new Rect(0, startY + spacing * 2, Screen.width, 40),
            "WAVES COMPLETED: " + completedWaves,
            scoreStyle
        );

        // Restart button.
        float buttonWidth = 240f;
        float buttonHeight = 55f;

        Rect restartRect = new Rect(
            (Screen.width - buttonWidth) / 2f,
            Screen.height * 0.72f,
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
