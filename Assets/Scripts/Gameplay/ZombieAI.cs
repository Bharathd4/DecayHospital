using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float detectionRange = 20f;

    [Header("Movement")]
    public float movementSpeed = 1.8f;
    public float attackRange = 1.6f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    private NavMeshAgent agent;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("FPS_Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        ApplyMovementSettings();
    }

    public void ConfigureBoss()
    {
        movementSpeed = 1.4f;
        attackDamage = 25f;
        attackCooldown = 1.2f;

        ApplyMovementSettings();
    }

    private void ApplyMovementSettings()
    {
        if (agent != null)
            agent.speed = movementSpeed;
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh)
            return;

        float distance = Vector3.Distance(
            transform.position, player.position
        );

        if (distance > detectionRange)
        {
            agent.isStopped = true;
            return;
        }

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            FacePlayer();
            AttackPlayer();
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
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

        nextAttackTime = Time.time + attackCooldown;

        PlayerHealth health = player.GetComponent<PlayerHealth>();

        if (health != null)
            health.TakeDamage(attackDamage);
    }
}