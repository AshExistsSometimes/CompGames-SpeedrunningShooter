using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class RangedEnemy : EnemyBase
{
    [Header("Ranged Attack Settings")]
    public float ProjectileSpeed = 20f;
    public float ProjectileLifetime = 5f;

    public GameObject ProjectilePrefab;

    [Tooltip("FirePivot always rotates to face player. FirePoint is where projectiles spawn.")]
    public Transform FirePivot;
    public Transform FirePoint;

    [Tooltip("Enemy tries to stand at AttackRange - RangeOffset ± RangeDeviation")]
    public float RangeOffset = 2f;
    public float RangeDeviation = 1f;

    [Header("Behavior Ranges")]
    [Tooltip("If player is inside this distance, the enemy will back up")]
    public float FleeRange = 3f;

    [Header("Shotgun Settings")]
    public bool EnableShotgun = false;
    public int ShotgunBulletAmount = 5;
    public float ShotgunSpreadAngle = 25f; // full cone angle

    [Header("Burst Settings")]
    public bool EnableBurst = false;
    public int BurstNumber = 3;
    public float BurstSpeed = 0.15f;

    [Header("Homing Settings")]
    public bool EnableHoming = false;
    [Range(0f, 1f)]
    public float HomingStrength = 0.5f;

    private float desiredRange;

    private void Start()
    {
        base.Start();

        desiredRange = AttackRange - RangeOffset + Random.Range(-RangeDeviation, RangeDeviation);
        desiredRange = Mathf.Max(1f, desiredRange);
    }

    private void Update()
    {
        base.Update();

        // FirePivot always points at player
        if (player != null && FirePivot != null)
        {
            Vector3 lookPos = (player.position - FirePivot.position).normalized;
            FirePivot.forward = lookPos;
        }
    }

    public override void GetInRangeOfPlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Too close → flee
        if (dist < FleeRange)
        {
            Vector3 dirAway = (transform.position - player.position).normalized;
            agent.SetDestination(transform.position + dirAway * (FleeRange + 1f));
            return;
        }

        // if the player can be seen, get in range
        if (CanSeePlayer())
        {

            Vector3 dir = (transform.position - player.position).normalized;
            Vector3 desiredPos = player.position + dir * desiredRange;

            agent.SetDestination(desiredPos);

            if (agent.velocity.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);

            return;
        }

        // if the player cant be seen, move closer
        agent.SetDestination(player.position);

        if (agent.velocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
    }

    public override void AttackPlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // flee instead of shooting
        if (dist < FleeRange)
        {
            Vector3 dirAway = (transform.position - player.position).normalized;
            agent.SetDestination(transform.position + dirAway * (FleeRange + 1f));
            return;
        }

        // Enemy body rotates flat-towards player
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        if (attackOnCooldown) return;

        // Fire depending on mode
        if (EnableBurst)
        {
            StartCoroutine(FireBurst());
        }
        else if (EnableShotgun)
        {
            FireShotgunVolley();
        }
        else
        {
            FireSingleProjectile();
        }

        // cooldown
        attackOnCooldown = true;
        attackCooldownTimer = 1f / AttackRate;
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < BurstNumber; i++)
        {
            if (EnableShotgun)
                FireShotgunVolley();
            else
                FireSingleProjectile();

            yield return new WaitForSeconds(BurstSpeed);
        }
    }
    private void FireShotgunVolley()
    {
        if (ProjectilePrefab == null || FirePoint == null) return;

        for (int i = 0; i < ShotgunBulletAmount; i++)
        {
            Quaternion spread = Quaternion.LookRotation(
                RandomConeDirection(FirePivot.forward, ShotgunSpreadAngle)
            );

            SpawnProjectile(spread);
        }
    }

    private void FireSingleProjectile()
    {
        SpawnProjectile(FirePivot.rotation);
    }

    private void SpawnProjectile(Quaternion rotation)
    {
        if (ProjectilePrefab == null || FirePoint == null) return;

        // Spawn projectile
        GameObject obj = Instantiate(ProjectilePrefab, FirePoint.position, rotation);
        Projectile p = obj.GetComponent<Projectile>();
        if (p == null) return;

        Vector3 initialDir = rotation * Vector3.forward;

        // Layers to ignore: enemies + self
        LayerMask ignore = LayerMask.GetMask("Enemy"); // put all enemies in "Enemy" layer
        ignore |= 1 << gameObject.layer;               // also ignore self

        p.Setup(
            AttackDamage,
            ProjectileSpeed,
            ProjectileLifetime,
            initialDir,
            gameObject, // shooter
            ignore
        );
    }
    private Vector3 RandomConeDirection(Vector3 forward, float angle)
    {
        float halfAngle = angle * 0.5f;
        float rad = halfAngle * Mathf.Deg2Rad;

        // random rotation axis
        Vector3 randomAxis = Random.onUnitSphere;
        float randomAngle = Random.Range(0f, rad);

        return (Quaternion.AngleAxis(randomAngle * Mathf.Rad2Deg, randomAxis) * forward).normalized;
    }

    private bool CanSeePlayer()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 target = player.position + Vector3.up * 0.9f;

        Vector3 dir = (target - origin).normalized;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, AttackRange + 1f))
        {
            return hit.collider.transform == player;
        }

        return false;
    }

    // ------------------------------------------------------------------------
    // Gizmos — visualizes shotgun cone from FirePivot
    private void OnDrawGizmosSelected()
    {
        if (!FirePivot) return;

        Gizmos.color = Color.red;

        Vector3 origin = FirePivot.position;
        Vector3 fwd = FirePivot.forward;

        float dist = 2f;
        float half = ShotgunSpreadAngle * 0.5f;

        Quaternion upR = Quaternion.AngleAxis(half, FirePivot.up);
        Quaternion upL = Quaternion.AngleAxis(-half, FirePivot.up);
        Quaternion rtR = Quaternion.AngleAxis(half, FirePivot.right);
        Quaternion rtL = Quaternion.AngleAxis(-half, FirePivot.right);

        Gizmos.DrawLine(origin, origin + upR * fwd * dist);
        Gizmos.DrawLine(origin, origin + upL * fwd * dist);
        Gizmos.DrawLine(origin, origin + rtR * fwd * dist);
        Gizmos.DrawLine(origin, origin + rtL * fwd * dist);
    }
}
