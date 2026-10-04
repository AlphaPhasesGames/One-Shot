using UnityEngine;
using TMPro;

public class PlayerTrickTracker : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;
    public PlayerGrapple grapple;
    public SpriteRenderer spriteRenderer;
    [Header("Live Rotation UI")]
    public GameObject rotationUI;
    public TMP_Text rotationText;

    [Header("Last Trick UI")]
    public GameObject lastTrickUI;
    public TMP_Text lastTrickText;
    public TMP_Text lastTrickPointsText;

    [Header("Spin")]
    public int pointsPerSpin = 150;

    private int completedSpins;
    private int spinDirection;

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

    private bool facingLeftWhenTrickStarted;
    private bool trickDirectionLocked;

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
            trickDirectionLocked = false;

            completedSpins = 0;
            spinDirection = 0;

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

        if (!trickDirectionLocked &&
        Mathf.Abs(rotationChange) > 0.1f)
        {
            facingLeftWhenTrickStarted =
                spriteRenderer != null && spriteRenderer.flipX;

            trickDirectionLocked = true;
        }

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

        // Nothing was performed.
        if (completedRotations <= 0 && completedSpins <= 0)
            return;

        int flipScore =
            completedRotations * pointsPerRotation;

        int spinScore =
            completedSpins * pointsPerSpin;

        int trickScore =
            flipScore + spinScore;

        totalScore += trickScore;

        if (lastTrickUI != null)
            lastTrickUI.SetActive(true);

        if (lastTrickText != null)
        {
            string trickName = "";

            // Add flips.
            if (completedRotations > 0)
            {
                string flipDirection = GetFlipDirection();

                if (completedRotations == 1)
                {
                    trickName = flipDirection;
                }
                else
                {
                    trickName =
                        completedRotations + "x " + flipDirection;
                }
            }

            // Add spins.
            if (completedSpins > 0)
            {
                string spinName =
                    spinDirection < 0
                        ? "LEFT SPIN"
                        : "RIGHT SPIN";

                if (trickName != "")
                    trickName += " + ";

                if (completedSpins == 1)
                {
                    trickName += spinName;
                }
                else
                {
                    trickName +=
                        completedSpins + "x " + spinName;
                }
            }

            lastTrickText.text = trickName;
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

        string text = "";

        // Show flip rotation.
        if (Mathf.Abs(totalRotation) >= minimumRotationToShow)
        {
            text = degrees + "°";

            if (completedRotations > 0)
            {
                string flipDirection = GetFlipDirection();

                text +=
                    "\n" + completedRotations + "x " + flipDirection;
            }
        }

        // Show spins.
        if (completedSpins > 0)
        {
            string spinName =
                spinDirection < 0
                    ? "LEFT SPIN"
                    : "RIGHT SPIN";

            if (text != "")
                text += "\n";

            text += completedSpins + "x " + spinName;
        }

        rotationText.text = text;
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null)
            return;

        scoreText.text = totalScore.ToString();
    }

    private string GetFlipDirection()
    {
        //bool facingLeft = spriteRenderer != null && spriteRenderer.flipX;
        bool positiveRotation = totalRotation > 0f;

        // Mirroring Granny reverses which physical rotation
        // appears to be a forward/back flip.
        if (facingLeftWhenTrickStarted)
        {
            return positiveRotation
                ? "FORWARD FLIP"
                : "BACKFLIP";
        }
        else
        {
            return positiveRotation
                ? "BACKFLIP"
                : "FORWARD FLIP";
        }
    }

    public void RegisterSpin(int direction)
    {
        completedSpins++;
        spinDirection = direction;

        if (rotationUI != null)
            rotationUI.SetActive(true);

        UpdateRotationUI();

        Debug.Log("Spin registered! Total spins: " + completedSpins);
    }
}