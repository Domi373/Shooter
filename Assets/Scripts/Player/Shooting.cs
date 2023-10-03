using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Shooting : MonoBehaviour
{
    [Header("Shooting Variables")]
    public float dmg;
    //public float demageMultiplier;
    //public float reloadTime;
    //public float bulletRangeDevider;
    public LineRenderer lr;

    public Transform rayStart;
    public Transform rayDir;
    private Vector3 rayEnd;

    private RaycastHit hit;

    [Header("Key Binding")]
    public KeyCode shoot = KeyCode.Mouse0;

    private CameraMovement cm;
    private GameObject mainCamera;

    private TakingDMG tDMG;

    private void Start()
    {
        if (mainCamera = GameObject.FindGameObjectWithTag("MainCamera"))
        {
            cm = mainCamera.GetComponent<CameraMovement>();
        }
    }

    private void Update()
    {
        CheckInput();
    }

    private void CheckInput()
    {
        if (Input.GetKeyDown(shoot) && cm.basicCamera == false)
            Shoot();
    }

    private void Shoot()
    {
        if (Physics.Raycast(rayDir.position, rayDir.forward, out hit))
        {
            rayEnd = hit.point;

            Vector3 realDirection = rayEnd - rayStart.position;

            if (Physics.Raycast(rayStart.position, realDirection.normalized, out hit))
            {
                rayEnd = hit.point;

                lr.enabled = true;
                lr.SetPosition(0, rayStart.position);
                lr.SetPosition(1, rayEnd);

                Invoke(nameof(VanishBulletTrait), 1f);

                if (hit.transform.tag == "EnemyBot")
                    if (tDMG = hit.transform.gameObject.GetComponent<TakingDMG>())
                        tDMG.TakeDmg(dmg);
            }
        }
    }

    private void VanishBulletTrait()
    {
        lr.enabled = false;
    }
}
