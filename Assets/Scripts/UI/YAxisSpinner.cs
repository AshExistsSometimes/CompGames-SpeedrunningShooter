using UnityEngine;

public class YAxisSpinner : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 45f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
