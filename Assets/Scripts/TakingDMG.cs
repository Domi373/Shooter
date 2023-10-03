using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakingDMG : MonoBehaviour
{
    [SerializeField]
    private float HP = 100;

    public void TakeDmg(float weaponDmg)
    {
        if (HP > 0)
            HP -= weaponDmg;

        if (HP <= 0)
            Destroy(gameObject);
    }
}
