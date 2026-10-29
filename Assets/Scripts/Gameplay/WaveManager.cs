
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Zombie Prefab")]
    public GameObject zombiePrefab;

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

    private int zombiesToSpawn;
    private bool spawning;
    private bool gameWon;

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
        if (zombiePrefab == null)
        {
            Debug.LogError("WaveManager: Assign Zombie Prefab.");
            enabled = false;
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveManager: Assign at least one Spawn Point.");
            enabled = false;
            return;
        }

        StartCoroutine(StartNextWave());
    }

    private IEnumerator StartNextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        if (currentWave >= zombiesPerWave.Length)
        {
            gameWon = true;
            Debug.Log("SURVIVAL COMPLETE!");
            yield break;
        }

        currentWave++;
        zombiesToSpawn = zombiesPerWave[currentWave - 1];

        Debug.Log(
            $"Wave {currentWave} started. " +
            $"Zombies to spawn: {zombiesToSpawn}"
        );

        StartCoroutine(SpawnZombies());
    }

    private IEnumerator SpawnZombies()
    {
        spawning = true;

        while (zombiesToSpawn > 0)
        {
            SpawnOneZombie();
            zombiesToSpawn--;

            yield return new WaitForSeconds(spawnInterval);
        }

        spawning = false;
    }

    private void SpawnOneZombie()
    {
        Transform point = spawnPoints[
            Random.Range(0, spawnPoints.Length)
        ];

        // The final zombie spawned in the final wave is the boss.
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

            Debug.Log("BOSS SPAWNED! Hospital Boss has arrived.");
        }

        zombiesAlive++;
    }

    public void RegisterZombieKilled(int reward = 100)
    {
        zombiesAlive = Mathf.Max(0, zombiesAlive - 1);
        totalKills++;
        points += Mathf.Max(0, reward);
        Debug.Log(
            $"Kills: {totalKills} | Points: {points} | " +
            $"Zombies alive: {zombiesAlive}"
        );

        if (zombiesAlive == 0 && !spawning && !gameWon)
            StartCoroutine(StartNextWave());
    }

    private void OnGUI()
    {
        GUI.Label(
            new Rect(20, 60, 400, 30),
            $"WAVE: {currentWave}/{zombiesPerWave.Length}"
        );

        GUI.Label(
            new Rect(20, 90, 400, 30),
            $"ZOMBIES: {zombiesAlive}"
        );

        GUI.Label(
            new Rect(20, 120, 400, 30),
            $"KILLS: {totalKills}   POINTS: {points}"
        );

        if (gameWon)
        {
            GUI.Label(
                new Rect(20, 160, 400, 40),
                "ALL WAVES COMPLETED!"
            );
        }
    }
}
