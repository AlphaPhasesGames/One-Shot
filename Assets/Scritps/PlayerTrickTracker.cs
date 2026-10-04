using UnityEngine;
using TMPro;

public class PlayerTrickTracker : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public PlayerGrapple grapple;
    [Header("Live Rotation UI")]
    public GameObject rotationUI;
    public TMP_Text rotationText;

    [Header("Last Trick UI")]
    public GameObject lastTrickUI;
    public TMP_Text lastTrickText;
    public TMP_Text lastTrickPointsText;

    [Header("Score UI")]
    public TMP_Text scoreText;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.7f;

    [Header("Rotation")]
    public float minimumRotationToShow = 5f;
    public int pointsPerRotation = 100;

    private float previousRotation;
    private float totalRotation;
    private bool grappleReleasedForRotation;
    private bool wasGrounded;

    private int totalScore;

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        previousRotation = rb.rotation;

        if (rotationUI != null)
            rotationUI.SetActive(false);

        if (lastTrickUI != null)
            lastTrickUI.SetActive(false);

        UpdateRotationUI();
        UpdateScoreUI();
    }

    private void Update()
    {
        bool isGrounded = Physics2D.Raycast(
            rb.position,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        if (wasGrounded && !isGrounded)
        {
            totalRotation = 0f;
            previousRotation = rb.rotation;
            grappleReleasedForRotation = false;

            if (rotationUI != null)
                rotationUI.SetActive(false);
        }



        if (!isGrounded)
        {
            TrackRotation();
        }

        // Granny has just landed.
        if (!wasGrounded && isGrounded)
        {
            FinishRotationTrick();

            if (rotationUI != null)
                rotationUI.SetActive(false);

            totalRotation = 0f;

            UpdateRotationUI();
        }

        wasGrounded = isGrounded;
    }

    private void TrackRotation()
    {
        float currentRotation = rb.rotation;

        float rotationChange = Mathf.DeltaAngle(
            previousRotation,
            currentRotation
        );

        totalRotation += rotationChange;
        previousRotation = currentRotation;

        // Release grapple after one complete rotation.
        if (!grappleReleasedForRotation &&
            grapple != null &&
            grapple.IsGrappling &&
            Mathf.Abs(totalRotation) >= 360f)
        {
            grapple.DisconnectGrapple();
            grappleReleasedForRotation = true;
        }

        if (rotationUI != null &&
            Mathf.Abs(totalRotation) >= minimumRotationToShow)
        {
            rotationUI.SetActive(true);
        }

        UpdateRotationUI();
    }

    private void FinishRotationTrick()
    {
        int completedRotations =
            Mathf.FloorToInt(Mathf.Abs(totalRotation) / 360f);

        // Didn't complete a full rotation.
        if (completedRotations <= 0)
            return;

        int completedDegrees = completedRotations * 360;
        int trickScore = completedRotations * pointsPerRotation;

        totalScore += trickScore;

        if (lastTrickUI != null)
            lastTrickUI.SetActive(true);

        if (lastTrickText != null)
        {
            lastTrickText.text =
                completedDegrees + "° ROTATION";
        }

        if (lastTrickPointsText != null)
        {
            lastTrickPointsText.text =
                "+" + trickScore;
        }

        UpdateScoreUI();
    }

    private void UpdateRotationUI()
    {
        if (rotationText == null)
            return;

        int degrees =
            Mathf.FloorToInt(Mathf.Abs(totalRotation));

        int completedRotations =
            Mathf.FloorToInt(Mathf.Abs(totalRotation) / 360f);

        rotationText.text = degrees + "°";

        if (completedRotations > 0)
        {
            rotationText.text +=
                "\n" + completedRotations + "x Rotation";
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null)
            return;

        scoreText.text = totalScore.ToString();
    }


}