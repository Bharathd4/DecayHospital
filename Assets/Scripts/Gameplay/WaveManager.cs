
using Unity.Netcode;
using System.Collections;
using UnityEngine;

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

    private int zombiesToSpawn;
    private bool spawning;
    private bool gameWon;
    private bool soloStarted;
    private bool coOpStarted;

    // Wave announcement
    private int lastAnnouncedWave = 0;
    private float announcementEndTime = 0f;

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

        Debug.Log(
            "WaveManager ready. Waiting for game mode selection."
        );
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

        netWave.Value = 0;
        netAlive.Value = 0;
        netKills.Value = 0;
        netPoints.Value = 0;
        netCompletedWaves.Value = 0;

        completedWaves = 0;

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
    }

    [ClientRpc]
    private void RestorePlayersHealthClientRpc()
    {
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>();

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


    // ---------------- HUD AND ANNOUNCEMENT ----------------

    private void OnGUI()
    {
        // SOLO: show after solo mode starts.
        bool soloGameActive = soloStarted;

        // CO-OP: show after the networked first wave starts.
        bool coOpGameActive =
            IsSpawned && netWave.Value > 0;

        if (!soloGameActive && !coOpGameActive)
            return;

        int displayWave =
            coOpGameActive ? netWave.Value : currentWave;

        int displayZombies =
            coOpGameActive ? netAlive.Value : zombiesAlive;

        int displayKills =
            coOpGameActive ? netKills.Value : totalKills;

        int displayPoints =
            coOpGameActive ? netPoints.Value : points;

        GUI.Label(
            new Rect(20, 60, 400, 30),
            $"WAVE: {displayWave}/{zombiesPerWave.Length}"
        );

        GUI.Label(
            new Rect(20, 90, 400, 30),
            $"ZOMBIES: {displayZombies}"
        );

        GUI.Label(
            new Rect(20, 120, 400, 30),
            $"KILLS: {displayKills}   POINTS: {displayPoints}"
        );

        if (gameWon)
        {
            GUI.Label(
                new Rect(20, 160, 400, 40),
                "ALL WAVES COMPLETED!"
            );
        }

        // Highlighted wave-start announcement.
        if (Time.time < announcementEndTime)
        {
            GUIStyle waveStyle =
                new GUIStyle(GUI.skin.label);

            waveStyle.fontSize = 48;
            waveStyle.fontStyle = FontStyle.Bold;
            waveStyle.alignment = TextAnchor.MiddleCenter;
            waveStyle.normal.textColor = Color.yellow;

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
