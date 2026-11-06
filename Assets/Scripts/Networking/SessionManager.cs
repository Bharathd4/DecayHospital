
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    public ISession CurrentSession { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async Task InitializeServices()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task<string> CreateGame()
    {
        try
        {
            await InitializeServices();

            var options = new SessionOptions { MaxPlayers = 8 };

            CurrentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log("SESSION CREATED: " + CurrentSession.Code);
            return CurrentSession.Code;
        }
        catch (Exception e)
        {
            Debug.LogError("CREATE SESSION ERROR: " + e);
            return null;
        }
    }

    public async Task<bool> JoinGame(string joinCode)
    {
        try
        {
            await InitializeServices();

            CurrentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(
                    joinCode.Trim().ToUpper()
                );

            Debug.Log("SESSION JOINED");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("JOIN SESSION ERROR: " + e);
            return false;
        }
    }

    public async Task<bool> StartGame()
    {
        try
        {
            if (CurrentSession == null || !CurrentSession.IsHost)
                return false;

            await CurrentSession.AsHost().Network.StartRelayNetworkAsync(
                new RelayNetworkOptions()
            );

            Debug.Log("RELAY NETWORK STARTED");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("START GAME ERROR: " + e);
            return false;
        }
    }
}
