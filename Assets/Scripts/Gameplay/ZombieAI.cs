
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float detectionRange = 20f;
    [SerializeField] private float targetRefreshInterval = 1f;

    [Header("Movement")]
    public float movementSpeed = 2.2f;
    public float attackRange = 1.6f;

    [Header("Crowd Handling")]
    [SerializeField] private float stuckCheckInterval = 1f;
    [SerializeField] private float stuckRecoveryTime = 2.5f;
    [SerializeField] private float movementThreshold = 0.2f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private NetworkObject networkObject;

    private float nextAttackTime;
    private float nextTargetRefresh;
    private float nextStuckCheck;
    private float stuckTimer;

    private Vector3 lastPosition;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        ApplyMovementSettings();

        lastPosition = transform.position;
        RefreshTarget();
    }

    private void ApplyMovementSettings()
    {
        if (agent == null)
            return;

        agent.speed = movementSpeed;
        agent.angularSpeed = 540f;
        agent.acceleration = 12f;
        agent.autoBraking = true;
        agent.autoRepath = true;

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // Give zombies different avoidance priorities.
        agent.avoidancePriority = Random.Range(30, 70);
    }

    private bool IsMultiplayer()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }

    private void RefreshTarget()
    {
        if (IsMultiplayer())
        {
            // Only the server selects targets.
            if (!NetworkManager.Singleton.IsServer)
                return;

            PlayerHealth[] players =
                FindObjectsByType<PlayerHealth>(
                    FindObjectsSortMode.None
                );

            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (PlayerHealth health in players)
            {
                if (health == null ||
                    !health.gameObject.activeInHierarchy)
                    continue;

                NetworkObject playerNetworkObject =
                    health.GetComponent<NetworkObject>();

                if (playerNetworkObject == null ||
                    !playerNetworkObject.IsSpawned)
                    continue;

                float distance = Vector3.Distance(
                    transform.position,
                    health.transform.position
                );

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = health.transform;
                }
            }

            player = closestPlayer;
        }
        else
        {
            // SOLO player detection.
            GameObject playerObject =
                GameObject.Find("FPS_Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        nextTargetRefresh =
            Time.time + targetRefreshInterval;
    }

    public void ConfigureBoss()
    {
        movementSpeed = 1.4f;
        attackDamage = 25f;
        attackCooldown = 1.2f;

        ApplyMovementSettings();
    }

    private void Update()
    {
        // Clients receive zombie movement through NetworkTransform.
        if (networkObject != null &&
            networkObject.IsSpawned &&
            IsMultiplayer() &&
            !NetworkManager.Singleton.IsServer)
        {
            if (agent.enabled)
                agent.enabled = false;

            return;
        }

        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
            return;

        // Periodically find the nearest player.
        if (Time.time >= nextTargetRefresh)
            RefreshTarget();

        if (player == null ||
            !player.gameObject.activeInHierarchy)
        {
            agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > detectionRange)
        {
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        if (distance <= attackRange)
        {
            agent.isStopped = true;

            FacePlayer();
            AttackPlayer();
        }
        else
        {
            agent.isStopped = false;

            // Update destination without unnecessarily
            // recalculating the path every frame.
            if (!agent.pathPending &&
                (!agent.hasPath ||
                 Vector3.Distance(
                     agent.destination,
                     player.position
                 ) > 0.5f))
            {
                agent.SetDestination(player.position);
            }

            CheckIfStuck();
        }
    }

    private void CheckIfStuck()
    {
        if (Time.time < nextStuckCheck)
            return;

        nextStuckCheck =
            Time.time + stuckCheckInterval;

        float movedDistance = Vector3.Distance(
            transform.position,
            lastPosition
        );

        lastPosition = transform.position;

        if (agent.hasPath &&
            !agent.pathPending &&
            agent.remainingDistance > attackRange &&
            movedDistance < movementThreshold)
        {
            stuckTimer += stuckCheckInterval;

            if (stuckTimer >= stuckRecoveryTime)
            {
                // Recalculate the path toward the current target.
                agent.ResetPath();
                agent.SetDestination(player.position);

                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void FacePlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                8f * Time.deltaTime
            );
        }
    }

    private void AttackPlayer()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime =
            Time.time + attackCooldown;

        PlayerHealth health =
            player.GetComponent<PlayerHealth>();

        if (health != null)
            health.TakeDamage(attackDamage);
    }
}
