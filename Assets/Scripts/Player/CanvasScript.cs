using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CanvasScript : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI movementState;
    public TextMeshProUGUI moveSpeed;

    private PlayerMovement pm;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        movementState.text = pm.movementState.ToString();
        moveSpeed.text = Mathf.Round(Mathf.Sqrt(Mathf.Pow(pm.rb.velocity.magnitude, 2) - Mathf.Pow(pm.rb.velocity.y, 2)) * 10f) * 0.1f + " / " + pm.desiredMoveSpeed.ToString();
    }
}
