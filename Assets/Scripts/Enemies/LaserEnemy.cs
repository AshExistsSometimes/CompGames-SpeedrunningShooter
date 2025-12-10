using UnityEngine;
using System.Collections;

public class LaserEnemy : EnemyBase
{
    [Header("References (FirePivot / FirePoint like RangedEnemy)")]
    public Transform FirePivot;            // should aim at player (we'll rotate this each frame)
    public Transform FirePoint;            // where the telegraph and laser originate

    [Header("Line & Prefab")]
    public LineRenderer telegraphLine;     // telegraph line (world space)
    public GameObject laserStrikePrefab;   // prefab root (can contain rotated child capsule)
    public float maxGroundDistance = 200f; // fallback distance if no ground hit

    [Header("Telegraph")]
    public int flashCount = 3;
    public float flashInterval = 0.2f;

    [Header("Damage")]
    public int laserDamage = 20;

    [Header("Prefab alignment")]
    [Tooltip("Rotation offset applied to prefab after aligning forward -> direction. Useful when the actual visual capsule is rotated inside prefab.")]
    public Vector3 PrefabAlignmentEuler = new Vector3(0f, 90f, 0f);
    public enum LengthAxis { X, Y, Z }
    [Tooltip("Which local axis of the prefab represents the 'length' that should be scaled to the distance")]
    public LengthAxis lengthAxis = LengthAxis.Y;
    [Tooltip("Multiplier applied when mapping world distance -> prefab local scale on length axis. Tweak to match prefab unit sizes.")]
    public float LengthScaleMultiplier = 0.5f;

    [Header("Raycast")]
    [Tooltip("Maximum raycast used for initial telegraph (this is not ground-check distance)")]
    public float telegraphMaxDistance = 40f;
    public LayerMask telegraphHitMask = ~0;

    private void Start()
    {
        base.Start();
        if (telegraphLine != null)
            telegraphLine.enabled = false;
    }

    private void Update()
    {
        base.Update();

        // Keep FirePivot pointing at player if assigned
        if (player != null && FirePivot != null)
        {
            Vector3 look = (player.position - FirePivot.position).normalized;
            FirePivot.forward = look;
        }
    }

    public override void AttackPlayer()
    {
        if (player == null) return;

        // Stop moving
        agent.SetDestination(transform.position);

        // Face player on the body (flat)
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        if (attackOnCooldown) return;

        StartCoroutine(FireLaserCoroutine());
        attackOnCooldown = true;
        attackCooldownTimer = 1f / Mathf.Max(0.0001f, AttackRate);
    }

    private IEnumerator FireLaserCoroutine()
    {
        // Source origin: prefer FirePoint if set, otherwise fallback to transform position + up
        Vector3 origin = FirePoint != null ? FirePoint.position : (transform.position + Vector3.up * 1.2f);

        // Determine initial shot direction (towards player for telegraph)
        Vector3 shotDir = player != null ? (player.position + Vector3.up * 1f - origin).normalized : transform.forward;

        // Telegraph raycast to preview line
        RaycastHit teleHit;
        Vector3 teleEnd;
        if (Physics.Raycast(origin, shotDir, out teleHit, telegraphMaxDistance, telegraphHitMask))
            teleEnd = teleHit.point;
        else
            teleEnd = origin + shotDir * telegraphMaxDistance;

        // Show telegraph line
        if (telegraphLine != null)
        {
            telegraphLine.enabled = true;
            telegraphLine.positionCount = 2;
            telegraphLine.SetPosition(0, origin);
            telegraphLine.SetPosition(1, teleEnd);

            telegraphLine.material = new Material(telegraphLine.material);
            telegraphLine.material.color = Color.red;
        }

        // Flash sequence: white <-> red
        if (telegraphLine != null)
        {
            Material mat = telegraphLine.material;
            for (int i = 0; i < flashCount; i++)
            {
                mat.color = Color.white;
                yield return new WaitForSeconds(flashInterval);
                mat.color = Color.red;
                yield return new WaitForSeconds(flashInterval);
            }
        }

        // Hide telegraph
        if (telegraphLine != null)
            telegraphLine.enabled = false;

        // Ground-only raycast to determine final laser length
        Vector3 finalEnd;
        RaycastHit groundHit;
        if (Physics.Raycast(origin, shotDir, out groundHit, maxGroundDistance, groundLayer))
            finalEnd = groundHit.point;
        else
            finalEnd = origin + shotDir * maxGroundDistance; // fallback long distance

        // Spawn laser prefab
        if (laserStrikePrefab != null)
        {
            float distance = Vector3.Distance(origin, finalEnd);
            Vector3 midPoint = (origin + finalEnd) * 0.5f;
            Quaternion lookRotation = Quaternion.LookRotation(shotDir, Vector3.up);

            GameObject root = Instantiate(laserStrikePrefab, midPoint, lookRotation);

            // Get visual child
            Transform laserChild = root.transform.Find("Laser") ?? (root.transform.childCount > 0 ? root.transform.GetChild(0) : null);

            if (laserChild != null)
            {
                // Scale child along its Y
                Vector3 childScale = laserChild.localScale;
                childScale.y = distance;
                laserChild.localScale = childScale;

                // Position so base stays at root
                Vector3 childPos = laserChild.localPosition;
                childPos.z = distance * 0.5f;
                laserChild.localPosition = childPos;

                // Set damage
                Laser childLaserScript = laserChild.GetComponent<Laser>();
                if (childLaserScript != null)
                    childLaserScript.damage = laserDamage;
            }
            else
            {
                Laser rootLaser = root.GetComponent<Laser>();
                if (rootLaser != null)
                    rootLaser.damage = laserDamage;
            }
        }
    }


}