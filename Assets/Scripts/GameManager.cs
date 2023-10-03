using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore;

public class GameManager : MonoBehaviour
{
    private void Start()
    {
        float fInf = Mathf.Infinity;
        int iInt = (int)fInf;
        Application.targetFrameRate = iInt;
    }
}
