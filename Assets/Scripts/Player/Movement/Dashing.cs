using UnityEngine;

public class Dashing : MonoBehaviour
{
    [Header("References")]
    public Transform Orientation;
    public Transform playerCam;
    private Rigidbody rb;
    private PlayerMovement playerMovement;

    [Header("Dashing")]
    public float dashForce = 10f;
    public float dashUpForce = 7f;
    public float dashDuration = 0.5f;
    public float MaxDashYSpeed = 5f;

    [Header("CameraEffects")]
    public PlayerCam cam;
    public float dashFOV;
    public float FOVLerpSpeed;

    [Header("Settings")]
    public bool UseCameraForward = true;
    public bool allowAllDirections = true;
    public bool disableGravity = false;
    public bool resetVel = true;

    [Header("Cooldown")]
    public float DashCooldown = 0.5f;
    public float cooldownTimer;

    [Header("Input")]
    public KeyCode dashKey = KeyCode.LeftShift;

    //

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(dashKey))
        {
            Dash();
        }

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void Dash()
    {
        if (cooldownTimer > 0) {  return; }
        else cooldownTimer = DashCooldown;

        playerMovement.dashing = true;
        playerMovement.maxYSpeed = MaxDashYSpeed;

        cam.DoFov(dashFOV, FOVLerpSpeed);

        Transform forwardT;

        if (UseCameraForward)
        {
            forwardT = playerCam;
        }
        else
        {
            forwardT = Orientation;
        }

        Vector3 direction = GetDirection(forwardT);

        Vector3 forceToApply = direction * dashForce + Orientation.up * dashUpForce;

        if (disableGravity)
        {
            rb.useGravity = false;
        }

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDash), 0.025f);

        Invoke(nameof(ResetDash), dashDuration);
    }

    private Vector3 delayedForceToApply;
    private void DelayedDash()
    {
        if (resetVel)
        {
            rb.linearVelocity = Vector3.zero;
        }

        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        playerMovement.dashing = false;
        playerMovement.maxYSpeed = 0;

        cam.ResetFOV(FOVLerpSpeed);
        cam.ForceResetFOV();

        if (disableGravity)
        {
            rb.useGravity = true;
        }
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3();

        if (allowAllDirections)
        {
            direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;
        }
        else
        {
            direction = forwardT.forward;
        }

        if (verticalInput == 0 && horizontalInput == 0)
        {
            direction = forwardT.forward;
        }

        return direction.normalized;

    }
}
