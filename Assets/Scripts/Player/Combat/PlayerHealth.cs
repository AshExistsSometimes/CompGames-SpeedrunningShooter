using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [Header("Health Settings")]
    public int HP = 100;
    public int MaxHP = 100;
    [Space]
    public float ImmunityTime = 0.25f;
    private bool IsInvulnerable = false;
    [Space]
    [Header("Death Settings")]
    public float LevelRestartCooldownTimer = 0.25f;
    private bool CanRestartLevel = false;
    [Space]
    [Header("References")]
    public Slider HPSlider;
    public GameObject DeathScreen;
    public PlayerMovement player;

    //
    private void Start()
    {
        HPSlider.maxValue = MaxHP;
        UpdateSlider();

        HP = MaxHP;

        CanRestartLevel = false;

        DeathScreen.SetActive(false);
    }

    private void Update()
    {
        if (!player.isDead) { return; }

        if (CanRestartLevel && Input.anyKeyDown)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
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
        DeathScreen.SetActive(true);
        player.isDead = true;
        StartCoroutine(WaitForCooldown());
    }

    //

    private void UpdateSlider()
    {
        HPSlider.value = HP;
    }

    private IEnumerator WaitForCooldown()
    {
        yield return new WaitForSeconds(LevelRestartCooldownTimer);
        CanRestartLevel = true;
    }
}
