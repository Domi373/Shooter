using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingV2 : MonoBehaviour
{
    [Header("Grappling variables")]
    public LineRenderer lr;
    public Transform cam;
    public Transform gunTip;
    public Transform player;
    public Transform orientation;
    public Rigidbody rb;
    public float cooldown;
    public float airControlForce;
    private float grapplingcd = 0f;

    private PlayerMovement pm;

    [Header("Key bind")]
    public KeyCode grappling = KeyCode.Q;

    [Header("Joint variables")]
    public float drive;
    public float maxForce;
    public float breakForce;

    private Vector3 swingPoint;
    private ConfigurableJoint joint;
    private Vector3 currentGrapplePosition;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(grappling)) StartGrappling();
        if (Input.GetKeyUp(grappling)) StopGrappling();

        if (grapplingcd > 0f)
            grapplingcd -= Time.deltaTime;

        if (pm.grounded) pm.exitedSwingInAir = false;
    }

    private void FixedUpdate()
    {
        if (pm.swinging) AirControl();
    }

    private void LateUpdate()
    {
        DrawRope();
    }

    private void StartGrappling()
    {
        if (grapplingcd > 0f) return;

        pm.swinging = true;

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit))
        {
            swingPoint = hit.point;
            joint = player.gameObject.AddComponent<ConfigurableJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = swingPoint;
            joint.axis = Vector3.zero;
            joint.secondaryAxis = Vector3.zero;

            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Limited;
            joint.zMotion = ConfigurableJointMotion.Limited;

            joint.xDrive = new JointDrive()
            {
                positionSpring = drive,
                positionDamper = 0f,
                maximumForce = maxForce,
                useAcceleration = false
            };

            joint.yDrive = new JointDrive()
            {
                positionSpring = drive,
                positionDamper = 0f,
                maximumForce = maxForce,
                useAcceleration = false
            };

            joint.zDrive = new JointDrive()
            {
                positionSpring = drive,
                positionDamper = 0f,
                maximumForce = maxForce,
                useAcceleration = false
            };

            joint.linearLimit = new SoftJointLimit()
            {
                limit = Mathf.Infinity,
                bounciness = 0f,
                contactDistance = 0f
            };

            lr.positionCount = 2;
            currentGrapplePosition = gunTip.position;

        }
    }

    private void StopGrappling()
    {
        if (grapplingcd > 0f) return;

        lr.positionCount = 0;
        Destroy(joint);
        pm.swinging = false;
        pm.airControl = false;
        grapplingcd = cooldown;

        pm.exitedSwingInAir = (pm.grounded) ? false : true;
    }

    private void DrawRope()
    {
        if (!joint) return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);

        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, swingPoint);
    }

    private void AirControl()
    {
        Vector3 moveDirection = orientation.forward * Input.GetAxisRaw("Vertical") + orientation.right * Input.GetAxisRaw("Horizontal");
        rb.AddForce(moveDirection * airControlForce, ForceMode.Force);
    }
}
