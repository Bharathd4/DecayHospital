
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject overviewPanel;
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
        overviewPanel.SetActive(false);
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
            ShowOverview();
        }
    }

    private void SetGameplayActive(bool active)
    {
        if (player != null) player.SetActive(active);
        if (zombie != null) zombie.SetActive(active);
    }

    private void SetMobileControls(bool show)
    {
        if (mobileControls == null) return;

        bool platformAllowed =
            Application.isMobilePlatform ||
            (Application.isEditor && showMobileControlsInEditor);

        mobileControls.SetActive(show && platformAllowed);
    }

    public void ShowOverview()
    {
        introPanel.SetActive(false);
        overviewPanel.SetActive(true);
    }

    public void ContinueToModeSelection()
    {
        overviewPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void ShowModeSelection()
    {
        ShowOverview();
    }

    public void PlaySinglePlayer()
    {
        introPanel.SetActive(false);
        overviewPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(false);

        Time.timeScale = 1f;
        SetGameplayActive(true);

        if (crosshair != null) crosshair.SetActive(true);

        Door.OpenAllDoors();

        if (waveManager != null)
            waveManager.StartSoloWaves();

        SetMobileControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenMultiplayer()
    {
        Time.timeScale = 1f;
        SetMobileControls(false);

        if (crosshair != null) crosshair.SetActive(false);

        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
        SetGameplayActive(false);
    }

    public void BackToIntro()
    {
        overviewPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        introPanel.SetActive(true);

        SetMobileControls(false);
    }

    public void BackToMainMenu()
    {
        overviewPanel.SetActive(false);
        introPanel.SetActive(true);
    }

    public void ExitGameplayToMainMenu()
    {
        Time.timeScale = 0f;

        if (waveManager != null)
            waveManager.StopAllCoroutines();

        SetGameplayActive(false);
        SetMobileControls(false);

        if (crosshair != null)
            crosshair.SetActive(false);

        multiplayerPanel.SetActive(false);
        modeSelectionPanel.SetActive(false);
        overviewPanel.SetActive(false);
        introPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
