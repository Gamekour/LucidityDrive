using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKHandle : MonoBehaviour
{
    [SerializeField] bool right = false;
    private void OnEnable()
    {
        if (right)
            PlayerInfo.forceIK_RH = true;
        else
            PlayerInfo.forceIK_LH = true;
    }

    private void OnDisable()
    {
        if (right)
            PlayerInfo.forceIK_RH = false;
        else
            PlayerInfo.forceIK_LH = false;
    }

    private void Update()
    {
        if (right)
        {
            PlayerInfo.IK_RH.position = transform.position;
            PlayerInfo.IK_RH.rotation = transform.rotation;
        }
        else
        {
            PlayerInfo.IK_LH.position = transform.position;
            PlayerInfo.IK_LH.rotation = transform.rotation;
        }
    }
}
