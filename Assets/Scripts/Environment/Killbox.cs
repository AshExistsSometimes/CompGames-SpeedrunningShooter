using UnityEngine;

public class Killbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        IDamagable obj = other.GetComponentInParent<IDamagable>();
        if (obj != null)
        {
            obj.Die();
            return;
        }
    }
}
