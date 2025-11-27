using UnityEngine;

public class Explosion : MonoBehaviour
{
    public int Damage;

    public void Setup(int damage)
    {
        this.Damage = damage;
    }
    private void OnTriggerEnter(Collider other)
    {
        IDamagable damagable = other.GetComponentInParent<IDamagable>();

        if (damagable != null)
        {
            damagable.TakeDamage(Damage);
            return;
        }
    }
}
