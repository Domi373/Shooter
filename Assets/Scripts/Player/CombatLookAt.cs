using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CombatLookAt : MonoBehaviour
{
    public float rayLength;

    private RaycastHit hit;
    private float deepth;

    private void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.right, out hit, rayLength))
        {
            deepth = rayLength - hit.distance;
            transform.localPosition -= new Vector3(deepth, 0, 0);
        }
    }
}
