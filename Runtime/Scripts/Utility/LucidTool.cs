using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LucidTool : MonoBehaviour
{
    public Transform PrimaryGripL;
    public Transform SecondaryGripL;
    public Transform PrimaryGripR;
    public Transform SecondaryGripR;
    public Transform itemPosePrimaryL;
    public Transform itemPoseSecondaryL;
    public Transform itemPosePrimaryR;
    public Transform itemPoseSecondaryR;
    public bool grabLockPrimary = true;
    public bool grabLockSecondary = false;

    public UnityEvent Use;
}
