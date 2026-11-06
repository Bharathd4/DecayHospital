
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject modeSelectionPanel;
    public GameObject multiplayerPanel;

    [Header("Gameplay")]
    public GameObject player;
    public GameObject zombie;
    public WaveManager waveManager;
    public GameObject crosshair;

    [Header("Mobile Controls")]
    public GameObject mobileControls;
    public bool showMobileControlsInEditor = true;

    private void Start()
    {
        introPanel.SetActive(true);
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(false);

        Time.timeScale = 0f;

        SetGameplayActive(false);
        SetMobileControls(false);
    }

    private void Update()
    {
        if (introPanel.activeSelf &&
            (Input.GetMouseButtonDown(0) ||
             Input.anyKeyDown ||
             Input.touchCount > 0))
        {
            ShowModeSelection();
        }
    }

    private void SetGameplayActive(bool active)
    {
        if (player != null)
            player.SetActive(active);

        if (zombie != null)
            zombie.SetActive(active);
    }

    private void SetMobileControls(bool show)
    {
        if (mobileControls == null)
            return;

        bool platformAllowed =
            Application.isMobilePlatform ||
            (Application.isEditor && showMobileControlsInEditor);

        mobileControls.SetActive(show && platformAllowed);
    }

    public void ShowModeSelection()
    {
        introPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);

        SetMobileControls(false);
    }

    public void PlaySinglePlayer()
    {
        introPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(false);

        Time.timeScale = 1f;

        SetGameplayActive(true);

        if (crosshair != null)
            crosshair.SetActive(true);

        Door.OpenAllDoors();

        if (waveManager != null)
            waveManager.StartSoloWaves();

        SetMobileControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenMultiplayer()
    {
        Debug.Log("Opening Co-op Multiplayer");

        Time.timeScale = 1f;

        SetMobileControls(false);

        if (crosshair != null)
            crosshair.SetActive(false);

        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(true);

        SetGameplayActive(false);
    }

    public void BackToIntro()
    {
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        introPanel.SetActive(true);

        SetMobileControls(false);
    }
}
