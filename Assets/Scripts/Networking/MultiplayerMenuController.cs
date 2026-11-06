
using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MultiplayerMenuController : MonoBehaviour
{
    [Header("Room UI")]
    public TMP_InputField roomCodeInput;
    public TMP_Text roomCodeText;

    [Header("Mobile Controls")]
    public GameObject mobileControls;
    public bool showMobileControlsInEditor = true;

    private void Start()
    {
        SetMobileControls(false);
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

    public async void CreateRoom()
    {
        try
        {
            string code =
                await SessionManager.Instance.CreateGame();

            if (string.IsNullOrEmpty(code))
            {
                roomCodeText.text = "ROOM CREATION FAILED";
                return;
            }

            roomCodeText.text = "ROOM CODE: " + code;
            Debug.Log("Room created: " + code);
        }
        catch (System.Exception e)
        {
            roomCodeText.text = "CREATE ROOM FAILED";
            Debug.LogError("Create room failed: " + e);
        }
    }

    public async void JoinRoom()
    {
        try
        {
            string code =
                roomCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(code))
            {
                roomCodeText.text = "ENTER A ROOM CODE";
                return;
            }

            roomCodeText.text = "JOINING ROOM...";

            bool joined =
                await SessionManager.Instance.JoinGame(code);

            if (!joined)
            {
                roomCodeText.text = "JOIN FAILED";
                return;
            }

            roomCodeText.text = "JOINED! WAITING FOR HOST...";

            Debug.Log(
                "Joined session. Waiting for host Relay connection."
            );

            StartCoroutine(WaitForClientConnection());
        }
        catch (System.Exception e)
        {
            roomCodeText.text = "JOIN FAILED";
            Debug.LogError("Join room failed: " + e);
        }
    }

    private IEnumerator WaitForClientConnection()
    {
        float elapsed = 0f;
        float timeout = 90f;

        while (elapsed < timeout)
        {
            NetworkManager nm = NetworkManager.Singleton;

            if (nm != null &&
                nm.IsClient &&
                nm.IsConnectedClient)
            {
                Debug.Log("CLIENT CONNECTED TO HOST!");

                Door.OpenAllDoors();

                roomCodeText.text =
                    "CONNECTED! ENTERING GAME...";

                SetMobileControls(true);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                gameObject.SetActive(false);

                yield break;
            }

            elapsed += 0.25f;

            yield return new WaitForSecondsRealtime(0.25f);
        }

        roomCodeText.text =
            "CONNECTION TIMEOUT - TRY AGAIN";

        Debug.LogError(
            "Client connection timed out. Check host Relay and NGO."
        );
    }

    public async void StartSurvival()
    {
        if (SessionManager.Instance == null ||
            SessionManager.Instance.CurrentSession == null)
        {
            roomCodeText.text =
                "CREATE OR JOIN A ROOM FIRST";
            return;
        }

        if (!SessionManager.Instance.CurrentSession.IsHost)
        {
            roomCodeText.text =
                "WAITING FOR HOST TO START";
            return;
        }

        roomCodeText.text = "STARTING CO-OP...";

        bool started =
            await SessionManager.Instance.StartGame();

        if (!started)
        {
            roomCodeText.text =
                "START FAILED - CHECK CONSOLE";

            Debug.LogError(
                "Could not start Co-op Survival."
            );

            return;
        }

        Debug.Log("RELAY NETWORK STARTED");

        roomCodeText.text = "CONNECTING PLAYERS...";

        WaveManager waveManager = WaveManager.Instance;

        if (waveManager == null)
        {
            roomCodeText.text =
                "WAVE MANAGER NOT FOUND";

            Debug.LogError("WaveManager not found.");

            return;
        }

        StartCoroutine(WaitForWaveManager(waveManager));
    }

    private IEnumerator WaitForWaveManager(
        WaveManager waveManager)
    {
        float elapsed = 0f;
        float timeout = 15f;

        while (!waveManager.IsSpawned &&
               elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!waveManager.IsSpawned)
        {
            roomCodeText.text =
                "NETWORK SETUP FAILED";

            Debug.LogError(
                "WaveManager did not spawn. Check its NetworkObject and scene setup."
            );

            yield break;
        }

        if (!NetworkManager.Singleton.IsServer)
            yield break;

        Door.OpenAllDoors();

        waveManager.StartCoOpWaves();

        SetMobileControls(true);

        Debug.Log("CO-OP waves started by host.");

        roomCodeText.text =
            "CO-OP SURVIVAL STARTED";

        gameObject.SetActive(false);
    }
}
