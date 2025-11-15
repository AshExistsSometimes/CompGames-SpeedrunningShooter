using UnityEngine;

public class Swinging : MonoBehaviour
{
    [Header("Input")]
    public KeyCode swingKey = KeyCode.Mouse1;// Right Click

    [Header("References")]
    public LineRenderer GrappleLine;
    public Transform GrappleTip, cam, player;
    public LayerMask GrappleLayers;
    public PlayerMovement playerMovement;
    private Rigidbody rb;
    public Transform orientation;

    [Header("Swinging")]
    public float maxSwingDistance = 25f;
    private Vector3 swingPoint;
    private SpringJoint joint;
    [Space]
    public float GrappleSpringValue = 4.5f;
    public float GrappleDamperValue = 7f;
    public float GrappleMassScale = 4.5f;
    private Vector3 currentGrapplePosition;

    [Header("Swinging Movement")]
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(swingKey))
        {
            StartSwing();
        }
        if (Input.GetKeyUp(swingKey))
        {
            StopSwing();
        }

        if (joint != null) GrappleMovement();
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    // Logic
    private void StartSwing()
    {
        playerMovement.swinging = true;

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxSwingDistance, GrappleLayers))
        {
            swingPoint = hit.point;
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = swingPoint;

            float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

            // Distance the grappling hook tries to keep from the grapple point
            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

            joint.spring = GrappleSpringValue;
            joint.damper = GrappleDamperValue;
            joint.massScale = GrappleMassScale;

            GrappleLine.positionCount = 2;
            currentGrapplePosition = GrappleTip.position;
        }
    }

    private void StopSwing()
    {
        playerMovement.swinging = false;
        GrappleLine.positionCount = 0;
        Destroy(joint);
    }

    private void DrawRope()
    {
        if (!joint) { return; }

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 0.1f);

        GrappleLine.SetPosition(0, GrappleTip.position);
        GrappleLine.SetPosition(1, swingPoint);
    }


    private void GrappleMovement()
    {
        // Right
        if (Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * horizontalThrustForce * Time.deltaTime);
        // Left
        if (Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * horizontalThrustForce * Time.deltaTime);

        // Forward
        if (Input.GetKey(KeyCode.W)) rb.AddForce(orientation.forward * forwardThrustForce * Time.deltaTime);


        // Shorten Cable
        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;
        }
        // Extend Cable
        if (Input.GetKey(KeyCode.S))
        {
            float extendedDistanceFromPoint = Vector3.Distance(transform.position, swingPoint) + extendCableSpeed;

            joint.maxDistance = extendedDistanceFromPoint * 0.8f;
            joint.minDistance = extendedDistanceFromPoint * 0.25f;
        }
    }

}
