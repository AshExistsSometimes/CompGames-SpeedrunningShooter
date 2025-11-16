using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float Xsensitivity = 400f;
    public float Ysensitivity = 400f;

    public float myFOV; public float DefaultFOV;

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

        DefaultFOV = GetComponent<Camera>().fieldOfView;
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

    public void DoFov(float endValue, float lerpTime)
    {
        myFOV = GetComponent<Camera>().fieldOfView;

        GetComponent<Camera>().fieldOfView = Mathf.Lerp(myFOV, endValue, lerpTime);
    }

    public void ResetFOV(float lerpTime)
    {
        myFOV = GetComponent<Camera>().fieldOfView;

        GetComponent<Camera>().fieldOfView = Mathf.Lerp(myFOV, DefaultFOV, lerpTime);
    }
    public void ForceResetFOV()
    {
        GetComponent<Camera>().fieldOfView = DefaultFOV;
    }
}
