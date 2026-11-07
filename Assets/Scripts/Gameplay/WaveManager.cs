
using Unity.Netcode;
using System.Collections;
using UnityEngine;
using TMPro;

public class WaveManager : NetworkBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Zombie Prefabs")]
    public GameObject zombiePrefab;
    public GameObject coOpZombiePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Wave Settings")]
    public int[] zombiesPerWave = { 8, 12, 16, 20, 25 };
    public float spawnInterval = 1.5f;
    public float timeBetweenWaves = 5f;

    [Header("Progress")]
    public int currentWave = 0;
    public int zombiesAlive = 0;
    public int totalKills = 0;
    public int points = 0;
    public int completedWaves = 0;

    [Header("Victory Screen")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text wavesCompletedText;
    [SerializeField] private TMP_Text zombiesEliminatedText;
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Main Menu Navigation")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject multiplayerPanel;

    [SerializeField] private float autoReturnDelay = 30f;

    private int zombiesToSpawn;
    private bool spawning;
    private bool gameWon;
    private bool soloStarted;
    private bool coOpStarted;
    private bool victoryShown;

    private Coroutine returnTimer;

    // Wave announcement
    private int lastAnnouncedWave = 0;
    private float announcementEndTime = 0f;

    // HUD styles
    private GUIStyle hudStyle;
    private GUIStyle headingStyle;
    private GUIStyle waveStyle;
    private GUIStyle healthStyle;
    private bool stylesInitialized;

    // CO-OP network variables
    private NetworkVariable<int> netWave =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> netAlive =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> netKills =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> netPoints =
        new NetworkVariable<int>(0);

    private NetworkVariable<int> netCompletedWaves =
        new NetworkVariable<int>(0);

    // ---------------- INITIALIZATION ----------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "WaveManager: Assign at least one Spawn Point."
            );

            enabled = false;
            return;
        }

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        Debug.Log(
            "WaveManager ready. Waiting for game mode selection."
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (currentWave > 0 &&
            currentWave != lastAnnouncedWave)
        {
            lastAnnouncedWave = currentWave;
            announcementEndTime = Time.time + 2.5f;
        }
    }

    // ---------------- NETWORK INITIALIZATION ----------------

    public override void OnNetworkSpawn()
    {
        netWave.OnValueChanged += OnWaveChanged;
        netAlive.OnValueChanged += OnAliveChanged;
        netKills.OnValueChanged += OnKillsChanged;
        netPoints.OnValueChanged += OnPointsChanged;
        netCompletedWaves.OnValueChanged += OnCompletedWavesChanged;

        RefreshNetworkValues();
    }

    public override void OnNetworkDespawn()
    {
        netWave.OnValueChanged -= OnWaveChanged;
        netAlive.OnValueChanged -= OnAliveChanged;
        netKills.OnValueChanged -= OnKillsChanged;
        netPoints.OnValueChanged -= OnPointsChanged;
        netCompletedWaves.OnValueChanged -= OnCompletedWavesChanged;
    }

    // ---------------- SOLO MODE ----------------

    public void StartSoloWaves()
    {
        if (soloStarted)
            return;

        if (zombiePrefab == null)
        {
            Debug.LogError(
                "WaveManager: Assign the SOLO Zombie_Test prefab."
            );

            return;
        }

        soloStarted = true;

        currentWave = 0;
        zombiesAlive = 0;
        totalKills = 0;
        points = 0;
        completedWaves = 0;

        gameWon = false;
        victoryShown = false;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        StartCoroutine(SoloWaveRoutine());

        Debug.Log("SOLO survival started.");
    }

    private IEnumerator SoloWaveRoutine()
    {
        for (int i = 0; i < zombiesPerWave.Length; i++)
        {
            if (i > 0)
                yield return new WaitForSeconds(timeBetweenWaves);

            currentWave = i + 1;
            zombiesToSpawn = zombiesPerWave[i];

            Debug.Log(
                "SOLO Wave " + currentWave + " started."
            );

            spawning = true;

            while (zombiesToSpawn > 0)
            {
                SpawnSoloZombie();
                zombiesToSpawn--;

                yield return new WaitForSeconds(spawnInterval);
            }

            spawning = false;

            // Wait until every zombie in this wave is killed.
            while (zombiesAlive > 0)
                yield return null;

            // Count completed wave.
            completedWaves++;

            // Restore player health after completing the wave.
            PlayerHealth playerHealth =
                FindFirstObjectByType<PlayerHealth>();

            if (playerHealth != null)
                playerHealth.RestoreFullHealth();
        }

        gameWon = true;

        Debug.Log("SOLO SURVIVAL COMPLETE!");

        ShowVictoryScreen(
            completedWaves,
            totalKills,
            points
        );
    }

    private void SpawnSoloZombie()
    {
        Transform point = GetRandomSpawnPoint();

        if (point == null)
            return;

        bool isBoss =
            currentWave == zombiesPerWave.Length &&
            zombiesToSpawn == 1;

        GameObject zombie = Instantiate(
            zombiePrefab,
            point.position,
            point.rotation
        );

        zombie.name = isBoss ? "Hospital_Boss" : "Zombie";

        ZombieAI ai = zombie.GetComponent<ZombieAI>();
        ZombieHealth health = zombie.GetComponent<ZombieHealth>();

        GameObject player = GameObject.Find("FPS_Player");

        if (ai != null && player != null)
            ai.player = player.transform;

        if (isBoss)
        {
            if (health != null)
                health.ConfigureStats(500f, 500);

            if (ai != null)
                ai.ConfigureBoss();

            Debug.Log("SOLO BOSS SPAWNED!");
        }

        zombiesAlive++;
    }

    public void RegisterZombieKilled(int reward = 100)
    {
        // Do not use the SOLO counter in a network session.
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            return;
        }

        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
        totalKills++;
        points += Mathf.Max(0, reward);

        Debug.Log(
            $"SOLO Kills: {totalKills} | Points: {points} | Zombies alive: {zombiesAlive}"
        );
    }

    // ---------------- CO-OP MODE ----------------

    public void StartCoOpWaves()
    {
        if (!IsSpawned || !IsServer)
        {
            Debug.LogError(
                "WaveManager must be spawned on the server before starting CO-OP."
            );

            return;
        }

        if (coOpStarted)
            return;

        if (coOpZombiePrefab == null)
        {
            Debug.LogError(
                "WaveManager: Assign the CO-OP CoOp_Zombie prefab."
            );

            return;
        }

        coOpStarted = true;
        gameWon = false;
        victoryShown = false;

        netWave.Value = 0;
        netAlive.Value = 0;
        netKills.Value = 0;
        netPoints.Value = 0;
        netCompletedWaves.Value = 0;

        completedWaves = 0;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        StartCoroutine(CoOpWaveRoutine());

        Debug.Log("CO-OP survival started by host.");
    }

    private IEnumerator CoOpWaveRoutine()
    {
        for (int i = 0; i < zombiesPerWave.Length; i++)
        {
            if (i > 0)
                yield return new WaitForSeconds(timeBetweenWaves);

            netWave.Value = i + 1;
            currentWave = i + 1;

            zombiesToSpawn = zombiesPerWave[i];

            Debug.Log(
                "CO-OP Wave " + currentWave + " started."
            );

            spawning = true;

            while (zombiesToSpawn > 0)
            {
                SpawnCoOpZombie();
                zombiesToSpawn--;

                yield return new WaitForSeconds(spawnInterval);
            }

            spawning = false;

            // Wait until all players have eliminated this wave.
            while (netAlive.Value > 0)
                yield return null;

            // Count completed CO-OP wave.
            netCompletedWaves.Value++;
            completedWaves = netCompletedWaves.Value;

            // Restore health for all connected players.
            RestorePlayersHealthClientRpc();
        }

        gameWon = true;

        Debug.Log("ALL CO-OP WAVES COMPLETED!");

        ShowCoOpVictoryClientRpc(
            netCompletedWaves.Value,
            netKills.Value,
            netPoints.Value
        );
    }

    [ClientRpc]
    private void ShowCoOpVictoryClientRpc(
        int waves,
        int kills,
        int score)
    {
        gameWon = true;

        completedWaves = waves;
        totalKills = kills;
        points = score;

        ShowVictoryScreen(waves, kills, score);
    }

    [ClientRpc]
    private void RestorePlayersHealthClientRpc()
    {
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsSortMode.None
            );

        foreach (PlayerHealth player in players)
        {
            if (player != null)
                player.RestoreFullHealth();
        }
    }

    private void SpawnCoOpZombie()
    {
        if (!IsServer)
            return;

        Transform point = GetRandomSpawnPoint();

        if (point == null)
            return;

        bool isBoss =
            currentWave == zombiesPerWave.Length &&
            zombiesToSpawn == 1;

        GameObject zombie = Instantiate(
            coOpZombiePrefab,
            point.position,
            point.rotation
        );

        NetworkObject networkObject =
            zombie.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Debug.LogError(
                "CoOp_Zombie requires a NetworkObject component."
            );

            Destroy(zombie);
            return;
        }

        networkObject.Spawn();

        zombie.name = isBoss ? "Hospital_Boss" : "Zombie";

        if (isBoss)
        {
            CoOpZombieHealth health =
                zombie.GetComponent<CoOpZombieHealth>();

            ZombieAI ai = zombie.GetComponent<ZombieAI>();

            if (health != null)
                health.ConfigureStats(500f, 500);

            if (ai != null)
                ai.ConfigureBoss();

            Debug.Log("CO-OP BOSS SPAWNED!");
        }

        netAlive.Value++;
    }

    public void RegisterCoOpZombieKilled(int reward)
    {
        if (!IsServer || !IsSpawned)
            return;

        netAlive.Value = Mathf.Max(0, netAlive.Value - 1);
        netKills.Value++;
        netPoints.Value += Mathf.Max(0, reward);

        Debug.Log(
            $"CO-OP Kills: {netKills.Value} | Points: {netPoints.Value} | Zombies alive: {netAlive.Value}"
        );
    }

    // ---------------- VICTORY SCREEN ----------------

    private void ShowVictoryScreen(
        int waves,
        int kills,
        int score)
    {
        if (victoryShown)
            return;

        victoryShown = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        else
            Debug.LogWarning(
                "Victory Panel is not assigned in WaveManager."
            );

        if (wavesCompletedText != null)
        {
            wavesCompletedText.text =
                "Waves Completed: " + waves + "/" + zombiesPerWave.Length;
        }

        if (zombiesEliminatedText != null)
        {
            zombiesEliminatedText.text =
                "Total Zombies Eliminated: " + kills;
        }

        if (finalScoreText != null)
        {
            finalScoreText.text =
                "Final Score: " + score;
        }

        // Stop local player movement.
        FPSMovement movement =
            FindFirstObjectByType<FPSMovement>();

        if (movement != null)
            movement.enabled = false;

        // Stop local pistol firing.
        PistolRaycast pistol =
            FindFirstObjectByType<PistolRaycast>();

        if (pistol != null)
            pistol.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (returnTimer != null)
            StopCoroutine(returnTimer);

        returnTimer = StartCoroutine(AutoReturnToMainMenu());

        Debug.Log("Victory screen displayed.");
    }

    private IEnumerator AutoReturnToMainMenu()
    {
        yield return new WaitForSecondsRealtime(autoReturnDelay);

        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        if (returnTimer != null)
        {
            StopCoroutine(returnTimer);
            returnTimer = null;
        }

        // Shut down the network session when returning from CO-OP.
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(false);

        if (multiplayerPanel != null)
            multiplayerPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Returned to Main Menu.");
    }

    // ---------------- SHARED HELPERS ----------------

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "WaveManager: No spawn points assigned!"
            );

            return null;
        }

        System.Collections.Generic.List<Transform> validPoints =
            new System.Collections.Generic.List<Transform>();

        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                validPoints.Add(point);
        }

        if (validPoints.Count == 0)
        {
            Debug.LogError(
                "WaveManager: All spawn points are empty!"
            );

            return null;
        }

        return validPoints[
            Random.Range(0, validPoints.Count)
        ];
    }

    // ---------------- NETWORK VALUE REFRESH ----------------

    private void RefreshNetworkValues()
    {
        currentWave = netWave.Value;
        zombiesAlive = netAlive.Value;
        totalKills = netKills.Value;
        points = netPoints.Value;
        completedWaves = netCompletedWaves.Value;
    }

    private void OnWaveChanged(int oldValue, int newValue)
    {
        currentWave = newValue;
    }

    private void OnAliveChanged(int oldValue, int newValue)
    {
        zombiesAlive = newValue;
    }

    private void OnKillsChanged(int oldValue, int newValue)
    {
        totalKills = newValue;
    }

    private void OnPointsChanged(int oldValue, int newValue)
    {
        points = newValue;
    }

    private void OnCompletedWavesChanged(int oldValue, int newValue)
    {
        completedWaves = newValue;
    }

    // ---------------- HUD STYLES ----------------

    private void InitializeHUDStyles()
    {
        if (stylesInitialized)
            return;

        hudStyle = new GUIStyle(GUI.skin.label);
        hudStyle.fontSize = 20;
        hudStyle.fontStyle = FontStyle.Bold;
        hudStyle.normal.textColor = Color.white;

        headingStyle = new GUIStyle(GUI.skin.label);
        headingStyle.fontSize = 23;
        headingStyle.fontStyle = FontStyle.Bold;
        headingStyle.normal.textColor = Color.yellow;

        waveStyle = new GUIStyle(GUI.skin.label);
        waveStyle.fontSize = 48;
        waveStyle.fontStyle = FontStyle.Bold;
        waveStyle.alignment = TextAnchor.MiddleCenter;
        waveStyle.normal.textColor = Color.yellow;

        healthStyle = new GUIStyle(GUI.skin.label);
        healthStyle.fontSize = 20;
        healthStyle.fontStyle = FontStyle.Bold;

        stylesInitialized = true;
    }

    // ---------------- IN-GAME HUD ----------------


    private void OnGUI()
    {
        bool soloGameActive = soloStarted;
        bool coOpGameActive = IsSpawned && netWave.Value > 0;

        if (!soloGameActive && !coOpGameActive)
            return;

        if (victoryShown)
            return;

        InitializeHUDStyles();

        int displayWave = coOpGameActive ? netWave.Value : currentWave;
        int displayZombies = coOpGameActive ? netAlive.Value : zombiesAlive;
        int displayKills = coOpGameActive ? netKills.Value : totalKills;
        int displayPoints = coOpGameActive ? netPoints.Value : points;

        GUI.Box(new Rect(10, 10, 310, 255), "");

        GUI.Label(
            new Rect(25, 18, 285, 32),
            "DECAY - SURVIVAL STATUS",
            headingStyle
        );

        GUI.Label(
            new Rect(25, 55, 280, 28),
            "WAVE: " + displayWave + "/" + zombiesPerWave.Length,
            hudStyle
        );

        GUI.Label(
            new Rect(25, 85, 280, 28),
            "ZOMBIES LEFT: " + displayZombies,
            hudStyle
        );

        GUI.Label(
            new Rect(25, 115, 280, 28),
            "KILLS: " + displayKills,
            hudStyle
        );

        GUI.Label(
            new Rect(25, 145, 280, 28),
            "SCORE: " + displayPoints,
            hudStyle
        );

        // Local player's health
        PlayerHealth playerHealth =
            FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            float healthPercent =
                playerHealth.maxHealth > 0
                ? playerHealth.CurrentHealth / playerHealth.maxHealth
                : 0f;

            healthStyle.normal.textColor =
                healthPercent <= 0.25f ? Color.red :
                healthPercent <= 0.5f ? Color.yellow :
                Color.green;

            GUI.Label(
                new Rect(25, 175, 280, 28),
                "HEALTH: " +
                Mathf.CeilToInt(playerHealth.CurrentHealth) +
                "/" +
                Mathf.CeilToInt(playerHealth.maxHealth),
                healthStyle
            );
        }

        // Correct weapon ammunition for each game mode
        if (coOpGameActive)
        {
            CoOpPistolRaycast pistol =
                CoOpPistolRaycast.LocalInstance;

            if (pistol != null)
            {
                string ammoStatus = pistol.IsReloading
                    ? "RELOADING..."
                    : "AMMO: " + pistol.CurrentAmmo +
                      " / " + pistol.ReserveAmmo;

                GUI.Label(
                    new Rect(25, 205, 280, 28),
                    ammoStatus,
                    hudStyle
                );
            }
            else
            {
                GUI.Label(
                    new Rect(25, 205, 280, 28),
                    "AMMO: CONNECTING...",
                    hudStyle
                );
            }
        }
        else
        {
            PistolRaycast pistol =
                FindFirstObjectByType<PistolRaycast>();

            if (pistol != null)
            {
                GUI.Label(
                    new Rect(25, 205, 280, 28),
                    "AMMO: " + pistol.CurrentAmmo +
                    " / " + pistol.ReserveAmmo,
                    hudStyle
                );
            }
        }

        // Wave announcement
        if (Time.time < announcementEndTime)
        {
            GUI.Label(
                new Rect(
                    0,
                    Screen.height * 0.30f,
                    Screen.width,
                    80
                ),
                "WAVE " + displayWave,
                waveStyle
            );
        }
    }

}

