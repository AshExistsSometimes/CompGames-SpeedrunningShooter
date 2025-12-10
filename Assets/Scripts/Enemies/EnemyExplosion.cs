using System.Collections.Generic;
using UnityEngine;

public class EnemyExplosion : MonoBehaviour
{
    [Header("Size & Timing")]
    public float FinalRadius = 4f;         // final radius in world units (meters)
    public float ExpandDuration = 0.5f;    // time in seconds to expand from 0 -> FinalRadius

    [Header("Damage")]
    public int Damage = 25;
    public LayerMask DamageLayers = ~0;    // which layers to consider for damage checks (default: everything)

    [Header("Visual")]
    [Tooltip("Starting alpha (0..1). The material must support transparency.")]
    public float StartAlpha = 0.6f;

    private float timer = 0f;
    private HashSet<Transform> alreadyDamaged = new HashSet<Transform>();

    private void Awake()
    {
        // start fully scaled-down
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        // advance timer
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(ExpandDuration > 0f ? timer / ExpandDuration : 1f);

        float finalDiameter = FinalRadius * 2f;
        float currentDiameter = Mathf.Lerp(0f, finalDiameter, t);
        transform.localScale = Vector3.one * currentDiameter;

        // Damage check: overlap with current radius (use transform.localScale.x / 2 as radius)
        float currentRadius = (transform.localScale.x * 0.5f);
        if (currentRadius > 0f)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, DamageLayers);
            foreach (var c in hits)
            {
                if (c == null) continue;

                // find IDamagable on the collider or its parent
                IDamagable dmg = c.GetComponentInParent<IDamagable>();
                if (dmg != null)
                {
                    Transform root = c.transform.root;
                    if (!alreadyDamaged.Contains(root))
                    {
                        alreadyDamaged.Add(root);
                        dmg.TakeDamage(Damage);
                    }
                }
            }
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    // debug visual for editor
    private void OnDrawGizmosSelected()
    {
        float diam = FinalRadius * 2f;
        Vector3 s = transform.localScale;
        float currentRadius = (s.x * 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentRadius);
    }
}