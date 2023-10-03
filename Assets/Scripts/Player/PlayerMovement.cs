using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed;
    public float sprintSpeed;
    public float slideSpeed;
    public float swingSpeed;

    private float moveSpeed;

    [HideInInspector]
    public float desiredMoveSpeed;

    private float lastDesiredMoveSpeed;

    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump = true;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Ground check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Wallrunning")]
    public LayerMask whatIsWall;
    public float wallRunForce;
    public float maxWallRunTime;
    public float maxWallSpeed;
    bool isWallRight, isWallLeft;
    bool isWallRunning;

    [Space(30)]
    
    public Transform orientation;
    public Transform playerObj;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    [HideInInspector]
    public Rigidbody rb;

    private GameObject cameraObj;
    [HideInInspector]
    public string cameraStyle;

    public enum MovementState
    {
        freeze,
        swinging,
        walking,
        sprinting,
        sliding,
        air
    }

    public bool freeze;

    public bool activeGrapple;

    public bool swinging;

    public bool airControl;

    public MovementState movementState;

    [HideInInspector]
    public bool sliding;
    private Sliding sl;
    [HideInInspector]
    public float angle = 0;

    public bool exitedSwingInAir;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        sl = GetComponent<Sliding>();

    }

    private void Update()
    {
        // ground check
        if (grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround))
        {
            readyToJump = true;
            airControl = true;
        }
        else
        {
            if (Input.GetKeyDown(sl.slideKey))
            {
                sl.moveSpeed = Mathf.Sqrt(Mathf.Pow(rb.velocity.magnitude, 2) - Mathf.Pow(rb.velocity.y, 2));
                sl.groundSlide = false;
            }
            else if (Input.GetKeyUp(sl.slideKey))
                sl.groundSlide = true;
        }

        MyInput();
        SpeedControl();
        StateHandler();
        CheckForWall();
        WallRunInput();

        // handle drag
        if (grounded  && !activeGrapple && !swinging)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // jump
        if(Input.GetKeyDown(jumpKey) && readyToJump)
        {
            if (movementState == MovementState.air && isWallRunning == false) readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void StateHandler()
    {
        CameraMovement cameraScript = cameraObj.GetComponent<CameraMovement>();
        cameraStyle = cameraScript.currentStyle.ToString();

        // Mode - Freeze
        if (freeze)
        {
            movementState = MovementState.freeze;
            moveSpeed = 0;
            rb.velocity = Vector3.zero;
        }

        // Mode - swinging
        else if (swinging)
        {
            movementState = MovementState.swinging;
            desiredMoveSpeed = moveSpeed;
        }

        // Mode - Sliding
        else if (sliding && cameraStyle == "Basic")
        {
            movementState = MovementState.sliding;

            if (OnSlope() && rb.velocity.y < 0.1f)
                desiredMoveSpeed = slideSpeed;

            else
                desiredMoveSpeed = sprintSpeed;
        }

        // Mode - sprinting
        else if (grounded && Input.GetKey(sprintKey) && cameraStyle == "Basic")
        {
            movementState = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }

        // Mode - walking
        else if(grounded)
        {
            movementState = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }

        // Mode - air
        else
        {
            movementState = MovementState.air;
            desiredMoveSpeed = 10f;
        }

        // check if desiredMoveSpeed has changed drasitcally
        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > 4f && moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            moveSpeed = desiredMoveSpeed;
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // smoothly lerp movementspeed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (time < difference)
        {
            if (rb.velocity == Vector3.zero) yield break;
            if (movementState == MovementState.swinging) yield break;
            if (movementState == MovementState.sliding) yield break;


            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        if (activeGrapple) return;
        if (swinging) return;
        if (sliding) return;

        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;



        // onslope
        if (OnSlope() && !exitingSlope && !sliding)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > -0.5f)
                rb.AddForce(Vector3.down * 100f, ForceMode.Force);
        }

        // on ground
        else if (grounded && !sliding)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        // turn gravity off while on slope
        //rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (activeGrapple) return;
        if (swinging) return;
        if (!airControl) return;

        // limit speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed on ground/air
            Debug.Log(moveSpeed);
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }

    }

    private void Jump()
    {
        if (swinging == true) return;
        exitingSlope = true;
        if (sliding) sl.StopSlide();

        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (isWallRunning)
        {

            // siewards jump
            if (isWallLeft && Input.GetKey(KeyCode.D) || isWallRight && Input.GetKey(KeyCode.A))
            {
                rb.AddForce(orientation.forward * jumpForce, ForceMode.Impulse);
                rb.AddForce(transform.up * jumpForce * 0.5f, ForceMode.Impulse);
            }
            if (isWallRight && Input.GetKey(KeyCode.A)) rb.AddForce(-orientation.right * jumpForce * 5f, ForceMode.Impulse);
            if (isWallLeft && Input.GetKey(KeyCode.D)) rb.AddForce(orientation.right * jumpForce * 5f, ForceMode.Impulse);
        }
        else rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        exitingSlope = false;
    }

    private bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;

        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);

        Invoke(nameof(ResetRestrictions), 3f);
    }

    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;

        rb.velocity = velocityToSet;
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            GetComponent<Grappling>().StopGrapple();
        }
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0f;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void WallRunInput()
    {
        if (Input.GetKey(KeyCode.D) && isWallRight) StartWallRun();
        if (Input.GetKey(KeyCode.A) && isWallLeft) StartWallRun();
    }
    
    private void StartWallRun()
    {
        rb.useGravity = false;
        isWallRunning = true;
        readyToJump = true;

        if (rb.velocity.magnitude <= maxWallSpeed)
        {
            rb.AddForce(orientation.forward * wallRunForce * Time.deltaTime);

            // Make sure character sticks to wall
            if (isWallRight)
                rb.AddForce(orientation.right * wallRunForce / 5 * Time.deltaTime);
            else
                rb.AddForce(-orientation.right * wallRunForce / 5 * Time.deltaTime);
        }
    }

    private void StopWallRun()
    {
        rb.useGravity = true;
        isWallRunning = false;
    }

    private void CheckForWall()
    {
        isWallRight = Physics.Raycast(transform.position, orientation.right, 1f, whatIsWall);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, 1f, whatIsWall);

        // leave wallrun
        if (!isWallRight && !isWallLeft) StopWallRun();
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }
}
