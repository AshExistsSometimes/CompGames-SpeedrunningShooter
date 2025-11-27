using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamagable
{
    [Header("Health Settings")]
    public int HP = 10;
    public int MaxHP = 10;
    public Color DamageFlashColour = Color.white;
    [Space]
    public float DamageFlashTime = 0.1f;
    [Space]

    [Header("Damage Settings")]
    [Tooltip("Damage dealt per attack")]
    public int AttackDamage = 5;
    [Tooltip("Attacks per second")]
    public float AttackRate = 1;
    [Space]
    [HideInInspector]public bool attackOnCooldown = false;
    [HideInInspector] public float attackCooldownTimer;
    [Tooltip("Distance that the enemy can see the player from")]
    public float TargetingRange = 15f;
    private bool playerInTargetRange;
    [Tooltip("Distance that the enemy can attack the player from")]
    public float AttackRange = 15f;
    [HideInInspector]public bool playerInAttackRange;
    [Space]

    [Header("AI Settings")]
    public NavMeshAgent agent;
    [Space]

    [Header("Movement Settings")]
    public float Speed = 3.5f;
    [Space]

    [Header("References")]
    public Transform player;
    public LayerMask groundLayer, playerLayer;

    private Renderer renderer;
    private Color originalColour;

    public void Start()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();

        HP = MaxHP;

        agent.speed = Speed;

        renderer = GetComponentInChildren<Renderer>();
        originalColour = renderer.material.color;
    }

    public void Update()
    {
        // Cooldown logic
        if (attackOnCooldown)
        {
            attackCooldownTimer -= Time.deltaTime;
            if (attackCooldownTimer <= 0f)
                attackOnCooldown = false;
        }


        // Check if the player is in targeting range
        playerInTargetRange = Physics.CheckSphere(transform.position, TargetingRange, playerLayer);
        // Check if the player is in attacking range
        playerInAttackRange = Physics.CheckSphere(transform.position, AttackRange, playerLayer);


        SelectMovementState();
    }

    public void SelectMovementState()
    {
        if (!playerInTargetRange && !playerInAttackRange) Idle();
        if (playerInTargetRange && !playerInAttackRange) GetInRangeOfPlayer();
        if (playerInTargetRange && playerInAttackRange) AttackPlayer();
    }

    // Taking Damage Logic //
    public void TakeDamage(int damage)
    {
        HP -= damage;

        StartCoroutine(DamageFlash());

        if (HP <= 0)
        {
            Die();
        }
    }

    public IEnumerator DamageFlash()
    {
        renderer.material.color = DamageFlashColour;
        yield return new WaitForSeconds(DamageFlashTime);
        renderer.material.color = originalColour;
    }


    public void Die()
    {
        Destroy(gameObject);
    }

    // AI Logic //
    public virtual void Idle()
    {
        // Logic for when the player is outside the targeting range, stand still by default

        // DEFAULT - MELEE
        agent.SetDestination(transform.position);
    }
    
    public virtual void GetInRangeOfPlayer()
    {
        // Logic to get player in attack range, walk up to player by default, but can be overwritten in scripts that are subclasses of this, IE: Ranged Enemies

        // DEFAULT - MELEE
        agent.SetDestination(player.position);

        // Face movement direction
        if (agent.velocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
    }

    public virtual void AttackPlayer()
    {
        // Logic to attack player, melee by default, but can be overwritten in scripts that are subclasses of this, IE: Ranged Enemies

        // DEFAULT - MELEE
        agent.SetDestination(transform.position);

        // Look at player
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);

        // Attack if not on cooldown
        if (!attackOnCooldown)
        {
            IDamagable dmg = player.GetComponent<IDamagable>();
            if (dmg != null)
                dmg.TakeDamage(AttackDamage);

            attackOnCooldown = true;
            attackCooldownTimer = 1f / AttackRate;
        }

    }


}
