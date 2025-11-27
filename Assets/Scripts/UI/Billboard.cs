using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    [Header("Size Settings")]
    public float Size = .3f;
    public bool scalesWithDistance = false;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogWarning("Billboard: No main camera found!");

        transform.localScale = Vector3.one * Size;
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        transform.forward = mainCamera.transform.forward;

        if (scalesWithDistance)
        {
            float scaleFactor = Vector3.Distance(transform.position, mainCamera.transform.position) * 0.1f;
            transform.localScale = Vector3.one * scaleFactor;
        }    
    }
}

