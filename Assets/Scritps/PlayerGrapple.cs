using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrapple : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference aimAction;
    public InputActionReference grappleAction;
    public InputActionReference grapplePullAction;
    public InputActionReference grappleReelAction;
    public InputActionReference grappleScrollAction;
    [Header("Grapple")]
    public Transform grappleOrigin;
    public LayerMask grappleSurfaceLayer;
    public float maxGrappleDistance = 10f;
    private float originalGrappleDistance;
    [Header("Components")]
    public DistanceJoint2D grappleJoint;
    public LineRenderer ropeRenderer;

    [Header("Continuous Reel")]
    public float grappleReelSpeed = 2f;

    private bool reachedInsideTargetDistance;

    [Header("Scroll Reel")]
    public float scrollReelAmount = 0.5f;

    private Camera mainCamera;
    private Vector2 grapplePoint;
    public bool IsGrappling => isGrappling;
    public bool IsRopeTaut => isGrappling && !grappleIsSlack;
    private bool isGrappling;
    private bool grappleIsSlack;

    [Header("Grapple Pull")]
    public float grapplePullForce = 6f;
    public float grappleShortenAmount = 1.5f;
    //public float grappleReelSpeed = 3f;
    public float minimumGrappleDistance = 1f;
    private bool wasGrounded;

    private Rigidbody2D rb;

    private float targetGrappleDistance;
    //private bool isReeling;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();

        grappleJoint.enabled = false;
        ropeRenderer.enabled = false;
    }

    private void OnEnable()
    {
        aimAction.action.Enable();
        grappleAction.action.Enable();
        grapplePullAction.action.Enable();
        grappleReelAction.action.Enable();
        grappleScrollAction.action.Enable();

        grappleAction.action.performed += StartGrapple;
        grappleAction.action.canceled += StopGrapple;
        grapplePullAction.action.performed += GrapplePull;
    }

    private void OnDisable()
    {
        grappleAction.action.performed -= StartGrapple;
        grappleAction.action.canceled -= StopGrapple;
        grapplePullAction.action.performed -= GrapplePull;

        aimAction.action.Disable();
        grappleAction.action.Disable();
        grapplePullAction.action.Disable();
        grappleReelAction.action.Disable();
        grappleScrollAction.action.Disable();
    }

    private void Update()
    {
        if (isGrappling)
        {
            ropeRenderer.SetPosition(0, grappleOrigin.position);
            ropeRenderer.SetPosition(1, grapplePoint);
        }
    }

    private void FixedUpdate()
    {
        if (!isGrappling)
            return;

        if (!grappleIsSlack)
        {
            float scrollY =
                grappleScrollAction.action.ReadValue<Vector2>().y;

            if (scrollY > 0.01f)
            {
                // Scroll UP = shorten rope.
                grappleJoint.distance = Mathf.Max(
                    grappleJoint.distance - scrollReelAmount,
                    minimumGrappleDistance
                );
            }
            else if (scrollY < -0.01f)
            {
                // Scroll DOWN = extend rope.
                grappleJoint.distance = Mathf.Min(
                    grappleJoint.distance + scrollReelAmount,
                    maxGrappleDistance
                );
            }
        }

        // MMB - continuously reel the rope in without a pulse.
        // Only do this while the rope is taut.
        if (!grappleIsSlack &&
            grappleReelAction.action.IsPressed())
        {
            grappleJoint.distance = Mathf.MoveTowards(
                grappleJoint.distance,
                minimumGrappleDistance,
                grappleReelSpeed * Time.fixedDeltaTime
            );
        }

        // If we're not in an LMB pulse, there's nothing else to do.
        if (!grappleIsSlack)
            return;

        Vector2 fromGrapple = rb.position - grapplePoint;
        float distanceFromGrapple = fromGrapple.magnitude;

        // SAFETY ROPE:
        // While the joint is disabled for the pulse,
        // don't allow Granny farther away than the original rope length.
        if (distanceFromGrapple > originalGrappleDistance)
        {
            Vector2 ropeDirection = fromGrapple.normalized;

            rb.position =
                grapplePoint +
                ropeDirection * originalGrappleDistance;

            // Remove velocity travelling away from the grapple point.
            float outwardVelocity =
                Vector2.Dot(rb.linearVelocity, ropeDirection);

            if (outwardVelocity > 0f)
            {
                rb.linearVelocity -=
                    ropeDirection * outwardVelocity;
            }

            distanceFromGrapple = originalGrappleDistance;
        }

        // Granny must first travel inside the new shorter radius.
        if (!reachedInsideTargetDistance)
        {
            if (distanceFromGrapple < targetGrappleDistance)
            {
                reachedInsideTargetDistance = true;
            }

            return;
        }

        // Granny has travelled inward and is now moving back out.
        // Catch her at the new shorter rope length.
        if (distanceFromGrapple >= targetGrappleDistance)
        {
            grappleJoint.distance = targetGrappleDistance;
            grappleJoint.enableCollision = true;
            grappleJoint.enabled = true;

            grappleIsSlack = false;
            reachedInsideTargetDistance = false;
        }
    }

    private void StartGrapple(InputAction.CallbackContext context)
    {
        Vector2 mouseScreenPosition =
            aimAction.action.ReadValue<Vector2>();

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 direction =
            ((Vector2)mouseWorldPosition -
            (Vector2)grappleOrigin.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            grappleOrigin.position,
            direction,
            maxGrappleDistance,
            grappleSurfaceLayer
        );

        if (hit.collider == null)
            return;

        grapplePoint = hit.point;

        grappleJoint.connectedBody = null;
        grappleJoint.connectedAnchor = grapplePoint;

        // RMB simply attaches the rope at its current length.
        float currentDistance = Vector2.Distance(
            rb.position,
            grapplePoint
        );

        grappleJoint.distance = currentDistance;

        grappleJoint.enableCollision = true;
        grappleJoint.enabled = true;

        ropeRenderer.positionCount = 2;
        ropeRenderer.SetPosition(0, grappleOrigin.position);
        ropeRenderer.SetPosition(1, grapplePoint);
        ropeRenderer.enabled = true;

        isGrappling = true;
        grappleIsSlack = false;
        reachedInsideTargetDistance = false;
    }

    private void StopGrapple(InputAction.CallbackContext context)
    {


        grappleJoint.enabled = false;
        ropeRenderer.enabled = false;

        isGrappling = false;
        grappleIsSlack = false;
        reachedInsideTargetDistance = false;
        DisconnectGrapple();
    }

    private void GrapplePull(InputAction.CallbackContext context)
    {
        if (!isGrappling || grappleIsSlack)
            return;

        float currentDistance = Vector2.Distance(
            rb.position,
            grapplePoint
        );

        // Remember how long the rope was before the winch.
        // Even while slack, Granny must never get farther away than this.
        originalGrappleDistance = currentDistance;

        targetGrappleDistance = Mathf.Max(
            currentDistance - grappleShortenAmount,
            minimumGrappleDistance
        );

        Vector2 pullDirection =
            (grapplePoint - rb.position).normalized;

        grappleJoint.enabled = false;

        grappleIsSlack = true;
        reachedInsideTargetDistance = false;

        rb.AddForce(
            pullDirection * grapplePullForce,
            ForceMode2D.Impulse
        );
    }

    public void DisconnectGrapple()
    {
        grappleJoint.enabled = false;
        ropeRenderer.enabled = false;

        isGrappling = false;
        grappleIsSlack = false;
        reachedInsideTargetDistance = false;
    }
}