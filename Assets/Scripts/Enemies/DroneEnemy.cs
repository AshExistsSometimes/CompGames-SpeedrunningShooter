using UnityEngine;

public class DroneEnemy : RangedEnemy
{
    [Header("Drone Movement Settings")]
    public float MoveSpeed = 10f;
    public float HoverSwaySpeed = 1.5f;
    public float HoverSwayAmount = 0.6f;

    [Header("Patrol Settings")]
    public float PatrolArea = 8f;

    [Header("Target Behavior")]
    public float OrbitTargetDistance = 6f;
    public float AbovePlayerOffset = 3f;

    [Header("Drone Detection")]
    public float DetectionRange = 40f;

    private Vector3 startPosition;
    private float hoverTime;

    private bool playerVisible = false;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        base.Update(); // Attack cooldown logic from EnemyBase

        hoverTime += Time.deltaTime;

        DetectPlayer();

        if (playerVisible)
        {
            AttackBehavior();
        }
        else
        {
            PatrolBehavior();
        }
    }

    // Player Detection
    private void DetectPlayer()
    {
        playerVisible = CanSeePlayer();
    }


    // Patrol
    private void PatrolBehavior()
    {
        // Pick a random point inside sphere, but very slowly
        Vector3 randomOffset = new Vector3(
            Mathf.PerlinNoise(hoverTime * 0.2f, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, hoverTime * 0.2f) - 0.5f,
            Mathf.PerlinNoise(hoverTime * 0.2f, hoverTime * 0.2f) - 0.5f
        ) * PatrolArea;

        Vector3 patrolTarget = startPosition + randomOffset;

        MoveToPoint(patrolTarget);
        AddHoverMotion();
    }

    // Attack Player
    private void AttackBehavior()
    {
        if (player == null) return;

        // Always face player
        transform.LookAt(player.position + Vector3.up * 1f);

        float dist = Vector3.Distance(transform.position, player.position);

        // Orbit target position around player
        Vector3 dir = (transform.position - player.position).normalized;
        if (dir == Vector3.zero) dir = Vector3.forward;

        Vector3 orbitPoint = player.position
                           + dir * OrbitTargetDistance
                           + Vector3.up * AbovePlayerOffset;

        MoveToPoint(orbitPoint);
        AddHoverMotion();

        // If in range, attack (inherited logic)
        if (CanSeePlayer())
            AttackPlayer();
    }

    // Movement
    private void MoveToPoint(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        Vector3 move = dir.normalized * MoveSpeed * Time.deltaTime;

        // Prevent overshooting the point
        if (move.sqrMagnitude > dir.sqrMagnitude)
            move = dir;

        transform.position += move;
    }

    private void AddHoverMotion()
    {
        transform.position += new Vector3(
            0f,
            Mathf.Sin(hoverTime * HoverSwaySpeed) * HoverSwayAmount * Time.deltaTime,
            0f
        );
    }

    // Line of Sight Check
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position;
        Vector3 target = player.position + Vector3.up * 0.9f;

        Vector3 dir = (target - origin).normalized;
        float dist = Vector3.Distance(origin, target);

        // Must be within detection range
        if (dist > DetectionRange)
            return false;

        // LOS check
        if (Physics.Raycast(origin, dir, out RaycastHit hit, DetectionRange))
        {
            return hit.collider.transform == player;
        }

        return false;
    }

    // -----------------------------
    // GIZMOS
    // -----------------------------
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, PatrolArea);

        Gizmos.color = Color.yellow;
        if (player != null)
            Gizmos.DrawWireSphere(player.position, OrbitTargetDistance);
    }
}
