using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health Settings")]
    public int HP = 100;
    public int MaxHP = 100;
    [Space]
    public float ImmunityTime = 0.25f;
    private bool IsInvulnerable = false;
    [Space]
    [Header("References")]
    public Slider HPSlider;

    //
    private void Start()
    {
        HPSlider.maxValue = MaxHP;
        UpdateSlider();

        HP = MaxHP;
    }

    public void TakeDamage(int damage)
    {
        HP -= damage;

        UpdateSlider();

        if (HP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // Death Logic
    }

    //

    private void UpdateSlider()
    {
        HPSlider.value = HP;
    }
}
