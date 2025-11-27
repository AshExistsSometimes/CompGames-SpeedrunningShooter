using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private int damage;
    private Vector3 direction;
    private GameObject shooter;
    private LayerMask ignoreLayers;

    private float timer;
    public void Setup(int damage, float speed, float lifetime, Vector3 direction, GameObject shooter, LayerMask ignoreLayers)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.direction = direction.normalized;
        this.shooter = shooter;
        this.ignoreLayers = ignoreLayers;

        timer = 0f;
    }

    private void Update()
    {
        // Move the projectile
        transform.position += direction * speed * Time.deltaTime;

        // Lifetime check
        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == shooter) return;
        if (((1 << other.gameObject.layer) & ignoreLayers) != 0) return;

        // Attempt to get IDamagable from the object or its parent
        IDamagable dmg = other.GetComponentInParent<IDamagable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Anything else: destroy projectile
        Destroy(gameObject);
    }
}
