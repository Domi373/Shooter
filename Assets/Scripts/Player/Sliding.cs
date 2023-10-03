using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement pm;
    private Grappling gp;

    [Header("Sliding")]
    public float slideForce;
    private float sliderTimer;
    private float slideCd = 0f;
    public float cooldown;

    [HideInInspector]
    public bool groundSlide = true;

    public float slideYScale;
    private float startYScale;

    private bool doSlide;
    [HideInInspector]
    public float moveSpeed;
    private float slideMaxSpeed;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;

    private Vector3 inputDirection;
    private Vector3 desiredInputDirection;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        gp = GetComponent<Grappling>();

        startYScale = playerObj.localScale.y;
        groundSlide = true;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey))
            PrepareSlide(true);

        if (Input.GetKeyUp(slideKey))
        {
            PrepareSlide(false);
            if (pm.sliding)
                StopSlide();
        }

        if (doSlide && pm.cameraStyle == "Basic" && gp.grappling == false && pm.grounded && !pm.sliding)
            StartSlide();

        if (slideCd > 0f)
            slideCd -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (pm.sliding)
            SlidingMovement();
    }

    private void PrepareSlide(bool switchSlide)
    {
        doSlide = switchSlide;
    }

    private void StartSlide()
    {
        if (slideCd > 0f) return;

        doSlide = false;

        pm.sliding = true;

        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        sliderTimer = 0f;
    }

    private void SlidingMovement()
    {
        inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if (desiredInputDirection == Vector3.zero) desiredInputDirection = inputDirection;
        else desiredInputDirection += inputDirection * 0.07f;

        if (!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            if (!groundSlide)
                rb.AddForce(new Vector3(rb.velocity.x, 0f, rb.velocity.z).normalized * moveSpeed, ForceMode.Impulse);
            else
            {
                rb.AddForce(new Vector3(rb.velocity.x, 0f, rb.velocity.z).normalized * moveSpeed, ForceMode.Impulse);
            }

            slideMaxSpeed = moveSpeed;
            if (sliderTimer < slideMaxSpeed)
            moveSpeed = Mathf.Lerp(slideMaxSpeed, pm.desiredMoveSpeed, sliderTimer / slideMaxSpeed);
            sliderTimer += Time.deltaTime;
        }

        // sliding down a slope
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(desiredInputDirection) * slideForce * pm.angle * 0.15f, ForceMode.Force);
        }

        if (moveSpeed <= 10.3f)
            StopSlide();
    }

    public void StopSlide()
    {
        pm.sliding = false;
        desiredInputDirection = Vector3.zero;

        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z);

        groundSlide = true;

        slideCd = cooldown;
    }
}
