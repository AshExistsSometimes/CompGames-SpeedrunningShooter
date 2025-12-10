using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Laser : MonoBehaviour
{
    public int damage = 20;
    public float lifetime = 0.25f;
    private bool hasDealtDamage = false;

    private void Start()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;

        Destroy(gameObject, lifetime);
    }

    // Deal damage to IDamagable when player enters the capsule trigger
    private void OnTriggerEnter(Collider other)
    {
        if (hasDealtDamage) return;

        IDamagable dmg = other.GetComponentInParent<IDamagable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }
}