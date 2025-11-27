using UnityEngine;
using UnityEngine.UI;

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

    private Transform grappledEnemy;
    private Transform swingPointTransform; // Only used when grappling enemies

    [Header("Cooldown")]
    public int SwingCounter = 3;
    public int maxSwings = 3;
    public Slider GrappleCooldownSlider;

    [Header("Swinging Movement")]
    public float horizontalThrustForce;
    public float forwardThrustForce;
    public float extendCableSpeed;

    [Header("Prediction")]
    public RaycastHit predictionHit;
    public float predictionSphereCastRadius = 3f;
    public Transform predictionPoint;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(swingKey) && !playerMovement.isDead)
        {
            StartSwing();
        }
        if (Input.GetKeyUp(swingKey))
        {
            StopSwing();
        }

        CheckForSwingPoints();

        if (joint != null) GrappleMovement();

        if (playerMovement.grounded)
        {
            SwingCounter = maxSwings;
            GrappleCooldownSlider.value = SwingCounter;
        }

        if (playerMovement.isDead)
        {
            StopSwing();
        }
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    // Logic
    private void StartSwing()
    {
        // if theres nowhere to swing to, return
        if (predictionHit.point == Vector3.zero) { return; }
        if (SwingCounter <= 0) { return; }

        playerMovement.swinging = true;
        SwingCounter -= 1;
        GrappleCooldownSlider.value = SwingCounter;

        // Check if hit object is on Enemy layer
        bool hitEnemy = predictionHit.transform != null &&
                        predictionHit.transform.gameObject.layer == LayerMask.NameToLayer("Enemy");

        if (hitEnemy)
        {
            grappledEnemy = predictionHit.transform;

            // Create a moving grapple point as a child of the enemy
            GameObject movingPoint = new GameObject("GrappleAttachPoint");
            movingPoint.transform.SetParent(grappledEnemy);
            movingPoint.transform.position = predictionHit.point;

            swingPointTransform = movingPoint.transform;
            swingPoint = swingPointTransform.position;

            predictionPoint.position = swingPoint;
        }
        else
        {
            grappledEnemy = null;
            swingPointTransform = null;
            swingPoint = predictionHit.point;
            predictionPoint.position = swingPoint;
        }

        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;

        if (hitEnemy)
        {
            Rigidbody enemyRb = grappledEnemy.GetComponent<Rigidbody>();

            joint.connectedBody = enemyRb;
            joint.connectedAnchor = enemyRb.transform.InverseTransformPoint(swingPointTransform.position);
        }
        else
        {
            joint.connectedAnchor = swingPoint; // original behavior
        }

        float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        joint.spring = GrappleSpringValue;
        joint.damper = GrappleDamperValue;
        joint.massScale = GrappleMassScale;

        GrappleLine.positionCount = 2;
        currentGrapplePosition = GrappleTip.position;
    }

    private void StopSwing()
    {
        playerMovement.swinging = false;
        GrappleLine.positionCount = 0;

        if (swingPointTransform != null)
            Destroy(swingPointTransform.gameObject);

        grappledEnemy = null;
        swingPointTransform = null;

        Destroy(joint);
    }

    private void DrawRope()
    {
        if (!joint) return;

        // If grappling enemy, update dynamic point
        if (swingPointTransform != null)
            swingPoint = swingPointTransform.position;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 0.1f);

        GrappleLine.SetPosition(0, GrappleTip.position);
        GrappleLine.SetPosition(1, swingPoint);

        // Keep prediction point stuck to rope end
        predictionPoint.position = swingPoint;

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


    private void CheckForSwingPoints()
    {
        if (SwingCounter <= 0) { predictionPoint.gameObject.SetActive(false); return; }
        if (joint != null) return;

        RaycastHit sphereCastHit;
        Physics.SphereCast(cam.position, predictionSphereCastRadius, cam.forward, out sphereCastHit, maxSwingDistance, GrappleLayers);

        RaycastHit raycastHit;
        Physics.Raycast(cam.position, cam.forward, out raycastHit, maxSwingDistance, GrappleLayers);


        Vector3 realHitPoint;
        // Direct Hit
        if (raycastHit.point != Vector3.zero)
        {
            realHitPoint = raycastHit.point;
        }

        // Predicted Hit
        else if (sphereCastHit.point != Vector3.zero)
        {
            realHitPoint = sphereCastHit.point;
        }

        // Miss
        else { realHitPoint = Vector3.zero; }


        // realHitPoint found
        if (realHitPoint != Vector3.zero)
        {
            predictionPoint.gameObject.SetActive(true);
            predictionPoint.position = realHitPoint;
        }
        // No realHitPoint found
        else
        {
            predictionPoint.gameObject.SetActive(false);
        }

        predictionHit = raycastHit.point == Vector3.zero ? sphereCastHit : raycastHit;
    }
}
