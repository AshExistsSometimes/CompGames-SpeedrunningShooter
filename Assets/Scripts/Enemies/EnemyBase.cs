using UnityEngine;

public class EnemyBase : MonoBehaviour, IDamagable
{
    [Header("Health Values")]
    public int HP = 10;
    public int MaxHP = 10;
    [Space]
    [Header("Damage Values")]
    public int Damage = 5;

    private void Start()
    {
        HP = MaxHP;
    }
    public void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
