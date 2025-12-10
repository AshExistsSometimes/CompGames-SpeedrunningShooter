using UnityEngine;
using System.Collections;

public class ExplodingEnemy : EnemyBase
{
    [Header("Exploder Settings")]
    public int flashCount = 3;                  // number of flash cycles before exploding
    public float flashInterval = 0.2f;          // time between flash color and revert
    public GameObject explosionPrefab;          // prefab instantiated on death (Explosion.cs)

    private bool isExploding = false;

    // local renderer + colour so we DON'T rely on EnemyBase's private renderer field
    private Renderer localRenderer;
    private Color localOriginalColour;

    private void Start()
    {
        // Ensure base initialisation runs (sets player, agent, cooldowns, etc.)
        base.Start();

        // Cache our own renderer and original colour for flashing
        localRenderer = GetComponentInChildren<Renderer>();
        if (localRenderer != null)
            localOriginalColour = localRenderer.material.color;
    }

    private void Update()
    {
        // Keep base.Update behaviour (cooldowns, range checks, etc.)
        base.Update();

        if (isExploding) return;

        // If in attack range (EnemyBase maintains playerInAttackRange), begin explode
        if (playerInAttackRange)
        {
            StartCoroutine(ExplodeRoutine());
        }
    }

    /// <summary>
    /// Flash red a number of times while stopping movement, then instantiate explosion and die.
    /// </summary>
    private IEnumerator ExplodeRoutine()
    {
        isExploding = true;

        // Stop moving
        if (agent != null)
            agent.SetDestination(transform.position);

        // Flash sequence
        for (int i = 0; i < flashCount; i++)
        {
            if (localRenderer != null)
                localRenderer.material.color = Color.red;

            yield return new WaitForSeconds(flashInterval);

            if (localRenderer != null)
                localRenderer.material.color = localOriginalColour;

            yield return new WaitForSeconds(flashInterval);
        }

        // Spawn explosion prefab at this position (Explosion handles damage)
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // Destroy this enemy
        Die();
    }

    /// <summary>
    /// Prevent default melee damage behavior; explosion handles damage.
    /// </summary>
    public override void AttackPlayer()
    {
        // Intentionally empty - this enemy uses the explode routine instead.
    }
}