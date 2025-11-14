using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float Xsensitivity = 400f;
    public float Ysensitivity = 400f;

    public Transform orientation;

    float xRot;
    float yRot;

    [Tooltip("Min and Max Look angle (looking up and down)")]
    public Vector2 YLookBounds;

    public bool CursorLocked = true;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Mouse Input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * Xsensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * Ysensitivity;

        yRot += mouseX;
        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, YLookBounds.x, YLookBounds.y);

        // Rotate Camera and Orientation
        transform.rotation = Quaternion.Euler(xRot, yRot, 0);
        orientation.rotation = Quaternion.Euler(0, yRot,0);
    }
}
